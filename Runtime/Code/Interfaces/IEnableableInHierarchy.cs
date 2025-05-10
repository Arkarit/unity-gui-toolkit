namespace GuiToolkit
{
	public interface IEnableableInHierarchy
	{
		// Tell if the class is actually enableable
		public bool IsEnableableInHierarchy { get; }

		public bool StoreEnabledInHierarchy { get; set; }

		public IEnableableInHierarchy[] Children { get; }
		public bool EnabledInHierarchy { get; set; }

		public void OnEnabledInHierarchyChanged( bool _enabled );
		
	}

	public static class EnableableInHierarchyUtility
	{
		public static bool GetEnabledInHierarchy( IEnableableInHierarchy _enableable ) => _enableable.StoreEnabledInHierarchy;

		public static void SetEnabledInHierarchy( IEnableableInHierarchy _enableable, bool _enabled )
		{
			if (_enableable.StoreEnabledInHierarchy == _enabled)
				return;
			_enableable.StoreEnabledInHierarchy = _enabled;
			_enableable.OnEnabledInHierarchyChanged(_enableable.StoreEnabledInHierarchy);

			IEnableableInHierarchy[] childComponents = _enableable.Children;

			// We can not call 'Enabled' recursively - otherwise every called child would call recursively too
			foreach (var childComponent in childComponents)
			{
				if (!childComponent.IsEnableableInHierarchy)
					continue;

				if (childComponent.StoreEnabledInHierarchy != _enabled)
				{
					childComponent.StoreEnabledInHierarchy = _enabled;
					childComponent.OnEnabledInHierarchyChanged(_enableable.StoreEnabledInHierarchy);
				}
			}
		}
	}
}