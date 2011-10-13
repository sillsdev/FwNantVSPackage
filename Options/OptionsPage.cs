// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
using System.Drawing;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SIL.FwNantVSPackage.Options
{
	public class OptionsPage: DialogPage
	{
		private AddinOptions m_AddinOptions = new AddinOptions { Location = new Point(0, 0) };

		protected override void Dispose(bool disposing)
		{
			if (disposing && m_AddinOptions != null)
				m_AddinOptions.Dispose();
			m_AddinOptions = null;
			base.Dispose(disposing);
		}

		protected override System.Windows.Forms.IWin32Window Window
		{
			get { return m_AddinOptions; }
		}

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			m_AddinOptions.OnApply();
		}
	}
}
