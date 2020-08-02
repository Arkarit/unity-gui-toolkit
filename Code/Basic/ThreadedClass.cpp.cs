#include "pch.h"
#include "ThreadedClass.h"
#include "baseassert.h"
#include "logging.h"
#include "common.h"

#include <chrono>

#ifdef _WIN32
#include <windows.h>

const DWORD MS_VC_EXCEPTION=0x406D1388;

#pragma pack(push,8)
typedef struct tagTHREADNAME_INFO
{
	DWORD dwType; // Must be 0x1000.
	LPCSTR szName; // Pointer to name (in user addr space).
	DWORD dwThreadID; // Thread ID (-1=caller thread).
	DWORD dwFlags; // Reserved for future use, must be zero.
} THREADNAME_INFO;
#pragma pack(pop)

static void setThreadName( const char* threadName)
{
	THREADNAME_INFO info;
	info.dwType = 0x1000;
	info.szName = threadName;
	info.dwThreadID = (DWORD) -1;
	info.dwFlags = 0;

	__try
	{
		RaiseException( MS_VC_EXCEPTION, 0, sizeof(info)/sizeof(ULONG_PTR), (ULONG_PTR*)&info );
	}
	__except(EXCEPTION_EXECUTE_HANDLER)
	{
	}
}
#else
static void setThreadName( const char* threadName) {}
#endif

#define CATCH_AND_RETHROW_IN_MAIN(TYPE)				\
catch( TYPE val )									\
{													\
	if( m_throwWorkerInMain )						\
	{												\
		callMain( [val]() {							\
			throw val;								\
		});											\
	}												\
	else											\
		throw;										\
}

namespace Base
{

	using std::chrono::high_resolution_clock;



	void CThreadedClass::checkThreaded() const {
		base_assert( m_threaded && "Action call is invalid in non-threaded state" );
	}

	CThreadedClass::CThreadedClass( const std::string& _name, Base::ThreadPriority _priority )
		: m_workerName(_name)
		, m_priority(_priority)
	{

	}

	CThreadedClass::~CThreadedClass()
	{
		stop(true);
	}

	void CThreadedClass::processMainThread()
	{
		processQueueInMainThread( m_toMainQueue, m_mainInProgress );

		// in multi mode, the worker thread does the rest.
		if( m_threaded )
			return;

		// we call the worker onProcessCallback() in main context
		float deltaSeconds = trackTime();
		onProcessWorker(deltaSeconds);

		processQueueInMainThread( m_toWorkerQueue, m_workerInProgress );

		// and run the main queue again, to process stuff, which has been enqueued by worker queue
		processQueueInMainThread( m_toMainQueue, m_mainInProgress );
	}

	void CThreadedClass::setSeparateQueueLocking(bool _val)
	{
		checkInMain();
		base_assert( m_threadState == ThreadState::Stopped && "This setting can only be changed when thread is stopped" );
		std::recursive_mutex* mutex = _val ? &m_mutex1 : & m_mutex0;
		m_toWorkerMutex = mutex;
		m_toWorkerQueue.setMutex(mutex);
	}

	void CThreadedClass::wakeUp( bool _wait )
	{
		if( !m_threaded )
			return;

		m_wakeUp.fetch_add(1);
		m_trigger.notify_one();

		if( _wait )
			waitForWorker();
	}

	void CThreadedClass::waitForWorker()
	{
		if( isInWorker() || !m_threaded )
			return;

		while (!sleepConditionMet())
		{
			m_trigger.notify_one();
			sleep(1);
		}
	}

	std::thread::id CThreadedClass::getCurrentId()
	{
		return std::this_thread::get_id();
	}

	void CThreadedClass::setThreaded(bool _val)
	{
		checkInMain();
		if( _val == m_threaded )
			return;

		if( _val )
			start();
		else
			stop();

		m_threaded = _val;
	}

	void CThreadedClass::setTimeTracking(bool _val)
	{
		m_trackTime = _val;
		if( _val )
			m_lastTime = high_resolution_clock().now();
	}

