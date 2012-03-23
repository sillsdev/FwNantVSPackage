// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Eclipse Public License (EPL-1.0) or the
//		GNU Lesser General Public License (LGPLv3), as specified in the LICENSING.txt file.
// </copyright>
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
	public partial class EnvironmentControl : UserControl
	{
		public EnvironmentControl()
		{
			InitializeComponent();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			InitializeValues();
			base.OnVisibleChanged(e);
		}

		internal void InitializeValues()
		{
			dataGridView1.Rows.Clear();
			foreach (var variable in Settings.Default.EnvironmentVariables)
			{
				var parts = variable.Split('=');
				if (parts.Length < 2)
					continue;
				dataGridView1.Rows.Add(parts[0], parts[1]);
			}
		}

		internal void SaveValues()
		{
			Settings.Default.EnvironmentVariables.Clear();
			foreach (DataGridViewRow row in dataGridView1.Rows)
			{
				if (row.IsNewRow || string.IsNullOrEmpty(row.Cells[0].Value as string))
					continue;

				var variable = string.Format("{0}={1}", row.Cells[0].Value, row.Cells[1].Value);
				Settings.Default.EnvironmentVariables.Add(variable);
			}
			Settings.Default.Save();
		}
	}
}
