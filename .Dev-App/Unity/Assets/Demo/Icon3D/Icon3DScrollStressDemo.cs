using GuiToolkit;
using UnityEngine;

/// <summary>
/// Stress demo for 3D icons (phase 3 of .Dev-App/Planning/3D-Icons.md): a scroll view full of icons whose objects
/// are loaded by canonical id. With shuffling on, random icons get a new object every few frames - like pooled list
/// items being reused while their previous load is still pending. Nothing may show the wrong object, flicker, leak
/// or log errors. Built by "Gui Toolkit / 3D Icons / Create Scroll Stress Demo".
/// </summary>
public class Icon3DScrollStressDemo : MonoBehaviour
{
	[SerializeField] private UiIcon3D[] m_icons = new UiIcon3D[0];
	[SerializeField] private string[] m_ids = new string[0];
	[Tooltip("Seconds between reassignments; 0 = off")]
	[SerializeField] private float m_shuffleInterval = 0f;
	[SerializeField] private int m_iconsPerShuffle = 5;

	private float m_nextShuffle;
	private float m_frameMs;

	private void Start()
	{
		for (int i = 0; i < m_icons.Length; i++)
			m_icons[i].PrefabId = m_ids[i % m_ids.Length];
	}

	private void Update()
	{
		m_frameMs = Mathf.Lerp(m_frameMs, Time.unscaledDeltaTime * 1000f, 0.05f);

		if (m_shuffleInterval <= 0 || Time.unscaledTime < m_nextShuffle || m_icons.Length == 0 || m_ids.Length == 0)
			return;

		m_nextShuffle = Time.unscaledTime + m_shuffleInterval;
		for (int i = 0; i < m_iconsPerShuffle; i++)
			m_icons[Random.Range(0, m_icons.Length)].PrefabId = m_ids[Random.Range(0, m_ids.Length)];
	}

	private void OnGUI()
	{
		var rect = new Rect(10, 10, 420, 110);
		GUI.Box(rect, GUIContent.none);
		GUI.Label(new Rect(20, 15, 400, 20), $"Frame: {m_frameMs:F1} ms");
		GUI.Label(new Rect(20, 35, 400, 20), $"Renders total: {UiIcon3DRenderer.RenderCount}   distinct icons: {UiIcon3DRenderer.RequestCount}");
		GUI.Label(new Rect(20, 55, 400, 20), $"Loaded objects: {Icon3DAssetCache.Count}   render textures: {RenderTextureManager.Count}");

		bool shuffle = m_shuffleInterval > 0;
		bool newShuffle = GUI.Toggle(new Rect(20, 80, 400, 20), shuffle, " Shuffle (reassign icons while loading)");
		if (newShuffle != shuffle)
			m_shuffleInterval = newShuffle ? 0.05f : 0;
	}

	/// Used by the scene builder.
	public void Setup( UiIcon3D[] _icons, string[] _ids )
	{
		m_icons = _icons;
		m_ids = _ids;
	}
}
