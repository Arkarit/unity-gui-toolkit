using System;
using System.IO;
using System.Threading;

namespace GuiToolkit.Storage
{
	public static class Storage
	{
		private static IDocumentStore? s_documents;

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

		public static IDocumentStore Documents
		{
			get
			{
				if (s_documents == null)
				{
					throw new InvalidOperationException("Storage is not initialized.");
				}

				return s_documents;
			}
		}

		public static void InitializeLocal( string _rootDir )
		{
			IByteStore byteStore = new FileByteStore(_rootDir);
			ISerializer serializer = new NewtonsoftJsonSerializer();

			s_documents = new DocumentStore(byteStore, serializer);
		}

		public static void InitializeLocalDefault( string _appName )
		{
			if (string.IsNullOrWhiteSpace(_appName))
			{
				throw new ArgumentException("App name must not be null or whitespace.", nameof(_appName));
			}

			string rootDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				_appName,
				"storage");

			InitializeLocal(rootDir);
		}
	}
}