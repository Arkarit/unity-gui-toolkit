using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Abstract base class for PropertyDrawer implementations.
	/// Should simplify creating custom property drawers; no more dealing with awkward rects,
	/// instead more like a custom Editor
	/// </summary>
	/// <summary>
	/// State shared by every <see cref="AbstractPropertyDrawer{T}"/>, regardless of its type argument.
	/// It has to live outside the generic class: a static field there exists once PER type argument, so a
	/// nesting counter or an on/off switch would silently be one per drawer type - and a style drawer
	/// nested inside a skin drawer would consider itself outermost.
	/// </summary>
	public static class PropertyDrawerView
	{
		/// <summary>
		/// Off switch for visibility culling, in case a drawer somewhere turns out to depend on being
		/// drawn while invisible. Nothing should, but a one-liner beats a rollback.
		/// </summary>
		public static bool CullingEnabled = true;

		/// <summary>
		/// How far outside the visible area a row is still drawn. Culling that reacted to every pixel of
		/// scrolling would change the set of created controls between the events of a single frame, and
		/// IMGUI hands out control IDs in creation order - so focus and hot control would move under the
		/// user's hands. A generous margin keeps the set stable across small scroll deltas.
		/// </summary>
		public const float CullMargin = 250;

		private static Func<Rect> s_visibleRectGetter;
		private static bool s_visibleRectResolved;

		// Only the outermost drawer call is timed; nested drawers are inside its measurement already.
		internal static int NestingDepth;

		// Row heights, keyed by property path plus whatever else the height depends on. Shared rather than
		// per type argument, so one call clears it for every drawer.
		internal static readonly Dictionary<string, float> HeightCache = new();

		/// <summary>
		/// Drops all remembered row heights. Anything that changes how tall a row draws has to call this,
		/// or rows overlap: a foldout opening or closing, a filter change, a value becoming applicable.
		/// </summary>
		public static void ClearHeightCache() => HeightCache.Clear();

		/// <summary>
		/// The currently visible part of the GUI, in the same space as the rect handed to OnGUI. This is
		/// UnityEngine.GUIClip.visibleRect, which is internal, so it is reached through a delegate bound
		/// once. If that ever fails, the fallback is an infinite rect - i.e. no culling, as before.
		///
		/// Note for anyone tempted by GUIUtility.GUIToScreenPoint and Screen.height instead: Screen.height
		/// is the display, not the inspector's viewport, so that comparison is off by however far the
		/// window sits from the top of the screen and by whatever is docked around it. That is why an
		/// earlier attempt at this "did not properly work".
		/// </summary>
		public static Rect VisibleRect
		{
			get
			{
				if (!s_visibleRectResolved)
				{
					s_visibleRectResolved = true;
					var type = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
					var getter = type?.GetProperty("visibleRect",
						BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetGetMethod(true);

					if (getter != null)
						s_visibleRectGetter = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), getter);
					else
						UiLog.LogWarning("GUIClip.visibleRect is unavailable, drawing every row without culling.");
				}

				if (s_visibleRectGetter == null)
					return new Rect(-1e9f, -1e9f, 2e9f, 2e9f);

				return s_visibleRectGetter();
			}
		}

		public static bool IsFarOutsideView( Rect _rect )
		{
			if (!CullingEnabled)
				return false;

			var visible = VisibleRect;
			return _rect.yMax < visible.yMin - CullMargin
			    || _rect.yMin > visible.yMax + CullMargin;
		}

		/// <summary>
		/// What the drawers spent, for measuring. Off by default and free while off.
		/// </summary>
		public static class Stats
		{
			public static bool Enabled;
			public static long DrawTicks;
			public static long HeightTicks;
			public static int DrawCalls;
			public static int HeightCalls;
			public static int Culled;

			public static void Reset()
			{
				DrawTicks = HeightTicks = 0;
				DrawCalls = HeightCalls = Culled = 0;
			}

			public static string Report()
			{
				double draw = DrawTicks * 1000.0 / Stopwatch.Frequency;
				double height = HeightTicks * 1000.0 / Stopwatch.Frequency;
				return $"{DrawCalls} draw passes: {draw:F1} ms total" +
				       (DrawCalls > 0 ? $" ({draw / DrawCalls:F1} ms each)" : "") +
				       $" | {HeightCalls} height passes: {height:F1} ms total" +
				       (HeightCalls > 0 ? $" ({height / HeightCalls:F1} ms each)" : "") +
				       $" | {Culled} rows culled";
			}
		}
	}

	public abstract class AbstractPropertyDrawer<T> : PropertyDrawer where T : class
	{
		private const float FoldoutHeight = 16;
		private const float IndentWidth = 20;

		protected Rect m_Rect;
		protected Rect m_currentRect;
		private bool m_collectHeightMode;
		private int m_horizontalMode;
		private float m_savedX;
		private float m_savedWidth;
		private float m_height;
		private SerializedProperty m_property;
		private static readonly Dictionary<object, bool> s_foldouts = new ();
		private bool m_heightCacheEnabled;
		private static readonly List<SerializedProperty> s_tempProperties = new();

		protected virtual void OnEnable() {}

		protected virtual void OnInspectorGUI() {}

		protected SerializedProperty Property => m_property;
		protected float SingleLineHeight => EditorGUIUtility.singleLineHeight;
		/// <summary>
		/// Caution: for a plain [Serializable] class (property type Generic) this is a FRESH COPY
		/// on every access, not the object in the target - writing to it is silently lost. Only
		/// SerializeReference (ManagedReference) properties return the real instance. Use it to
		/// read/display; write through SerializedProperty, or resolve the real object yourself
		/// (see UiSkinDrawer.FindRealSkin).
		/// </summary>
		protected T EditedClassInstance => Property.boxedValue as T;
		protected bool IsHorizontal => m_horizontalMode > 0;
		protected Rect CurrentRect => m_currentRect;
		protected bool CollectHeightMode => m_collectHeightMode;

		private void IncreaseHeight(float _height)
		{
			if (IsHorizontal)
				return;

			m_height += _height;
		}

		/// <summary>
		/// State beyond the property path and its expanded flag that this drawer's row heights depend on.
		/// Anything a derived drawer switches on while measuring belongs here, or two different heights
		/// end up sharing one cache entry - which is why the cache could not simply be turned on before.
		/// </summary>
		protected virtual string HeightCacheKeySuffix => string.Empty;

		protected virtual float GetPropertyHeight(SerializedProperty _property)
		{
			if (!HeightCacheEnabled)
				return EditorGUI.GetPropertyHeight(_property);
			
			string key = $"{_property.propertyPath}~{_property.isExpanded}~{HeightCacheKeySuffix}";
			if (PropertyDrawerView.HeightCache.TryGetValue(key, out float result))
				return result;
			
			result = EditorGUI.GetPropertyHeight(_property);
			if (result == 0)
				return result;
			
			PropertyDrawerView.HeightCache.Add(key, result);
			
			return result;
		}
		
		protected bool HeightCacheEnabled
		{
			get => m_heightCacheEnabled;
			set => m_heightCacheEnabled = value;
		}
		protected void InvalidateHeightCache() => PropertyDrawerView.ClearHeightCache();
		
		protected void PropertyField(SerializedProperty _property, bool _withChildren = true, float _gap = 0)
		{
			var propertyHeight = GetPropertyHeight(_property) + _gap;
			if (propertyHeight == 0)
				return;

			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			EditorGUI.PropertyField(drawRect, _property, _withChildren);
			NextRect(propertyHeight);
		}

		protected void LabelField(string _label, float _gap = 0, GUIStyle _style = null)
		{
			var propertyHeight = SingleLineHeight + _gap;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			if (_style != null)
				EditorGUI.LabelField(drawRect, _label, _style);
			else
				EditorGUI.LabelField(drawRect, _label);

			NextRect(propertyHeight);
		}

		protected bool Toggle(string _label, bool _currentValue, float _gap = 0, GUIStyle _style = null)
		{
			var propertyHeight = SingleLineHeight + _gap;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return _currentValue;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			bool result;
			if (_style != null)
				result = EditorGUI.Toggle(drawRect, _label, _currentValue, _style);
			else
				result = EditorGUI.Toggle(drawRect, _label, _currentValue);

			NextRect(propertyHeight);
			return result;
		}

		protected bool StringPopupField(
			string _labelText, 
			List<string> _strings, 
			string _current,
			out string _newSelection,
			string _labelText2 = null, 
			bool showRemove = false, 
			string _addItemHeadline = null,
			string _addItemDescription = null
		)
		{
			_newSelection = string.Empty;
			var propertyHeight = SingleLineHeight;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return false;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			var result = EditorUiUtility.StringPopup(drawRect, _labelText, _strings, _current, out _newSelection,
				_labelText2, showRemove, _addItemHeadline, _addItemDescription);

			NextRect(propertyHeight);
			return result;
		}

		protected T EnumPopupField<T>( string _labelText, T _current) where T:Enum
		{
			var propertyHeight = SingleLineHeight;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return _current;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			var result = (T) EditorGUI.EnumPopup(drawRect, _labelText, _current);

			NextRect(propertyHeight);
			return result;
		}

		protected void EnumPopupField<T>( string _labelText, SerializedProperty _serializedProperty) where T:Enum
		{
			var propertyHeight = SingleLineHeight;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			T val = (T)(object) _serializedProperty.intValue;
			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			val = (T) EditorGUI.EnumPopup(drawRect, _labelText, val);
			_serializedProperty.intValue = (int)(object) val;

			NextRect(propertyHeight);
		}

		protected void Space(float _gap)
		{
			var propertyHeight = _gap;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			NextRect(propertyHeight);
		}

		protected void Line(Color _color, float _gap = 0, float _width = 0, float _height = 1)
		{
			var propertyHeight = _gap + _height;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			var width = _width == 0 ? m_currentRect.width : _width;
			var lineRect = new Rect(
				m_currentRect.x, 
				m_currentRect.y,
				width,
				_height
			);

			EditorGUI.DrawRect(lineRect, _color );
			NextRect(propertyHeight);
		}

		protected void Line(float _gap = 0, float _width = 0, float _height = 1) =>
			Line(Color.gray, _gap, _width, _height);

		protected bool Foldout(object _id, string _title, Action _onFoldout) => Foldout(_id, _title, true, _onFoldout);

		protected bool Foldout(object _id, string _title, bool _default, Action _onFoldout)
		{
			var foldoutRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width *.5f, FoldoutHeight);
			if (!s_foldouts.ContainsKey(_id))
				s_foldouts.Add(_id, _default);

			var active = s_foldouts[_id];

			if (!m_collectHeightMode)
			{
				bool wasActive = active;
				active = EditorGUI.Foldout(foldoutRect, active, _title, true);

				// Opening or closing a foldout changes how tall its owner draws, so remembered heights
				// (this drawer's and everyone else's) are no longer valid.
				if (active != wasActive)
					PropertyDrawerView.ClearHeightCache();
			}

			m_currentRect.y += FoldoutHeight;
			IncreaseHeight(FoldoutHeight);

			if (active)
				Indent(() => _onFoldout());

			s_foldouts[_id] = active;
			return active;
		}

		protected float Float(GUIContent _label, float _value, float _width = -1)
		{
			if (m_collectHeightMode)
			{
				IncreaseHeight(SingleLineHeight);
				return _value;
			}

			if (_width == -1)
				_width = m_currentRect.width;

			var floatRect = new Rect
			(
				m_currentRect.x,
				m_currentRect.y,
				_width,
				SingleLineHeight
			);

			float result = string.IsNullOrEmpty(_label.text) ?
				EditorGUI.FloatField(floatRect, _value):
				EditorGUI.FloatField(floatRect, _label, _value);

			NextRect(SingleLineHeight);
			return result;
		}

		protected float Float(string _label, float _value, float _width = -1) =>
			Float(new GUIContent(_label), _value, _width);
		protected float Float(float _value, float _width = -1) =>
			Float(new GUIContent(), _value, _width);


		protected bool Button(GUIContent _content, float _width = -1)
		{
			if (m_collectHeightMode)
			{
				IncreaseHeight(SingleLineHeight);
				return false;
			}

			if (_width == -1)
				_width = m_currentRect.width;

			var buttonRect = new Rect
			(
				m_currentRect.x,
				m_currentRect.y,
				_width,
				SingleLineHeight
			);

			bool result = GUI.Button(buttonRect, _content);
			NextRect(SingleLineHeight);
			return result;
		}

		protected bool Button(string _s, float _width = -1) => Button(new GUIContent(_s), _width);

		protected void IncreaseX(float _width)
		{
			if (_width < 0)
			{
				_width = m_currentRect.width + _width;
			}

			m_currentRect.x += _width;
			m_currentRect.width -= _width;
		}

		protected void Horizontal(float _height, Action _onHorizontal)
		{
			if (!IsHorizontal)
			{
				if (m_collectHeightMode)
					IncreaseHeight(SingleLineHeight);

				m_savedX = m_currentRect.x;
				m_savedWidth = m_currentRect.width;
			}

			m_horizontalMode++;
			_onHorizontal();
			m_horizontalMode--;

			if (!IsHorizontal)
			{
				m_currentRect.x = m_savedX;
				m_currentRect.width = m_savedWidth;
			}

			NextRect(_height);
		}

		protected void Background(float _xOffs = 0, float _yOffs = 0, float _plusWidth = 0, float _plusHeight = 0) =>
			Background(
				EditorUiUtility.ColorPerSkin(new Color(0, 0, 0, .05f),new Color(1,1,1,0.05f)), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_plusHeight
			);

		protected void Background(Color _lightSkin, Color _darkSkin, float _xOffs = 0, float _yOffs = 0, float _plusWidth = 0, float _plusHeight = 0) =>
			Background(
				EditorUiUtility.ColorPerSkin(_lightSkin, _darkSkin), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_plusHeight
			);

		protected void Background(Color _color, float _xOffs = 0, float _yOffs = 0, float _plusWidth = 0, float _plusHeight = 0)
		{
			if (m_collectHeightMode)
				return;

			var rect = new Rect
			(
				m_currentRect.x + _xOffs, 
				m_currentRect.y + _yOffs, 
				m_currentRect.width + _plusWidth, 
				m_currentRect.height + _plusHeight
			);

			EditorGUI.DrawRect(rect, _color);
		}

		protected void BackgroundBox(float _xOffs, float _yOffs, float _plusWidth, float _height) =>
			BackgroundBox(
				EditorUiUtility.ColorPerSkin(new Color(0, 0, 0, .05f),new Color(1,1,1,0.05f)), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_height
			);

		protected void BackgroundBox(Color _lightSkin, Color _darkSkin, float _xOffs, float _yOffs, float _plusWidth, float _height) =>
			BackgroundBox(
				EditorUiUtility.ColorPerSkin(_lightSkin, _darkSkin), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_height
			);

		protected void BackgroundBox(Color _color, float _xOffs, float _yOffs, float _plusWidth, float _height)
		{
			if (m_collectHeightMode)
				return;

			var rect = new Rect
			(
				m_currentRect.x + _xOffs, 
				m_currentRect.y + _yOffs, 
				m_currentRect.width + _plusWidth, 
				_height
			);

			EditorGUI.DrawRect(rect, _color);
		}

		protected void Indent(Action _onIndent)
		{
			m_currentRect.x += IndentWidth;
			m_currentRect.width -= IndentWidth;
			_onIndent();
			m_currentRect.x -= IndentWidth;
			m_currentRect.width += IndentWidth;
		}

		protected void Outdent(Action _onIndent)
		{
			m_currentRect.x -= IndentWidth;
			m_currentRect.width += IndentWidth;
			_onIndent();
			m_currentRect.x += IndentWidth;
			m_currentRect.width -= IndentWidth;
		}

		private void NextRect(float _propertyHeight)
		{
			if (IsHorizontal)
				return;

			m_currentRect.y += _propertyHeight;
			m_currentRect.height -= _propertyHeight;
		}

