using System.Collections;
using System.Collections.Generic;
using GuiToolkit.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class TestLockedQueue
    {
		private const int SimpleTestElementCount = 5;

        // A Test behaves as an ordinary method
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
		}

		/*
				// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
				// `yield return null;` to skip a frame.
				[UnityTest]
				public IEnumerator TestLockedQueueWithEnumeratorPasses()
				{
					// Use the Assert class to test conditions.
					// Use yield to skip a frame.
					yield return null;
				}
		*/
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

			bool popped = _queue.PopList(ref poppedVals);
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
