using System;
using UnityEngine;

namespace GuiToolkit.Storage
{

	[CreateAssetMenu]
	public class StorageConfig : MonoBehaviour
	{
		public StorageCollectionDefinition[] Definitions = Array.Empty<StorageCollectionDefinition>();
	}
}