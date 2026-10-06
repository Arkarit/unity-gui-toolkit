using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace GuiToolkit
{
	/// <summary>
	/// Which <see cref="IIcon3DRenderBackend"/> renders icons in the current render pipeline. The URP and HDRP backends
	/// live in assemblies of their own (they need the pipeline packages, which a project may not have) and register
	/// themselves here; the renderer picks by <see cref="GraphicsSettings.currentRenderPipeline"/> and switches when
	/// that changes. Without a match, Built-in.
	/// </summary>
	public static class Icon3DBackends
	{
		private struct Entry
		{
			public string Name;
			public Func<RenderPipelineAsset, bool> Matches;
			public Func<IIcon3DRenderBackend> Create;
		}

		private static readonly List<Entry> s_entries = new();

		/// <summary>Registers a backend. Call again with the same _name to replace it. Registrations do not survive a domain reload.</summary>
		public static void Register( string _name, Func<RenderPipelineAsset, bool> _matches, Func<IIcon3DRenderBackend> _create )
		{
			s_entries.RemoveAll(_e => _e.Name == _name);
			s_entries.Add(new Entry { Name = _name, Matches = _matches, Create = _create });
		}

		public static IReadOnlyList<string> RegisteredNames
		{
			get
			{
				var names = new List<string>();
				foreach (var entry in s_entries)
					names.Add(entry.Name);
				return names;
			}
		}

		/// <summary>The backend for _pipeline (null = Built-in).</summary>
		public static IIcon3DRenderBackend CreateFor( RenderPipelineAsset _pipeline )
		{
			if (_pipeline != null)
			{
				foreach (var entry in s_entries)
					if (entry.Matches(_pipeline))
						return entry.Create();

				UiLog.LogWarning($"3D icons: no backend for the render pipeline '{_pipeline.GetType().Name}'. Rendering as Built-in, which most likely looks wrong. " +
					"Install the pipeline's package (URP, HDRP) in the project, or set UiIcon3DRenderer.Backend.");
			}

			return new BuiltinIcon3DBackend();
		}
	}
}
