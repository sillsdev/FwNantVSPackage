// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
using Microsoft.VisualStudio.Shell;

namespace SIL.FwNantVSPackage.Options
{
	public class RemoveCommandsPage: DialogPage
	{
		private RemoveCommandsControl m_RemoveCommandsControl = new RemoveCommandsControl();

		protected override void Dispose(bool disposing)
		{
			if (disposing && m_RemoveCommandsControl != null)
				m_RemoveCommandsControl.Dispose();
			m_RemoveCommandsControl = null;

			base.Dispose(disposing);
		}

		protected override System.Windows.Forms.IWin32Window Window
		{
			get { return m_RemoveCommandsControl; }
		}

		protected override void OnActivate(System.ComponentModel.CancelEventArgs e)
		{
			m_RemoveCommandsControl.InitializeListbox();
			base.OnActivate(e);
		}
	}
}
