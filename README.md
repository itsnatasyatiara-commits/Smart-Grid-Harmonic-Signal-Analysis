# Smart Grid Harmonic Signal Analysis Simulation

![GitHub](https://img.shields.io/github/license/username/repo-name)
![Platform](https://img.shields.io/badge/platform-Windows%20Forms%20%7C%20.NET-blue)
![Language](https://img.shields.io/badge/language-C%23-green)

A comprehensive simulation tool for analyzing **3-Phase Power Systems**, modeling sensor behaviors, detecting harmonic distortions using FFT, and evaluating control system stability through S-Domain and Z-Domain analysis.

![Dashboard Screenshot](Assets/dashboard_preview.png)
*(Note: Replace `Assets/dashboard_preview.png` with an actual screenshot of your program)*

## 🚀 Key Features

### 1. Real-Time 3-Phase Simulation
- Visualizes **Voltage (V)** and **Current (I)** waveforms for phases R, S, and T in real-time.
- Simulates a standard **380V/50Hz** grid environment.

### 2. Advanced Sensor Modeling
Implements mathematical transfer functions to simulate real-world sensor characteristics:
- **ZMPT101B (Voltage):** Step-down transformation simulation.
- **ACS712 (Current):** Hall-effect behavior with first-order lag response.
- **H11A1 (Zero Crossing):** Frequency detection logic.
- **PZEM-004T:** Power quality monitoring with time-delay simulation.
- **DRV5053:** Magnetic field sensing.

### 3. Harmonic Analysis (FFT)
- Performs **Fast Fourier Transform (FFT)** to analyze frequency spectrums.
- Detects signal noise and harmonic distortions (THD) injected into the grid.

### 4. Stability Analysis
- **S-Domain (Continuous):** Plots Pole-Zero map using Padé approximation for stability checks.
- **Z-Domain (Discrete):** Visualizes system stability on the Unit Circle for digital control implementations.

### 5. Interactive Dashboard
- Fully adjustable parameters via GUI sliders:
  - Noise Level & Harmonic Injection.
  - Phase Shift & Frequency Deviation.
  - Amplitude Scaling & DC Offset.

## 🛠️ Tech Stack
- **Language:** C# (C-Sharp)
- **Framework:** .NET Framework (Windows Forms)
- **Libraries:**
  - `System.Windows.Forms.DataVisualization` (Charting)
  - `System.Numerics` (Complex Number Calculation for FFT)

## 📦 Installation & Usage

1. **Clone the Repository**
   ```bash
   git clone (https://github.com/itsnatasyatiara-commits/Smart-Grid-Harmonic-Signal-Analysis/tree/8201a63f19479174a5c697128322434ae4a0d21b)
