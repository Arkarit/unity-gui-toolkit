// NOTE: structurally identical to DoxygenThreadSafeOutput. When a third consumer of this
// pattern appears, lift a shared ProcessThreadSafeOutput into Runtime/Code/Helpers/ and
// have both bridges depend on it.

using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Thread-safe output buffer for shuttling stdout/stderr lines from a worker thread
	/// running an external process (ImageMagick) to the Unity main thread (Editor GUI).
	/// </summary>
	public class ImageMagickThreadSafeOutput
	{
		private readonly ReaderWriterLockSlim m_outputLock = new();
		private string m_currentOutput = string.Empty;
		private List<string> m_fullLog = new();
		private bool m_isFinished;
		private bool m_isStarted;

		public string ReadLine()
		{
			m_outputLock.EnterReadLock();
			try
			{
				return m_currentOutput;
			}
			finally
			{
				m_outputLock.ExitReadLock();
			}
		}

		public void SetStarted()
		{
			m_outputLock.EnterWriteLock();
			try
			{
				m_isStarted = true;
			}
			finally
			{
				m_outputLock.ExitWriteLock();
			}
		}

		public void SetFinished()
		{
			m_outputLock.EnterWriteLock();
			try
			{
				m_isFinished = true;
			}
			finally
			{
				m_outputLock.ExitWriteLock();
			}
		}

		public bool IsStarted()
		{
			m_outputLock.EnterReadLock();
			try
			{
				return m_isStarted;
			}
			finally
			{
				m_outputLock.ExitReadLock();
			}
		}

		public bool IsFinished()
		{
			m_outputLock.EnterReadLock();
			try
			{
				return m_isFinished;
			}
			finally
			{
				m_outputLock.ExitReadLock();
			}
		}

		public string ReadFullLog()
		{
			m_outputLock.EnterReadLock();
			try
			{
				var sb = new StringBuilder();
				foreach (var line in m_fullLog)
					sb.AppendLine(line);
				return sb.ToString();
			}
			finally
			{
				m_outputLock.ExitReadLock();
			}
		}

		public void WriteFullLog(List<string> newLog)
		{
			m_outputLock.EnterWriteLock();
			try
			{
				m_fullLog = newLog;
			}
			finally
			{
				m_outputLock.ExitWriteLock();
			}
		}

		public void WriteLine(string newOutput)
		{
			m_outputLock.EnterWriteLock();
			try
			{
				m_currentOutput = newOutput;
			}
			finally
			{
				m_outputLock.ExitWriteLock();
			}
		}
	}
}
