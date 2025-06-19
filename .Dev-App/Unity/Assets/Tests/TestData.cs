using GuiToolkit;
using UnityEngine;

[CreateAssetMenu(fileName = "TestData", menuName = StringConstants.CREATE_TEST_DATA)]
public class TestData : AbstractSingletonScriptableObject<TestData>
{
	[Header("EditorAssetUtility")]
	public PathField[] SortByPrefabHierarchyPaths;
}
