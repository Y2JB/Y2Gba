using GbaDebugger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Forms;

using Gba.Core;

namespace WinFormRender
{
    public partial class ConsoleWindow : Form
    {
        RichTextBox console = new RichTextBox();
        RichTextBox codeWnd = new RichTextBox();
        TextBox commandInput = new TextBox();
        TextBox dmgSnapshot = new TextBox();
        Button okButton = new Button();

        GameboyAdvance Gba;
        GbaDebugConsole dbgConsole;

        List<string> commandHistory = new List<string>();
        int historyIndex = -1;

        int lastestConsoleLine;

        public ConsoleWindow(GameboyAdvance gba, GbaDebugConsole dbgConsole)
        {
            this.Gba = gba;
            this.dbgConsole = dbgConsole;

            InitializeComponent();

            this.ClientSize = new System.Drawing.Size(1200, 775);
            this.Text = "GBA Console";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // This is the only way i found to stop the annoying bong sound effect when pressing enter on a text box!
            this.Controls.Add(okButton);
            okButton.Visible = false;
            this.AcceptButton = okButton;

            codeWnd.Location = new System.Drawing.Point(10, 10);
            codeWnd.Multiline = true;
            codeWnd.ReadOnly = true;
            codeWnd.Width = 800;
            codeWnd.Height = 350;
            codeWnd.Enabled = true;
            codeWnd.Font = new Font(FontFamily.GenericMonospace, console.Font.Size);
            this.Controls.Add(codeWnd);

            console.Location = new System.Drawing.Point(10, 370);
            console.Multiline = true;
            console.ReadOnly = true;
            console.Width = 800;
            console.Height = 350;
            console.Enabled = true;
            console.Font = new Font(FontFamily.GenericMonospace, console.Font.Size);
            this.Controls.Add(console);

            commandInput.Location = new System.Drawing.Point(10, console.Location.Y + console.Height + 10);
            commandInput.Width = ClientSize.Width - 20;
            commandInput.KeyUp += CommandInput_KeyUp;
            this.Controls.Add(commandInput);
            commandInput.Focus();

            dmgSnapshot.Location = new System.Drawing.Point(console.Location.X + console.Width + 10, 10);
            dmgSnapshot.Multiline = true;
            dmgSnapshot.Width = 350;
            dmgSnapshot.Height = 720;
            dmgSnapshot.Enabled = false;
            dmgSnapshot.Font = new Font(FontFamily.GenericMonospace, console.Font.Size);
            this.Controls.Add(dmgSnapshot);

            RefreshDmgSnapshot();
        }
        

        public void RefreshDmgSnapshot()
        {
            dmgSnapshot.Text = String.Format("CPU State");

            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText((dbgConsole.EmulatorMode == GbaDebugConsole.Mode.BreakPoint) ? "BREAK" : "RUNNING");

            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText((Gba.Cpu.State == Cpu.CpuState.Arm) ? "ARM" : "THUMB");

            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(Gba.Cpu.ToString());

           
            RefreshConsoleText();
        }


        public void RefreshConsoleText()
        {
            for(; lastestConsoleLine < dbgConsole.ConsoleText.Count; lastestConsoleLine++)
            {
                console.AppendText(dbgConsole.ConsoleText[lastestConsoleLine]);
                console.AppendText(Environment.NewLine);
            }
            
            console.ScrollToCaret();

            codeWnd.Text = String.Empty;
            foreach(var str in dbgConsole.ConsoleCodeText)
            {
                codeWnd.AppendText(str);
                codeWnd.AppendText(Environment.NewLine);
            }
        }


        private void CommandInput_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (commandInput.Text != String.Empty)
                    {
                        //ConsoleAddString(commandInput.Text);

                        commandHistory.Add(commandInput.Text);

                        dbgConsole.RunCommand(commandInput.Text);

                        if (commandInput.Text.Equals("x") ||
                            commandInput.Text.Equals("exit"))
                        {
                            Application.Exit();
                        }

                        commandInput.Text = String.Empty;
                        historyIndex = -1;

                        RefreshDmgSnapshot();
                        RefreshConsoleText();
                    }
                    break;

                case Keys.Up:                
                    if (historyIndex < commandHistory.Count - 1) historyIndex++;
                    commandInput.Text = commandHistory[commandHistory.Count - historyIndex - 1];
                    commandInput.Select(commandInput.Text.Length, 0);
                    break;

                case Keys.Down:           
                    if (historyIndex > -1) historyIndex--;

                    if (historyIndex >= 0)
                    {
                        commandInput.Text = commandHistory[commandHistory.Count - historyIndex - 1];
                        commandInput.Select(commandInput.Text.Length, 0);
                    }
                    else commandInput.Text = String.Empty;
                    break;
            
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


    }
}
