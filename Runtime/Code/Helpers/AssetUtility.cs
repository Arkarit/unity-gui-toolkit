using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GuiToolkit
{
	public static class AssetUtility
	{
		public static bool ReadLines(string _textAssetResourcePath, List<string> _linesList, bool _removeEmpty = false)
		{
			_linesList.Clear();
			if (string.IsNullOrEmpty(_textAssetResourcePath)) 
				return false;

			TextAsset text = Resources.Load<TextAsset>(_textAssetResourcePath);
			if (text == null)
				return false;

			GetLines(text, _linesList, _removeEmpty);
			return true;
		}

		public static List<string> ReadLines(string _textAssetResourcePath, bool _removeEmpty = false )
		{
			List<string> result = new List<string>();
			ReadLines(_textAssetResourcePath, result, _removeEmpty);
			return result;
		}

		public static void GetLines( TextAsset _asset, List<string> _linesList, bool _removeEmpty = false )
		{
			_linesList.Clear();
			if (_asset == null)
				return;

			using (StringReader reader = new StringReader(_asset.text))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (_removeEmpty && line.Length == 0)
						continue;

					_linesList.Add(line);
				}
			}
		}

		public static List<string> GetLines( TextAsset _asset, bool _removeEmpty = false )
		{
			List<string> result = new List<string>();
			GetLines(_asset, result, _removeEmpty);
			return result;
		}

	}
}