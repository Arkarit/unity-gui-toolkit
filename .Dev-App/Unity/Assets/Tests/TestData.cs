using UnityEngine;

namespace GuiToolkit.Test
{
	[CreateAssetMenu(fileName = "TestData", menuName = StringConstants.CREATE_TEST_DATA)]
	public class TestData : AbstractSingletonScriptableObject<TestData>
	{
		[Header("EditorAssetUtility")]
		[PathField(_isFolder:true, _relativeToPath:".")]
		[SerializeField] private PathField[] m_sortByPrefabHierarchyPaths = new PathField[0];
	
		public PathField[] SortByPrefabHierarchyPaths => m_sortByPrefabHierarchyPaths;
	}
}
