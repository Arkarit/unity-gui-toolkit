using System;
using System.Threading;
using UnityEngine;

namespace GuiToolkit.Base
{
	using FunctionQueue = LockedQueueWithSingle<Action>;
#if false

	public enum ThreadPriority {
		Idle,
		Lowest,
		BelowNormal,
		Normal,
		AboveNormal,
		Highest,
		TimeCritical,
	};

	// Thread class for a class shared between a main thread and one worker thread.
	// This class has two benefits:
	// - No external message queue is necessary. You can call all methods of the other threads safe via std::function
	// - It can be easily and transparently switched between a two threaded mode and a single threaded mode (see also note below)
	// - You can optionally catch exceptions in the main thread, which were thrown in the worker thread.
	//   Supported exception types are char*, wchar_t*, std::string, std::wstring and std::exception (and derivates of the latter)
	//   WARNING: there is a lifetime problem with char* and wchar_t*. The pointers have to stay valid until the exception was caught
	//   and evaluated in the main thread. Usually this is not a problem, since char* and wchar_t* exceptions are thrown with static decalarations.
	//   You have to switch this on with setThrowWorkerInMain(true) and must call processMainThread() regularly in that case.
	//
	// Note: Debugging can be sometimes pretty difficult with this class, since all thread communication is done via std::function, and additionally
	// the threaded execution uses try/catch to rethrow worker exceptions in main.
	// This e.g. means, if your callWorker() lambda does a bad cast of an expired object, you might end up with a std::__non_rtti_object thrown in main,
	// but you don't have any clue where it comes from, since c++ exceptions very unfortunately don't contain any creator informations (file, line number etc)
	// So for debugging purposes of such cases it's sometimes very useful to switch threading off during the debug session with setThreaded(false);
	// This means, that the debugger now will instantly break _when the exception is thrown_.

	public class CThreadedClass : MonoBehaviour
	{
		private enum ThreadType
		{
			Worker,
			Main,
		};

		// _____________________________________________________________________________
		// members
		//

		string											m_workerName;
		Thread											m_thread;
		int												m_mainId							= Thread.CurrentThread.ManagedThreadId;
		int												m_workerId							= Thread.CurrentThread.ManagedThreadId;
		ManualResetEventSlim							m_trigger;
		bool											m_inIdle							= false;

		bool											m_stop								= false;
		bool											m_stopInstant						= false;
		bool											m_pause								= false;

		const int										MAX_USER_FUNC_ID					= 10000;
		int												m_functionIdCounterWorker			= MAX_USER_FUNC_ID+1;
		int												m_functionIdCounterMain				= MAX_USER_FUNC_ID+1;
		object											m_functionIdCounterWorkerMutex;
		object											m_functionIdCounterMainMutex;
		object											m_mutex0;

		FunctionQueue									m_toWorkerQueue;
		FunctionQueue									m_toMainQueue;	

		bool											m_threaded							= true;
		bool											m_nonThrdDirectExec					= false;

		bool											m_trackTime							= false;
//		std::chrono::high_resolution_clock::time_point	m_lastTime;

		Action											m_onThreadStartup;

		bool											m_throwWorkerInMain					= false;

		ThreadPriority									m_priority							= ThreadPriority.Normal;

		bool											m_workerInProgress					= false;
		bool											m_mainInProgress					= false;

		bool											m_reEnqueue							= false;

		int												m_currentWorkerFnId					= -1;
		int												m_currentMainFnId					= -1;

		// _____________________________________________________________________________
		// implementation
		//

		public CThreadedClass( string _name, ThreadPriority _priority = ThreadPriority.Normal )
		{
			m_workerName = _name;
			m_priority = _priority;
			m_mutex0 = this;
			m_toWorkerQueue = new FunctionQueue(null, m_mutex0);
			m_toMainQueue = new FunctionQueue(null, m_mutex0);
		}

		// this has to be called periodically by the main thread to process all function calls to main.
		protected virtual void Update()
		{
			processQueueInMainThread( m_toMainQueue, ref m_mainInProgress );

			// in multi mode, the worker thread does the rest.
			if( m_threaded )
				return;

			// we call the worker onProcessCallback() in main context
			float deltaSeconds = trackTime();
			onProcessWorker(deltaSeconds);

			processQueueInMainThread( m_toWorkerQueue, ref m_workerInProgress );

			// and run the main queue again, to process stuff, which has been enqueued by worker queue
			processQueueInMainThread( m_toMainQueue,ref m_mainInProgress );
		}

