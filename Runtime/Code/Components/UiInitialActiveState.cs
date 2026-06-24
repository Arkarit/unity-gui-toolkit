using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public enum EInitialActiveState
	{
		Active,
		Inactive,
	}

	public enum EInitialActiveTarget
	{
		Children,
		List,
	}

	/// <summary>
	/// Forces a set of target GameObjects active or inactive at runtime, regardless of how
	/// they were stored in the scene/prefab. Useful when authoring requires the opposite
	/// state in the editor (e.g. visible for layout, hidden at runtime — or vice versa).
	/// </summary>
	/// <remarks>
	/// <para>Uses <see cref="DefaultExecutionOrderAttribute"/> with <see cref="int.MinValue"/>
	/// so this Awake runs as early as possible. In practice this fires before the targets'
	/// own Awakes within the same scene-load dispatch, so deactivated targets never see an
	/// Awake call. Strict guarantees (target sets even lower execution order) would require
	/// an Editor-time pre-save hook, which is intentionally out of scope here.</para>
	/// </remarks>
	[DefaultExecutionOrder(int.MinValue)]
	public class UiInitialActiveState : MonoBehaviour
	{
		[Tooltip("Desired state of the targets at runtime.")]
		[SerializeField] private EInitialActiveState m_state = EInitialActiveState.Inactive;

		[Tooltip("Children: all direct children of this GameObject.\n" +
			"List: only the GameObjects listed below.")]
		[SerializeField] private EInitialActiveTarget m_target = EInitialActiveTarget.Children;

		[Tooltip("Targets to apply the state to. Used only when Target is List.")]
		[SerializeField] private List<GameObject> m_objects = new();

		private void Awake()
		{
			bool desired = m_state == EInitialActiveState.Active;

			switch (m_target)
			{
				case EInitialActiveTarget.Children:
				{
					var t = transform;
					int n = t.childCount;
					for (int i = 0; i < n; i++)
					{
						var child = t.GetChild(i).gameObject;
						if (child.activeSelf != desired)
							child.SetActive(desired);
					}
					break;
				}

				case EInitialActiveTarget.List:
				{
					foreach (var go in m_objects)
					{
						if (go != null && go.activeSelf != desired)
							go.SetActive(desired);
					}
					break;
				}
			}
		}
	}
}
