using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SIL.FwNantVSPackage.Options
{
	public class OptionsPage: DialogPage
	{
		private AddinOptions m_AddinOptions = new AddinOptions { Location = new Point(0, 0) };
		protected override System.Windows.Forms.IWin32Window Window
		{
			get { return m_AddinOptions; }
		}

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			((IDTToolsOptionsPage)m_AddinOptions).OnOK();
		}
	}
}
