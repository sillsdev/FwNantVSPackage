// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace SIL.FwNantVSPackage
{
	/// <summary>
	/// Start NAnt
	/// </summary>
	internal class NantRunner: IDisposable
	{
		private readonly string m_ProgramFileName;
		private readonly string m_CommandLine;
		private readonly string m_WorkingDirectory;
		private readonly AddinLogListener m_Log;
		private readonly Dictionary<string, StreamReader> m_ThreadStream = new Dictionary<string, StreamReader>();
		private Thread m_NantThread;
		private bool m_fThreadRunning;
		private Process m_Process;

		internal delegate void BuildStatusHandler(bool fFinished);
		internal event BuildStatusHandler BuildStatusChange;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NantRunner"/> class.
		/// </summary>
		/// <param name="fileName">The filename and path of NAnt</param>
		/// <param name="commandLine">Command line</param>
		/// <param name="workingDirectory">Working directory</param>
		/// <param name="log">Log listener</param>
		/// <param name="handler">Build status handler</param>
		/// -----------------------------------------------------------------------------------
		internal NantRunner(string fileName, string commandLine, string workingDirectory,
			AddinLogListener log, BuildStatusHandler handler)
		{
			m_ProgramFileName = fileName;
			m_CommandLine = commandLine;
			m_WorkingDirectory = workingDirectory;
			m_Log = log;
			BuildStatusChange += handler;
		}

		#region IDisposable Members & Co.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting 
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue 
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FwNantVSPackage.NantRunner"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~NantRunner()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disposes the specified disposing.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources here
				if (m_Process != null)
					m_Process.Dispose();
			}
			m_Process = null;
		}
		#endregion

		/// <summary>
		/// Sets the StartInfo Options and returns a new Process that can be run.
		/// </summary>
		/// <returns>new Process with information about programs to run, etc.</returns>
		protected virtual void PrepareProcess(ref Process process)
		{
			// create process (redirect standard output to temp buffer)
			process.StartInfo.FileName = m_ProgramFileName;
			process.StartInfo.Arguments = m_CommandLine;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			//required to allow redirects
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = m_WorkingDirectory;
		}

		//Starts the process and handles errors.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		protected virtual Process StartProcess()
		{
			var p = new Process();
			PrepareProcess(ref p);
			try
			{
				string msg = string.Format(
					CultureInfo.InvariantCulture,
					"Starting '{1} ({2})' in '{0}'",
					p.StartInfo.WorkingDirectory,
					p.StartInfo.FileName,
					p.StartInfo.Arguments);

				m_Log.WriteLine(msg);

				p.Start();
			}
			catch (Exception)
			{
				string msg = string.Format("{0} failed to start.", p.StartInfo.FileName);
				m_Log.WriteLine(msg);
				throw;
			}
			return p;
		}

		public void Run()
		{
			try
			{
				m_NantThread = new Thread(StartNant) { Name = "NAnt" };

				m_NantThread.Start();
				m_fThreadRunning = true;
			}
			catch (Exception e)
			{
				string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
				m_Log.WriteLine(msg);
				throw new Exception(msg, e);
			}
		}

		public int RunSync()
		{
			StartNant();
			return m_Process.ExitCode;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Wait for NAnt to exit.
		/// </summary>
		/// <returns>NAnt exit code, i.e. <c>0</c> if no build errors. Returns <c>-1</c>
		/// if NAnt not run.</returns>
		/// ------------------------------------------------------------------------------------
		public int WaitExit()
		{
			// Wait for NAnt to finish
			if (m_NantThread != null)
			{
				m_Process.WaitForExit();
				m_NantThread.Join();
				if (m_Process != null)
					return m_Process.ExitCode;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fFinished"></param>
		/// ------------------------------------------------------------------------------------
		protected void OnBuildStatusChange(bool fFinished)
		{
			if (BuildStatusChange != null)
				BuildStatusChange(fFinished);

			m_fThreadRunning = !fFinished;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartNant()
		{
			Thread outputThread = null;
			Thread errorThread = null;
			try
			{
				OnBuildStatusChange(false);

				// Start the external process
				m_Process = StartProcess();
				outputThread = new Thread(StreamReaderThreadOutput) { Name = "Output" };
				errorThread = new Thread(StreamReaderThreadError) { Name = "Error" };
				m_ThreadStream[outputThread.Name] = m_Process.StandardOutput;
				m_ThreadStream[errorThread.Name] = m_Process.StandardError;

				outputThread.Start();
				errorThread.Start();

				// Wait for the process to terminate
				m_Process.WaitForExit();
				// Wait for the threads to terminate
				outputThread.Join();
				errorThread.Join();

				if (m_Process.ExitCode != 0)
				{
					string msg = string.Format(
						"External Program Failed: {0} (return code was {1})",
						m_ProgramFileName, m_Process.ExitCode);
					m_Log.WriteLine(msg);
				}
			}
			catch (ThreadAbortException)
			{
				try
				{
					if (outputThread != null)
						outputThread.Abort();
					if (errorThread != null)
						errorThread.Abort();
					m_Log.WriteLine("Build canceled");
				}
				catch (Exception e)
				{
					string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
					m_Log.WriteLine(msg);
				}
			}
			catch (Exception e)
			{
				string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
				m_Log.WriteLine(msg);
			}
			finally
			{
				m_ThreadStream.Clear();
				m_Log.WriteLine("---------------------- Done ----------------------");

				OnBuildStatusChange(true);
			}
		}

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void StreamReaderThreadOutput()
		{
			var reader = m_ThreadStream[Thread.CurrentThread.Name];
			while (true)
			{
				string strLogContents = reader.ReadLine();
				if (strLogContents == null)
					break;
				// Ensure only one thread writes to the log at any time
				lock (m_ThreadStream)
				{
					m_Log.WriteLine(strLogContents);
				}
			}
		}

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void StreamReaderThreadError()
		{
			var reader = m_ThreadStream[Thread.CurrentThread.Name];
			while (true)
			{
				string strLogContents = reader.ReadLine();
				if (strLogContents == null)
					break;
				// Ensure only one thread writes to the log at any time
				lock (m_ThreadStream)
				{
					m_Log.WriteLine(strLogContents);
				}
			}
		}

		internal void Abort()
		{
			try
			{
				if (m_fThreadRunning)
				{
					m_Process.Kill();
					m_NantThread.Abort();
				}
			}
			catch (Exception e)
			{
				string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
				m_Log.WriteLine(msg);
			}
		}

		internal bool IsRunning
		{
			get { return m_fThreadRunning; }
		}
	}
}
