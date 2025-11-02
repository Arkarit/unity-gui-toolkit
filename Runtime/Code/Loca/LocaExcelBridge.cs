using System;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEngine;



namespace GuiToolkit
{
	[CreateAssetMenu(fileName = nameof(LocaExcelBridge), menuName = StringConstants.LOCA_EXCEL_BRIDGE)]
	public class LocaExcelBridge : ScriptableObject
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

		[SerializeField] private List<ColumnDescription> m_columnDescriptions;
	}
}