namespace WinFormsRenderer
{
    partial class TtyConsole
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.ttyTextBox = new System.Windows.Forms.RichTextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.clearButton = new System.Windows.Forms.Button();
            this.enableCheckBox = new System.Windows.Forms.CheckBox();
            this.logLevelCb = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ttyTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 98);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(770, 427);
            this.panel1.TabIndex = 0;
            // 
            // ttyTextBox
            // 
            this.ttyTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ttyTextBox.Location = new System.Drawing.Point(0, 0);
            this.ttyTextBox.Name = "ttyTextBox";
            this.ttyTextBox.ReadOnly = true;
            this.ttyTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.ttyTextBox.Size = new System.Drawing.Size(770, 427);
            this.ttyTextBox.TabIndex = 0;
            this.ttyTextBox.Text = "";
            this.ttyTextBox.WordWrap = false;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.clearButton);
            this.panel2.Controls.Add(this.enableCheckBox);
            this.panel2.Controls.Add(this.logLevelCb);
            this.panel2.Location = new System.Drawing.Point(3, -1);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1399, 106);
            this.panel2.TabIndex = 1;
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(428, 12);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(150, 46);
            this.clearButton.TabIndex = 2;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // enableCheckBox
            // 
            this.enableCheckBox.AutoSize = true;
            this.enableCheckBox.Location = new System.Drawing.Point(286, 18);
            this.enableCheckBox.Name = "enableCheckBox";
            this.enableCheckBox.Size = new System.Drawing.Size(117, 36);
            this.enableCheckBox.TabIndex = 1;
            this.enableCheckBox.Text = "Enable";
            this.enableCheckBox.UseVisualStyleBackColor = true;
            this.enableCheckBox.CheckedChanged += new System.EventHandler(this.enableCheckBox_CheckedChanged);
            // 
            // logLevelCb
            // 
            this.logLevelCb.FormattingEnabled = true;
            this.logLevelCb.Items.AddRange(new object[] {
            "Message",
            "Warning",
            "Error"});
            this.logLevelCb.Location = new System.Drawing.Point(10, 14);
            this.logLevelCb.Name = "logLevelCb";
            this.logLevelCb.Size = new System.Drawing.Size(242, 40);
            this.logLevelCb.TabIndex = 0;
            // 
            // TtyConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(770, 525);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TtyConsole";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "TtyConsole";
            this.Load += new System.EventHandler(this.TtyConsole_Load);
            this.VisibleChanged += new System.EventHandler(this.TtyConsole_VisibleChanged);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RichTextBox ttyTextBox;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ComboBox logLevelCb;
        private System.Windows.Forms.CheckBox enableCheckBox;
        private System.Windows.Forms.Button clearButton;
    }
}