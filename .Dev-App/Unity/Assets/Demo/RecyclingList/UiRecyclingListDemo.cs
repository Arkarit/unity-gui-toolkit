using GuiToolkit;
using UnityEngine;

/// <summary>
/// Demo for UiRecyclingList: a thousand cards with animated 3D icons in a horizontal scroll list, of which only the
/// ones in view (plus the buffer) exist. Tap selects a card; the selection is kept here, by index, and survives
/// scrolling, because the bind callback sets it on whatever item shows the index. Built by
/// "Gui Toolkit / 3D Icons / Create Recycling List Demo".
/// </summary>
public class UiRecyclingListDemo : MonoBehaviour
{
	[SerializeField][Mandatory] private UiRecyclingList m_list;
	[SerializeField][Mandatory] private UiRecyclingListDemoItem m_itemPrefab;
	[SerializeField] private GameObject[] m_objects = new GameObject[0];
	[SerializeField][Min(0)] private int m_count = 1000;

	private int m_selected = -1;
	private float m_frameMs;

	private void Start() => Show(m_count);

	private void Show( int _count )
	{
		m_count = _count;
		if (m_selected >= m_count)
			m_selected = -1;

		m_list.SetData(m_itemPrefab, m_count, Bind);
	}

	private void Bind( UiRecyclingListDemoItem _item, int _index )
	{
		var obj = m_objects.Length > 0 ? m_objects[_index % m_objects.Length] : null;
		_item.Set(_index, obj, _index * 0.37f % 2f, _index == m_selected, () => Select(_index));
	}

	private void Select( int _index )
	{
		int previous = m_selected;
		m_selected = _index;
		m_list.Refresh(previous);
		m_list.Refresh(_index);
	}

	private void Update() => m_frameMs = Mathf.Lerp(m_frameMs, Time.unscaledDeltaTime * 1000f, 0.05f);

	private void OnGUI()
	{
		GUI.Box(new Rect(10, 10, 460, 120), GUIContent.none);
		GUI.Label(new Rect(20, 15, 440, 20), $"Frame: {m_frameMs:F1} ms   entries: {m_list.Count}   items alive: {m_list.Items.Count}");
		GUI.Label(new Rect(20, 35, 440, 20), $"3D renders total: {UiIcon3DRenderer.RenderCount}   render textures: {RenderTextureManager.Count}");
		GUI.Label(new Rect(20, 55, 440, 20), $"Selected: {(m_selected < 0 ? "-" : "#" + m_selected)}");

		if (GUI.Button(new Rect(20, 80, 60, 30), "0"))
			Show(0);
		if (GUI.Button(new Rect(85, 80, 60, 30), "5"))
			Show(5);
		if (GUI.Button(new Rect(150, 80, 60, 30), "1000"))
			Show(1000);
		if (GUI.Button(new Rect(215, 80, 120, 30), "Show selected") && m_selected >= 0)
			m_list.EnsureVisible(m_selected);
		if (GUI.Button(new Rect(340, 80, 120, 30), "Random"))
			m_list.EnsureVisible(Random.Range(0, m_count));
	}

	/// Used by the scene builder.
	public void Setup( UiRecyclingList _list, UiRecyclingListDemoItem _itemPrefab, GameObject[] _objects )
	{
		m_list = _list;
		m_itemPrefab = _itemPrefab;
		m_objects = _objects;
	}
}