	void CThreadedClass::start(bool _wait, const std::function<void()>& _onThreadStartup)
	{
		checkInMain();
		
		m_onThreadStartup = _onThreadStartup;

		if( m_threadState ==ThreadState:: Running || m_threadState == ThreadState::Starting )
			return;

		// if the thread is about to shut down, we have to wait until it has settled
		while( m_threadState == ThreadState::ShuttingDown )
			sleep(1);

		base_assert_log_dbgbreak( m_threadState == ThreadState::Stopped );

		m_stop = false;
		m_stopInstant = false;
		m_threadState = ThreadState::Starting;

		m_toWorkerQueue.clear();
		m_toMainQueue.clear();

		// move
		m_thread = std::thread( &CThreadedClass::_execute, this );

		if( _wait )
		{
			while( getThreadState() != ThreadState::Running )
				sleep(1);
		}

	}

	void CThreadedClass::stop( bool _instant )
	{
		if( m_threadState == ThreadState::Stopped)
			return;

		if( isInWorker() && getThreaded() )
		{
			callMain( [this,_instant]() { stop(_instant); } );
			return;
		}

		// if it's a freshly started thread, we have to wait until it's running
		while( m_threadState == ThreadState::Starting )
			sleep(1);

		base_assert_log_dbgbreak( m_threadState == ThreadState::Running || m_threadState == ThreadState::ShuttingDown );

		m_threadState = ThreadState::ShuttingDown;

		m_stopInstant = _instant;
		m_stop = true;
		while( m_threadState == ThreadState::ShuttingDown )
		{
			m_trigger.notify_one();
			sleep(1);
		}

		if( m_thread.joinable() )
			m_thread.join();
	}

	void CThreadedClass::setWorkerName( const std::string& _name ) {
		m_workerName = _name;
		// We only have to set the current thread name if we are currently threaded.
		// Non-threaded doesn't have a thread name, and when switching from non- to threaded,
		// _execute() sets it anyway.
		if( m_threaded )
			callWorker( [this](){ setThreadName( m_workerName.c_str() ); } );
	}

	void CThreadedClass::execute()
	{
		checkInWorker();
		for(;;)
		{
			std::unique_lock<std::mutex> lock(m_triggerMutex);

			// First, we have to check if the wake up variable is > 0 at all.
			// We have other wake up conditions besides the wake up variable, and it would be bad to decrement it to < 0.
			// We usually want the thread to sleep now, so we decrement the wake up variable.
			// If no wakeUp() was called in the meantime, this decrements to 0 - good night!
			// But if a wakeUp() was called, this decrements only to 1, meaning we will have to run the loop again.
			// If even more wakeUp()'s were called, we might have a result > 1, which makes no sense, since we have to run the
			// loop only once to do all the work. So in that case we decrease until 1 to run just 1 loop again.
			if(m_wakeUp.load() > 0)
				while(m_wakeUp.fetch_sub( 1 ) > 2)
					; // empty intended

			if( sleepConditionMet() ) {
				m_inIdle = true;

				do {
					// Note that a simple wait() is not sufficient here.
					// Under rare circumstances the trigger may miss the notify_one(), and thus sleep despite it shouldn't.
					// Thus we use wait_for() to wake up frequently to check the condition manually.
					m_trigger.wait_for(lock, std::chrono::milliseconds(10), [this] { return wakeUpConditionMet(); } );
				} while( !wakeUpConditionMet() );
			}

			m_inIdle = false;

			if( m_stopInstant )
				return;

			float deltaSeconds = trackTime();

			std::unique_ptr<FunctionQueue> queue;
			try {
				onProcessWorker(deltaSeconds);

				while( !m_pause && !m_toWorkerQueue.empty() )
				{
					m_workerInProgress = true;
					Action fn;
					{
						std::lock_guard<std::recursive_mutex> l(m_toWorkerQueue);
						// we have to check a second time for queue not empty - first check
						// was not locked, and queue may be emptied in the meantime.
						// BUT the function mustn't be executed yet, because it may also lock -> deadlocks
						if( !m_toWorkerQueue.empty() )
							fn = m_toWorkerQueue.pop();
					}
					if( fn ) {
						m_currentWorkerFnId = fn.getId();
						fn();
						m_currentWorkerFnId = (size_t) -1;
					}

					if( evalReEnqueue() ) {
						std::lock_guard<std::recursive_mutex> l(m_toWorkerQueue);
						m_toWorkerQueue.push( fn );
					}

					m_workerInProgress = false;

					if( m_stopInstant )
						return;
				}
			} 
			CATCH_AND_RETHROW_IN_MAIN( const char* const )
			CATCH_AND_RETHROW_IN_MAIN( const wchar_t* const )		
			CATCH_AND_RETHROW_IN_MAIN( const std::string )		
			CATCH_AND_RETHROW_IN_MAIN( const std::wstring )		
			catch( ... )
			{
				if( m_throwWorkerInMain )
				{
					std::exception_ptr pexception = std::current_exception();
					if( pexception == nullptr )
						throw;

					callMain( [pexception]() {
						std::rethrow_exception(pexception);
					});
				}
				else
					throw;
			}

			if( m_stop )
				return;
		}
	}
	
