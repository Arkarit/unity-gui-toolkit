using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A runtime popup menu anchored near a triggering UI element (context-menu / dropdown style).
	/// Scrollable content area supports any arbitrary item prefabs, set either in the Inspector
	/// or added at runtime via <see cref="SetOptions"/> / <see cref="AddItem"/>.
	/// Closes on tap outside via the <see cref="UiModal"/> click-catcher pattern.
	///
	/// Required prefab hierarchy (create in Unity Editor):
	/// <code>
	///   UiPopupMenu (Canvas, CanvasScaler, GraphicRaycaster, UiModal, UiPopupMenu)
	///   ├── ClickCatcher  (RectTransform stretch-fill, UiClickCatcher, Button) ← UiModal.m_clickCatcher
	///   └── PopupContainer (RectTransform, manually sized or via LayoutGroup)  ← m_popupContainer
	///       ├── Background (Image – optional visual)
	///       └── ScrollView (ScrollRect)
	///           └── Viewport (RectTransform, Mask)
	///               └── Content (RectTransform, VerticalLayoutGroup)           ← m_contentContainer
	/// </code>
	/// Set the <c>Layer</c> field on this component to <see cref="EUiLayerDefinition.Popup"/> in the prefab.
	/// </summary>
	public class UiPopupMenu : UiView
	{
		/// <summary>Configuration for a single popup menu invocation.</summary>
		public class Options
		{
			/// <summary>UI element to anchor the popup near. Pass null to skip repositioning.</summary>
			public RectTransform AnchorElement = null;

			/// <summary>Close the menu when tapping outside it. Default: true.</summary>
			public bool AllowOutsideTap = true;

			/// <summary>Automatically close the menu when any item is clicked. Default: true.</summary>
			public bool CloseOnItemClick = true;

			/// <summary>
			/// Item prefabs to instantiate at show time (in addition to inspector pre-fills).
			/// Each prefab is instantiated as a child of the scroll content container.
			/// </summary>
			public GameObject[] Items = null;

			/// <summary>
			/// Called after each item is spawned, in order of insertion.
			/// Parameters: spawned GameObject, zero-based index.
			/// </summary>
			public Action<GameObject, int> OnItemAdded = null;

			/// <summary>
			/// Called when an item that has a <see cref="UiButton"/> or Unity <see cref="Button"/>
			/// component on its root is clicked.
			/// Parameters: clicked GameObject, zero-based index.
			/// </summary>
			public Action<GameObject, int> OnItemClicked = null;

			/// <summary>
			/// Plain-text labels to turn into simple TMP entries (no prefab needed).
			/// Spawned after <see cref="Items"/> prefabs, in array order.
			/// </summary>
			public string[] StringItems = null;

			/// <summary>Called when the popup menu finishes closing.</summary>
			public Action OnClose = null;

			/// <summary>Additional offset applied to the final popup position in canvas local space.</summary>
			public Vector2 Offset = Vector2.zero;
		}

		[Tooltip("The panel that is repositioned near the anchor element.")]
		[SerializeField] private RectTransform m_popupContainer;

		[Tooltip("The scroll content parent. Items are instantiated here.")]
		[SerializeField] private RectTransform m_contentContainer;

		[Tooltip("Item prefabs instantiated into the menu when it opens (inspector pre-fill). " +
		         "Items are instantiated in array order before any code-provided items.")]
		[SerializeField] private GameObject[] m_prefilledItemPrefabs = Array.Empty<GameObject>();

		private readonly List<GameObject> m_spawnedItems = new();
		private Options m_options;

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;
		public override bool ShowDestroyFieldsInInspector => false;

		// -------------------------------------------------------------------------
		// Public API
		// -------------------------------------------------------------------------

		/// <summary>
		/// Configure the popup before calling <see cref="UiPanel.Show"/>.
		/// Call this every time you want to reuse the popup with different content.
		/// </summary>
		public void SetOptions( Options options )
		{
			m_options = options;
		}

		/// <summary>
		/// Instantiate a single item prefab into the scroll content.
		/// Can be called after <see cref="UiPanel.Show"/> to add items dynamically.
		/// </summary>
		public void AddItem( GameObject prefab )
		{
			if (prefab == null || m_contentContainer == null)
				return;

			var go = Instantiate(prefab, m_contentContainer);
			int index = m_spawnedItems.Count;
			m_spawnedItems.Add(go);
			m_options?.OnItemAdded?.Invoke(go, index);
			WireItemClick(go, index);
		}

		/// <summary>
		/// Create and add a plain-text item with no background.
		/// A <see cref="TextMeshProUGUI"/> component provides the label;
		/// a <see cref="Button"/> makes it clickable.
		/// Can be called after <see cref="UiPanel.Show"/> to add items dynamically.
		/// </summary>
		public void AddStringItem( string _label )
		{
			if (m_contentContainer == null)
				return;

			SpawnStringItem(_label);
		}

		/// <summary>
		/// Destroy all spawned items (both inspector pre-fills and code-added).
		/// </summary>
		public void ClearItems()
		{
			foreach (var item in m_spawnedItems)
				item.SafeDestroy();
			m_spawnedItems.Clear();
		}

		// -------------------------------------------------------------------------
		// UiPanel lifecycle overrides
		// -------------------------------------------------------------------------

		public override void OnBeginShow()
		{
			base.OnBeginShow();

			if (m_options == null)
				m_options = new Options();

			OnClickCatcher = m_options.AllowOutsideTap ? (Action)Hide : null;

			SpawnItems();

			if (m_options.AnchorElement != null)
			{
				// Delay by one frame: UiView applies the CanvasScaler template one frame after
				// OnEnable, so coordinates are wrong if we position on the very first show.
				m_popupContainer.gameObject.SetActive(false);
				var anchor = m_options.AnchorElement;
				ExecuteFrameDelayed(() =>
				{
					if (m_popupContainer == null)
						return;
					PositionAtAnchor(anchor);
					m_popupContainer.gameObject.SetActive(true);
				});
			}
		}

		public override void OnEndHide()
		{
			base.OnEndHide();

			// Capture callback before clearing state so it fires after cleanup.
			var onClose = m_options?.OnClose;
			ClearItems();
			m_options = null;
			onClose?.Invoke();
		}

		// -------------------------------------------------------------------------
		// Private helpers
		// -------------------------------------------------------------------------

		private void SpawnItems()
		{
			if (m_prefilledItemPrefabs != null)
			{
				foreach (var prefab in m_prefilledItemPrefabs)
					SpawnItem(prefab);
			}

			if (m_options?.Items != null)
			{
				foreach (var prefab in m_options.Items)
					SpawnItem(prefab);
			}

			if (m_options?.StringItems != null)
			{
				foreach (var label in m_options.StringItems)
					SpawnStringItem(label);
			}
		}

		private void SpawnStringItem( string _label )
		{
			if (m_contentContainer == null)
				return;

			var go = new GameObject(_label, typeof(RectTransform));
			var tmp = go.AddComponent<TextMeshProUGUI>();
			tmp.text = _label;
			var button = go.AddComponent<Button>();
			button.targetGraphic = tmp;
			go.transform.SetParent(m_contentContainer, false);

			int index = m_spawnedItems.Count;
			m_spawnedItems.Add(go);
			m_options?.OnItemAdded?.Invoke(go, index);
			WireItemClick(go, index);
		}

		private void SpawnItem( GameObject prefab )
		{
			if (prefab == null || m_contentContainer == null)
				return;

			var go = Instantiate(prefab, m_contentContainer);
			int index = m_spawnedItems.Count;
			m_spawnedItems.Add(go);
			m_options?.OnItemAdded?.Invoke(go, index);
			WireItemClick(go, index);
		}

		private void WireItemClick( GameObject item, int index )
		{
			if (m_options == null)
				return;

			bool hasClickCallback = m_options.OnItemClicked != null;
			bool autoClose = m_options.CloseOnItemClick;

			if (!hasClickCallback && !autoClose)
				return;

			// Prefer UiButton over Unity Button for consistent event handling.
			var uiButton = item.GetComponent<UiButton>();
			if (uiButton != null)
			{
				uiButton.OnClick.AddListener(() => HandleItemClick(index));
				return;
			}

			var button = item.GetComponent<Button>();
			if (button != null)
				button.onClick.AddListener(() => HandleItemClick(index));
		}

		private void HandleItemClick( int index )
		{
			if (m_options == null)
				return;

			var item = (index >= 0 && index < m_spawnedItems.Count) ? m_spawnedItems[index] : null;
			m_options.OnItemClicked?.Invoke(item, index);

			if (m_options.CloseOnItemClick)
				Hide();
		}

		/// <summary>
		/// Position <see cref="m_popupContainer"/> near the anchor element.
		/// Default: popup appears directly below the anchor, left-aligned.
		/// Flips above the anchor when the popup would overflow the canvas bottom.
		/// X is clamped so the popup stays within the canvas bounds.
		/// </summary>
		private void PositionAtAnchor( RectTransform anchor )
		{
			if (m_popupContainer == null || anchor == null)
				return;

			Canvas canvas = Canvas;
			if (canvas == null)
				return;

			// Force layout rebuild so rect sizes are up to date.
			Canvas.ForceUpdateCanvases();

			var canvasRect = (RectTransform)canvas.transform;
			Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

			// World corners: [0]=BL [1]=TL [2]=TR [3]=BR
			Vector3[] corners = new Vector3[4];
			anchor.GetWorldCorners(corners);

			// Convert anchor bottom-left and top-left to popup-canvas local space.
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect,
				RectTransformUtility.WorldToScreenPoint(cam, corners[0]),
				cam,
				out Vector2 anchorBL);

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect,
				RectTransformUtility.WorldToScreenPoint(cam, corners[1]),
				cam,
				out Vector2 anchorTL);

			// Use top-left pivot so anchoredPosition represents the popup's top-left corner.
			// anchorMin/Max (0.5, 0.5) centres the anchor on the canvas, matching the
			// canvas-centred local-space coordinates returned by ScreenPointToLocalPointInRectangle.
			m_popupContainer.anchorMin = new Vector2(0.5f, 0.5f);
			m_popupContainer.anchorMax = new Vector2(0.5f, 0.5f);
			m_popupContainer.pivot = new Vector2(0f, 1f);

			Vector2 popupSize = m_popupContainer.rect.size;
			Rect canvasBounds = canvasRect.rect;

			// Default: popup's top-left sits at anchor's bottom-left (popup below anchor).
			Vector2 pos = anchorBL + (m_options?.Offset ?? Vector2.zero);

			// Flip above anchor when the popup would overflow the canvas bottom.
			if (pos.y - popupSize.y < canvasBounds.yMin)
				pos.y = anchorTL.y + popupSize.y;

			// Clamp X so the popup stays within the canvas width.
			if (pos.x + popupSize.x > canvasBounds.xMax)
				pos.x = canvasBounds.xMax - popupSize.x;
			if (pos.x < canvasBounds.xMin)
				pos.x = canvasBounds.xMin;

			m_popupContainer.anchoredPosition = pos;
		}
	}
}
