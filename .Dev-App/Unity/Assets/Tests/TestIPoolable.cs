using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class TestIPoolable
	{
		private GameObject m_prefab;
		private UiPool m_pool;

		private class TestPoolable : MonoBehaviour, IPoolable
		{
			public int CreatedCount = 0;
			public int ReleasedCount = 0;

			public void OnPoolCreated() => CreatedCount++;
			public void OnPoolReleased() => ReleasedCount++;
		}

		[SetUp]
		public void SetUp()
		{
			m_prefab = new GameObject("PrefabWithPoolable");
			m_prefab.AddComponent<TestPoolable>();

			var poolGO = new GameObject("UiPool");
			m_pool = poolGO.AddComponent<UiPool>();
			m_pool.m_poolContainer = new GameObject("PoolContainer").transform;
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_prefab);
			Object.DestroyImmediate(m_pool.gameObject);
		}

		[Test]
		public void OnPoolCreated_Is_Called_On_GetT()
		{
			var instance = m_pool.Get<TestPoolable>(m_prefab.GetComponent<TestPoolable>());
			Assert.AreEqual(1, instance.CreatedCount);
		}

		[Test]
		public void OnPoolReleased_Is_Called_On_ReleaseT()
		{
			var instance = m_pool.Get<TestPoolable>(m_prefab.GetComponent<TestPoolable>());
			m_pool.Release(instance);
			Assert.AreEqual(1, instance.ReleasedCount);
		}

		[Test]
		public void Multiple_Reuse_Calls_OnPoolable_Correctly()
		{
			var instance = m_pool.Get<TestPoolable>(m_prefab.GetComponent<TestPoolable>());
			m_pool.Release(instance);
			instance = m_pool.Get<TestPoolable>(m_prefab.GetComponent<TestPoolable>());
			m_pool.Release(instance);

			Assert.AreEqual(2, instance.CreatedCount);
			Assert.AreEqual(2, instance.ReleasedCount);
		}

		[Test]
		public void Get_By_GameObject_Does_Not_Trigger_IPoolable()
		{
			var instance = m_pool.Get(m_prefab);
			var poolable = instance.GetComponent<TestPoolable>();

			Assert.AreEqual(0, poolable.CreatedCount);
			Assert.AreEqual(0, poolable.ReleasedCount);
		}

		[Test]
		public void Releasing_Alien_With_IPoolable_Triggers_Callback_And_Warning()
		{
			var alien = new GameObject("Alien");
			var poolable = alien.AddComponent<TestPoolable>();
		
			LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*not created by the pool.*"));
			m_pool.Release(poolable);
		
			Assert.AreEqual(1, poolable.ReleasedCount);
			Object.DestroyImmediate(alien);
		}
	}
}