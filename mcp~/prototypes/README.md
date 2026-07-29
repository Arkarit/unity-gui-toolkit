# Prototypes

Throwaway analysis scripts, kept for the knowledge encoded in them rather than for their code. They exist
because there was no way to answer "what did a human change in this baked prefab?" — the planned
`capture_prefab_values` / `apply_prefab_values` bridge tools are meant to replace both.

Node only, no dependencies. Run from a Unity project root.

| Script | Answers |
|---|---|
| `prefabdiff.js A.prefab B.prefab` | Structural difference: hierarchy, child order, rects, component sets, active flags. |
| `valuediff.js A.prefab B.prefab` | Serialized component VALUES of matching components — the dimension the structural diff cannot see. |

Compare two BAKED prefabs, never a prefab against its description: both sides then come out of the same
generator, so everything the baker or a style produces cancels out and only real differences remain.
Comparing a prefab against its screen description instead reports every style-applied value and every
template-internal override as if a human had made it — that mistake cost a full round of wrong conclusions.

## Prefab YAML pitfalls these scripts had to learn

- `m_PrefabInstance: {fileID: 0}` appears in **every** document; 0 means "not a nested prefab instance". The
  string `"0"` is truthy in JS, so without normalising it every node looks like a template instance.
- A nested prefab instance's name is not on a GameObject — it lives in the instance's modification list as
  `propertyPath: m_Name`. Its child objects appear as `stripped` documents with no usable data.
- A prefab's root GameObject is renamed to the asset file name when it is saved, so the root's name never
  matches the node id it was authored under. Match roots positionally.
- Override targets in a prefab *variant* are object ids that Unity remaps internally — they cannot be
  resolved by searching the base prefab files. Do not try; ask a human or inspect in the editor.
- Fields that differ between two files without meaning anything: identity (`m_GameObject`, `m_Script`, …),
  anything containing a `fileID:` (cross-file references), and TMP's cached render data (`m_textInfo`,
  `m_mesh`, `m_RenderedWidth`, …). `valuediff.js` keeps that skip list.
- TMP stores a font size twice, in `m_fontSize` and `m_fontSizeBase`. Writing the field directly (as the
  baker's prop path does, fields being matched before properties) sets only the first.
