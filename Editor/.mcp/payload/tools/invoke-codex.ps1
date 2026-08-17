<#
.SYNOPSIS
    Runs Codex non-interactively and returns just its final answer.

.DESCRIPTION
    A thin, reproducible wrapper around `codex exec`, meant to be called by another agent
    (Claude Code) rather than by a human. It exists so the flags that matter are decided once
    instead of re-derived per call:

      -C <repo>            The MCP registration in .codex/config.toml is PROJECT-scoped: run Codex
                           from anywhere else and `codex mcp list` reports no servers at all. The
                           working root is derived from this script's own location, so the caller's
                           current directory cannot get it wrong.
      -s workspace-write   Keeps the sandbox. Note this bounds Codex's SHELL only -- MCP tools run
                           outside it, which is why .codex/config.toml carries an allow list.
      --strict-config      Unknown config keys are otherwise IGNORED SILENTLY, so a typo in the
                           allow list would buy a safety measure that isn't there.
      --color never        Otherwise the returned text carries ANSI escapes.
      -o <file>            `codex exec` prints its whole event stream to stdout; the final answer
                           has to be picked up from this file. That is what makes the wrapper's own
                           stdout clean.

    There is deliberately no approval flag: `codex exec` has none, and reports `approval: never` in
    its own banner. It either runs inside the sandbox or fails. The one flag that looks like the
    answer, --dangerously-bypass-approvals-and-sandbox, is exactly what we do not want.

    Nothing here is interactive (no Read-Host, no prompts) -- an interactive stall would simply hang
    until the timeout.

.PARAMETER Prompt
    The instructions for Codex. Three ways in, because a PowerShell pipeline and a redirected
    console stream are NOT the same thing and both occur in practice:
      - as a positional argument
      - through the PowerShell pipeline:  $text | ./invoke-codex.ps1
      - on redirected stdin from outside PowerShell:  cat brief.md | pwsh -File ./invoke-codex.ps1
    Prefer one of the stream forms for anything with quotes or newlines; Windows argument quoting is
    a reliable source of pain.

.PARAMETER Json
    Return the full JSONL event stream instead of only the final answer. For diagnosis.

.PARAMETER KeepSession
    Omit --ephemeral, so the run is persisted and can be resumed with `codex exec resume`.

.PARAMETER ReportUsage
    Snapshot the Codex account quota before and after the run and write the delta to STDERR, so
    stdout stays exactly what it was: Codex's answer and nothing else. Opt-in, because it costs two
    extra app-server round trips per call. This is the only honest way to price a job -- a single
    quota reading cannot distinguish a fresh window from an unused one. See tools/codex-usage.ps1.

.EXAMPLE
    ./tools/invoke-codex.ps1 "Reply with exactly: CODEX_OK"

.EXAMPLE
    Get-Content .\brief.md -Raw | ./tools/invoke-codex.ps1
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromPipeline = $true)]
    [AllowEmptyString()]
    [string] $Prompt,

    [string] $Model = 'gpt-5.6-luna',

    [ValidateSet('read-only', 'workspace-write', 'danger-full-access')]
    [string] $Sandbox = 'workspace-write',

    [string] $WorkDir,

    [int] $TimeoutSec = 300,

    [switch] $Json,

    [switch] $KeepSession,

    [switch] $ReportUsage,

    # A Unity project is very often not a git repo yet, and `codex exec` then refuses outright:
    # "Not inside a trusted directory and --skip-git-repo-check was not specified." The flag only
    # waives the repo check -- the sandbox from -s still applies -- but it does waive the safety net
    # that lets you undo Codex's edits with git, so it stays off by default.
    [switch] $SkipGitRepoCheck,

    # Escape hatch for anything this wrapper does not model. Passed through verbatim, ahead of the
    # stdin marker. Exists because this file is installer-managed: editing it locally to add one flag
    # would drift against the manifest hash and be overwritten on the next install.
    [string[]] $ExtraArgs,

    [string] $LogPath,

    # JSON Schema file describing the shape of Codex's final answer. Worth using for anything a
    # caller has to act on -- a path to act on should arrive as a field, not be fished out of prose.
    [string] $OutputSchema
)

begin {
    $ErrorActionPreference = 'Stop'
    $collected = [System.Collections.Generic.List[string]]::new()
}

process {
    # Runs once for a positional argument, once per item for a pipeline.
    if ($PSBoundParameters.ContainsKey('Prompt') -and -not [string]::IsNullOrEmpty($Prompt)) {
        $collected.Add($Prompt)
    }
}

