<#
.SYNOPSIS
    Reads Codex account rate limits and token usage, straight from the account backend.

.DESCRIPTION
    Answers "how much of the Codex quota is left, and what did that last job cost" without
    reconstructing anything from rollout logs.

    It talks to `codex app-server` over stdio using the documented JSON-RPC methods
    `account/rateLimits/read` and `account/usage/read`. Both were verified against the schema this
    very binary emits (`codex app-server generate-json-schema --out <dir>`), not against the docs --
    the app server is marked [experimental], so its surface is worth re-checking after a
    `codex update`.

    No new authentication: the app server reuses the existing Codex login. With ChatGPT auth (what
    this machine uses) both methods answer; an API-key-only setup would not have account usage to
    report.

    Deliberately NOT a long-running service. There is a matching `account/rateLimits/updated`
    notification, but it only pays off for a client that stays connected, and we have none: every
    call here starts the server, asks, and shuts it down again.

    THE TWO NUMBERS MEAN DIFFERENT THINGS, and that is the point of recording both. `usedPercent` is
    what the plan actually meters; `dailyUsageBuckets`/`lifetimeTokens` count token activity. On
    2026-08-13 this machine reported 10,228,395 tokens spent the previous day and `usedPercent: 0`
    in the same breath, so the two are plainly not the same quantity. Snapshots therefore keep the
    complete raw payload of both methods -- the readable summary is a view, not the record.

.PARAMETER Snapshot
    Write the full raw state to this file, for a later -Since comparison.

.PARAMETER Since
    Compare the current state against a snapshot file and print the delta. This is the only honest
    way to price a single job: a lone reading cannot tell a fresh window from an unused one.

.PARAMETER Json
    Print the raw payload instead of the summary. With -Since, prints {before, after}.

.PARAMETER Quiet
    Print nothing. For callers that only want the -Snapshot side effect.

.EXAMPLE
    ./tools/codex-usage.ps1

.EXAMPLE
    ./tools/codex-usage.ps1 -Snapshot before.json -Quiet
    ./tools/invoke-codex.ps1 "draw seven icons"
    ./tools/codex-usage.ps1 -Since before.json
#>
[CmdletBinding()]
param(
    [string] $Snapshot,

    [string] $Since,

    [switch] $Json,

    [switch] $Quiet,

    [int] $TimeoutSec = 45
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------- codex executable
# Same resolution as invoke-codex.ps1: prefer the real .exe over the npm .cmd shim, which would put
# cmd.exe and a second round of argument parsing in the way.

function Resolve-CodexExe {
    $exe = (Get-Command 'codex' -All -ErrorAction SilentlyContinue |
        Where-Object { $_.Source -like '*.exe' } |
        Select-Object -First 1 -ExpandProperty Source)

    if (-not $exe) {
        $vendored = Join-Path $env:APPDATA 'npm\node_modules\@openai\codex\node_modules\@openai'
        $exe = Get-ChildItem -Path $vendored -Filter 'codex.exe' -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $exe) { $exe = (Get-Command 'codex' -ErrorAction SilentlyContinue).Source }
    if (-not $exe) { throw 'Codex CLI not found on PATH.' }
    $exe
}

# ---------------------------------------------------------------- app-server conversation

function Read-CodexAccountState {
    param([int] $TimeoutSec)

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName               = Resolve-CodexExe
    $psi.Arguments              = 'app-server'
    $psi.WorkingDirectory       = (Split-Path -Parent $PSScriptRoot)
    $psi.RedirectStandardInput  = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true
    $psi.UseShellExecute        = $false
    $psi.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)

    $proc = [System.Diagnostics.Process]::Start($psi)
    try {
        function Send($obj) {
            $proc.StandardInput.WriteLine(($obj | ConvertTo-Json -Depth 10 -Compress))
            $proc.StandardInput.Flush()
        }

        # The handshake is mandatory -- requests sent before it are rejected. No `jsonrpc` member:
        # this protocol wants id/method/params and nothing else.
        Send @{ id = 0; method = 'initialize'; params = @{
            clientInfo = @{ name = 'notr-codex-usage'; title = 'NOTR Codex usage'; version = '1.0' } } }
        Send @{ method = 'initialized'; params = @{} }
        Send @{ id = 1; method = 'account/rateLimits/read'; params = @{} }
        Send @{ id = 2; method = 'account/usage/read';      params = @{} }

        $results  = @{}
        $deadline = (Get-Date).AddSeconds($TimeoutSec)

        while ((Get-Date) -lt $deadline -and $results.Count -lt 3) {
            $pending = $proc.StandardOutput.ReadLineAsync()
            if (-not $pending.Wait(2000)) { continue }

            $line = $pending.Result
            if ($null -eq $line) { break }   # server closed stdout

            $msg = $null
            try { $msg = $line | ConvertFrom-Json } catch { continue }

            # Notifications (remoteControl/status/changed and friends) carry no id; skip them.
            if ($null -eq $msg.PSObject.Properties['id']) { continue }
            if ($null -ne $msg.PSObject.Properties['error']) {
                throw "app-server rejected request $($msg.id): $($msg.error | ConvertTo-Json -Compress)"
            }
            $results["$($msg.id)"] = $msg.result
        }

        foreach ($id in 0, 1, 2) {
            if (-not $results.ContainsKey("$id")) {
                throw "No answer to request $id within $TimeoutSec s. Is `codex app-server` still supported by this CLI version?"
            }
        }

        [pscustomobject]@{
            capturedAt      = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
            capturedAtLocal = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
            userAgent       = $results['0'].userAgent   # carries the CLI version, free of charge
            rateLimits      = $results['1']
            usage           = $results['2']
        }
    }
    finally {
        try { $proc.StandardInput.Close() } catch { }
        try { if (-not $proc.HasExited) { $proc.Kill() } } catch { }
    }
}

