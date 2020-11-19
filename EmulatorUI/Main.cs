using System;
using System.Windows.Forms;
using System_NES;
using SystemBase;

namespace EmulatorUI
{
    public partial class Main : Form
    {
        private ISystem loadedSystem;

        public Main()
        {
            InitializeComponent();
            systemToolStripMenuItem.Visible = false;
            debugToolStripMenuItem.Visible = false;
            startEmulationToolStripMenuItem.Enabled = false;
            stopEmulationToolStripMenuItem.Enabled = false;
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

                loadedSystem.LoadProgramFile(dlg.FileName);
            }

            loadedSystem.Start();

            stopEmulationToolStripMenuItem.Enabled = true;
        }

        private void startEmulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadedSystem.Start();
            
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

        private void CleanUp()
        {
            loadedSystem?.Stop();
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
    }
}
