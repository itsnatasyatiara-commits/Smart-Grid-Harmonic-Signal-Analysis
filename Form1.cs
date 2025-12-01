/*
 * -----------------------------------------------------------------------------
 * Project:       Smart Grid Harmonic Signal Analysis Simulation
 * Description:   A real-time simulation of 3-phase power systems, analyzing 
 *                sensor behavior, harmonic distortion, and control stability 
 *                (S-Domain & Z-Domain).
 * Author:        [Natasya Tiara Regina]
 * License:       MIT License (Open Source) / HKI Registered
 * Created Date:  [30 November 2025]
 * -----------------------------------------------------------------------------
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Numerics;
using System.IO;

namespace SmartGridHarmonicAnalysis
{
    /// <summary>
    /// Parameter configuration structure for sensor simulation.
    /// </summary>
    public class SensorParams
    {
        public double NoiseLevel { get; set; }
        public double HarmonicMagnitude { get; set; }
        public double PhaseShift { get; set; }
        public double FrequencyDeviation { get; set; }
        public double Offset { get; set; }
        public double AmplitudeScale { get; set; }
        public double DistortionLevel { get; set; }
        public double MagneticFieldStrength { get; set; }
    }

    public partial class MainForm : Form
    {
        #region Constants & Configuration
        private const int SAMPLE_RATE = 1000;
        private const int NUM_SAMPLES = 128;
        private const double FUNDAMENTAL_FREQ = 50.0;
        private const double NOMINAL_VOLTAGE_PHASE = 220.0;
        private const double NOMINAL_CURRENT = 10.0;
        private const double HALL_EFFECT_OFFSET_V = 1.02;
        private const double HALL_EFFECT_SENSITIVITY = 0.045;

        // Physics Constants
        private const double TAU_ACS712 = 4.375e-6; // 4.375 microseconds
        private const double DELAY_PZEM = 0.1;      // 100ms
        private const double TS_SAMPLING = 0.1;     // Sampling time for Z-Domain
        #endregion

        #region Fields & Controls
        // Data Models
        private SensorParams[] _sensorParams = new SensorParams[5];

        // UI Controls - Charts
        private Chart[] phaseCharts = new Chart[5];
        private Chart[] frequencyCharts = new Chart[5];
        private Chart[] systemVoltCurrentCharts = new Chart[3];
        private Chart sDomainChart;
        private Chart zDomainChart;

        // UI Controls - Inputs & Labels
        private Label[] freqInfoLabels = new Label[5];
        private TrackBar[] noiseTrackBars = new TrackBar[5];
        private TrackBar[] harmonicTrackBars = new TrackBar[5];
        private TrackBar[] phaseTrackBars = new TrackBar[5];
        private TrackBar[] freqDevTrackBars = new TrackBar[5];
        private TrackBar[] offsetTrackBars = new TrackBar[5];
        private TrackBar[] amplitudeTrackBars = new TrackBar[5];
        private TrackBar[] distortionTrackBars = new TrackBar[5];
        private TrackBar[] magneticFieldTrackBars = new TrackBar[5];

        // Image Handling
        private PictureBox customPictureBox;
        private Image originalTopologyImage;
        #endregion
        
        public MainForm()
        {
            InitializeComponent();

            // Konfigurasi tambahan manual
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1800, 1000);
            this.AutoScroll = true;

            // Lanjut inisialisasi logika
            InitializeData();
            InitializeUI();
            LoadTopologyImage();
        }

        private void InitializeData()
        {
            for (int i = 0; i < 5; i++)
            {
                _sensorParams[i] = new SensorParams();
            }
        }

        #region UI Construction

        /// <summary>
        /// Loads the topology image from a relative path or creates a placeholder.
        /// </summary>
        private void LoadTopologyImage()
        {
            try
            {
                // CHANGE FOR GITHUB: Use relative path instead of absolute path
                string relativePath = Path.Combine(Application.StartupPath, "Assets", "3DSmartGrid.png");

                if (File.Exists(relativePath))
                {
                    using (var tempImage = Image.FromFile(relativePath))
                    {
                        originalTopologyImage = new Bitmap(tempImage);
                    }
                }
                else
                {
                    // Fallback: Create a placeholder programmatically
                    Bitmap placeholder = new Bitmap(400, 250);
                    using (Graphics g = Graphics.FromImage(placeholder))
                    {
                        g.Clear(Color.WhiteSmoke);
                        g.DrawRectangle(Pens.DarkGray, 0, 0, 399, 249);
                        using (Font font = new Font("Segoe UI", 10, FontStyle.Italic))
                        {
                            string text = "Image not found.\nPlace '3DSmartGrid.png' in 'Assets' folder.";
                            SizeF textSize = g.MeasureString(text, font);
                            g.DrawString(text, font, Brushes.Gray,
                                (placeholder.Width - textSize.Width) / 2,
                                (placeholder.Height - textSize.Height) / 2);
                        }
                    }
                    originalTopologyImage = placeholder;
                }

                if (customPictureBox != null) customPictureBox.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Warning: Could not load topology image.\n{ex.Message}", "Resource Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeUI()
        {
            Panel mainPanel = new Panel
            {
                Location = new Point(10, 50),
                Size = new Size(1900, 1200), // Larger virtual canvas
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(mainPanel);

            // Header
            Label titleLabel = new Label
            {
                Text = "Smart Grid Harmonic Signal Analysis (3-Phase 380V System)",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(1200, 35),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(titleLabel);

            // Sensor Definitions
            string[] sensorNames = { "ZMPT101B (Voltage)", "ACS712 (Current)", "H11A1 (Frequency)",
                                    "PZEM-004T (Power Quality)", "DRV5053 (Magnetic Field)" };
            string[] sensorFormulas = {
                "H_ZMPT(s) ≈ K_ZMPT",
                "H_ACS(s) = K_ACS / (τ_i s + 1)",
                "H_ZCD(s) = e^(-0.01s)",
                "H_PZEM(s) = e^(-0.1s)",
                "H_DRV(s) = K_DRV / (τ_m s + 1)"
            };

            // Layout Calculation
            int yPosition = 10;
            string[] phaseNames = { "R", "S", "T" };
            Color[] phaseColors = { Color.Red, Color.Blue, Color.Green };
            int sysChartX = 1100;
            int sysChartW = 400;
            int sysChartH = 200;

            // 1. System V-I Charts
            for (int j = 0; j < 3; j++)
            {
                systemVoltCurrentCharts[j] = CreateSystemVoltCurrentChart($"System V-I Phase {phaseNames[j]}", phaseColors[j]);
                systemVoltCurrentCharts[j].Location = new Point(sysChartX, yPosition + (j * (sysChartH + 10)));
                systemVoltCurrentCharts[j].Size = new Size(sysChartW, sysChartH);
                mainPanel.Controls.Add(systemVoltCurrentCharts[j]);
            }

            // 2. Topology Image
            customPictureBox = new PictureBox
            {
                Location = new Point(sysChartX + sysChartW + 20, 10),
                Size = new Size(sysChartW, (sysChartH * 3) + 20),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            customPictureBox.Paint += (sender, e) =>
            {
                if (originalTopologyImage != null)
                {
                    // High quality rendering
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(originalTopologyImage, 0, 0, customPictureBox.Width, customPictureBox.Height);
                }
            };
            mainPanel.Controls.Add(customPictureBox);

            yPosition = systemVoltCurrentCharts[2].Bottom + 30;

            // 3. Sensor Analysis Rows
            int sensorRowY = 10;
            for (int i = 0; i < 5; i++)
            {
                CreateSensorRow(mainPanel, sensorNames[i], sensorFormulas[i], i, sensorRowY);
                sensorRowY += 260; // Spacing between rows
            }

            // 4. Stability Analysis (S-Domain & Z-Domain)
            int stabilityY = sensorRowY + 20;

            // S-Domain
            Label sLabel = new Label
            {
                Text = "S-Domain (Closed Loop Stability - Padé Approximation)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, stabilityY),
                Size = new Size(600, 25),
                ForeColor = Color.DarkSlateGray
            };
            mainPanel.Controls.Add(sLabel);

            sDomainChart = CreateStabilityChart("S-Domain Analysis", "Real (σ)", "Imaginary (jω)");
            sDomainChart.Location = new Point(10, stabilityY + 30);
            sDomainChart.Size = new Size(750, 350);
            mainPanel.Controls.Add(sDomainChart);

            // Z-Domain
            Label zLabel = new Label
            {
                Text = "Z-Domain (Discrete Pole-Zero - Delay Model)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(770, stabilityY),
                Size = new Size(600, 25),
                ForeColor = Color.DarkSlateGray
            };
            mainPanel.Controls.Add(zLabel);

            zDomainChart = CreateStabilityChart("Z-Domain Analysis", "Real", "Imaginary");
            zDomainChart.Location = new Point(770, stabilityY + 30);
            zDomainChart.Size = new Size(750, 350);
            mainPanel.Controls.Add(zDomainChart);

            // Initial Draw
            UpdateAllCharts();
        }

        private void CreateSensorRow(Panel parent, string name, string formula, int index, int yPos)
        {
            // Title
            Label lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, yPos + 10),
                Size = new Size(160, 40),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.WhiteSmoke
            };
            parent.Controls.Add(lblName);

            // Formula
            Label lblForm = new Label
            {
                Text = "Transfer Function:\n" + formula,
                Font = new Font("Consolas", 8, FontStyle.Regular),
                Location = new Point(10, yPos + 55),
                Size = new Size(160, 60),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
            parent.Controls.Add(lblForm);

            // Controls Group
            GroupBox gb = new GroupBox
            {
                Text = "Parameter Adjustment",
                Font = new Font("Segoe UI", 9),
                Location = new Point(180, yPos + 5),
                Size = new Size(200, 240)
            };
            parent.Controls.Add(gb);

            // Dynamic Controls Generation
            int curY = 20;
            AddTrackBar(gb, index, "Amp Scale", ref amplitudeTrackBars, 50, 150, 100, ref curY);
            AddTrackBar(gb, index, "Noise Level", ref noiseTrackBars, 0, 100, 0, ref curY);
            AddTrackBar(gb, index, "Harmonic", ref harmonicTrackBars, 0, 100, 0, ref curY);
            AddTrackBar(gb, index, "Phase Shift", ref phaseTrackBars, -180, 180, 0, ref curY);

            // Specific Controls based on Sensor Type
            switch (index)
            {
                case 0: // Voltage
                    AddTrackBar(gb, index, "DC Offset", ref offsetTrackBars, -100, 100, 0, ref curY);
                    break;
                case 1: // Current
                    AddTrackBar(gb, index, "Offset Dev", ref offsetTrackBars, -50, 50, 0, ref curY);
                    break;
                case 2: // Freq
                    AddTrackBar(gb, index, "Freq Dev", ref freqDevTrackBars, -50, 50, 0, ref curY);
                    break;
                case 3: // Power
                    AddTrackBar(gb, index, "Distortion", ref distortionTrackBars, 0, 100, 0, ref curY);
                    break;
                case 4: // Mag Field
                    AddTrackBar(gb, index, "B Strength", ref magneticFieldTrackBars, 0, 200, 100, ref curY);
                    break;
            }

            // Time Domain Chart
            phaseCharts[index] = CreateStandardChart("3-Phase Signals - " + name, "Time (s)", "Amplitude");
            phaseCharts[index].Location = new Point(390, yPos + 10);
            phaseCharts[index].Size = new Size(340, 200);

            // Add Series
            string[] sNames = { "R", "S", "T" };
            Color[] sColors = { Color.Red, Color.Blue, Color.Green };
            for (int k = 0; k < 3; k++)
            {
                phaseCharts[index].Series.Add(new Series(sNames[k]) { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = sColors[k] });
            }
            parent.Controls.Add(phaseCharts[index]);

            // Info Label
            freqInfoLabels[index] = new Label
            {
                Text = "Freq: -- Hz",
                Font = new Font("Segoe UI", 8),
                Location = new Point(390, yPos + 215),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(freqInfoLabels[index]);

            // Frequency Domain (FFT) Chart
            frequencyCharts[index] = CreateStandardChart("FFT Analysis", "Frequency (Hz)", "Magnitude");
            frequencyCharts[index].Location = new Point(740, yPos + 10);
            frequencyCharts[index].Size = new Size(350, 225);
            frequencyCharts[index].Series.Add(new Series("Mag") { ChartType = SeriesChartType.Column, Color = Color.RoyalBlue });
            parent.Controls.Add(frequencyCharts[index]);
        }

        private void AddTrackBar(GroupBox gb, int index, string label, ref TrackBar[] trackBarArray, int min, int max, int def, ref int curY)
        {
            int lw = 80; int tw = 100;
            gb.Controls.Add(new Label { Text = label + ":", Location = new Point(10, curY), Size = new Size(lw, 20) });
            trackBarArray[index] = new TrackBar
            {
                Location = new Point(lw + 5, curY - 5),
                Size = new Size(tw, 30),
                Minimum = min,
                Maximum = max,
                Value = def,
                TickFrequency = (max - min) / 10
            };
            trackBarArray[index].ValueChanged += (s, e) => UpdateAllCharts();
            gb.Controls.Add(trackBarArray[index]);
            curY += 35;
        }
        #endregion

        #region Chart Factory Methods
        private Chart CreateStandardChart(string title, string xLabel, string yLabel)
        {
            Chart chart = new Chart();
            ChartArea ca = new ChartArea("MainArea");
            ca.AxisX.MajorGrid.LineColor = Color.LightGray;
            ca.AxisY.MajorGrid.LineColor = Color.LightGray;
            ca.AxisX.LabelStyle.Format = "0.##";
            ca.AxisX.Title = xLabel;
            ca.AxisY.Title = yLabel;
            ca.BackColor = Color.White;

            chart.ChartAreas.Add(ca);
            chart.Titles.Add(new Title(title, Docking.Top, new Font("Segoe UI", 9, FontStyle.Bold), Color.Black));
            chart.Legends.Add(new Legend("L") { Docking = Docking.Bottom, Font = new Font("Segoe UI", 7), Enabled = true });
            return chart;
        }

        private Chart CreateSystemVoltCurrentChart(string title, Color iColor)
        {
            Chart chart = CreateStandardChart(title, "Time (s)", "V / I");
            chart.Series.Add(new Series("V") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.DarkOrange });
            chart.Series.Add(new Series("I") { ChartType = SeriesChartType.Line, BorderWidth = 3, Color = iColor, BorderDashStyle = ChartDashStyle.Dot });
            return chart;
        }

        private Chart CreateStabilityChart(string title, string xLabel, string yLabel)
        {
            Chart chart = CreateStandardChart(title, xLabel, yLabel);
            chart.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Gainsboro;
            chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Gainsboro;

            // Common Series for Stability Analysis
            Series stable = new Series("Stable Region") { ChartType = SeriesChartType.Area, Color = Color.FromArgb(40, Color.LightGreen) };
            chart.Series.Add(stable);
            chart.Series.Add(new Series("Re Axis") { ChartType = SeriesChartType.Line, Color = Color.Black, BorderWidth = 1 });
            chart.Series.Add(new Series("Im Axis") { ChartType = SeriesChartType.Line, Color = Color.Black, BorderWidth = 1 });

            Series poles = new Series("Poles (x)") { ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Cross, MarkerSize = 12, MarkerColor = Color.Red };
            chart.Series.Add(poles);

            Series zeros = new Series("Zeros (o)") { ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Circle, MarkerSize = 10, MarkerColor = Color.Blue, MarkerBorderColor = Color.Blue };
            chart.Series.Add(zeros);

            // Unit Circle for Z-Domain (Pre-created, populated later)
            Series unitCircle = new Series("Unit Circle") { ChartType = SeriesChartType.Line, Color = Color.Gray, BorderWidth = 1, BorderDashStyle = ChartDashStyle.Dash };
            chart.Series.Add(unitCircle);

            chart.Legends[0].Docking = Docking.Right;
            return chart;
        }
        #endregion

        #region Simulation Logic (Update)

        private void UpdateAllCharts()
        {
            RefreshSensorParams();

            for (int i = 0; i < 5; i++)
            {
                UpdatePhaseChart(phaseCharts[i], _sensorParams[i], i);
                UpdateFrequencyChart(frequencyCharts[i], _sensorParams[i], i);
            }

            UpdateSystemVoltCurrentCharts(_sensorParams[0], _sensorParams[1]);
            UpdateSDomainChart(_sensorParams);
            UpdateZDomainChart(_sensorParams);
        }

        private void RefreshSensorParams()
        {
            for (int i = 0; i < 5; i++)
            {
                _sensorParams[i].AmplitudeScale = amplitudeTrackBars[i].Value / 100.0;
                _sensorParams[i].NoiseLevel = noiseTrackBars[i].Value / 100.0;
                _sensorParams[i].HarmonicMagnitude = harmonicTrackBars[i].Value / 100.0;
                _sensorParams[i].PhaseShift = phaseTrackBars[i].Value * (Math.PI / 180.0);
            }

            // Specific params
            if (offsetTrackBars[0] != null) _sensorParams[0].Offset = offsetTrackBars[0].Value / 10.0;
            if (offsetTrackBars[1] != null) _sensorParams[1].Offset = offsetTrackBars[1].Value / 100.0;
            if (freqDevTrackBars[2] != null) _sensorParams[2].FrequencyDeviation = freqDevTrackBars[2].Value / 10.0;
            if (distortionTrackBars[3] != null) _sensorParams[3].DistortionLevel = distortionTrackBars[3].Value / 100.0;
            if (magneticFieldTrackBars[4] != null) _sensorParams[4].MagneticFieldStrength = magneticFieldTrackBars[4].Value / 100.0;
        }

        private void UpdateFrequencyChart(Chart chart, SensorParams param, int sensorIndex)
        {
            chart.Series[0].Points.Clear();
            Complex[] samples = new Complex[NUM_SAMPLES];
            Random rand = new Random();

            double currentFreq = FUNDAMENTAL_FREQ + param.FrequencyDeviation;
            if (freqInfoLabels[sensorIndex] != null)
                freqInfoLabels[sensorIndex].Text = $"Signal: {currentFreq:N2} Hz | Fs: {SAMPLE_RATE} Hz";

            double omega = 2 * Math.PI * currentFreq;

            // Generate Signal for FFT
            for (int i = 0; i < NUM_SAMPLES; i++)
            {
                double t = (double)i / SAMPLE_RATE;
                double signal = CalculateSignalValue(sensorIndex, param, omega, t, 0, rand);
                samples[i] = new Complex(signal, 0);
            }

            // Perform FFT
            Complex[] fft = FFT(samples);

            // Normalize and Plot
            double maxMag = 0;
            int limitIndex = NUM_SAMPLES / 2;
            for (int i = 0; i < limitIndex; i++)
            {
                double mag = (2.0 / NUM_SAMPLES) * fft[i].Magnitude;
                double f = (double)i * SAMPLE_RATE / NUM_SAMPLES;
                chart.Series[0].Points.AddXY(f, mag);
                if (mag > maxMag) maxMag = mag;
            }

            // Axis Scaling
            chart.ChartAreas[0].AxisX.Minimum = 0;
            chart.ChartAreas[0].AxisX.Maximum = 500;
            SetAxisLimits(chart.ChartAreas[0].AxisY, 0, maxMag * 1.2);
            chart.Invalidate();
        }

        private void UpdatePhaseChart(Chart chart, SensorParams param, int sensorIndex)
        {
            Random rand = new Random();
            double maxVal = double.MinValue;
            double minVal = double.MaxValue;
            bool hasData = false;

            foreach (Series s in chart.Series) s.Points.Clear();

            double omega = 2 * Math.PI * (FUNDAMENTAL_FREQ + param.FrequencyDeviation);

            for (int p = 0; p < 3; p++)
            {
                double phaseShiftRad = p * (2 * Math.PI / 3);
                Series s = chart.Series[p];

                for (int i = 0; i < NUM_SAMPLES; i++)
                {
                    double t = (double)i / SAMPLE_RATE;
                    double signal = CalculateSignalValue(sensorIndex, param, omega, t, phaseShiftRad, rand);

                    s.Points.AddXY(t, signal);

                    if (!hasData) { maxVal = signal; minVal = signal; hasData = true; }
                    else
                    {
                        if (signal > maxVal) maxVal = signal;
                        if (signal < minVal) minVal = signal;
                    }
                }
            }

            // Auto-scale Axis
            chart.ChartAreas[0].AxisX.Minimum = 0;
            chart.ChartAreas[0].AxisX.Maximum = (double)NUM_SAMPLES / SAMPLE_RATE;

            if (hasData)
            {
                double range = maxVal - minVal;
                double padding = (range < 1e-6) ? 1.0 : range * 0.1;
                SetAxisLimits(chart.ChartAreas[0].AxisY, minVal - padding, maxVal + padding);
            }
            chart.Invalidate();
        }

        /// <summary>
        /// Centralized signal generator logic for all sensors
        /// </summary>
        private double CalculateSignalValue(int sensorIndex, SensorParams param, double omega, double t, double phaseShiftRad, Random rand)
        {
            double signal = 0;
            double amp = 0;

            switch (sensorIndex)
            {
                case 0: // ZMPT
                    amp = NOMINAL_VOLTAGE_PHASE * Math.Sqrt(2) * param.AmplitudeScale;
                    signal = amp * Math.Sin(omega * t + phaseShiftRad + param.PhaseShift) + param.Offset;
                    break;
                case 1: // ACS712
                    double lagA = -Math.Atan(omega * TAU_ACS712);
                    double gainA = 1.0 / Math.Sqrt(1 + Math.Pow(omega * TAU_ACS712, 2));
                    double cur = NOMINAL_CURRENT * Math.Sqrt(2) * param.AmplitudeScale * Math.Sin(omega * t + phaseShiftRad + param.PhaseShift + lagA);
                    signal = 2.5 + (cur * 0.1 * gainA) + param.Offset;
                    amp = 1.0;
                    break;
                case 2: // H11A1
                    double td = t - 0.01;
                    double sq = (td >= 0) ? Math.Sin(omega * td + phaseShiftRad + param.PhaseShift) : 0;
                    signal = (sq > 0) ? 1.0 : 0.0;
                    signal += param.NoiseLevel * (rand.NextDouble() - 0.5) * 0.2;
                    amp = 1.0;
                    break;
                case 3: // PZEM
                    double tp = t - DELAY_PZEM;
                    amp = NOMINAL_VOLTAGE_PHASE * Math.Sqrt(2) * param.AmplitudeScale;
                    signal = (tp >= 0) ? amp * Math.Sin(omega * tp + phaseShiftRad + param.PhaseShift) : 0;
                    signal += param.DistortionLevel * 0.3 * amp * Math.Sin(3 * omega * t + phaseShiftRad);
                    break;
                case 4: // DRV
                    double lagM = -Math.Atan(omega * 7.9e-6);
                    double b = NOMINAL_CURRENT * param.MagneticFieldStrength * 0.1 * Math.Sin(omega * t + phaseShiftRad + param.PhaseShift + lagM);
                    signal = HALL_EFFECT_OFFSET_V + (b * HALL_EFFECT_SENSITIVITY);
                    amp = 1.0;
                    break;
            }

            // Inject Noise & Harmonics (except frequency sensor which is digital-like)
            if (sensorIndex != 2)
            {
                signal += param.HarmonicMagnitude * 0.2 * amp * Math.Sin(3 * omega * t);
                signal += param.NoiseLevel * (rand.NextDouble() - 0.5) * (amp != 0 ? amp * 0.05 : 0.5);
            }

            return signal;
        }

        private void UpdateSystemVoltCurrentCharts(SensorParams vP, SensorParams cP)
        {
            double wV = 2 * Math.PI * (FUNDAMENTAL_FREQ + vP.FrequencyDeviation);
            double wI = 2 * Math.PI * (FUNDAMENTAL_FREQ + cP.FrequencyDeviation);
            double lag = -Math.Atan(wI * TAU_ACS712);

            for (int p = 0; p < 3; p++)
            {
                Chart chart = systemVoltCurrentCharts[p];
                foreach (Series s in chart.Series) s.Points.Clear();

                double shift = p * (2 * Math.PI / 3);
                double maxVal = double.MinValue, minVal = double.MaxValue;

                for (int i = 0; i < NUM_SAMPLES; i++)
                {
                    double t = (double)i / SAMPLE_RATE;

                    double sigV = (NOMINAL_VOLTAGE_PHASE * Math.Sqrt(2) * vP.AmplitudeScale * Math.Sin(wV * t + shift + vP.PhaseShift)) + vP.Offset;
                    double sigI = 2.5 + ((NOMINAL_CURRENT * Math.Sqrt(2) * cP.AmplitudeScale * Math.Sin(wI * t + shift + cP.PhaseShift + lag)) * 0.1) + cP.Offset;

                    chart.Series["V"].Points.AddXY(t, sigV);
                    chart.Series["I"].Points.AddXY(t, sigI);

                    // Update min/max for scaling
                    if (sigV > maxVal) maxVal = sigV; if (sigV < minVal) minVal = sigV;
                    if (sigI > maxVal) maxVal = sigI; if (sigI < minVal) minVal = sigI;
                }

                chart.ChartAreas[0].AxisX.Maximum = (double)NUM_SAMPLES / SAMPLE_RATE;

                double range = maxVal - minVal;
                double padding = (range < 1e-6) ? 10.0 : range * 0.2;
                SetAxisLimits(chart.ChartAreas[0].AxisY, minVal - padding, maxVal + padding);
                chart.Invalidate();
            }
        }
        #endregion

        #region Stability Analysis (S & Z Domains)

        private void UpdateSDomainChart(SensorParams[] allParams)
        {
            // Clear Data
            sDomainChart.Series["Stable Region"].Points.Clear();
            sDomainChart.Series["Poles (x)"].Points.Clear();
            sDomainChart.Series["Zeros (o)"].Points.Clear();
            sDomainChart.Series["Re Axis"].Points.Clear();
            sDomainChart.Series["Im Axis"].Points.Clear();
            sDomainChart.Series["Unit Circle"].Points.Clear(); // Not used in S-Domain

            double K_Loop = allParams[0].AmplitudeScale * 1.5;
            double Td = DELAY_PZEM;
            double tau = TAU_ACS712;

            // Pade Approximation Coefficients
            double a = 0.5 * tau * Td;
            double b = tau + (0.5 * Td) - (0.5 * K_Loop * Td);
            double c = 1.0 + K_Loop;

            // Quadratic Equation Solver for Poles
            Complex p1, p2;
            double det = (b * b) - (4 * a * c);

            if (det >= 0)
            {
                p1 = new Complex((-b + Math.Sqrt(det)) / (2 * a), 0);
                p2 = new Complex((-b - Math.Sqrt(det)) / (2 * a), 0);
            }
            else
            {
                double r = -b / (2 * a);
                double i = Math.Sqrt(-det) / (2 * a);
                p1 = new Complex(r, i);
                p2 = new Complex(r, -i);
            }

            double zeroLoc = 2.0 / Td;

            // Visualization Bounds
            double viewMin = Math.Min(-50, Math.Min(p1.Real, p2.Real) - 10);
            double viewMax = Math.Max(25, zeroLoc + 10);
            double yLimit = Math.Max(30, Math.Abs(p1.Imaginary) + 10);

            // Draw Stability Region (LHP)
            sDomainChart.Series["Stable Region"].Points.AddXY(viewMin, -yLimit);
            sDomainChart.Series["Stable Region"].Points.AddXY(viewMin, yLimit);
            sDomainChart.Series["Stable Region"].Points.AddXY(0, yLimit);
            sDomainChart.Series["Stable Region"].Points.AddXY(0, -yLimit);

            // Draw Axes
            sDomainChart.Series["Im Axis"].Points.AddXY(0, -yLimit);
            sDomainChart.Series["Im Axis"].Points.AddXY(0, yLimit);
            sDomainChart.Series["Re Axis"].Points.AddXY(viewMin, 0);
            sDomainChart.Series["Re Axis"].Points.AddXY(viewMax, 0);

            // Plot Poles & Zeros
            if (!IsSingular(p1.Real)) sDomainChart.Series["Poles (x)"].Points.AddXY(p1.Real, p1.Imaginary);
            if (!IsSingular(p2.Real)) sDomainChart.Series["Poles (x)"].Points.AddXY(p2.Real, p2.Imaginary);
            sDomainChart.Series["Zeros (o)"].Points.AddXY(zeroLoc, 0);

            SetAxisLimits(sDomainChart.ChartAreas[0].AxisX, viewMin, viewMax);
            SetAxisLimits(sDomainChart.ChartAreas[0].AxisY, -yLimit, yLimit);
            sDomainChart.Invalidate();
        }

        private void UpdateZDomainChart(SensorParams[] allParams)
        {
            // Clear
            zDomainChart.Series["Unit Circle"].Points.Clear();
            zDomainChart.Series["Poles (x)"].Points.Clear();
            zDomainChart.Series["Zeros (o)"].Points.Clear();
            zDomainChart.Series["Re Axis"].Points.Clear();
            zDomainChart.Series["Im Axis"].Points.Clear();
            zDomainChart.Series["Stable Region"].Points.Clear(); // Not used typicaly, circle is enough

            // Draw Unit Circle
            for (int i = 0; i <= 360; i += 5)
            {
                double rad = i * Math.PI / 180;
                zDomainChart.Series["Unit Circle"].Points.AddXY(Math.Cos(rad), Math.Sin(rad));
            }

            // Draw Axes
            zDomainChart.Series["Re Axis"].Points.AddXY(-2.5, 0);
            zDomainChart.Series["Re Axis"].Points.AddXY(2.5, 0);
            zDomainChart.Series["Im Axis"].Points.AddXY(0, -2);
            zDomainChart.Series["Im Axis"].Points.AddXY(0, 2);

            double K_Loop = allParams[0].AmplitudeScale;
            int d = (int)(DELAY_PZEM / TS_SAMPLING);
            if (d < 1) d = 1;

            double magnitude = Math.Pow(Math.Abs(K_Loop), 1.0 / d);

            for (int k = 0; k < d; k++)
            {
                double angle = (Math.PI + (2 * Math.PI * k)) / d;
                double real = magnitude * Math.Cos(angle);
                double imag = magnitude * Math.Sin(angle);
                zDomainChart.Series["Poles (x)"].Points.AddXY(real, imag);
            }

            // Zero Mapping
            double s_zero = 2.0 / DELAY_PZEM;
            double z_zero_val = Math.Exp(s_zero * TS_SAMPLING);
            zDomainChart.Series["Zeros (o)"].Points.AddXY(z_zero_val, 0);

            SetAxisLimits(zDomainChart.ChartAreas[0].AxisX, -2.0, 2.0);
            SetAxisLimits(zDomainChart.ChartAreas[0].AxisY, -1.5, 1.5);
            zDomainChart.Invalidate();
        }
        #endregion

        #region Math & Utilities

        public static Complex[] FFT(Complex[] input)
        {
            int N = input.Length;
            if (N == 1) return new Complex[] { input[0] };
            if ((N & (N - 1)) != 0) throw new ArgumentException("Length must be power of 2");

            Complex[] even = new Complex[N / 2];
            Complex[] odd = new Complex[N / 2];
            for (int i = 0; i < N / 2; i++)
            {
                even[i] = input[2 * i];
                odd[i] = input[2 * i + 1];
            }

            Complex[] q = FFT(even);
            Complex[] r = FFT(odd);
            Complex[] output = new Complex[N];

            for (int k = 0; k < N / 2; k++)
            {
                double angle = -2 * Math.PI * k / N;
                Complex wk = new Complex(Math.Cos(angle), Math.Sin(angle));
                output[k] = q[k] + wk * r[k];
                output[k + N / 2] = q[k] - wk * r[k];
            }
            return output;
        }

        private void SetAxisLimits(Axis axis, double min, double max)
        {
            if (double.IsNaN(min) || double.IsInfinity(min)) min = 0;
            if (double.IsNaN(max) || double.IsInfinity(max)) max = 1;

            // Fix inverted limits
            if (min > max) { double t = min; min = max; max = t; }

            // Prevent zero range
            if ((max - min) < 1e-6)
            {
                double center = (max + min) / 2.0;
                min = center - 1.0;
                max = center + 1.0;
            }

            axis.Interval = 0; // Auto
            axis.IntervalOffset = 0;
            axis.Minimum = min;
            axis.Maximum = max;
        }

        private bool IsSingular(double val)
        {
            return double.IsNaN(val) || double.IsInfinity(val);
        }
        #endregion
    }
}