# ---------------------------------------------------------------- formatting helpers

function Format-Count($n) {
    if ($null -eq $n) { return '-' }
    '{0:N0}' -f [int64] $n
}

function Format-Window($mins) {
    if ($null -eq $mins) { return 'unknown window' }
    if ($mins % 1440 -eq 0) { $d = $mins / 1440; return "$d day$(if ($d -ne 1) {'s'})" }
    if ($mins % 60   -eq 0) { $h = $mins / 60;   return "$h h" }
    "$mins min"
}

function Format-Reset($resetsAt) {
    if ($null -eq $resetsAt) { return 'no reset reported' }
    $when = [DateTimeOffset]::FromUnixTimeSeconds([int64] $resetsAt).LocalDateTime
    $left = $when - (Get-Date)
    if ($left.TotalSeconds -lt 0) { return "$($when.ToString('yyyy-MM-dd HH:mm')) (passed)" }
    # Floor, not [int]: PowerShell's int cast rounds, which would report 6d23h left as "7d 23h".
    '{0} (in {1}d {2}h)' -f $when.ToString('yyyy-MM-dd HH:mm'), [Math]::Floor($left.TotalDays), $left.Hours
}

# Both the single-bucket view and the per-limit map are returned; prefer the map, fall back to the
# flat one so this keeps working if a plan ever reports only the legacy shape.
function Get-LimitBuckets($rateLimits) {
    $byId = $rateLimits.rateLimitsByLimitId
    if ($byId) {
        return $byId.PSObject.Properties | ForEach-Object {
            [pscustomobject]@{ Key = $_.Name; Snapshot = $_.Value }
        }
    }
    if ($rateLimits.rateLimits) {
        $key = if ($rateLimits.rateLimits.limitId) { $rateLimits.rateLimits.limitId } else { 'default' }
        return , [pscustomobject]@{ Key = $key; Snapshot = $rateLimits.rateLimits }
    }
    @()
}

function Get-BucketTokens($usage, [string] $date) {
    $hit = $usage.dailyUsageBuckets | Where-Object { $_.startDate -eq $date } | Select-Object -First 1
    if ($hit) { [int64] $hit.tokens } else { [int64] 0 }
}

