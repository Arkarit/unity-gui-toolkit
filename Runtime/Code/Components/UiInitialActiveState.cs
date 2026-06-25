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
	///
	/// Optionally also feeds the targets into <see cref="UiStartupOverlayQueue"/>: when
	/// <c>m_addToStartupOverlay</c> is enabled (only available alongside
	/// <see cref="EInitialActiveState.Active"/>), every target whose GameObject carries a
	/// component implementing <see cref="IUiStartupOverlay"/> is queued for the startup
	/// overlay sequence. This couples activation and queue-registration into one step, so
	/// there's no timing gap between "the prefab is alive" and "the queue knows about it".
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

		[Tooltip("When on and State is Active, every target whose GameObject carries an " +
			"IUiStartupOverlay component is enqueued in UiStartupOverlayQueue. Only " +
			"meaningful in the Active path — disabled targets wouldn't be alive to show.")]
		[SerializeField] private bool m_addToStartupOverlay;

		private void Awake()
		{
			bool desired = m_state == EInitialActiveState.Active;
			bool addToQueue = desired && m_addToStartupOverlay;

			switch (m_target)
			{
				case EInitialActiveTarget.Children:
				{
					var t = transform;
					int n = t.childCount;
					for (int i = 0; i < n; i++)
						Process(t.GetChild(i).gameObject, desired, addToQueue);
					break;
				}

				case EInitialActiveTarget.List:
				{
					foreach (var go in m_objects)
						Process(go, desired, addToQueue);
					break;
				}
			}
		}

		private static void Process( GameObject _go, bool _desired, bool _addToQueue )
		{
			if (_go == null)
				return;

			if (_go.activeSelf != _desired)
				_go.SetActive(_desired);

			if (!_addToQueue)
				return;

			var overlay = _go.GetComponent<IUiStartupOverlay>();
			if (overlay != null)
				UiStartupOverlayQueue.Add(overlay);
		}
	}
}
