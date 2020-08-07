using System;
using System.Threading;
using UnityEngine;

namespace GuiToolkit.Base
{
	/// <summary>
	/// Thread safe implementation of Random
	/// Not serializable by now.
	/// </summary>
	public static class TSRandom
	{
		[ThreadStatic]
		private static System.Random ts_random;

		public static System.Random Random
		{
			get
			{
				if (ts_random == null)
				{
					int seed = (int) ((1+Thread.CurrentThread.ManagedThreadId) * DateTime.UtcNow.Ticks );
					ts_random = new System.Random( seed );
				}

				return ts_random;
			}
		}

		public static float RandomFloat => (float) Random.NextDouble();

		public static int Range( int _min, int _max )
		{
			return _min + (int)((_max-_min) * RandomFloat);
		}

		public static float Range( float _min, float _max )
		{
			return _min + (float)(_max-_min) * RandomFloat;
		}


	}
}