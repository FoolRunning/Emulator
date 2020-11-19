
namespace EmulatorUI
{
    partial class DebugForm
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

            if (display != null)
                display.FrameFinished -= Display_FrameFinished;

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dispDebugOutput = new EmulatorUI.DisplayOutput();
            this.txtDebugText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // dispDebugOutput
            // 
            this.dispDebugOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dispDebugOutput.Location = new System.Drawing.Point(0, 0);
            this.dispDebugOutput.Name = "dispDebugOutput";
            this.dispDebugOutput.Size = new System.Drawing.Size(800, 450);
            this.dispDebugOutput.TabIndex = 0;
            this.dispDebugOutput.Text = "displayOutput1";
            // 
            // txtDebugText
            // 
            this.txtDebugText.BackColor = System.Drawing.Color.Black;
            this.txtDebugText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDebugText.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDebugText.ForeColor = System.Drawing.Color.White;
            this.txtDebugText.Location = new System.Drawing.Point(0, 0);
            this.txtDebugText.Multiline = true;
            this.txtDebugText.Name = "txtDebugText";
            this.txtDebugText.Size = new System.Drawing.Size(800, 450);
            this.txtDebugText.TabIndex = 1;
            // 
            // DebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtDebugText);
            this.Controls.Add(this.dispDebugOutput);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DebugForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Debug Display";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DisplayOutput dispDebugOutput;
        private System.Windows.Forms.TextBox txtDebugText;
    }
}