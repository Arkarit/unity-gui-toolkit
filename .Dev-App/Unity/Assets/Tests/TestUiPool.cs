using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class TestUiPool
	{
		private GameObject m_prefabA;
		private GameObject m_prefabB;
		private UiPool m_pool;

		[SetUp]
		public void SetUp()
		{
			m_prefabA = new GameObject("PrefabA");
			m_prefabB = new GameObject("PrefabB");

			var poolGO = new GameObject("UiPool");
			m_pool = poolGO.AddComponent<UiPool>();
			m_pool.m_poolContainer = new GameObject("PoolContainer").transform;
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_prefabA);
			Object.DestroyImmediate(m_prefabB);
			Object.DestroyImmediate(m_pool.gameObject);
		}

		[Test]
		public void Get_And_Release_Works()
		{
			var instance = m_pool.Get(m_prefabA);
			Assert.IsNotNull(instance);
			Assert.IsTrue(instance.activeSelf);

			m_pool.Release(instance);
			Assert.IsFalse(instance.activeSelf);
			Assert.AreEqual(m_pool.m_poolContainer, instance.transform.parent);
		}

		[Test]
		public void Instance_Is_Reused()
		{
			var instance1 = m_pool.Get(m_prefabA);
			m_pool.Release(instance1);
			var instance2 = m_pool.Get(m_prefabA);

			Assert.AreSame(instance1, instance2);
		}

		[Test]
		public void Different_Prefabs_Have_Independent_Pools()
		{
			var instanceA = m_pool.Get(m_prefabA);
			var instanceB = m_pool.Get(m_prefabB);

			Assert.AreNotSame(instanceA, instanceB);
			m_pool.Release(instanceA);
			m_pool.Release(instanceB);

			var reusedA = m_pool.Get(m_prefabA);
			var reusedB = m_pool.Get(m_prefabB);

			Assert.AreSame(instanceA, reusedA);
			Assert.AreSame(instanceB, reusedB);
		}

		[Test]
		public void Release_After_Clear_Leads_To_Destroy()
		{
			var instance = m_pool.Get(m_prefabA);
			m_pool.Clear();

			LogAssert.ignoreFailingMessages = true; // optional, um Unity-Warnungen zu unterdrücken
			Assert.DoesNotThrow(() => m_pool.Release(instance));
		}

		[Test]
		public void Release_Unknown_Object_Leads_To_Destroy()
		{
			var alien = new GameObject("I do not belong here");
			Assert.DoesNotThrow(() => m_pool.Release(alien));
			Object.DestroyImmediate(alien);
		}

		[Test]
		public void Destroy_Then_Release_Is_Handled_Gracefully()
		{
			var instance = m_pool.Get(m_prefabA);
			Object.DestroyImmediate(instance);
			LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*null.*"));
			Assert.DoesNotThrow(() => m_pool.Release(instance));
		}

		[Test]
		public void Get_After_Clear_Works_Again()
		{
			var instance1 = m_pool.Get(m_prefabA);
			m_pool.Release(instance1);
			m_pool.Clear();

			var instance2 = m_pool.Get(m_prefabA);
			Assert.IsNotNull(instance2);
		}
	}
}