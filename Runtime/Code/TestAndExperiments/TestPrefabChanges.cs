using GuiToolkit;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class TestPrefabChanges : MonoBehaviour
{
	public bool m_running;
	public bool m_useRevertOverridesScope = true;

	void Update()
	{
		if (!m_running)
			return;

		var renderers = GetComponentsInChildren<MeshRenderer>(true);
		foreach (var rend in renderers)
		{
			using (new RevertOverridesScope(m_useRevertOverridesScope ? rend : null))
			{
				rend.enabled = !rend.enabled;
				rend.shadowCastingMode = rend.shadowCastingMode == ShadowCastingMode.Off ? 
					ShadowCastingMode.On : 
					ShadowCastingMode.Off;
			}
		}
	}
}
