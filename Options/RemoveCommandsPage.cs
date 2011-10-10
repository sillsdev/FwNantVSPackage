using Microsoft.VisualStudio.Shell;

namespace SIL.FwNantVSPackage.Options
{
	public class RemoveCommandsPage: DialogPage
	{
		private RemoveCommandsControl m_RemoveCommandsControl = new RemoveCommandsControl();

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
