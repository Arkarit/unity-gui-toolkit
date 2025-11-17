using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[Flags]
	public enum GradientDirections
	{
		None = 0,
		Down = 1 << 0, // top=A, bottom=B
		Up = 1 << 1, // top=B, bottom=A
		Right = 1 << 2, // left=A, right=B
		Left = 1 << 3, // left=B, right=A
					   // Reserved for future: Diagonals
	}

	public class GradientSpriteGeneratorWindow : EditorWindow
	{
		[SerializeField] private Color m_colorA = Color.black;
		[SerializeField] private Color m_colorB = Color.white;

		[Min(1)][SerializeField] private int m_gradientCount = 15;
		[Min(2)][SerializeField] private int m_length = 128; // resolution along the gradient axis

		[SerializeField]
		private GradientDirections m_directions =
			GradientDirections.Down | GradientDirections.Up |
			GradientDirections.Right | GradientDirections.Left;

		[SerializeField] private string m_globalPrefix = "grad_";
		[SerializeField] private string m_prefixDown = "d_";
		[SerializeField] private string m_prefixUp = "u_";
		[SerializeField] private string m_prefixRight = "r_";
		[SerializeField] private string m_prefixLeft = "l_";

		[PathField(true)]
		[SerializeField] private PathField m_outputFolder = new();

		private const TextureFormat kTexFormat = TextureFormat.RGBA32;
		private bool m_hasAlpha;

		[MenuItem(StringConstants.GRADIENT_GENERATOR)]
		public static void Open()
		{
			var wnd = GetWindow<GradientSpriteGeneratorWindow>("Gradient Generator");
			wnd.minSize = new Vector2(380, 320);
			wnd.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
			m_colorA = EditorGUILayout.ColorField("Color A (start)", m_colorA);
			m_colorB = EditorGUILayout.ColorField("Color B (end)", m_colorB);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Series", EditorStyles.boldLabel);
			m_gradientCount = EditorGUILayout.IntField("Number of variations", m_gradientCount);
			m_length = EditorGUILayout.IntField("Gradient length (pixels)", m_length);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Directions", EditorStyles.boldLabel);
			m_directions = (GradientDirections)EditorGUILayout.EnumFlagsField("Generate", m_directions);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Naming", EditorStyles.boldLabel);
			m_globalPrefix = EditorGUILayout.TextField("Global prefix", m_globalPrefix);
			using (new EditorGUI.IndentLevelScope())
			{
				m_prefixDown = EditorGUILayout.TextField("Down prefix", m_prefixDown);
				m_prefixUp = EditorGUILayout.TextField("Up prefix", m_prefixUp);
				m_prefixRight = EditorGUILayout.TextField("Right prefix", m_prefixRight);
				m_prefixLeft = EditorGUILayout.TextField("Left prefix", m_prefixLeft);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty outputFolderProp = serializedObject.FindProperty("m_outputFolder");
			EditorGUILayout.PropertyField(outputFolderProp);
			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.HelpBox(
				"Width is 1 for vertical gradients and length for horizontal gradients. " +
				"Files are saved as PNG and imported as Sprites.", MessageType.Info);

			EditorGUILayout.Space();
			using (new EditorGUI.DisabledScope(m_directions == GradientDirections.None))
			{
				if (GUILayout.Button("Generate Gradient Sprites"))
				{
					Generate();
				}
			}
		}

		private void Generate()
		{
			m_hasAlpha = m_colorA.a < 1 || m_colorB.a < 1;
			
			string folder = GetOutputFolderPath();
			if (string.IsNullOrEmpty(folder))
			{
				EditorUtility.DisplayDialog("Output Folder",
					"Please select or assign a folder under Assets.", "OK");
				return;
			}

			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
				AssetDatabase.Refresh();
			}

			string endHex = ToHex6or8(m_colorB);

			int created = 0;

			for (int i = 0; i < m_gradientCount; i++)
			{
				// Keep the last variation below pure B->B to match the "eeeeee->ffffff" pattern.
				float t = (float)i / (float)m_gradientCount; // last < 1.0
				Color start = Color.Lerp(m_colorA, m_colorB, t);
				string startHex = ToHex6or8(start);

				if (Has(m_directions, GradientDirections.Down))
				{
					string name = m_globalPrefix + m_prefixDown + $"{startHex}_{endHex}";
					CreateGradientSprite(folder, name, start, m_colorB, Orientation.VerticalDown);
					created++;
				}
				if (Has(m_directions, GradientDirections.Up))
				{
					string name = m_globalPrefix + m_prefixUp + $"{startHex}_{endHex}";
					CreateGradientSprite(folder, name, start, m_colorB, Orientation.VerticalUp);
					created++;
				}
				if (Has(m_directions, GradientDirections.Right))
				{
					string name = m_globalPrefix + m_prefixRight + $"{startHex}_{endHex}";
					CreateGradientSprite(folder, name, start, m_colorB, Orientation.HorizontalRight);
					created++;
				}
				if (Has(m_directions, GradientDirections.Left))
				{
					string name = m_globalPrefix + m_prefixLeft + $"{startHex}_{endHex}";
					CreateGradientSprite(folder, name, start, m_colorB, Orientation.HorizontalLeft);
					created++;
				}
			}

			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("Gradient Generator", $"Created {created} sprite(s).", "OK");
		}

		private enum Orientation
		{
			VerticalDown,    // top=A, bottom=B
			VerticalUp,      // top=B, bottom=A
			HorizontalRight, // left=A, right=B
			HorizontalLeft   // left=B, right=A
		}

		private void CreateGradientSprite( string _folder, string _fileName,
			Color _a, Color _b, Orientation _orientation )
		{
			int width = (_orientation == Orientation.HorizontalLeft ||
						 _orientation == Orientation.HorizontalRight) ? m_length : 1;
			int height = (_orientation == Orientation.VerticalDown ||
						  _orientation == Orientation.VerticalUp) ? m_length : 1;

			Texture2D tex = new Texture2D(width, height, kTexFormat, false);
			tex.wrapMode = TextureWrapMode.Clamp;

			if (height == 1)
			{
				// Horizontal
				for (int x = 0; x < width; x++)
				{
					float t = (float)x / (width - 1);
					if (_orientation == Orientation.HorizontalLeft)
						t = 1f - t;
					Color c = Color.Lerp(_a, _b, t);
					tex.SetPixel(x, 0, c);
				}
			}
			else
			{
				// Vertical
				for (int y = 0; y < height; y++)
				{
					float t = (float)y / (height - 1);
					if (_orientation == Orientation.VerticalUp)
						t = 1f - t;
					Color c = Color.Lerp(_a, _b, t);
					tex.SetPixel(0, y, c);
				}
			}

			tex.Apply(false, false);

			byte[] png = tex.EncodeToPNG();
			UnityEngine.Object.DestroyImmediate(tex);

			string safeName = SanitizeFileName(_fileName) + ".png";
			string path = Path.Combine(_folder, safeName).Replace("\\", "/");
			File.WriteAllBytes(path, png);

			MarkAsSprite(path);
		}

		private static void MarkAsSprite( string _assetPath )
		{
			AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(_assetPath);
			if (importer == null)
				return;

			bool changed = false;
			if (importer.textureType != TextureImporterType.Sprite)
			{
				importer.textureType = TextureImporterType.Sprite;
				changed = true;
			}
			if (importer.wrapMode != TextureWrapMode.Clamp)
			{
				importer.wrapMode = TextureWrapMode.Clamp;
				changed = true;
			}
			if (importer.sRGBTexture == false)
			{
				importer.sRGBTexture = true;
				changed = true;
			}

			if (changed)
			{
				importer.SaveAndReimport();
			}
		}

		private string GetOutputFolderPath() => m_outputFolder.IsValid ? m_outputFolder : "Assets";

		private static bool Has( GradientDirections _mask, GradientDirections _flag )
		{
			return (_mask & _flag) == _flag;
		}

		private string ToHex6or8( Color _c )
		{
			int r = Mathf.Clamp(Mathf.RoundToInt(_c.r * 255f), 0, 255);
			int g = Mathf.Clamp(Mathf.RoundToInt(_c.g * 255f), 0, 255);
			int b = Mathf.Clamp(Mathf.RoundToInt(_c.b * 255f), 0, 255);
			if (!m_hasAlpha)
				return r.ToString("x2") + g.ToString("x2") + b.ToString("x2");
			
			int a = Mathf.Clamp(Mathf.RoundToInt(_c.a * 255f), 0, 255);
			return r.ToString("x2") + g.ToString("x2") + b.ToString("x2") + a.ToString("x2");
		}

		private static string SanitizeFileName( string _name )
		{
			foreach (char c in Path.GetInvalidFileNameChars())
				_name = _name.Replace(c.ToString(), "_");
			return _name;
		}
	}
}
