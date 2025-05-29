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


using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace GuiToolkit.Editor
{
	/// <summary>
	///  This class encapsulates the data output by Doxygen so it can be shared with Unity in a thread share way.	 
	/// </summary>
	public class DoxygenThreadSafeOutput
	{
		private ReaderWriterLockSlim outputLock = new ReaderWriterLockSlim();
		private string CurrentOutput = "";
		private List<string> FullLog = new List<string>();
		private bool Finished = false;
		private bool Started = false;

		public string ReadLine()
		{
			outputLock.EnterReadLock();
			try
			{
				return CurrentOutput;
			}
			finally
			{
				outputLock.ExitReadLock();
			}
		}

		public void SetFinished()
		{
			outputLock.EnterWriteLock();
			try
			{
				Finished = true;
			}
			finally
			{
				outputLock.ExitWriteLock();
			}
		}

		public void SetStarted()
		{
			outputLock.EnterWriteLock();
			try
			{
				Started = true;
			}
			finally
			{
				outputLock.ExitWriteLock();
			}
		}

		public bool isStarted()
		{
			outputLock.EnterReadLock();
			try
			{
				return Started;
			}
			finally
			{
				outputLock.ExitReadLock();
			}
		}

		public bool isFinished()
		{
			outputLock.EnterReadLock();
			try
			{
				return Finished;
			}
			finally
			{
				outputLock.ExitReadLock();
			}
		}

		public List<string> ReadFullLog()
		{
			outputLock.EnterReadLock();
			try
			{
				return FullLog;
			}
			finally
			{
				outputLock.ExitReadLock();
			}
		}

		public void WriteFullLog(List<string> newLog)
		{
			outputLock.EnterWriteLock();
			try
			{
				FullLog = newLog;
			}
			finally
			{
				outputLock.ExitWriteLock();
			}
		}

		public void WriteLine(string newOutput)
		{
			outputLock.EnterWriteLock();
			try
			{
				CurrentOutput = newOutput;
			}
			finally
			{
				outputLock.ExitWriteLock();
			}
		}
	}

}
