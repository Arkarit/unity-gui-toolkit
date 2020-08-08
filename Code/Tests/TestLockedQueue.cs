using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GuiToolkit.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class TestLockedQueue
    {
		private class ProducerData
		{
			public LockedQueue<int> Queue;
			public LockedQueue<int> ReferenceQueue;
			public int Id;
		}

		private class ConsumerData
		{
			public LockedQueue<int> Queue;
			public List<int> ResultsList;
		}

		private const int SimpleTestElementCount = 50;
		private const int ProducerThreadCount = 50;
		private const int ThreadedTestElementCount = 100;
		private const int ThreadFinishTimeout = 10000;
		private const float ProducerRandomSleepChance = 0.05f;
		private const int ProducerMinSleepMilliseconds = 1;
		private const int ProducerMaxSleepMilliseconds = 100;

		private Thread m_consumer;
		private readonly List<Thread> m_producers = new List<Thread>();
		private bool m_stopConsumer;


        [Test]
        public void TestLockedQueueSimple()
        {
            SimpleTest(new LockedQueue<int>());
        }

		[Test]
		public void TestLockedQueueWithSingleSimple()
		{
			SimpleTest(new LockedQueueWithSingle<int>());
			LockedQueueWithSingle<int> queue = new LockedQueueWithSingle<int>();
			PushIntsSingle(queue, SimpleTestElementCount);
			queue.PushSingle(SimpleTestElementCount);
			Assert.AreEqual(queue.Count, SimpleTestElementCount + 1);
			queue.PushSingle(0, false);
			Assert.AreEqual(queue.Count, SimpleTestElementCount + 1);

			var copy = new LockedQueueWithSingle<int>(queue);

			queue.PushSingle(0,true);
		}

		[Test]
		public void TestLockedQueueThreaded()
		{
			var queue = new LockedQueue<int>();
			var referenceQueue = new LockedQueue<int>();
			var resultsList = new List<int>();

			ExecuteProducerThreads(queue, referenceQueue);
			Assert.AreEqual(queue.Count, ProducerThreadCount * ThreadedTestElementCount);

			queue.Clear();
			referenceQueue.Clear();

			m_stopConsumer = false;
			m_consumer = new Thread(ConsumerStart);
			m_consumer.Start( new ConsumerData {Queue = queue, ResultsList = resultsList});

			ExecuteProducerThreads(queue, referenceQueue);
			Thread.Sleep(200);
			m_stopConsumer = true;
			m_consumer.Join();

			List<int> referenceList = null;
			referenceQueue.PopList(ref referenceList);

			Assert.AreEqual(resultsList.Count, referenceList.Count);

			if (resultsList.Count == referenceList.Count)
			{
				for (int i=0; i<resultsList.Count; i++)
				{
					if (resultsList[i] != referenceList[i])
					{
						Debug.Log($"*** Fail, List Mismatch: resultsList[{i}]:{resultsList[i]} referenceList[{i}]: {referenceList[i]}");
					}
				}
				for (int i=0; i<resultsList.Count; i++)
				{
					Assert.AreEqual(resultsList[i], referenceList[i]);
				}
			}

		}

		private void ExecuteProducerThreads( LockedQueue<int> _queue, LockedQueue<int> _referenceQueue )
		{
			for (int i = 0; i < ProducerThreadCount; i++)
				StartProducerThread(_queue, _referenceQueue, i);

			for (int i = 0; i < ProducerThreadCount; i++)
				Assert.That(m_producers[i].Join(ThreadFinishTimeout));

			m_producers.Clear();
		}

		private void StartProducerThread( LockedQueue<int> _queue, LockedQueue<int> _referenceQueue, int _i )
		{
			m_producers.Add(new Thread(ProducerStart));
			m_producers.Back().Start(new ProducerData {Queue = _queue, ReferenceQueue = _referenceQueue, Id = _i });
		}

		private void ConsumerStart( object _obj )
		{
			ConsumerData data = _obj as ConsumerData;
			var queue = data.Queue;
			var resultsList = data.ResultsList;
			List<int> list = null;

			while( !m_stopConsumer )
			{
				if (queue.PopList(ref list))
				{
					// Debug.Log($"Popped {list.Count} elements");
					resultsList.AddRange(list);
				}
				Thread.Sleep(10);
			}
		}

		private void ProducerStart( object _obj )
		{
			ProducerData data = _obj as ProducerData;
			var queue = data.Queue;
			var referenceQueue = data.ReferenceQueue;
			var id = data.Id;

			for (int i=0; i<ThreadedTestElementCount; i++)
			{
				queue.Push(id);
				referenceQueue.Push(id);
				// Debug.Log($"Push '{id}'");

				if (TSRandom.RandomFloat < ProducerRandomSleepChance)
					Thread.Sleep(TSRandom.Range(ProducerMinSleepMilliseconds, ProducerMaxSleepMilliseconds));
			}
		}

		private void SimpleTest(LockedQueue<int> _queue)
		{
			Assert.That(_queue.Empty);

			PushInts(_queue, SimpleTestElementCount);

			LockedQueue<int> copy = new LockedQueue<int>(_queue);
			Assert.AreEqual(copy.Count, SimpleTestElementCount);

			PopInts(_queue, SimpleTestElementCount);
			PopInts(copy, SimpleTestElementCount);

			PushInts(_queue, SimpleTestElementCount);

			LinkedList<int> poppedVals = null;

			bool popped = _queue.PopLinkedList(ref poppedVals);
			Assert.That(popped);
			Assert.That(_queue.Empty);

			PopIntsLinkedList(ref poppedVals, SimpleTestElementCount);
		}

		private void PushInts( LockedQueue<int> _queue, int _count )
		{
			_queue.Clear();
			Assert.That(_queue.Empty);

			for (int i=0; i<_count; i++)
			{
				_queue.Push(i);
				Assert.That(!_queue.Empty);
			}
			Assert.AreEqual(_queue.Count, _count);

			_queue.PushSingleLast(_count-1);
			Assert.AreEqual(_queue.Count, _count);
		}

		private void PushIntsSingle( LockedQueueWithSingle<int> _queue, int _count )
		{
			_queue.Clear();
			Assert.That(_queue.Empty);

			for (int i=0; i<_count; i++)
			{
				_queue.PushSingle(i);
				Assert.That(!_queue.Empty);
			}
			Assert.AreEqual(_queue.Count, _count);
		}

		private void PopInts( LockedQueue<int> queue, int _count )
		{
			int value = 0;
			bool popped;

			Assert.AreEqual(queue.Count, _count);

			for (int i=0; i<_count; i++)
			{
				popped = queue.Pop(ref value);
				Assert.That(popped);
				Assert.AreEqual(value, i);
				Assert.AreEqual(queue.Count, _count-i-1);

				bool isLast = i == _count-1;
				if (isLast)
					Assert.That(queue.Empty);
				else
					Assert.That(!queue.Empty);
			}

			popped = queue.Pop(ref value);
			Assert.That(!popped);
		}

		private void PopIntsLinkedList( ref LinkedList<int> poppedVals, int _count )
		{
			Assert.AreEqual(poppedVals.Count, _count);

			int value = 0;
			bool popped;

			for (int i=0; i<_count; i++)
			{
				popped = poppedVals.PopFront(ref value);
				Assert.That(popped);
				Assert.AreEqual(value, i);
				Assert.AreEqual(poppedVals.Count, _count-i-1);

				bool isLast = i == _count-1;
				if (isLast)
					Assert.That(poppedVals.Empty());
				else
					Assert.That(!poppedVals.Empty());
			}

			popped = poppedVals.PopFront(ref value);
			Assert.That(!popped);
		}

    }
}
