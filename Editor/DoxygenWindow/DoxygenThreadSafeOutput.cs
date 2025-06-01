/*
Original copyright notice by Jacob Pennock:

/// <summary>
/// <para> A Editor Plugin for automatic doc generation through Doxygen</para>
/// <para> Author: Jacob Pennock (http://Jacobpennock.com)</para>
/// <para> Version: 1.0</para>	 
/// </summary>

Permission is hereby granted, free of charge, to any person  obtaining a copy of this software and associated documentation  files (the "Software"), to deal in the Software without  restriction, including without limitation the rights to use,  copy, modify, merge, publish, distribute, sublicense, and/or sell  copies of the Software, and to permit persons to whom the  Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GuiToolkit.Editor
{
	/// <summary>
	///  This class encapsulates the data output by Doxygen so it can be shared with Unity in a thread share way.	 
	/// </summary>
	public class DoxygenThreadSafeOutput
	{
		private ReaderWriterLockSlim m_outputLock = new ();
		private string m_currentOutput = string.Empty;
		private List<string> m_fullLog = new ();
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

		public bool isStarted()
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

		public bool isFinished()
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
				StringBuilder sb = new();
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
