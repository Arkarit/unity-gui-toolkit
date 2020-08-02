using System;
using System.Collections.Generic;

namespace GuiToolkit.Base
{
	// A locked queue for thread usage.
	// Only safe for one or multiple producing threads (threads which only push) and a single consuming thread (thread which only pops). The producers may not remove any elements.
	public class CLockedQueue<T>
	{
		public delegate void QueueElementCombiner<T>(ref T _combinedTo, ref T _combined2);
		public delegate void QueueAccessor<T>( ref LinkedList<T> _queue );

		protected LinkedList<T> m_queue = new LinkedList<T>();
		protected IEqualityComparer<T> m_compEqual;
		protected object m_mutex;

		// ctor
		public CLockedQueue( IEqualityComparer<T> _comparer = null, object _mutex = null )
		{
			m_compEqual = _comparer ?? EqualityComparer<T>.Default;
			SetMutex(_mutex);
		}

		// copy ctor.
		// Note that in case of an external mutex only a shallow copy is made!
		public CLockedQueue( CLockedQueue<T> _other )
		{
			m_mutex = _other.m_mutex != _other.m_queue ? _other.m_mutex : m_queue;

			// we only need to lock _other's mutex, since we are just constructed.
			lock( _other.m_mutex )
			{
				m_queue = new LinkedList<T>( _other.m_queue );
				m_compEqual = _other.m_compEqual;
			}
		}

		// standard push. Urgent places directly at the front side of the queue.
		public void Push( T _t, bool _urgent = false )
		{
			lock( m_mutex )
			{
				if( _urgent )
					m_queue.AddFirst(_t);
				else
					m_queue.AddLast( _t );
			}
		}

		// push an element. If the last element in the queue equals the pushed element, nothing is pushed.
		// element operator == () or CmpEqual required.
		public void PushSingleLast( T _t )
		{
			lock( m_mutex )
			{
				if( m_queue.Empty() )
				{
					if( m_compEqual.Equals( m_queue.Last.Value, _t ))
						return;
				}

				m_queue.AddLast( _t );
			}
		}

		// push an element, which can be combined.
		// If an element of the same type is found at the queue front, _combiner is invoked, which combines the two elements into one.
		// element operator == () or CmpEqual required.
		public void PushSingleCombine( T _t, QueueElementCombiner<T> _combiner )
		{
			lock( m_mutex )
			{
				if( m_queue.Empty() )
				{
					Push( _t );
					return;
				}

				T last = m_queue.Last.Value;

				if (m_compEqual.Equals(last, _t))
				{
					_combiner(ref last, ref _t);
					m_queue.Last.Value = last;
				}
			}
		}

		// remove oldest element and return it.
		public virtual T Pop()
		{
			lock( m_mutex )
			{
				if (m_queue.Empty())
					throw new InvalidOperationException("Attempt to pop from an empty queue");

				T result = m_queue.First.Value;
				m_queue.RemoveFirst();
				return result;
			}
		}

		// fetch complete queue as linked list
		public LinkedList<T> FetchList()
		{
			lock( m_mutex )
			{
				LinkedList<T> result = m_queue;
				Clear();
				return result;
			}
		}

		// not locked - if your code depends on the thread safe return value, please lock from outside.
		public bool Empty()
		{
			return m_queue.Empty();
		}

		// not locked - if your code depends on the thread safe return value, please lock from outside.
		public int Count => m_queue.Count;

		void SetMutex( object _mutex = null )
		{
			m_mutex = _mutex ?? m_queue;
		}

		object GetMutex()
		{
			return m_mutex;
		}

		public virtual void Clear()
		{
			lock( m_mutex )
			{
				m_queue = new LinkedList<T>();
			}
		}

		// manipulate underlying list directly. Safe, because the function is called on an already locked list.
		void AccessQueue( QueueAccessor<T> _fn )
		{
			lock( m_mutex )
			_fn( ref m_queue );
		}
	};

	public class CLockedQueueWithSingle<T> : CLockedQueue<T>
	{
		private Dictionary<T, LinkedListNode<T>> m_singleEntries;

		// ctor
		public CLockedQueueWithSingle( IEqualityComparer<T> _comparer = null, object _mutex = null ) : base (_comparer, _mutex)
		{
			m_singleEntries = new Dictionary<T, LinkedListNode<T>>( _comparer ?? EqualityComparer<T>.Default );
		}

		// push an element, which shall be singular in the queue
		public void PushSingle( T _t )
		{
			lock (m_mutex)
			{
				if (m_singleEntries.TryGetValue(_t, out LinkedListNode<T> _node))
				{
					m_queue.Remove(_node);
					m_singleEntries.Remove(_t);
				}

				m_queue.AddLast(_t);
				m_singleEntries[_t] = m_queue.Last;
			}
		}

		// remove oldest element and return it.
		public override T Pop()
		{
			lock (m_mutex)
			{
				if (m_queue.Empty())
					throw new InvalidOperationException("Attempt to pop from an empty queue");

				T result = m_queue.First.Value;
				m_queue.RemoveFirst();
				m_singleEntries.Remove(result);
				return result;
			}
		}

		public override void Clear()
		{
			lock (m_mutex)
			{
				m_queue = new LinkedList<T>();
				m_singleEntries.Clear();
			}
		}
	};



}