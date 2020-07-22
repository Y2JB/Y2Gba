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
        //RichTextBox console = new RichTextBox();
        //RichTextBox codeWnd = new RichTextBox();
        //TextBox commandInput = new TextBox();
        //TextBox emuSnapshot = new TextBox();
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

            this.ClientSize = new System.Drawing.Size(1600, 1000);
            this.Text = "Y2Gba Debug Console";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            //this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // This is the only way i found to stop the annoying bong sound effect when pressing enter on a text box!
            this.Controls.Add(okButton);
            okButton.Visible = false;
            this.AcceptButton = okButton;

            codeWnd.Font = new Font(FontFamily.GenericMonospace, console.Font.Size);
            console.Font = new Font(FontFamily.GenericMonospace, console.Font.Size);
            emuSnapshot.Enabled = false;
            emuSnapshot.Font = new Font(FontFamily.GenericMonospace, console.Font.Size);

            commandInput.KeyUp += CommandInput_KeyUp;
            commandInput.Focus();
            
            RefreshEmuSnapshot();
        }
        

        public void RefreshEmuSnapshot()
        {
            emuSnapshot.Text = String.Format("CPU State");

            // Emu State
            emuSnapshot.AppendText(Environment.NewLine);
            emuSnapshot.AppendText((dbgConsole.EmulatorMode == GbaDebugConsole.Mode.BreakPoint) ? "BREAK" : "RUNNING");

            // Cpu State
            emuSnapshot.AppendText(Environment.NewLine);
            emuSnapshot.AppendText((Gba.Cpu.State == Cpu.CpuState.Arm) ? "ARM" : "THUMB");

            // Registers
            emuSnapshot.AppendText(Environment.NewLine);
            emuSnapshot.AppendText(Gba.Cpu.ToString());


            // LCD 
            string lcdState = String.Format("LCD: {0} ({1} / {2})", Gba.LcdController.Mode.ToString(),  Gba.LcdController.LcdCycles, Gba.LcdController.TotalTicksForState());
            emuSnapshot.AppendText(Environment.NewLine);
            emuSnapshot.AppendText(lcdState);
            emuSnapshot.AppendText(Environment.NewLine);
            emuSnapshot.AppendText(String.Format("Scanline: {0}  (0x{1:X})", Gba.LcdController.CurrentScanline, Gba.LcdController.CurrentScanline));
            emuSnapshot.AppendText(Environment.NewLine);
            emuSnapshot.AppendText(String.Format("VCount: {0}  (0x{1:X})", Gba.LcdController.DispStatRegister.VCountSetting, Gba.LcdController.DispStatRegister.VCountSetting));

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

                        RefreshEmuSnapshot();
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

        private void runToHBlankButton_Click(object sender, EventArgs e)
        {
            dbgConsole.RunToHBlankCommand();
        }

        private void runToVBlankButton_Click(object sender, EventArgs e)
        {
            dbgConsole.RunToVBlankCommand();
        }

        private void run1FrameButton_Click(object sender, EventArgs e)
        {
            dbgConsole.Run1FrameCommand();
        }

        private void continueOrBreakButton_Click(object sender, EventArgs e)
        {
            if(dbgConsole.EmulatorMode == GbaDebugConsole.Mode.Running)
            {
                dbgConsole.BreakCommand();
                continueOrBreakButton.Text = "Continue";
            }
            else            
            {
                dbgConsole.ContinueCommand();
                continueOrBreakButton.Text = "Break";
            }
        }

        private void stepButton_Click(object sender, EventArgs e)
        {
            dbgConsole.BreakpointStepAvailable = true;
        }

        private void stepOverButton_Click(object sender, EventArgs e)
        {
            dbgConsole.NextCommand();
        }
    }
}
