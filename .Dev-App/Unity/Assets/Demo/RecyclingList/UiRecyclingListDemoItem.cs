using GuiToolkit;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// One card of the recycling list demo: a 3D icon, its number, a selection colour. Display only - what it shows
/// comes from the demo's bind callback, every time it is reused for another index.
/// </summary>
public class UiRecyclingListDemoItem : MonoBehaviour
{
	[SerializeField][Mandatory] private Image m_background;
	[SerializeField][Mandatory] private UiIcon3D m_icon;
	[SerializeField][Mandatory] private TMP_Text m_label;
	[SerializeField][Mandatory] private Button m_button;

	private static readonly Color s_normal = new(0.22f, 0.22f, 0.27f, 1);
	private static readonly Color s_selected = new(0.85f, 0.55f, 0.15f, 1);

	private UnityAction m_onClick;

	public void Set( int _index, GameObject _object, float _animationStartTime, bool _selected, UnityAction _onClick )
	{
		m_label.text = $"#{_index}";
		m_icon.Prefab = _object;
		m_icon.AnimationStartTime = _animationStartTime;
		m_background.color = _selected ? s_selected : s_normal;
		m_onClick = _onClick;
	}

	private void Awake() => m_button.onClick.AddListener(() => m_onClick?.Invoke());

	/// Used by the scene builder.
	public void Setup( Image _background, UiIcon3D _icon, TMP_Text _label, Button _button )
	{
		m_background = _background;
		m_icon = _icon;
		m_label = _label;
		m_button = _button;
	}
}