	bool CThreadedClass::wakeUpConditionMet() const
	{
		return !m_pause &&( m_wakeUp.load() > 0 || !m_toWorkerQueue.empty() || m_stop || m_stopInstant );
	}

	bool CThreadedClass::sleepConditionMet() const
	{
		return m_pause || (m_toWorkerQueue.empty() && m_wakeUp.load() == 0) || m_threadState == ThreadState::Stopped;
	}

	size_t CThreadedClass::acquireFunctionId() {
		std::lock_guard<std::recursive_mutex> l(m_functionIdCounterWorkerMutex);
		size_t result = m_functionIdCounterWorker++;
		if (m_functionIdCounterWorker == 0)
			m_functionIdCounterWorker = MAX_USER_FUNC_ID + 1;
		return result;
	}

	size_t CThreadedClass::callWorker( const std::function<void()>& _f, bool _urgent )
	{
		size_t result;
		if( getDirectExecution( ThreadType::Worker ) )
		{
			_f();
			return 0;
		}
		{
			std::lock_guard<std::recursive_mutex> l(m_functionIdCounterWorkerMutex);
			m_toWorkerQueue.push(stdEx::function<void()>(m_functionIdCounterWorker, _f), _urgent);
			result = m_functionIdCounterWorker++;
			if (m_functionIdCounterWorker == 0)
				m_functionIdCounterWorker = MAX_USER_FUNC_ID + 1;
		}
		if( !getPause() )
			wakeUp();
		return result;
	}

	size_t CThreadedClass::callWorker( size_t _id, const std::function<void()>& _f, bool _urgent ) {
		if( getDirectExecution( ThreadType::Worker ) )
		{
			_f();
			return _id;
		}
		m_toWorkerQueue.push(stdEx::function<void()>(_id, _f), _urgent);
		if( !getPause() )
			wakeUp();
		return _id;
	}

	size_t CThreadedClass::callWorkerSingle( size_t _id, const std::function<void()>& _f )
	{
		if( getDirectExecution( ThreadType::Worker ) ) 
		{
			_f();
			return 0;
		}
		m_toWorkerQueue.pushSingle(stdEx::function<void()>(_id, _f));
		if( !getPause() )
			wakeUp();
		return _id;
	}

	size_t CThreadedClass::callWorkerSingleLast( size_t _id, const std::function<void()>& _f)
	{
		if( getDirectExecution( ThreadType::Worker ) ) 
		{
			_f();
			return 0;
		}
		m_toWorkerQueue.pushSingleLast(stdEx::function<void()>(_id, _f));
		if( !getPause() )
			wakeUp();
		return _id;
	}

	void CThreadedClass::setPriority( ThreadPriority _priority ) 
	{
		m_priority = _priority;
		applyThreadPriority();
	}

	void CThreadedClass::pause( bool _wait )
	{
		// waiting in worker would lead to an infinite loop
		if( isInWorker() ) {
			base_assert( !_wait );
			_wait = false;
		}

		base_assert( m_threadState == ThreadState::Running );
		m_pause = true;
		if( _wait )
			while( !m_inIdle )
				sleep( 0 );
	}

	void CThreadedClass::resume() 
	{
		checkNotInWorker();
		base_assert( m_threadState == ThreadState::Running );
		m_pause = false;
		wakeUp( false );
	}

	void CThreadedClass::waitForIdle() 
	{
		checkNotInWorker();
		base_assert( m_threadState == ThreadState::Running );
		while(!m_inIdle)
			sleep( 0 );
	}

