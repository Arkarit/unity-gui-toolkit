using System;
using System.Collections.Generic;
using UnityEngine;



namespace GuiToolkit
{
	[CreateAssetMenu(fileName = nameof(LocaExcelBridge), menuName = StringConstants.LOCA_EXCEL_BRIDGE)]
	public class LocaExcelBridge : ScriptableObject, ILocaProvider
	{
		public enum ColumnType
		{
			Ignore,
			Key,
			LanguageTranslation
		}

		[Serializable]
		public struct ColumnDescription
		{
			public ColumnType ColumnType;
		}

		[PathField(_isFolder:false, _relativeToPath:".", _extensions:"xlsx")]
		[SerializeField][Mandatory] private PathField m_excelPath;
		[SerializeField] private string m_group;
		[SerializeField] private List<ColumnDescription> m_columnDescriptions;

		public void InitData()
		{
			// TODO: read Json
		}

		public string Translate(string _s, string _group = null)
		{
			// TODO: translate singular key
			return string.Empty;
		}

		public string Translate(string _singularKey, string _pluralKey, int _n, string _group = null)
		{
			// TODO: translate plural key
			return string.Empty;
		}

#if UNITY_EDITOR
		public void CollectData()
		{
			//TODO: convert xlsx to json and store in Resources
		}
#endif
	}
}