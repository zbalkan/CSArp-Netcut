﻿using System.Windows.Forms;

namespace CSArp
{
    public interface IView
    {
        ListView ClientListView { get; }
        ToolStripStatusLabel ToolStripStatus { get; }
        ToolStripComboBox ToolStripComboBoxNetworkDeviceList { get; }
        Form MainForm { get; }
        NotifyIcon NotifyIcon1 { get; }
        ToolStripTextBox ToolStripTextBoxClientName { get; }
        ToolStripStatusLabel ToolStripStatusScan { get; }
        ToolStripProgressBar ToolStripProgressBarScan { get; }
        ToolStripMenuItem ShowLogToolStripMenuItem { get; }
        RichTextBox LogRichTextBox { get; }
        SaveFileDialog SaveFileDialogLog { get; }
    }
}