	void CThreadedClass::clear() {
		// clear() has to be handled separately, depending if we are non-threaded, in main or in worker thread.
		if( !m_threaded ) {
			// If we are non-threaded, we can clear everything without locking
			m_toMainQueue.clear(); 
			m_toWorkerQueue.clear();
		} else if( isInMain() ) {
			// If we are in main, we can clear the main queue without locking (we can't be there ;)
			m_toMainQueue.clear(); 
			// but we have to lock and wait for worker fn to finish before clear
			std::lock_guard<std::recursive_mutex> l( *m_toWorkerMutex );
			waitForWorkerFn();
			m_toWorkerQueue.clear();
		} else if( isInWorker() ) {
			// in worker, it's the opposite
			m_toWorkerQueue.clear();
			std::lock_guard<std::recursive_mutex> l( *m_toMainMutex );
			waitForMainFn();
			m_toMainQueue.clear();
		} else {
			// we are neither main nor worker - we have to wait for both queues
			{
				std::lock_guard<std::recursive_mutex> l( *m_toMainMutex );
				waitForMainFn();
				m_toMainQueue.clear();
			}
			{
				std::lock_guard<std::recursive_mutex> l( *m_toWorkerMutex );
				waitForWorkerFn();
				m_toWorkerQueue.clear();
			}
		}
	}

	void CThreadedClass::waitForWorkerFn() const {
		checkNotInWorker();
		while( m_workerInProgress )
			sleep( 0 );
	}

	void CThreadedClass::waitForMainFn() const {
		checkNotInMain();
		while( m_mainInProgress )
			sleep( 0 );
	}

	void CThreadedClass::reEnqueue() {
		checkInWorker(); 
		m_reEnqueue = true;
	}

	bool CThreadedClass::urgentOrRemove( size_t _id, bool _urgent )
	{
		bool result = false;

		m_toWorkerQueue.manipulateList( [&result, _id, _urgent]( std::list<Action>& _list ) {
			for( auto it = _list.begin(); it != _list.end(); ++it ) 
			{
				const Action& f = *it;
				if( f.getId() == _id ) 
				{
					const Action fc = std::move(f);
					_list.erase(it);
					if( _urgent )
						_list.emplace_front( fc );
					result = true;
					return;
				}
			}
		});

		return result;
	}

	bool CThreadedClass::hasWorkerFn( size_t _id ) const {
		bool result = false;

		m_toWorkerQueue.accessList( [&result, _id]( const std::list<Action>& _list ) {
			for( auto& f : _list ) 
			{
				if( f.getId() == _id ) 
				{
					result = true;
					return;
				}
			}
		});

		return result;
	}

	bool CThreadedClass::stillEnqueued( size_t _id ) const {
		bool result = false;

		m_toWorkerQueue.accessList( [&result, _id]( const std::list<Action>& _list ) {
			for( auto it = _list.begin(); it != _list.end(); ++it ) 
			{
				const Action& f = *it;
				if( f.getId() == _id ) 
				{
					result = true;
					return;
				}
			}
		});

		return result;
	}

	float CThreadedClass::trackTime() {
		float result = 0;
		if(m_trackTime) {
			auto now( high_resolution_clock().now() );
			std::chrono::duration<float> ddeltaSeconds = now - m_lastTime;
			result = ddeltaSeconds.count();
			m_lastTime = now;
		}
		return result;
	}

	bool CThreadedClass::evalReEnqueue() {
		bool result = m_reEnqueue; 
		m_reEnqueue = false; 
		return result;
	}

	void CThreadedClass::callMain( const std::function<void()>& _f )
	{
		if( getDirectExecution( ThreadType::Main ) )
		{
			_f();
			return;
		}
		std::lock_guard<std::recursive_mutex> l(m_functionIdCounterMainMutex);
		m_toMainQueue.push(stdEx::function<void()>(m_functionIdCounterMain, _f));
		if (++m_functionIdCounterMain == 0)
			m_functionIdCounterMain = MAX_USER_FUNC_ID + 1;
	}

	void CThreadedClass::callMainSingle(size_t _id, const std::function<void()>& _f)
	{
		if( getDirectExecution( ThreadType::Main ) )
		{
			_f();
			return;
		}
		m_toMainQueue.pushSingle(stdEx::function<void()>(_id, _f));
	}

