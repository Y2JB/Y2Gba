using System.Drawing;
using System.Windows.Forms;

namespace WinFormRender
{
    partial class ConsoleWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.BottomPanel = new System.Windows.Forms.Panel();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.commandInput = new System.Windows.Forms.TextBox();
            this.leftPanel = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.console = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.codeWnd = new System.Windows.Forms.RichTextBox();
            this.rightPanel = new System.Windows.Forms.Panel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.emuSnapshot = new System.Windows.Forms.RichTextBox();
            this.BottomPanel.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // BottomPanel
            // 
            this.BottomPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BottomPanel.Controls.Add(this.button4);
            this.BottomPanel.Controls.Add(this.button3);
            this.BottomPanel.Controls.Add(this.button2);
            this.BottomPanel.Controls.Add(this.button1);
            this.BottomPanel.Controls.Add(this.commandInput);
            this.BottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.BottomPanel.Location = new System.Drawing.Point(0, 796);
            this.BottomPanel.Name = "BottomPanel";
            this.BottomPanel.Size = new System.Drawing.Size(1339, 96);
            this.BottomPanel.TabIndex = 0;
            // 
            // button4
            // 
            this.button4.Dock = System.Windows.Forms.DockStyle.Right;
            this.button4.Location = new System.Drawing.Point(1189, 0);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(150, 57);
            this.button4.TabIndex = 1;
            this.button4.Text = "Continue";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(378, 4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(187, 46);
            this.button3.TabIndex = 2;
            this.button3.Text = "Run to VBlank";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(172, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(187, 46);
            this.button2.TabIndex = 2;
            this.button2.Text = "Run to HBlank";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(150, 46);
            this.button1.TabIndex = 1;
            this.button1.Text = "Step";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // commandInput
            // 
            this.commandInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.commandInput.Location = new System.Drawing.Point(0, 57);
            this.commandInput.Name = "commandInput";
            this.commandInput.Size = new System.Drawing.Size(1339, 39);
            this.commandInput.TabIndex = 0;
            // 
            // leftPanel
            // 
            this.leftPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.leftPanel.Controls.Add(this.groupBox2);
            this.leftPanel.Controls.Add(this.groupBox1);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftPanel.Location = new System.Drawing.Point(0, 0);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Size = new System.Drawing.Size(829, 796);
            this.leftPanel.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.console);
            this.groupBox2.Location = new System.Drawing.Point(0, 225);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(828, 570);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Console";
            // 
            // console
            // 
            this.console.Dock = System.Windows.Forms.DockStyle.Fill;
            this.console.Location = new System.Drawing.Point(3, 35);
            this.console.Name = "console";
            this.console.ReadOnly = true;
            this.console.Size = new System.Drawing.Size(822, 532);
            this.console.TabIndex = 0;
            this.console.Text = "";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.codeWnd);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(829, 222);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "ARM";
            // 
            // codeWnd
            // 
            this.codeWnd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.codeWnd.Location = new System.Drawing.Point(3, 35);
            this.codeWnd.Name = "codeWnd";
            this.codeWnd.ReadOnly = true;
            this.codeWnd.Size = new System.Drawing.Size(823, 184);
            this.codeWnd.TabIndex = 1;
            this.codeWnd.Text = "";
            // 
            // rightPanel
            // 
            this.rightPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rightPanel.AutoSize = true;
            this.rightPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.rightPanel.Location = new System.Drawing.Point(1338, 3);
            this.rightPanel.Name = "rightPanel";
            this.rightPanel.Size = new System.Drawing.Size(0, 0);
            this.rightPanel.TabIndex = 2;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.emuSnapshot);
            this.groupBox3.Location = new System.Drawing.Point(831, 0);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(507, 799);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "System";
            // 
            // emuSnapshot
            // 
            this.emuSnapshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.emuSnapshot.Location = new System.Drawing.Point(3, 35);
            this.emuSnapshot.Name = "emuSnapshot";
            this.emuSnapshot.ReadOnly = true;
            this.emuSnapshot.Size = new System.Drawing.Size(501, 761);
            this.emuSnapshot.TabIndex = 0;
            this.emuSnapshot.Text = "";
            // 
            // ConsoleWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1339, 892);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.rightPanel);
            this.Controls.Add(this.leftPanel);
            this.Controls.Add(this.BottomPanel);
            this.Name = "ConsoleWindow";
            this.BottomPanel.ResumeLayout(false);
            this.BottomPanel.PerformLayout();
            this.leftPanel.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Panel BottomPanel;
        private Panel leftPanel;
        private Panel rightPanel;
        private Button button1;
        private TextBox commandInput;
        private GroupBox groupBox2;
        private RichTextBox console;
        private GroupBox groupBox1;
        private RichTextBox codeWnd;
        private GroupBox groupBox3;
        private RichTextBox emuSnapshot;
        private Button button4;
        private Button button3;
        private Button button2;
    }
}