function Write-Summary($state) {
    $rl = $state.rateLimits
    Write-Output "Codex account  --  read $($state.capturedAtLocal)"

    $plan = $rl.rateLimits.planType
    if ($plan) { Write-Output "  Plan            $plan" }

    foreach ($bucket in Get-LimitBuckets $rl) {
        $s     = $bucket.Snapshot
        $label = if ($s.limitName) { "$($bucket.Key) ($($s.limitName))" } else { $bucket.Key }
        Write-Output "  Limit '$label'"

        foreach ($slot in 'primary', 'secondary') {
            $w = $s.$slot
            if (-not $w) { continue }
            Write-Output ("    {0,-10} {1,3} % used over {2}, resets {3}" -f `
                $slot, $w.usedPercent, (Format-Window $w.windowDurationMins), (Format-Reset $w.resetsAt))
        }
        if ($s.spendControlReached) { Write-Output '    spend control reached' }
        if ($s.rateLimitReachedType) { Write-Output "    limit reached: $($s.rateLimitReachedType)" }
    }

    $credits = $rl.rateLimits.credits
    if ($credits) {
        $text = if ($credits.unlimited) { 'unlimited' }
                elseif ($credits.hasCredits) { "balance $($credits.balance)" }
                else { 'none' }
        Write-Output "  Credits         $text"
    }
    if ($rl.rateLimitResetCredits) {
        Write-Output "  Reset credits   $($rl.rateLimitResetCredits.availableCount) available"
    }

    $u = $state.usage
    Write-Output "  Lifetime        $(Format-Count $u.summary.lifetimeTokens) tokens (peak day $(Format-Count $u.summary.peakDailyTokens))"

    $recent = $u.dailyUsageBuckets | Sort-Object startDate -Descending | Select-Object -First 5
    if ($recent) {
        Write-Output '  Daily tokens'
        foreach ($b in $recent) {
            Write-Output ("    {0}  {1,15}" -f $b.startDate, (Format-Count $b.tokens))
        }
    }
}

function Write-Delta($before, $after) {
    $elapsed = [TimeSpan]::FromSeconds($after.capturedAt - $before.capturedAt)
    Write-Output ("Codex usage delta  --  {0} -> {1}  ({2:hh\:mm\:ss} elapsed)" -f `
        $before.capturedAtLocal, $after.capturedAtLocal, $elapsed)

    $beforeBuckets = @{}
    foreach ($b in (Get-LimitBuckets $before.rateLimits)) { $beforeBuckets[$b.Key] = $b.Snapshot }

    foreach ($bucket in Get-LimitBuckets $after.rateLimits) {
        $old = $beforeBuckets[$bucket.Key]
        foreach ($slot in 'primary', 'secondary') {
            $now = $bucket.Snapshot.$slot
            if (-not $now) { continue }
            $was  = if ($old) { $old.$slot } else { $null }
            $wasP = if ($was) { $was.usedPercent } else { 0 }
            $diff = $now.usedPercent - $wasP
            Write-Output ("  {0} {1,-10} {2} % -> {3} %  ({4:+#;-#;0} points over {5})" -f `
                $bucket.Key, $slot, $wasP, $now.usedPercent, $diff, (Format-Window $now.windowDurationMins))
        }
    }

    # Every day touched by either reading, so a job spanning local midnight still adds up.
    $dates = @($before.usage.dailyUsageBuckets.startDate) + @($after.usage.dailyUsageBuckets.startDate) |
        Where-Object { $_ } | Sort-Object -Unique
    $daily = 0L
    foreach ($date in $dates) {
        $was = Get-BucketTokens $before.usage $date
        $now = Get-BucketTokens $after.usage  $date
        if ($now -eq $was) { continue }
        $daily += ($now - $was)
        Write-Output ("  tokens {0}  {1} -> {2}  ({3:+#,#;-#,#;0})" -f `
            $date, (Format-Count $was), (Format-Count $now), ($now - $was))
    }

    $life = [int64] $after.usage.summary.lifetimeTokens - [int64] $before.usage.summary.lifetimeTokens
    Write-Output ("  tokens total  {0:+#,#;-#,#;0}  (lifetime {1:+#,#;-#,#;0})" -f $daily, $life)

    if ($daily -eq 0 -and $life -eq 0) {
        Write-Output '  nothing recorded yet -- backend aggregation can lag a completed run'
    }
}

# ---------------------------------------------------------------- main

$state = Read-CodexAccountState -TimeoutSec $TimeoutSec

$previous = $null
if ($Since) {
    if (-not (Test-Path -LiteralPath $Since)) { throw "Snapshot not found: $Since" }
    $previous = Get-Content -LiteralPath $Since -Raw -Encoding utf8 | ConvertFrom-Json
    if (-not $previous.capturedAt) { throw "Not a codex-usage snapshot: $Since" }
}

if ($Snapshot) {
    $dir = Split-Path -Parent $Snapshot
    if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
    # Depth 20: the payload nests limits -> windows -> credits, and a truncated snapshot would be
    # worse than none at all.
    $state | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Snapshot -Encoding utf8
}

if ($Quiet) { return }

if ($Json) {
    if ($previous) {
        [pscustomobject]@{ before = $previous; after = $state } | ConvertTo-Json -Depth 20
    } else {
        $state | ConvertTo-Json -Depth 20
    }
    return
}

Write-Summary $state
if ($previous) {
    Write-Output ''
    Write-Delta $previous $state
}
