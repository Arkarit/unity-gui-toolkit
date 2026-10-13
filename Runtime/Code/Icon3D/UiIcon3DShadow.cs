using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A fake shadow for a 3D icon: the icon's own image drawn again, tinted. With the UI_Icon3D material
	/// (premultiplied alpha) a colour of (0, 0, 0, 0.2) turns the image into a black silhouette at 20% - a drop
	/// shadow that costs one more UI draw and no 3D render. It follows the icon's image every frame, so an
	/// animated icon's shadow moves along.
	///
	/// Put it on a RawImage with the UI_Icon3D material, a sibling placed BEFORE the icon (so it is drawn behind
	/// it), and offset it with its RectTransform. The tint is the RawImage's colour. Edges are as hard as the
	/// image's: for a soft shadow the image would need a blur pass.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(RawImage))]
	public class UiIcon3DShadow : MonoBehaviour
	{
		[Tooltip("The icon whose image is the shadow's shape")]
		[SerializeField][Mandatory] private UiIcon3D m_icon;

		private RawImage m_rawImage;

		public UiIcon3D Icon
		{
			get => m_icon;
			set => m_icon = value;
		}

		private RawImage RawImage => m_rawImage != null ? m_rawImage : m_rawImage = GetComponent<RawImage>();

		// The icon swaps its texture (loading picture, finished render, a new size or object), so the shadow takes it
		// over every frame instead of once. Without a finished image it is off: a shadow of the loading picture would
		// stand there on its own.
		private void LateUpdate()
		{
			var shadow = RawImage;
			var source = m_icon != null ? m_icon.RawImage : null;
			bool show = source != null && m_icon.isActiveAndEnabled && source.enabled && m_icon.IsRendered && source.texture != null;

			if (shadow.enabled != show)
				shadow.enabled = show;

			if (!show)
				return;

			if (shadow.texture != source.texture)
				shadow.texture = source.texture;

			if (shadow.uvRect != source.uvRect)
				shadow.uvRect = source.uvRect;
		}

#if UNITY_EDITOR
		// Added next to an icon: take its material (the shadow needs the premultiplied one) and a shadow colour
		private void Reset()
		{
			m_icon = transform.parent != null ? transform.parent.GetComponentInChildren<UiIcon3D>(true) : null;
			var rawImage = RawImage;
			if (m_icon != null && m_icon.RawImage != null)
				rawImage.material = m_icon.RawImage.material;
			rawImage.color = new Color(0, 0, 0, 0.2f);
			rawImage.raycastTarget = false;
		}
#endif
	}
}
