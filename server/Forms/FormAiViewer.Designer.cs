namespace Server.Forms
{
    partial class FormAiViewer
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
            this.components = new System.ComponentModel.Container();
            this.sessionTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // sessionTimer
            // 
            this.sessionTimer.Enabled = true;
            this.sessionTimer.Interval = 32;
            this.sessionTimer.Tick += new System.EventHandler(this.sessionTimer_Tick);
            // 
            // FormAiViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1075, 582);
            this.Name = "FormAiViewer";
            this.Text = "Ai Viewer";
            this.Load += new System.EventHandler(this.FormAiViewer_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.FormAiViewer_Paint);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer sessionTimer;
    }
}