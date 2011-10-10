using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FwNantVSPackage.Options
{
	public partial class RemoveCommandsControl : UserControl
	{
		public RemoveCommandsControl()
		{
			InitializeComponent();

			InitializeListbox();
		}

		internal void InitializeListbox()
		{
			listBox1.Items.Clear();
			foreach (var command in Settings.Default.BuildCommands)
				listBox1.Items.Add(command);
		}

		private void OnRemoveCommands(object sender, EventArgs e)
		{
			for (int i = listBox1.SelectedIndices.Count - 1; i >= 0; i--)
			{
				Settings.Default.BuildCommands.Remove((string)listBox1.SelectedItems[i]);
				listBox1.Items.RemoveAt(listBox1.SelectedIndices[i]);
			}
		}
	}
}
