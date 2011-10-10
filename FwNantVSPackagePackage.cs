using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Collections.Specialized;

namespace SIL.FwNantVSPackage
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	///
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the 
	/// IVsPackage interface and uses the registration attributes defined in the framework to 
	/// register itself and its components with the shell.
	/// </summary>
	// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
	// a package.
	[PackageRegistration(UseManagedResourcesOnly = true)]
	// This attribute is used to register the informations needed to show the this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	// This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideOptionPage(typeof(Options.OptionsPage), "FwNAnt", "General", 101, 106, true)]
	[ProvideOptionPage(typeof(Options.RemoveCommandsPage), "FwNAnt", "Remove Comands", 102, 107, true)]
	[Guid(GuidList.guidFwNantVSPackagePkgString)]
	public sealed class FwNantVSPackagePackage : Package
	{
		private string m_ComboValue;
		private readonly NAntBuild m_NantBuild;
		private MenuCommand CancelBtn { get; set; }

		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require 
		/// any Visual Studio service because at this point the package object is created but 
		/// not sited yet inside Visual Studio environment. The place to do all the other 
		/// initialization is the Initialize method.
		/// </summary>
		public FwNantVSPackagePackage()
		{
			Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
			m_NantBuild = new NAntBuild(this);
			m_NantBuild.BuildStatusChange += OnBuildStatusChange;
		}



		/////////////////////////////////////////////////////////////////////////////
		// Overriden Package Implementation
		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
			base.Initialize();

			// Add our command handlers for menu (commands must exist in the .vsct file)
			OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (null != mcs)
			{
				// Create the command for the menu item.
				CommandID menuCommandID = new CommandID(GuidList.guidFwNantVSPackageCmdSet, PkgCmdIDList.cmdidCancel);
				CancelBtn = new MenuCommand(OnBuildCancel, menuCommandID) { Enabled = false };
				mcs.AddCommand(CancelBtn);

				menuCommandID = new CommandID(GuidList.guidFwNantVSPackageCmdSet, PkgCmdIDList.cmdidTarget);
				MenuCommand menuItem = new OleMenuCommand(ComboHandler, menuCommandID);
				mcs.AddCommand(menuItem);

				menuCommandID = new CommandID(GuidList.guidFwNantVSPackageCmdSet, PkgCmdIDList.cmdidTargetGetList);
				menuItem = new OleMenuCommand(ComboHandlerGetList, menuCommandID);
				mcs.AddCommand(menuItem);

				menuCommandID = new CommandID(GuidList.guidFwNantVSPackageCmdSet, PkgCmdIDList.cmdidStartBuild);
				menuItem = new MenuCommand(OnStartBuild, menuCommandID);
				mcs.AddCommand(menuItem);
			}
		}

		protected override int QueryClose(out bool canClose)
		{
			Settings.Default.Save();
			return base.QueryClose(out canClose);
		}

		#endregion

		/// <summary>
		/// This function is the callback used to execute a command when the a menu item is clicked.
		/// See the Initialize method to see how the menu item is associated to this function using
		/// the OleMenuCommandService service and the MenuCommand class.
		/// </summary>
		private void OnBuildCancel(object sender, EventArgs e)
		{
			m_NantBuild.CancelBuild();
		}

		private void ComboHandler(object sender, EventArgs arguments)
		{
			var eventArgs = arguments as OleMenuCmdEventArgs;
			if (eventArgs == null) 
				return;

			var output = eventArgs.OutValue;
			var input = eventArgs.InValue;
			if (input != null)
			{
				var newCommand = input.ToString();
				m_ComboValue = newCommand;
				if (!Settings.Default.BuildCommands.Contains(newCommand))
					Settings.Default.BuildCommands.Add(newCommand);
				Settings.Default.Save();
			}
			else if (output != IntPtr.Zero)
			{
				Marshal.GetNativeVariantForObject(m_ComboValue,
					output);
			}
		}

		private void ComboHandlerGetList(object sender, EventArgs e)
		{
			var eventArgs = e as OleMenuCmdEventArgs;
			if (eventArgs == null)
				return;

			var vOut = eventArgs.OutValue;

			if (eventArgs.InValue != null || vOut == IntPtr.Zero)
				throw new ArgumentException();

			var commandCollection = Settings.Default.BuildCommands;
			if (commandCollection.Count == 0)
			{
				// no entries stored, so put in default list
				commandCollection.AddRange(new[] { "test all", "remakefw" });
			}

			var commandArray = new string[commandCollection.Count];
			commandCollection.CopyTo(commandArray, 0);
			Marshal.GetNativeVariantForObject(commandArray, vOut);
		}

		private void OnStartBuild(object sender, EventArgs arguments)
		{
			if (string.IsNullOrWhiteSpace(m_ComboValue) || m_NantBuild.IsRunning)
				return;

			m_NantBuild.RunNant(m_ComboValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Callback for build status
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnBuildStatusChange(bool fFinished)
		{
			CancelBtn.Enabled = !fFinished;
		}
	}
}
