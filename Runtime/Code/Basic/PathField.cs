using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// <summary>
	/// Lightweight path wrapper that can represent absolute or relative
	/// file / folder paths and offers helper utilities for asset loading in editor.
	/// </summary>
	[Serializable]
	public struct PathField
	{
		[FormerlySerializedAs("Path")] 
		[SerializeField] private string m_path;

		public string Path
		{
			get => m_path;
			set => m_path = value.Replace('\\', '/');
		}

		public bool   IsRelative => !string.IsNullOrEmpty(m_path) && m_path.StartsWith('.');
		public string FullPath
		{
			get
			{
				if (string.IsNullOrEmpty(m_path))
					return null;

				return IsRelative
					? System.IO.Path.GetFullPath("./" + m_path).Replace('\\', '/')
					: m_path.Replace('\\', '/');
			}
		}

		public bool Exists
		{
			get
			{
				var fp = FullPath;
				if (string.IsNullOrEmpty(fp))
					return false;

				try
				{
					return File.Exists(fp) || Directory.Exists(fp);
				}
				catch
				{
					return false;     // swallow any IO-ish edge cases
				}
			}
		}

		public bool IsFolder
		{
			get
			{
				var fp = FullPath;
				if (string.IsNullOrEmpty(fp))
					return false;

				try
				{
					return Directory.Exists(fp);
				}
				catch
				{
					return false;
				}
			}
		}

		public bool IsFile
		{
			get
			{
				var fp = FullPath;
				if (string.IsNullOrEmpty(fp))
					return false;

				try
				{
					return File.Exists(fp) && !Directory.Exists(fp);
				}
				catch
				{
					return false;
				}
			}
		}

		public bool IsValid => IsFile || IsFolder;

		public string Extension => IsFile ? System.IO.Path.GetExtension(FullPath).TrimStart('.').ToLowerInvariant() : null;

#if UNITY_EDITOR
		/// <summary>
		/// Main asset type at this path (editor-only, null if not an asset or path invalid).
		/// </summary>
		public Type AssetType => IsFile ? AssetDatabase.GetMainAssetTypeAtPath(FullPath) : null;
#endif

		// --------------- constructors / casts ---------------
		public PathField(string _val = null) => m_path = _val;
		public static implicit operator string(PathField _val) => _val.m_path;
		public override string ToString() => m_path;

		// --------------- NEW: loading helpers ---------------
#if UNITY_EDITOR
		/// <summary>Shorthand: true if an asset of type <typeparamref name="T"/> can be loaded.</summary>
		public T TryLoad<T>(bool _logError = true) where T : UnityEngine.Object
		{
			var result = TryLoad<T>(out string errorMessage);
			if (result == null && _logError)
				UiLog.LogError(errorMessage);
			return result;
		}

		/// <summary>
		/// Tries to load an asset of the given type.  
		/// Returns true on success; on failure <paramref name="_errorMessage"/> describes why.
		/// </summary>
		public T TryLoad<T>(out string _errorMessage) where T : UnityEngine.Object
		{
			_errorMessage = null;

			if (!IsFile)
			{
				_errorMessage = $"Path '{m_path}' is not a file.";
				return null;
			}

			try
			{
				var asset = AssetDatabase.LoadAssetAtPath<T>(FullPath);
				if (asset != null)
					return asset;
			}
			catch(Exception e)
			{
				_errorMessage = $"{e.GetType().Name}: '{e.Message}'";
				return null;
			}

			_errorMessage = $"No asset of type {typeof(T).Name} found at '{m_path}'.";
			return null;
		}
#else
		// In Player builds simply stub out the helpers.
		public T TryLoad<T>() where T : UnityEngine.Object => null;
		public T TryLoad<T>(out string _errorMessage) where T : UnityEngine.Object
		{
			_errorMessage = "Asset loading is editor-only.";
			return null;
		}
#endif
	}
}
