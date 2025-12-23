using System;
using System.Threading;

namespace GuiToolkit.Storage
{
	public static class Storage
	{
		private static SynchronizationContext? s_mainContext;

		public static void InitializeOnMainThread()
		{
			s_mainContext = SynchronizationContext.Current;
		}

		public static void PostToMainThread( Action _action )
		{
			if (s_mainContext == null)
			{
				_action();
				return;
			}

			s_mainContext.Post(_ => _action(), null);
		}
	}
}