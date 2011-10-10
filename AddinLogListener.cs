// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
using System;
using System.Text.RegularExpressions;

namespace SIL.FwNantVSPackage
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Log messages during a <c>NAnt</c> build
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class AddinLogListener: IDisposable
	{
		private PaneWriter m_BuildOutputWindow;
		private readonly NAntBuild m_Parent;

		/// <summary>
		/// Use this <see cref="PaneWriter"/> for all output messages.
		/// </summary>
		public PaneWriter OutputBuild
		{
			get
			{
				if (m_BuildOutputWindow == null && m_Parent != null)
				{
					// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
					m_BuildOutputWindow = new PaneWriter(m_Parent.DTE, m_Parent.BuildPaneName);
					// ReSharper restore ConvertIfStatementToConditionalTernaryExpression
				}
				return m_BuildOutputWindow;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new Addin Log Listener.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// ------------------------------------------------------------------------------------
		internal AddinLogListener(NAntBuild parent)
		{
			m_Parent = parent;
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
		/// <see cref="T:SIL.FwNantVSPackage.AddinLogListener"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~AddinLogListener()
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
				if (m_BuildOutputWindow != null)
					m_BuildOutputWindow.Dispose();
			}
			m_BuildOutputWindow = null;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes a string to the output. If it's an error or warning message, a task is added
		/// to the task list
		/// </summary>
		/// <param name="message">The message to output</param>
		/// <param name="fNewLine"><c>true</c> to write a new line after the message</param>
		/// ------------------------------------------------------------------------------------
		private void OutputString(string message, bool fNewLine)
		{
			lock (this)
			{
				if (OutputBuild == null)
					return;

				bool fError = false;
				if (message.IndexOf("error") >= 0 || message.IndexOf("warning") >= 0)
				{
					// C:\Documents and Settings\Eberhard\TrashBin\Simple\Simple.cs(4,4): error CS1002: ; expected
					var regex = new Regex("\\s*(?<filename>[^(]+)\\((?<line>\\d+),(?<column>\\d+)\\):[^:]+: (?<description>.*)");
					Match match = regex.Match(message);
					if (match.Value.Length > 0)
					{
						fError = true;
						string filename = match.Groups["filename"].Value;
						string line = match.Groups["line"].Value;
						string descr = match.Groups["description"].Value;
						OutputBuild.OutputTaskItemString(message,
							EnvDTE.vsTaskPriority.vsTaskPriorityHigh,
							EnvDTE.vsTaskCategories.vsTaskCategoryBuildCompile,
							EnvDTE.vsTaskIcon.vsTaskIconCompile, filename, Convert.ToInt32(line) - 1,
							descr, true);
					}
				}

				if (!fError)
					OutputBuild.Write(message);

				if (fNewLine)
					OutputBuild.WriteLine();
			}
		}

		/// <summary>
		/// Occurs when a Message is written to the Log.
		/// </summary>
		/// <param name="message">The Message that was written</param>
		public void Write(string message)
		{
			OutputString(message, false);
		}

		/// <summary>
		/// Occurs when a Line is written to the Log.
		/// </summary>
		/// <param name="line">The Line that was written.</param>
		public void WriteLine(string line)
		{
			OutputString(line, true);
		}
	}
}