		private void processQueueInMainThread( FunctionQueue _queue, ref bool _progressFlag )
		{
			bool isWorkerQueue = _queue == m_toWorkerQueue;

			if( !_queue.Empty() )
			{
				// no try/catch and rethrow in main required here, since we are in main anyway

				while( ( !m_pause || !isWorkerQueue ) && !_queue.Empty() )
				{
					_progressFlag = true;
					Action fn = null;

					lock (_queue.GetMutex())
					{
						// we have to check a second time for queue not empty - first check
						// was not locked, and queue may be emptied in the meantime
						// BUT the function mustn't be executed yet, because it may also lock -> deadlocks
						if( !_queue.Empty() )
							fn = _queue.Pop();
					}

					if( fn != null ) {

						if( isWorkerQueue )
							m_currentWorkerFnId = fn.getId();
						else
							m_currentMainFnId = fn.getId();

						fn();

						if( isWorkerQueue )
							m_currentMainFnId = (size_t) -1;
						else
							m_currentMainFnId = (size_t) -1;
					}

					if( isWorkerQueue && evalReEnqueue() ) {
						std::lock_guard<std::recursive_mutex> l(m_toWorkerQueue);
						m_toWorkerQueue.push( fn );
					}

					_progressFlag = false;
				}
			}

		}

		virtual void		start						( bool _wait = true, const std::function<void()>& _onThreadStartup = nullptr );
		virtual void		stop						( bool _instant = false );

		const std::string&	getWorkerName				() const { return m_workerName; }
		void				setWorkerName				( const std::string& _name );
		bool				getRunning					() const { return m_threadState == ThreadState::Running; }
		bool				getWorking					() const { return !sleepConditionMet(); }
		bool				getLeizure					() const { return sleepConditionMet(); }
		bool				getIdle						() const { return m_inIdle; }
		bool				getPauseAndIdle				() const { return m_inIdle && m_pause; }

		// the queues to main and worker can be separately locked. This is vulnerable to deadlocks, so
		// by default only one lock is used and can be configured here (before thread has been started).
		void				setSeparateQueueLocking		( bool _val );
		bool				getSeparateQueueLocking		() const { return m_toWorkerMutex != m_toMainMutex; }

		void				wakeUp						( bool _wait = false );
		void				waitForWorker				();

		std::thread::id		getMainId					() { return m_mainId; }
		std::thread::id		getWorkerId					() { return m_workerId; }
		std::thread::id		getCurrentId				();

		ThreadState			getThreadState				() const { return m_threadState; }

		// multi mode switches between multi (threaded) and single (non-threaded) mode.
		bool				getThreaded					() const { return m_threaded; }
		void				setThreaded					(bool _val);

		// single (non-threaded) mode offers two options:
		// non-direct execution stores all callWorker()-calls in a queue and evaluates them in processMainThread(). This resembles the threading behavior better and thus is default.
		// direct execution executes callWorker()-calls instantly.
		bool				getNonThreadedDirectExecution	() const { return m_nonThrdDirectExec; }
		void				setNonThreadedDirectExecution	( bool _val ) { m_nonThrdDirectExec = _val; }

		// set/use this, if you need time delta in process
		bool				getTrackTime				() const { return m_trackTime; }
		void				setTimeTracking				(bool _val);

		// These are the conditions to wake up thread or wait for thread sleeping.

		// Note that, if you override wakeUpConditionMet, you always have to OR with the condition of the base class, e.g.
		// bool MySubClass::wakeUpConditionMet() const override { return myWakeUpCondition || __super::wakeUpConditionMet(); }
		// If you want a free running thread e.g. for polling, you can also simply return true here; perhaps combined with a short Sleep() to reduce CPU utilization.
		virtual bool		wakeUpConditionMet			() const;

		// Note that, if you override sleepConditionMet, you always have to AND with the condition of the base class, e.g.
		// bool MySubClass::sleepConditionMet() const override { return mySleepCondition && __super::sleepConditionMet(); }
		// If you want a free running thread e.g. for polling, you can also simply return false here
		virtual bool		sleepConditionMet			() const;

		bool				shuttingDown				() const { return m_threadState == ThreadState::ShuttingDown; }

		bool				getThrowWorkerInMain		() const { return m_throwWorkerInMain; }
		void				setThrowWorkerInMain		( bool _val ) { m_throwWorkerInMain = _val; }

		size_t				acquireFunctionId			();

