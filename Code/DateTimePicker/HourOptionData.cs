/// <summary>
/// Originally taken from "UnityCalender"
/// https://github.com/n-sundara-pandian/UnityCalender
/// MIT License
/// </summary>
/// 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Dropdown))]
	public class HourOptionData : MonoBehaviour
	{
		[FormerlySerializedAs("is24Hour")]
		[SerializeField] protected bool m_is24Hour = false;

		private Dropdown m_dropDownComponent = null;

		private Dropdown DropDownComponent
		{
			get
			{
				if (m_dropDownComponent == null)
				{
					m_dropDownComponent = GetComponent<Dropdown>();
				}
				return m_dropDownComponent;
			}
		}

		public int SelectedValue
		{
			get
			{
				return DropDownComponent.value;
			}
			set
			{
				DropDownComponent.value = value;
			}
		}

		private void PopulateOptionData()
		{
			DropDownComponent.ClearOptions();
			var lst24 = new List<string>();
			int h = 0;
			string tm = "am";
			string strHour = string.Empty;
			for (int i = 0; i < 24; i++)
			{
				h = i;
				if (!m_is24Hour && h > 11)
				{
					if (h > 12)
					{
						h = h - 12;
					}
					tm = "pm";
				}
				else if (!m_is24Hour && h == 0)
				{
					h = 12;
				}
				if (!m_is24Hour)
				{
					strHour = string.Format("{0} {1}", h.ToString(), tm);
				}
				else
				{
					strHour = h.ToString("D2");
				}
				lst24.Add(strHour);
			}
			DropDownComponent.AddOptions(lst24);
		}

		// Start is called before the first frame update
		protected virtual void Start()
		{
			PopulateOptionData();
		}
	}
}