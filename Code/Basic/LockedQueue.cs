using System;
using System.Collections.Generic;

namespace GuiToolkit.Base
{
	/// \brief A locked queue for thread usage.
	public class LockedQueue<T>
	{
		public delegate void QueueElementCombiner<T>(ref T _combinedTo, ref T _combined2);
		public delegate void QueueAccessor<T>( ref LinkedList<T> _queue );

		protected LinkedList<T> m_queue = new LinkedList<T>();
		protected IEqualityComparer<T> m_compEqual;
		protected Lock m_lock;

		protected readonly Lock m_defaultLock = new Lock();
		private static readonly LinkedList<T> s_emptyLinkedList = new LinkedList<T>();
		private static readonly List<T> s_emptyList = new List<T>();


		/// Is Queue empty? 
		/// \attention Not locked - if your code depends on the thread safe return value, please lock from outside.
		public bool Empty => m_queue.Empty();

		/// Queue count
		/// \attention Not locked - if your code depends on the thread safe return value, please lock from outside.
		public int Count => m_queue.Count;

		/// Lock object. Can be set external or internal. If you hand over null in setter, internal is used.
		public Lock Lock
		{
			get { return m_lock; }
			private set { m_lock = value ?? m_defaultLock; }
		}

		public bool IsLockedExternally => m_lock != m_defaultLock;

		// ctor
		public LockedQueue( Lock _lock = null, IEqualityComparer<T> _comparer = null )
		{
			m_compEqual = _comparer ?? EqualityComparer<T>.Default;
			Lock = _lock;
		}

		// copy ctor.
		// Note that in case of an external mutex only a shallow copy is made!
		public LockedQueue( LockedQueue<T> _other )
		{
			m_lock = _other.IsLockedExternally ? _other.m_lock : m_defaultLock;

			// we only need to lock _other's mutex, since we are just constructed.
			lock( _other.m_lock )
			{
				m_queue = new LinkedList<T>( _other.m_queue );
				m_compEqual = _other.m_compEqual;
			}
		}

		// standard push. Urgent places directly at the front side of the queue.
		public void Push( T _t, bool _urgent = false )
		{
			lock( m_lock )
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
			lock( m_lock )
			{
				if( !m_queue.Empty() )
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
			lock( m_lock )
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
		public virtual bool Pop( ref T elem )
		{
			lock( m_lock )
			{
				return m_queue.PopFront(ref elem);
			}
		}

		// fetch complete queue as linked list
		public bool PopLinkedList( ref LinkedList<T> _list )
		{
			_list = s_emptyLinkedList;

			lock ( m_lock )
			{
				if (m_queue.Empty())
					return false;

				_list = m_queue;
				Clear();
				return true;
			}
		}

		// fetch complete queue as list
		public bool PopList( ref List<T> _list )
		{
			LinkedList<T> ll = null;

			bool result = PopLinkedList(ref ll);

			if (result)
			{
				if (_list != null)
					_list.Clear();
				else
					_list = new List<T>();

				foreach( var elem in ll )
					_list.Add(elem);
			}

			return result;
		}

		public virtual void Clear()
		{
			lock( m_lock )
			{
				m_queue = new LinkedList<T>();
			}
		}

		// manipulate underlying list directly. Safe, because the function is called on an already locked list.
		public void AccessQueue( QueueAccessor<T> _fn )
		{
			lock( m_lock )
			{
				_fn( ref m_queue );
			}
		}

	};

	public class LockedQueueWithSingle<T> : LockedQueue<T>
	{
		private Dictionary<T, LinkedListNode<T>> m_singleEntries;

		// ctor
		public LockedQueueWithSingle( Lock _lock = null, IEqualityComparer<T> _comparer = null ) : base (_lock, _comparer)
		{
			m_singleEntries = new Dictionary<T, LinkedListNode<T>>( _comparer ?? EqualityComparer<T>.Default );
		}

		public LockedQueueWithSingle( LockedQueueWithSingle<T> _other)
		{
			m_lock = _other.IsLockedExternally ? _other.m_lock : m_defaultLock;

			// we only need to lock _other's mutex, since we are just constructed.
			lock( _other.m_lock )
			{
				m_queue = new LinkedList<T>( _other.m_queue );
				m_compEqual = _other.m_compEqual;
			}
		}

		public void PushSingle( T _t, bool _addAtEndIfExists = true )
		{
			lock (m_lock)
			{
				if (m_singleEntries.TryGetValue(_t, out LinkedListNode<T> _node))
				{
					if (!_addAtEndIfExists)
					{
						_node.Value = _t;
						return;
					}
					m_queue.Remove(_node);
					m_singleEntries.Remove(_t);
				}

				m_queue.AddLast(_t);
				m_singleEntries[_t] = m_queue.Last;
			}
		}

		// remove oldest element and return it.
		public override bool Pop( ref T elem )
		{
			lock( m_lock )
			{
				if (m_queue.Empty())
					return false;

				elem = m_queue.First.Value;
				m_queue.RemoveFirst();
				m_singleEntries.Remove(elem);
				return true;
			}
		}

		public override void Clear()
		{
			lock (m_lock)
			{
				m_queue = new LinkedList<T>();
				m_singleEntries.Clear();
			}
		}
	};



}