namespace WinFormsRenderer
{
    partial class GfxInspector
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.TilesTab = new System.Windows.Forms.TabPage();
            this.PalettesTab = new System.Windows.Forms.TabPage();
            this.Bg0Tab = new System.Windows.Forms.TabPage();
            this.Bg1Tab = new System.Windows.Forms.TabPage();
            this.Bg2Tab = new System.Windows.Forms.TabPage();
            this.Bg3Tab = new System.Windows.Forms.TabPage();
            this.ObjTab = new System.Windows.Forms.TabPage();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.TilesTab);
            this.tabControl.Controls.Add(this.PalettesTab);
            this.tabControl.Controls.Add(this.Bg0Tab);
            this.tabControl.Controls.Add(this.Bg1Tab);
            this.tabControl.Controls.Add(this.Bg2Tab);
            this.tabControl.Controls.Add(this.Bg3Tab);
            this.tabControl.Controls.Add(this.ObjTab);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 6;
            this.tabControl.Size = new System.Drawing.Size(1047, 1221);
            this.tabControl.TabIndex = 0;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
            // 
            // TilesTab
            // 
            this.TilesTab.Location = new System.Drawing.Point(8, 46);
            this.TilesTab.Name = "TilesTab";
            this.TilesTab.Padding = new System.Windows.Forms.Padding(3);
            this.TilesTab.Size = new System.Drawing.Size(646, 1167);
            this.TilesTab.TabIndex = 0;
            this.TilesTab.Text = "Tiles";
            this.TilesTab.UseVisualStyleBackColor = true;
            // 
            // PalettesTab
            // 
            this.PalettesTab.Location = new System.Drawing.Point(8, 46);
            this.PalettesTab.Name = "PalettesTab";
            this.PalettesTab.Padding = new System.Windows.Forms.Padding(3);
            this.PalettesTab.Size = new System.Drawing.Size(646, 1167);
            this.PalettesTab.TabIndex = 1;
            this.PalettesTab.Text = "Palettes";
            this.PalettesTab.UseVisualStyleBackColor = true;
            // 
            // Bg0Tab
            // 
            this.Bg0Tab.Location = new System.Drawing.Point(8, 46);
            this.Bg0Tab.Name = "Bg0Tab";
            this.Bg0Tab.Size = new System.Drawing.Size(646, 1167);
            this.Bg0Tab.TabIndex = 2;
            this.Bg0Tab.Text = "BG 0";
            // 
            // Bg1Tab
            // 
            this.Bg1Tab.Location = new System.Drawing.Point(8, 46);
            this.Bg1Tab.Name = "Bg1Tab";
            this.Bg1Tab.Size = new System.Drawing.Size(646, 1167);
            this.Bg1Tab.TabIndex = 3;
            this.Bg1Tab.Text = "BG 1";
            // 
            // Bg2Tab
            // 
            this.Bg2Tab.Location = new System.Drawing.Point(8, 46);
            this.Bg2Tab.Name = "Bg2Tab";
            this.Bg2Tab.Size = new System.Drawing.Size(646, 1167);
            this.Bg2Tab.TabIndex = 4;
            this.Bg2Tab.Text = "BG 2";
            // 
            // Bg3Tab
            // 
            this.Bg3Tab.Location = new System.Drawing.Point(8, 46);
            this.Bg3Tab.Name = "Bg3Tab";
            this.Bg3Tab.Size = new System.Drawing.Size(646, 1167);
            this.Bg3Tab.TabIndex = 5;
            this.Bg3Tab.Text = "BG 3";
            // 
            // ObjTab
            // 
            this.ObjTab.Location = new System.Drawing.Point(8, 46);
            this.ObjTab.Name = "ObjTab";
            this.ObjTab.Padding = new System.Windows.Forms.Padding(3);
            this.ObjTab.Size = new System.Drawing.Size(646, 1167);
            this.ObjTab.TabIndex = 6;
            this.ObjTab.Text = "Obj";
            this.ObjTab.UseVisualStyleBackColor = true;
            // 
            // GfxInspector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1047, 1221);
            this.Controls.Add(this.tabControl);
            this.Name = "GfxInspector";
            this.Text = "GfxInspector";
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage TilesTab;
        private System.Windows.Forms.TabPage PalettesTab;
        private System.Windows.Forms.TabPage Bg0Tab;
        private System.Windows.Forms.TabPage Bg1Tab;
        private System.Windows.Forms.TabPage Bg2Tab;
        private System.Windows.Forms.TabPage Bg3Tab;
        private System.Windows.Forms.TabPage ObjTab;
    }
}