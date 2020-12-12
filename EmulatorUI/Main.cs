using System;
using System.Diagnostics;
using System.Windows.Forms;
using System_NES;
using SystemBase;
using NAudio.Wave;

namespace EmulatorUI
{
    public partial class Main : Form
    {
        private DirectSoundOut soundOutputDevice;
        private ISystem loadedSystem;
        private bool running;

        public Main()
        {
            InitializeComponent();
            systemToolStripMenuItem.Visible = false;
            debugToolStripMenuItem.Visible = false;
            startEmulationToolStripMenuItem.Enabled = false;
            stopEmulationToolStripMenuItem.Enabled = false;
            displayOutput.SmoothPixels = false;
        }

        private void nintendoEntertainmentSystemNESToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Load systems dynamically
            loadedSystem = new NES();
            if (loadedSystem.MainDisplay != null)
                displayOutput.SetPixelDisplay(loadedSystem.MainDisplay);

            foreach (IDisplay display in loadedSystem.OtherDisplayableComponents)
            {
                DebugForm debugDisplay = new DebugForm(display);
                debugDisplay.Scale = 2;
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
                
                loadedSystem.LoadProgramFile(dlg.FileName);
                
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

            //SignalGenerator gen = new SignalGenerator(44100, 1);
            //gen.Type = SignalGeneratorType.Square;
            //MixingSampleProvider mixer = new MixingSampleProvider(new[] { });
            //mixer.ReadFully = true;
            soundOutputDevice.Init(new SampleGenerator(loadedSystem.SoundGenerator, loadedSystem.SoundGenerator.ChannelCount));
            soundOutputDevice.Play();

            loadedSystem.Start();
            running = true;
        }

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
    }
}
