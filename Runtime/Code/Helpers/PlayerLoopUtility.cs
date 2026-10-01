using System;
using System.Collections.Generic;
using UnityEngine.LowLevel;

namespace GuiToolkit
{
	/// <summary>
	/// Inserts update callbacks into Unity's player loop.
	/// </summary>
	public static class PlayerLoopUtility
	{
		/// <summary>
		/// Insert a system right before PostLateUpdate.FinishFrameRendering, i.e. after the canvas layout
		/// (PostLateUpdate.PlayerUpdateCanvases) and before the cameras render. Systems inserted later end up
		/// behind systems inserted earlier. Idempotent per marker type.
		/// </summary>
		/// <param name="_marker">Type identifying the system; inserting the same marker twice does nothing.</param>
		/// <returns>true if the system is (now) part of the player loop</returns>
		public static bool InsertBeforeFrameRendering( Type _marker, PlayerLoopSystem.UpdateFunction _update )
		{
			var loop = PlayerLoop.GetCurrentPlayerLoop();
			if (loop.subSystemList == null)
				return false;

			for (int i = 0; i < loop.subSystemList.Length; i++)
			{
				ref var system = ref loop.subSystemList[i];
				if (system.type != typeof(UnityEngine.PlayerLoop.PostLateUpdate) || system.subSystemList == null)
					continue;

				var subSystems = new List<PlayerLoopSystem>(system.subSystemList);
				if (subSystems.Exists(s => s.type == _marker))
					return true;

				int index = subSystems.FindIndex(s => s.type == typeof(UnityEngine.PlayerLoop.PostLateUpdate.FinishFrameRendering));
				if (index < 0)
					index = subSystems.Count;

				subSystems.Insert(index, new PlayerLoopSystem
				{
					type = _marker,
					updateDelegate = _update
				});

				system.subSystemList = subSystems.ToArray();
				PlayerLoop.SetPlayerLoop(loop);
				return true;
			}

			return false;
		}
	}
}