// Don't override as of here (unless you've got a real cause)

		public override void OnGUI(Rect _rect, SerializedProperty _property, GUIContent _label)
		{
			// A row far outside the viewport is not drawn at all. Unity still asks for its height, so the
			// layout stays correct and scrolling lands where it should - only the drawing, and with it
			// every nested drawer of that row, is skipped.
			if (PropertyDrawerView.IsFarOutsideView(_rect))
			{
				if (PropertyDrawerView.Stats.Enabled)
					PropertyDrawerView.Stats.Culled++;

				return;
			}

			bool outermost = PropertyDrawerView.NestingDepth++ == 0;
			long startTicks = outermost && PropertyDrawerView.Stats.Enabled ? Stopwatch.GetTimestamp() : 0;

			try
			{
				EditorGUI.BeginProperty(_rect, _label, _property);
				m_property = _property;
				m_currentRect = m_Rect = _rect;
				OnEnable();
				OnInspectorGUI();

				EditorGUI.EndProperty();
			}
			finally
			{
				PropertyDrawerView.NestingDepth--;
				if (outermost && PropertyDrawerView.Stats.Enabled)
				{
					PropertyDrawerView.Stats.DrawTicks += Stopwatch.GetTimestamp() - startTicks;
					PropertyDrawerView.Stats.DrawCalls++;
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _)
		{
			bool outermost = PropertyDrawerView.NestingDepth++ == 0;
			long startTicks = outermost && PropertyDrawerView.Stats.Enabled ? Stopwatch.GetTimestamp() : 0;

			try
			{
				m_collectHeightMode = true;
				m_height = 0;
				m_property = _property;
				OnEnable();
				OnInspectorGUI();
				m_collectHeightMode = false;
				return m_height;
			}
			finally
			{
				PropertyDrawerView.NestingDepth--;
				if (outermost && PropertyDrawerView.Stats.Enabled)
				{
					PropertyDrawerView.Stats.HeightTicks += Stopwatch.GetTimestamp() - startTicks;
					PropertyDrawerView.Stats.HeightCalls++;
				}
			}
		}

		protected delegate bool ChildPropertyDelegate(SerializedProperty childProperty);
		protected void ForEachChildProperty(SerializedProperty _property, ChildPropertyDelegate callback)
		{
			CollectChildPropertiesSorted(_property, s_tempProperties);
			foreach (var property in s_tempProperties)
			{
				if (!callback(property))
					break;
			}

			s_tempProperties.Clear();
		}

		protected void CollectChildPropertiesSorted(SerializedProperty _property, List<SerializedProperty> list)
		{
			var enumerator = _property.Copy().GetEnumerator();
			int depth = _property.depth;

			while (enumerator.MoveNext())
			{
				var property = enumerator.Current as SerializedProperty;
				if (property == null || property.depth > depth + 1)
					continue;

				list.Add(property.Copy());
			}

			list.Sort((a, b) => a.displayName.CompareTo(b.displayName));
		}
	}
}