	void CThreadedClass::callMainSingleLast(size_t _id, const std::function<void()>& _f)
	{
		if( getDirectExecution( ThreadType::Main ) )
		{
			_f();
			return;
		}
		m_toMainQueue.pushSingleLast(stdEx::function<void()>(_id, _f));
	}

	bool CThreadedClass::isInMain() const
	{
		return std::this_thread::get_id() == m_mainId;
	}

	bool CThreadedClass::isInWorker() const
	{
		return m_threaded ? std::this_thread::get_id() == m_workerId : std::this_thread::get_id() == m_mainId;
	}

	void CThreadedClass::_execute()
	{
		// preparing thread
		setThreadName( m_workerName.c_str() );
		m_workerId = std::this_thread::get_id();
		m_threadState = ThreadState::Running;
		if( m_onThreadStartup )
			m_onThreadStartup();

		applyThreadPriority();

		// thread main loop
		execute();

		m_threadState = ThreadState::Stopped;

	}

	void CThreadedClass::processQueueInMainThread( FunctionQueue& _queue, bool& _progressFlag ) {
		bool isWorkerQueue = &_queue == &m_toWorkerQueue;

		std::unique_ptr<FunctionQueue> queue;

		if( !_queue.empty() )
		{
			// no try/catch and rethrow in main required here, since we are in main anyway

			while( ( !m_pause || !isWorkerQueue ) && !_queue.empty() )
			{
				_progressFlag = true;
				Action fn;
				{
					std::lock_guard<std::recursive_mutex> l(_queue);
					// we have to check a second time for queue not empty - first check
					// was not locked, and queue may be emptied in the meantime
					// BUT the function mustn't be executed yet, because it may also lock -> deadlocks
					if( !_queue.empty() )
						fn = _queue.pop();
				}
				if( fn ) {
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

	void CThreadedClass::checkInMain() const
	{
		if( m_threaded )
		{
			base_assert( std::this_thread::get_id() == m_mainId && "Call from wrong thread" );
		}
	}

	void CThreadedClass::checkInWorker() const
	{
		if( m_threaded )
		{
			base_assert( std::this_thread::get_id() == m_workerId && "Call from wrong thread" );
		}
	}

	void CThreadedClass::checkNotInMain() const {
		if( m_threaded )
		{
			base_assert( std::this_thread::get_id() != m_mainId && "Call from wrong thread" );
		}
	}

	void CThreadedClass::checkNotInWorker() const {
		if( m_threaded )
		{
			base_assert( std::this_thread::get_id() != m_workerId && "Call from wrong thread" );
		}
	}

	bool CThreadedClass::getDirectExecution( ThreadType _threadType ) const {
		if( getPause() )
			return false;

		if( _threadType == ThreadType::Main && isInMain() )
			return true;
		if( _threadType == ThreadType::Worker && m_threaded && getRunning() && isInWorker() )
			return true;

		return !m_threaded && m_nonThrdDirectExec;
	}

	void CThreadedClass::applyThreadPriority() {
#ifdef _WIN32
		if( getRunning() ) {
			HANDLE handle = static_cast<HANDLE>(m_thread.native_handle());
			switch( m_priority ) {
				case ThreadPriority::Idle:			SetThreadPriority( handle, THREAD_PRIORITY_IDLE ); break;
				case ThreadPriority::Lowest:		SetThreadPriority( handle, THREAD_PRIORITY_LOWEST ); break;
				case ThreadPriority::BelowNormal:	SetThreadPriority( handle, THREAD_PRIORITY_BELOW_NORMAL ); break;
				case ThreadPriority::Normal:		SetThreadPriority( handle, THREAD_PRIORITY_NORMAL ); break;
				case ThreadPriority::AboveNormal:	SetThreadPriority( handle, THREAD_PRIORITY_ABOVE_NORMAL ); break;
				case ThreadPriority::Highest:		SetThreadPriority( handle, THREAD_PRIORITY_HIGHEST ); break;
				case ThreadPriority::TimeCritical:	SetThreadPriority( handle, THREAD_PRIORITY_TIME_CRITICAL ); break;
			}
		}
#endif
	}

}