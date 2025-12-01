/*
 * -----------------------------------------------------------------------------
 * Project:       Smart Grid Harmonic Signal Analysis Simulation
 * File:          MainForm.Designer.cs
 * Description:   Designer support code for Windows Forms. 
 *                Contains auto-generated initialization logic.
 * Author:        [Natasya Tiara Regina]
 * License:       MIT License (Open Source) / HKI Registered
 * -----------------------------------------------------------------------------
 */

namespace SmartGridHarmonicAnalysis
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1184, 761); // Ukuran default, akan dimaximized oleh code utama
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Smart Grid Harmonic Signal Analysis";
            this.ResumeLayout(false);

        }

        #endregion
    }
}