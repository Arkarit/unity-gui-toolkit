using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Storage
{
	public abstract class AbstractStorageFactory : ScriptableObject
	{
		public abstract IReadOnlyList<StorageRoutingConfig> CreateRoutingConfigs();
	}
}