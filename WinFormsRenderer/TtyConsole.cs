using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Gba.Core;


namespace WinFormsRenderer
{
    public partial class TtyConsole : Form
    {
        GameboyAdvance gba;

        public TtyConsole(GameboyAdvance gba)
        {
            this.gba = gba;
            InitializeComponent();

            ttyTextBox.Font = new Font(FontFamily.GenericMonospace, ttyTextBox.Font.Size);

            gba.OnLogMessage = null;

            logLevelCb.SelectedIndex = 0;
        }

        private void TtyConsole_Load(object sender, EventArgs e)
        {

        }

        private void OnLogMessage(string msg)
        {
            ttyTextBox.AppendText(msg);
            ttyTextBox.AppendText(Environment.NewLine);
            ttyTextBox.ScrollToCaret();
        }

        private void enableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if(enableCheckBox.Checked)
            {
                gba.OnLogMessage = OnLogMessage;
            }
            else
            {
                gba.OnLogMessage = null;
            }
        }

        private void TtyConsole_VisibleChanged(object sender, EventArgs e)
        {
            if(Visible == false)
            {
                gba.OnLogMessage = null;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            ttyTextBox.Clear();
        }
    }
}
