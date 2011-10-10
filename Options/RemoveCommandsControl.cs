// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
using System;
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
