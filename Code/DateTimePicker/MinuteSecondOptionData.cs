/// <summary>
/// Originally taken from "UnityCalender"
/// https://github.com/n-sundara-pandian/UnityCalender
/// MIT License
/// </summary>
/// 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Dropdown))]
	public class MinuteSecondOptionData : MonoBehaviour
	{
		private Dropdown m_dropDownComponent = null;
		private Dropdown dropDownComponent
		{
			get
			{
				if (m_dropDownComponent == null)
				{
					m_dropDownComponent = this.GetComponent<Dropdown>();
				}
				return m_dropDownComponent;
			}
		}

		public int SelectedValue
		{
			get
			{
				return dropDownComponent.value;
			}
			set
			{
				dropDownComponent.value = value;
			}
		}

		public void PopulateOptionData()
		{
			dropDownComponent.ClearOptions();
			var lst60 = new List<string>();
			for (int i = 0; i < 60; i++)
			{
				lst60.Add(i.ToString("D2"));
			}
			dropDownComponent.AddOptions(lst60);
		}
		// Start is called before the first frame update
		void Start()
		{
			PopulateOptionData();
		}
	}
}