		virtual size_t		callWorker					( const std::function<void()>& _f, bool _urgent = false);
		inline size_t		callWorkerUrgent			( const std::function<void()>& _f ) { return callWorker(_f, true); }
		virtual size_t		callWorker					( size_t _id, const std::function<void()>& _f, bool _urgent = false);
		inline size_t		callWorkerUrgent			( size_t _id, const std::function<void()>& _f) { return callWorker(_id, _f, true); }
		virtual size_t		callWorkerSingle			( size_t _id, const std::function<void()>& _f);
		virtual size_t		callWorkerSingleLast		( size_t _id, const std::function<void()>& _f);

		void				callMain					( const std::function<void()>& _f);
		void				callMainSingle				( size_t _id, const std::function<void()>& _f);
		void				callMainSingleLast			( size_t _id, const std::function<void()>& _f);

		// Thread priority
		virtual ThreadPriority		getPriority			() const { return m_priority; }
		virtual void				setPriority			( ThreadPriority _priority );

		// moves the function with _id to the front of the queue. returns false if not found.
		bool					makeWorkerFnUrgent			( size_t _id ) { return urgentOrRemove(_id, true ); }

		// removes a worker function from the queue
		bool					removeWorkerFn				( size_t _id ) { return urgentOrRemove(_id, false ); }

		bool					hasWorkerFn					( size_t _id ) const;
		
		// checks, if a given function id is still enqueued. Very useful if you want to wait until execution has begun.
		bool					stillEnqueued				( size_t _id ) const;

		// get the ID of the currently running worker function.
		// May only be called from the worker thread. Returns (size_t)-1 if no worker is running.
		// Calling this function is only useful in the worker function itself; there, getCurrentWorkerFnId is guaranteed to be valid.
		size_t					getCurrentWorkerFnId		() const { checkThreaded(); checkInWorker(); return m_currentWorkerFnId; }
		// Same, but for main function
		size_t					getCurrentMainFnId			() const { checkInMain(); return m_currentMainFnId; }

		bool					anyWorkerEnqueued			() const { return !m_toWorkerQueue.empty(); }
		bool					anyMainEnqueued				() const { return !m_toMainQueue.empty(); }

		// Pauses execution. May only be called from main thread.
		// Note: only the thread itself is paused. Main thread processing can never be paused.
		// Functions, which are in the middle of execution, are not paused, only the pending functions in the queue are not executed anymore.
		// _wait waits for the finishing of the currently executed function until it returns.
		// To pause the execution is e.g. very useful if you want to cancel a lot of queued operations, because it avoids otherwise necessary excessive locking.
		virtual void			pause						( bool _wait = true );
		// Resume from pause.
		virtual void			resume						();

		virtual bool			getPause					() const { return m_pause; }

		// Waits until the thread enters its wait state. May only be called from main thread.
		void					waitForIdle					();

		// Waits for the currently running function, and then clears all queues
		// May be called non-threaded, from main or worker or any other thread
		void					clear						();

		// must be called with an already locked worker mutex - otherwise the next function can be already in execution when returned
		// may be only called from main thread
		void					waitForWorkerFn				() const;
		// same for main fn - may be only called from worker
		void					waitForMainFn				() const;
		std::recursive_mutex&	getWorkerMutex				() const { return *m_toWorkerMutex; }

		// This re-enqueues the currently executed worker function. This is a poor man's "yield return":
		// If your function can currently not perform what it wants to do, it can call reEnqueue and return.
		// The function is then re-pushed to the front of the queue, and again executed at a later time.
		// May obviously only be called from worker.
		void					reEnqueue					();

	protected:
		// override and call super if you need initialization/destruction (already in thread context here)
		virtual void		execute						();

		// can be used for additional stuff like e.g. additional message queues.
		// normally, it should not be necessary to override this.
		// _deltaSeconds is only valid, if time tracking is switched on
		virtual void		onProcessWorker				( float _deltaSeconds ) { _deltaSeconds; }

		bool				isInMain					() const;
		bool				isInWorker					() const;

		void				checkInMain					() const;
		void				checkInWorker				() const;
		void				checkNotInMain				() const;
		void				checkNotInWorker			() const;
		void				checkThreaded				() const;

		bool				getDirectExecution			( ThreadType _threadType ) const;

		void				applyThreadPriority			();

	private:
		void				_execute					();
		bool				urgentOrRemove				( size_t _id, bool _urgent );
		float				trackTime					();
		bool				evalReEnqueue				();

	};
#endif

}