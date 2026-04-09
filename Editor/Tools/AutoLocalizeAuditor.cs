using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
/// <summary>
/// Previously scanned the project and patched <c>m_autoLocalize</c> on
/// <see cref="UiLocalizedTextMeshProUGUI"/> components. The flag has been removed;
/// use the placeholder convention (<c>[Text]</c>) in the design-time text to mark a
/// component as non-translatable.
/// </summary>
public static class AutoLocalizeAuditor
{
private const string TOOL_TAG = "[AutoLocalizeAuditor]";

[MenuItem(StringConstants.LOCA_MISC_FIX_AUTO_LOCALIZE_MENU_NAME,
          priority = Constants.LOCA_MISC_FIX_AUTO_LOCALIZE_MENU_PRIORITY)]
public static void RunAuditAndFix()
{
Debug.Log($"{TOOL_TAG} This tool is no longer needed. " +
          "The m_autoLocalize flag has been removed from UiLocalizedTextMeshProUGUI. " +
          "Use the placeholder convention [Text] in m_text/m_locaKey to mark components as non-translatable.");
}
}
}
