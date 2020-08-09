using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GuiToolkit.Base
{
	using FunctionQueue = LockedQueueWithSingle<Action>;
	using ThreadPriority = System.Threading.ThreadPriority;

	public class ThreadedMonoBehaviour : MonoBehaviour
	{
		// We use our own ThreadState, because System.Threading.ThreadState doesn't know if a running thread is already operational.
		// The "Running" here means fully operational.
		public enum ThreadState
		{
			Stopping,
			Stopped,
			Starting,
			Running,
		};

		protected const int PeriodicWakeUpPeriodMs = 100;

		[SerializeField]
		protected ThreadPriority m_threadPriority = ThreadPriority.Normal;
		[SerializeField]
		protected bool m_threaded = true;
		[SerializeField]
		protected bool m_autoStartThread = true;
		[SerializeField]
		protected bool m_waitForThreadStart = false;

		protected FunctionQueue m_toWorkerQueue;
		protected FunctionQueue m_toMainQueue;

		protected Lock m_lock = new Lock();

		protected Thread m_thread;
		protected ThreadState m_threadState = ThreadState.Stopped;
		protected EventWaitHandle m_waitHandle = new ManualResetEvent(true); 


		protected virtual Lock ToWorkerLock => m_lock;
		protected virtual Lock ToMainLock => m_lock;

		private bool m_stop;
		private bool m_stopInstant;

		private int m_mainThreadId;
		private int m_workerThreadId;

		public bool Running => m_threadState == ThreadState.Running;
		public bool Starting => m_threadState == ThreadState.Starting;
		public bool StartingOrRunning => Starting || Running;
		public bool Stopped => m_threadState == ThreadState.Stopped;
		public bool Stopping => m_threadState == ThreadState.Stopping;
		public bool StoppingOrStopped => Stopping || Stopped;

		protected virtual void Start()
		{
			m_toMainQueue = new FunctionQueue(ToMainLock);
			m_toWorkerQueue = new FunctionQueue(ToWorkerLock);
			m_mainThreadId = Thread.CurrentThread.ManagedThreadId;
			if (m_threaded && m_autoStartThread)
				StartThread(m_waitForThreadStart);
		}

		protected virtual void Update()
		{
			Debug.Assert(m_toMainQueue != null, "Main queue is null. Did you forget to call base.Start() in override?");
			#if UNITY_EDITOR
				if (m_toMainQueue == null)
					return;
			#endif

			ProcessQueueInMainThread( m_toMainQueue );

			if (!m_threaded)
				return;

			OnProcessWorker();
			ProcessQueueInMainThread(m_toWorkerQueue);
		}

		public virtual void StartThread( bool _wait = true )
		{
			CheckInMain();

			if( m_threadState == ThreadState.Running || m_threadState == ThreadState.Starting )
				return;

			// if the thread is about to shut down, we have to wait until it has settled
			while( m_threadState == ThreadState.Stopping )
				Thread.Sleep(1);

			m_stop = m_stopInstant = false;
			m_threadState = ThreadState.Starting;

			m_thread = new Thread(SThreadStartup);
			m_thread.Start(this);

			if (_wait)
			{
				while(m_threadState != ThreadState.Running)
					Thread.Sleep(1);
			}
		}

		public virtual void	StopThread( bool _wait = true, bool _instant = false )
		{
			CheckInMain();

			if (!m_threaded)
				return;

			m_stopInstant = _instant;
			m_stop = true;
			m_waitHandle.Set();

			if (_wait)
			{
				while (m_threadState != ThreadState.Stopped)
					Thread.Sleep(1);
			}
		}

		protected virtual void OnThreadStarting()
		{
		}

		protected virtual void OnThreadStopping()
		{
		}

		protected virtual void ThreadMainLoop()
		{
			CheckInWorker();
			LinkedList<Action> actionList = null;

			for (;;)
			{
				if (m_toWorkerQueue.Empty)
					m_waitHandle.WaitOne(PeriodicWakeUpPeriodMs);

				m_waitHandle.Reset();

				if (m_toWorkerQueue.PopLinkedList(ref actionList))
				{
					foreach (var action in actionList)
					{
						if (m_stop && m_stopInstant)
							return;

						action.Invoke();
					}
				}

				if (m_stop && m_toWorkerQueue.Empty)
					return;
			}
		}

		public void CallWorker(Action _action, bool _urgent = false)
		{
			m_toWorkerQueue.Push(_action, _urgent);
			m_waitHandle.Set();
		}

		public void CallWorkerSingle(Action _action, bool _urgent = false)
		{
			m_toWorkerQueue.PushSingle(_action, _urgent);
			m_waitHandle.Set();
		}

		public void CallWorkerSingleLast(Action _action)
		{
			m_toWorkerQueue.PushSingleLast(_action);
			m_waitHandle.Set();
		}

		public void CallMain(Action _action, bool _urgent = false)
		{
			m_toMainQueue.Push(_action, _urgent);
		}

		public void CallMainSingle(Action _action, bool _urgent = false)
		{
			m_toMainQueue.PushSingle(_action, _urgent);
		}

		public void CallMainSingleLast(Action _action)
		{
			m_toMainQueue.PushSingleLast(_action);
		}

		public virtual void Execute()
		{
			CheckInWorker();

			m_workerThreadId = Thread.CurrentThread.ManagedThreadId;

			OnThreadStarting();

			m_threadState = ThreadState.Running;

			ThreadMainLoop();

			m_threadState = ThreadState.Stopping;

			OnThreadStopping();

			m_threadState = ThreadState.Stopped;
		}

		protected virtual void OnProcessWorker() {}

		private void ProcessQueueInMainThread( FunctionQueue _queue )
		{
			CheckInMain();

			LinkedList<Action> popped = null;
			if (_queue.PopLinkedList(ref popped))
			{
				foreach (var action in popped)
					action.Invoke();
			}
		}

		private static void SThreadStartup(object _obj)
		{
			ThreadedMonoBehaviour tmb = _obj as ThreadedMonoBehaviour;
			tmb.Execute();
		}

		private void CheckInWorker()
		{
			if (m_threaded && m_threadState == ThreadState.Running)
			{
				Debug.Assert(m_workerThreadId == Thread.CurrentThread.ManagedThreadId, "Call from wrong thread");
			}
		}

		private void CheckInMain()
		{
			Debug.Assert(m_mainThreadId == Thread.CurrentThread.ManagedThreadId, "Call from wrong thread");
		}


	}
}