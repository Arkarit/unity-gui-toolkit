using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Creates storage routing configuration instances.
	/// </summary>
	/// <remarks>
	/// Factories are ScriptableObjects so they can be configured as Unity assets.
	/// The produced routing configs are consumed by Storage.Initialize().
	/// </remarks>
	/// <seealso cref="Storage"/>
	public abstract class AbstractStorageFactory : ScriptableObject
	{
		private IByteStore m_defaultByteStore;
		private ISerializer m_defaultSerializer;

		public IByteStore DefaultByteStore
		{
			get
			{
				if (m_defaultByteStore == null)
					m_defaultByteStore = CreateDefaultByteStore();
				return m_defaultByteStore;
			}
		}

		public ISerializer DefaultSerializer
		{
			get
			{
				if (m_defaultSerializer == null)
					m_defaultSerializer = CreateDefaultSerializer();

				return m_defaultSerializer;
			}
		}

		public abstract IByteStore CreateDefaultByteStore();
		public abstract ISerializer CreateDefaultSerializer();

		/// <summary>
		/// Creates routing configurations used to initialize the storage system.
		/// </summary>
		/// <returns>A list of routing configs describing stores, serializer and per-collection policies.</returns>
		public abstract IReadOnlyList<StorageRoutingConfig> CreateRoutingConfigs();
	}
}