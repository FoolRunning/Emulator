using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System_NES;
using SystemBase;
using EmulatorUI.Properties;
using EmulatorUI.Scalers;
using NAudio.Wave;

namespace EmulatorUI
{
    public partial class Main : Form
    {
        #region Member variables
        private FormWindowState prevWindowState = FormWindowState.Normal;
        private Rectangle prevBounds;

        private IWavePlayer soundOutputDevice;
        private ISystem loadedSystem;
        private bool running;
        #endregion

        #region Constructor
        public Main()
        {
            InitializeComponent();
            systemToolStripMenuItem.Visible = false;
            debugToolStripMenuItem.Visible = false;
            startEmulationToolStripMenuItem.Enabled = false;
            stopEmulationToolStripMenuItem.Enabled = false;
            
            SetExactScaling(Settings.Default.ExactScaling, false);
            SetSmoothing(Settings.Default.OutputSmoothing, false);
            SetScalingMode(Settings.Default.ScalingMode, false);

            RgbYuv.Initialize(); // Fairly expensive static constructor, so initialize it before showing
        }
        #endregion

        #region Event handlers
        private void nintendoEntertainmentSystemNESToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Load systems dynamically
            loadedSystem = new NES();
            if (loadedSystem.MainDisplay != null)
                displayOutput.SetPixelDisplay(loadedSystem.MainDisplay);

            foreach (IDisplay display in loadedSystem.OtherDisplayableComponents)
            {
                DebugForm debugDisplay = new DebugForm(display);
                debugDisplay.ScaleFactor = 2;
                debugDisplay.Show(this);
            }
            
            systemToolStripMenuItem.Visible = true;
            debugToolStripMenuItem.Visible = true;
        }
        
        private void loadProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = loadedSystem.AcceptableFileExtensionsForPrograms;
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                if (running)
                    loadedSystem.CPU.Pause();

                try
                {
                    loadedSystem.LoadProgramFile(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to load program:\n" + ex.Message, "Emulator", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                loadedSystem.Bus.Reset();
                if (running)
                    loadedSystem.CPU.Resume();
            }

            Start();
            stopEmulationToolStripMenuItem.Enabled = true;
        }

        private void startEmulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Start();
            