end {
    # ------------------------------------------------------------ inputs

    $text = if ($collected.Count -gt 0) { $collected -join "`n" } else { '' }

    if ([string]::IsNullOrWhiteSpace($text) -or $text -eq '-') {
        if ([Console]::IsInputRedirected) {
            $text = [Console]::In.ReadToEnd()
        }
    }
    if ([string]::IsNullOrWhiteSpace($text)) {
        throw 'No prompt. Pass it as an argument, pipe it in, or redirect it on stdin.'
    }

    if (-not $WorkDir) {
        # The installer puts this script in <project>/tools/, so the parent is normally right. Walk up
        # for .codex/config.toml anyway: that file is what -C actually has to find, and a script moved
        # elsewhere would otherwise send Codex to a folder with no MCP registration at all -- which
        # fails as "no servers configured", not as a path error.
        $WorkDir = Split-Path -Parent $PSScriptRoot
        for ($dir = $PSScriptRoot; $dir; $dir = Split-Path -Parent $dir) {
            if (Test-Path -LiteralPath (Join-Path $dir '.codex/config.toml')) { $WorkDir = $dir; break }
        }
    }
    if (-not (Test-Path -LiteralPath $WorkDir)) { throw "WorkDir does not exist: $WorkDir" }

    # ------------------------------------------------------------ codex executable
    # Prefer the real .exe over the npm .cmd shim: a .cmd goes through cmd.exe, which re-parses the
    # arguments and is one more place for quoting to go wrong.

    $codex = (Get-Command 'codex' -All -ErrorAction SilentlyContinue |
        Where-Object { $_.Source -like '*.exe' } |
        Select-Object -First 1 -ExpandProperty Source)

    if (-not $codex) {
        $vendored = Join-Path $env:APPDATA 'npm\node_modules\@openai\codex\node_modules\@openai'
        $codex = Get-ChildItem -Path $vendored -Filter 'codex.exe' -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $codex) {
        $codex = (Get-Command 'codex' -ErrorAction SilentlyContinue).Source
    }
    if (-not $codex) { throw 'Codex CLI not found on PATH.' }

    # ------------------------------------------------------------ run

    $stamp   = [guid]::NewGuid().ToString('N').Substring(0, 8)
    $tmp     = [System.IO.Path]::GetTempPath()
    $fPrompt = Join-Path $tmp "codex-$stamp-prompt.txt"
    $fEvents = if ($LogPath) { $LogPath } else { Join-Path $tmp "codex-$stamp-events.log" }
    $fErr    = Join-Path $tmp "codex-$stamp-stderr.log"
    $fAnswer = Join-Path $tmp "codex-$stamp-answer.txt"
    $fUsage  = Join-Path $tmp "codex-$stamp-usage-before.json"

    # ------------------------------------------------------------ optional quota accounting
    # Never allowed to break the actual call: a quota reading is nice to have, the answer is not.

    $usageTool = Join-Path $PSScriptRoot 'codex-usage.ps1'

    function Write-ToStderr([string] $Text) {
        foreach ($line in ($Text -split "`r?`n")) { [Console]::Error.WriteLine($line) }
    }

    $usageBaseline = $false
    if ($ReportUsage) {
        try {
            & $usageTool -Snapshot $fUsage -Quiet
            $usageBaseline = $true
        } catch {
            Write-ToStderr "usage baseline unavailable: $($_.Exception.Message)"
        }
    }

    # UTF-8 without BOM: a BOM would arrive as part of the prompt text.
    [System.IO.File]::WriteAllText($fPrompt, $text, (New-Object System.Text.UTF8Encoding($false)))

    $codexArgs = @(
        'exec'
        '--strict-config'
        '--color', 'never'
        '-C', $WorkDir
        '-s', $Sandbox
        '-m', $Model
        '-o', $fAnswer
    )
    if (-not $KeepSession)   { $codexArgs += '--ephemeral' }
    if ($Json)               { $codexArgs += '--json' }
    if ($SkipGitRepoCheck)   { $codexArgs += '--skip-git-repo-check' }
    if ($OutputSchema) {
        if (-not (Test-Path -LiteralPath $OutputSchema)) { throw "OutputSchema file not found: $OutputSchema" }
        $codexArgs += @('--output-schema', (Resolve-Path -LiteralPath $OutputSchema).Path)
    }
    if ($ExtraArgs)          { $codexArgs += $ExtraArgs }
    $codexArgs += '-'   # read the prompt from stdin

    Write-Verbose "$codex $($codexArgs -join ' ')"

    $exitCode = 0
    try {
        $proc = Start-Process -FilePath $codex -ArgumentList $codexArgs `
            -NoNewWindow -PassThru `
            -RedirectStandardInput $fPrompt `
            -RedirectStandardOutput $fEvents `
            -RedirectStandardError $fErr

        Wait-Process -Id $proc.Id -Timeout $TimeoutSec -ErrorAction SilentlyContinue
        if (-not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            $exitCode = 124
            Write-Error "Codex exceeded $TimeoutSec s and was killed. Event log: $fEvents"
            exit $exitCode
        }
        $exitCode = $proc.ExitCode

        if ($exitCode -ne 0) {
            if (Test-Path -LiteralPath $fErr) {
                Get-Content -LiteralPath $fErr -Raw | Write-Host -ForegroundColor Red
            }
            Write-Error "Codex exited with $exitCode. Event log: $fEvents"
            exit $exitCode
        }

        if ($Json) {
            if (Test-Path -LiteralPath $fEvents) {
                Get-Content -LiteralPath $fEvents -Raw -Encoding utf8
            }
        } elseif (Test-Path -LiteralPath $fAnswer) {
            # -Raw keeps the answer verbatim; TrimEnd only drops the trailing newline of the file.
            (Get-Content -LiteralPath $fAnswer -Raw -Encoding utf8).TrimEnd("`r", "`n")
        } else {
            $exitCode = 1
            Write-Error "Codex reported success but wrote no answer file. Event log: $fEvents"
            exit $exitCode
        }
    }
    finally {
        # In the finally block on purpose: a run that failed or timed out still spent quota, and
        # that is exactly when you want to know how much.
        if ($usageBaseline) {
            try {
                Write-ToStderr (& $usageTool -Since $fUsage | Out-String)
            } catch {
                Write-ToStderr "usage report failed: $($_.Exception.Message)"
            }
            Remove-Item -LiteralPath $fUsage -ErrorAction SilentlyContinue
        }

        Remove-Item -LiteralPath $fPrompt -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $fAnswer -ErrorAction SilentlyContinue
        # The event log and stderr survive a failure on purpose -- they are the only diagnosis there is.
        if ($exitCode -eq 0) {
            if (-not $LogPath) { Remove-Item -LiteralPath $fEvents -ErrorAction SilentlyContinue }
            Remove-Item -LiteralPath $fErr -ErrorAction SilentlyContinue
        }
    }

    exit $exitCode
}
