// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
using System;
using System.IO;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = EnvDTE.Constants;

namespace SIL.FwNantVSPackage
{
	public class NAntBuild: IDisposable
	{
		private IServiceProvider Parent { get; set; }
		private PaneWriter m_OutputBuild;
		private NantRunner m_nantRunner;
		private AddinLogListener m_LogListener;
		internal event NantRunner.BuildStatusHandler BuildStatusChange;

		private const string m_BuildPaneName = "NAnt build";

		public NAntBuild(IServiceProvider parent)
		{
			Parent = parent;
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
		/// <see cref="T:SIL.FwNantVSPackage.NAntBuild"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~NAntBuild()
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
				if (m_OutputBuild != null)
					m_OutputBuild.Dispose();
				if (m_LogListener != null)
					m_LogListener.Dispose();
				if (m_nantRunner != null)
					m_nantRunner.Dispose();
			}
			m_OutputBuild = null;
			m_LogListener = null;
			m_nantRunner = null;
		}

		#endregion

		private string NAnt
		{
			get
			{
				using (var addinOptions = new AddinOptions(Parent))
					return addinOptions.NAntExe;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries to find the buildfile based on the passed in path
		/// </summary>
		/// <param name="path">Path of the solution</param>
		/// ------------------------------------------------------------------------------------
		private string RetrieveBuildFile(string path)
		{
			// try to find the right base directory based on the project path
			using (var options = new AddinOptions(Parent))
			{
				foreach (var baseDir in options.BaseDirectories)
				{
					var dirToTest = baseDir + "\\";
					if (path.ToLower().StartsWith(dirToTest.ToLower()))
					{
						return Path.GetFullPath(Path.Combine(baseDir, options.Buildfile));
					}
				}

				// no success, so take first base directory that we have, or just build file
				return Path.GetFullPath(options.BaseDirectories.Length > 0 ? 
					Path.Combine(options.BaseDirectories[0], options.Buildfile) :
					options.Buildfile);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the build. This involves activating the output window, saving all files
		/// and printing a message.
		/// </summary>
		/// <param name="msg"></param>
		/// ------------------------------------------------------------------------------------
		private void StartBuild(string msg)
		{
			OutputBuild.Clear();
			DTE.Windows.Item(Constants.vsWindowKindOutput).Activate();
			DTE.ExecuteCommand("File.SaveAll", string.Empty);
			OutputBuild.Write(msg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the target framework.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string TargetFramework
		{
			get
			{
				string targetFramework;
				if (string.IsNullOrEmpty(Settings.Default.TargetFramework))
					targetFramework = string.Empty;
				else
					targetFramework = "-t:" + Settings.Default.TargetFramework;
				return targetFramework;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the build pane.
		/// </summary>
		/// <value>The name of the build pane.</value>
		/// ------------------------------------------------------------------------------------
		public string BuildPaneName
		{
			get { return m_BuildPaneName; }
		}
		
		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Use this PaneWriter for all output messages.
		/// </summary>
		/// <value>The output build.</value>
		/// ------------------------------------------------------------------------------------------
		public PaneWriter OutputBuild
		{
			get
			{
				if (m_OutputBuild == null)
				{
					m_OutputBuild = new PaneWriter(DTE, m_BuildPaneName);
				}
				return m_OutputBuild;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the DTE.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DTE2 DTE
		{
			get { return Parent.GetService(typeof(DTE)) as DTE2; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if a build is currently running
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public bool IsRunning
		{
			get { return m_nantRunner != null && m_nantRunner.IsRunning; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cancel the current build
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CancelBuild()
		{
			if (IsRunning)
				m_nantRunner.Abort();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs Nant.
		/// </summary>
		/// <param name="cmdLine">The CMD line.</param>
		/// ------------------------------------------------------------------------------------
		public void RunNant(string cmdLine)
		{
			if (string.IsNullOrWhiteSpace(cmdLine))
				return;

			try
			{
				var solution = Parent.GetService(typeof(SVsSolution)) as IVsSolution;
				object solutionPath;
				solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out solutionPath);
				var buildFile = RetrieveBuildFile(solutionPath as string);

				cmdLine = string.Format("-e+ -buildfile:\"{0}\" {1} {2}", buildFile, TargetFramework, cmdLine);
				var workingDir = Path.GetFullPath(Path.GetDirectoryName(buildFile));

				StartBuild(string.Format("------ Build started: {0} ------\n", cmdLine));
				OutputBuild.Activate();
				m_LogListener = new AddinLogListener(this);
				m_nantRunner = new NantRunner(NAnt, cmdLine, workingDir,
					m_LogListener,
					BuildStatusChange);
				m_nantRunner.Run();
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch (Exception e)
			{
				if (m_nantRunner != null && m_nantRunner.IsRunning)
					m_nantRunner.Abort();
				OutputBuild.Write("\nINTERNAL ERROR\n\t");
				OutputBuild.WriteLine(e.Message);
			}
		}
	}
}