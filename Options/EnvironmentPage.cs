// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
using Microsoft.VisualStudio.Shell;

namespace SIL.FwNantVSPackage.Options
{
	public class EnvironmentPage: DialogPage
	{
		private EnvironmentControl m_EnvironmentControl = new EnvironmentControl();

		protected override void Dispose(bool disposing)
		{
			if (disposing && m_EnvironmentControl != null)
				m_EnvironmentControl.Dispose();
			m_EnvironmentControl = null;

			base.Dispose(disposing);
		}

		protected override System.Windows.Forms.IWin32Window Window
		{
			get { return m_EnvironmentControl; }
		}

		protected override void OnApply(PageApplyEventArgs e)
		{
			if (e.ApplyBehavior == ApplyKind.Apply)
				m_EnvironmentControl.SaveValues();
			base.OnApply(e);
		}
	}
}
