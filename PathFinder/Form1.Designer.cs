namespace PathFinder
{
    partial class Form1
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
            this.pbCanvas = new System.Windows.Forms.PictureBox();
            this.statusbar = new System.Windows.Forms.StatusStrip();
            this.menubar = new System.Windows.Forms.MenuStrip();
            ((System.ComponentModel.ISupportInitialize)(this.pbCanvas)).BeginInit();
            this.SuspendLayout();
            // 
            // pbCanvas
            // 
            this.pbCanvas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbCanvas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.pbCanvas.Location = new System.Drawing.Point(12, 37);
            this.pbCanvas.Name = "pbCanvas";
            this.pbCanvas.Size = new System.Drawing.Size(860, 556);
            this.pbCanvas.TabIndex = 0;
            this.pbCanvas.TabStop = false;
            this.pbCanvas.Paint += new System.Windows.Forms.PaintEventHandler(this.pbCanvas_Paint);
            this.pbCanvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pbCanvas_MouseDown);
            this.pbCanvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbCanvas_MouseMove);
            this.pbCanvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbCanvas_MouseUp);
            // 
            // statusbar
            // 
            this.statusbar.Location = new System.Drawing.Point(0, 609);
            this.statusbar.Name = "statusbar";
            this.statusbar.Size = new System.Drawing.Size(884, 22);
            this.statusbar.TabIndex = 1;
            this.statusbar.Text = "statusStrip1";
            // 
            // menubar
            // 
            this.menubar.Location = new System.Drawing.Point(0, 0);
            this.menubar.Name = "menubar";
            this.menubar.Size = new System.Drawing.Size(884, 24);
            this.menubar.TabIndex = 2;
            this.menubar.Text = "menuStrip1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DimGray;
            this.ClientSize = new System.Drawing.Size(884, 631);
            this.Controls.Add(this.statusbar);
            this.Controls.Add(this.menubar);
            this.Controls.Add(this.pbCanvas);
            this.MainMenuStrip = this.menubar;
            this.Name = "Form1";
            this.Text = "PathFinder";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbCanvas)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbCanvas;
        private System.Windows.Forms.StatusStrip statusbar;
        private System.Windows.Forms.MenuStrip menubar;
    }
}