            startEmulationToolStripMenuItem.Enabled = false;
        }

        private void stopEmulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CleanUp();
            startEmulationToolStripMenuItem.Enabled = false;
            stopEmulationToolStripMenuItem.Enabled = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanUp();
        }

        private void displayOutput_KeyDown(object sender, KeyEventArgs e)
        {
            if (loadedSystem == null)
                return;

            foreach (IController controller in loadedSystem.Controllers)
                controller.KeyboardKeyDown((ConsoleKey)e.KeyValue);
        }

        private void displayOutput_KeyUp(object sender, KeyEventArgs e)
        {
            if (loadedSystem == null)
                return;

            foreach (IController controller in loadedSystem.Controllers)
                controller.KeyboardKeyUp((ConsoleKey)e.KeyValue);
        }

        private void smoothOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSmoothing(!smoothOutputToolStripMenuItem.Checked, true);
        }

        private void nearestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetScalingMode(ScalingMode.Nearest, true);
        }

        private void cRTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetScalingMode(ScalingMode.CRT, true);
        }

        private void hqx2xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetScalingMode(ScalingMode.Hqx2x, true);
        }
        
        private void exactScalingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetExactScaling(!exactScalingToolStripMenuItem.Checked, true);
        }
        
        private void fullscreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetFullScreen(!fullscreenToolStripMenuItem.Checked, true);
        }
        #endregion

        #region Private helper methods
        private void SetExactScaling(bool exact, bool save)
        {
            exactScalingToolStripMenuItem.Checked = exact;
            displayOutput.ExactScaling = exact;
            if (save)
            {
                Settings.Default.ExactScaling = exact;
                Settings.Default.Save();
            }
        }

        private void SetSmoothing(bool smooth, bool save)
        {
            smoothOutputToolStripMenuItem.Checked = smooth;
            displayOutput.SmoothPixels = smooth;
            if (save)
            {
                Settings.Default.OutputSmoothing = smooth;
                Settings.Default.Save();
            }
        }

        private void SetScalingMode(ScalingMode newScalingMode, bool save)
        {
            nearestToolStripMenuItem.Checked = newScalingMode == ScalingMode.Nearest;
            cRTToolStripMenuItem.Checked = newScalingMode == ScalingMode.CRT;
            hqx2xToolStripMenuItem.Checked = newScalingMode == ScalingMode.Hqx2x;
            displayOutput.ScalingMode = newScalingMode;
            if (save)
            {
                Settings.Default.ScalingMode = newScalingMode;
                Settings.Default.Save();
            }
        }

        private void SetFullScreen(bool fullScreen, bool save)
        {
            if (fullscreenToolStripMenuItem.Checked == fullScreen)
                return;

            fullscreenToolStripMenuItem.Checked = fullScreen;
            TopMost = fullScreen;

            if (fullScreen)
            {
                // Workaround for bug where changing the border style resets the restore bounds
                prevWindowState = WindowState;
                prevBounds = prevWindowState != FormWindowState.Normal ? RestoreBounds : Bounds;
            }

            FormBorderStyle = fullScreen ? FormBorderStyle.None : FormBorderStyle.Sizable;

            if (!fullScreen)
            {
                WindowState = prevWindowState;
                Bounds = prevBounds;
            }
            else
            {
                if (WindowState == FormWindowState.Maximized)
                    WindowState = FormWindowState.Normal; // Workaround for bug where window does not handle border style change correctly when maximized

                WindowState = FormWindowState.Maximized;
            }
        }

        private void CleanUp()
        {
            if (soundOutputDevice != null)
            {
                soundOutputDevice?.Stop();
                soundOutputDevice.Dispose();
            }

            loadedSystem?.Stop();
        }

        private void Start()
        {
            if (running)
                return;

            Debug.Assert(soundOutputDevice == null);
            soundOutputDevice = new DirectSoundOut(100);
            //soundOutputDevice = new WaveOutEvent();
            //((WaveOutEvent)soundOutputDevice).NumberOfBuffers = 2;
            //((WaveOutEvent)soundOutputDevice).DesiredLatency = 100;

            //SignalGenerator gen = new SignalGenerator(44100, 1);
            //gen.Type = SignalGeneratorType.Square;
            //MixingSampleProvider mixer = new MixingSampleProvider(new[] { });
            //mixer.ReadFully = true;
            soundOutputDevice.Init(new SampleGenerator(loadedSystem.SoundGenerator, loadedSystem.SoundGenerator.ChannelCount));
            soundOutputDevice.Play();

            loadedSystem.Start();
            running = true;
        }
        #endregion

        #region SampleGenerator class
        private sealed class SampleGenerator : ISampleProvider
        {
            private const int Frequency = 44100;
            private const double TimeStep = 1.0 / Frequency;
            private readonly ISoundProvider generator;
            private readonly int channelCount;

            private double totalTime;
            //private readonly BiQuadFilter highPassFilter1 = BiQuadFilter.HighPassFilter(44100, 90, 1);
            //private readonly BiQuadFilter highPassFilter2 = BiQuadFilter.HighPassFilter(44100, 440, 1);
            //private readonly BiQuadFilter lowPassFilter = BiQuadFilter.LowPassFilter(44100, 14000, 1);
            
            public SampleGenerator(ISoundProvider generator, int channelCount)
            {
                this.generator = generator;
                this.channelCount = channelCount;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int end = offset + count;
                for (int i = offset; i < end; i += channelCount)
                {
                    for (int channel = 0; channel < channelCount; channel++)
                    {
                        float sample = generator.GetSample(channel, (float)totalTime, (float)TimeStep);
                        //sample = highPassFilter1.Transform(sample);
                        //sample = highPassFilter2.Transform(sample);
                        //sample = lowPassFilter.Transform(sample);
                        buffer[i + channel] = sample;
                    }

                    totalTime += TimeStep;
                }

                return count;
            }

            public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(Frequency, channelCount);
        }
        #endregion
    }
}
