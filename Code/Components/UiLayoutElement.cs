using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiLayoutElement : UIBehaviour, ILayoutElement, ILayoutIgnorer
	{
		public float minWidth => throw new System.NotImplementedException();

		public float preferredWidth => throw new System.NotImplementedException();

		public float flexibleWidth => throw new System.NotImplementedException();

		public float minHeight => throw new System.NotImplementedException();

		public float preferredHeight => throw new System.NotImplementedException();

		public float flexibleHeight => throw new System.NotImplementedException();

		public int layoutPriority => throw new System.NotImplementedException();

		public bool ignoreLayout => throw new System.NotImplementedException();

		public void CalculateLayoutInputHorizontal()
		{
			throw new System.NotImplementedException();
		}

		public void CalculateLayoutInputVertical()
		{
			throw new System.NotImplementedException();
		}

		// Start is called before the first frame update
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}