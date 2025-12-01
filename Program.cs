/*
 * -----------------------------------------------------------------------------
 * Project:       Smart Grid Harmonic Signal Analysis Simulation
 * File:          Program.cs
 * Description:   Main entry point for the application.
 *                Handles application startup and global exception catching.
 * Author:        [Natasya Tiara Regina]
 * License:       MIT License (Open Source) / HKI Registered
 * -----------------------------------------------------------------------------
 */

using System;
using System.Windows.Forms;

namespace SmartGridHarmonicAnalysis
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Standard Windows Forms initialization
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Launch the Main Form
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                // Global Error Handling
                // Captures crash information to display a helpful message instead of silently closing.
                MessageBox.Show(
                    $"A fatal error occurred during startup:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Application Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}