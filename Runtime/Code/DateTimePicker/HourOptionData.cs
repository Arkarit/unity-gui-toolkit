using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Dropdown))]
	public class HourOptionData : MonoBehaviour
	{
		[SerializeField] private bool m_is24hour;
		private Dropdown m_dropDownComponent = null;


		public bool Is24Hour
		{
			get => m_is24hour;
			private set => m_is24hour = value;
		}

		private Dropdown DropDownComponent
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
			get => DropDownComponent.value;
			set => DropDownComponent.value = value;
		}

		public void PopulateOptionData()
		{
			DropDownComponent.ClearOptions();
			var lst24 = new List<string>();
			int h = 0;
			string tm = "am";
			string strHour = string.Empty;
			for (int i = 0; i < 24; i++)
			{
				h = i;
				if (!Is24Hour && h > 11)
				{
					if (h > 12)
					{
						h = h - 12;
					}

					tm = "pm";
				}
				else if (!Is24Hour && h == 0)
				{
					h = 12;
				}

				if (!Is24Hour)
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
		void Start()
		{
			PopulateOptionData();
		}
	}
}