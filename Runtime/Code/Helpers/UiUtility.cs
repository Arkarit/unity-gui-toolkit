﻿#if UNITY_EDITOR
using System;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class UiUtility
	{
		private static readonly Vector3[] s_worldCorners = new Vector3[4];
		private static readonly Vector3[] s_screenCorners = new Vector3[4];

		
		/// <summary>
		/// Converts an event to a key code
		/// </summary>
		/// <param name="_keyEvent"></param>
		/// <param name="_suppressUpEvents">If true, key up and mouse key up events are ignored (return KeyCode.none)</param>
		/// <returns></returns>
		public static KeyCode EventToKeyCode(Event _keyEvent, bool _suppressUpEvents = false)
		{
			if (_keyEvent == null)
				return KeyCode.None;

			if (_keyEvent.isKey)
			{
				if (_suppressUpEvents &&  _keyEvent.type == EventType.KeyUp)
					return KeyCode.None;
				
				return _keyEvent.keyCode;
			}

			if (_keyEvent.isMouse)
			{
				if (_suppressUpEvents && _keyEvent.type == EventType.MouseUp)
					return KeyCode.None;
				
				int mouseButtonCode = ((int)(object) KeyCode.Mouse0 + _keyEvent.button);
				return (KeyCode)(object) mouseButtonCode;
			}

			return KeyCode.None;
		}

		public static bool IsMouse(KeyCode _keyCode)
		{
			return _keyCode >= KeyCode.Mouse0 && _keyCode <= KeyCode.Mouse6;
		}

		public static EScreenOrientation GetCurrentScreenOrientation()
		{
			return ScreenWidth() >= ScreenHeight() ? EScreenOrientation.Landscape : EScreenOrientation.Portrait;
		}

		public static Rect GetScreenRect(RectTransform _transform, Camera _cam = null, Canvas _canvas = null)
		{
			if (!_cam)
				_cam = Camera.main;

			_transform.GetWorldCorners(s_worldCorners);

			var scaleFactor = _canvas != null ? _canvas.scaleFactor : 1;
			var scaleVector = new Vector3(scaleFactor, scaleFactor, scaleFactor);
			
			for (int i = 0; i < 4; i++)
			{
				s_screenCorners[i] = Vector3.Scale(_cam.WorldToScreenPoint(s_worldCorners[i]), scaleVector);
			}

			return new Rect(s_screenCorners[0].x,
				s_screenCorners[0].y,
				s_screenCorners[2].x - s_screenCorners[0].x,
				s_screenCorners[2].y - s_screenCorners[0].y);
		}
		
		public static Rect GetRawImageRect(RawImage _rawImage)
		{
			Rect outer = _rawImage.uvRect;
			outer.xMin *= _rawImage.rectTransform.rect.width;
			outer.xMax *= _rawImage.rectTransform.rect.width;
			outer.yMin *= _rawImage.rectTransform.rect.height;
			outer.yMax *= _rawImage.rectTransform.rect.height;
			return outer;
		}
		
		// Fix wrong screen resolution in editor
		// https://forum.unity.com/threads/screen-width-is-wrong-in-editor-mode.94572/
		// http://muzykov.com/unity-get-game-view-resolution/
		public static int ScreenWidth()
		{
#if UNITY_EDITOR
			Vector2 v = Handles.GetMainGameViewSize();
			return (int) v.x;
#else
			return Screen.width;
#endif
		}

		public static int ScreenHeight()
		{
#if UNITY_EDITOR
			Vector2 v = Handles.GetMainGameViewSize();
			return (int) v.y;
#else
			return Screen.height;
#endif
		}
	}
}
