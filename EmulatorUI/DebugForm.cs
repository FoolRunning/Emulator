using System;
using System.Drawing;
using System.Windows.Forms;
using SystemBase;

namespace EmulatorUI
{
    public partial class DebugForm : Form
    {
        private readonly IDisplay display;

        public DebugForm(IDisplay display)
        {
            InitializeComponent();

            this.display = display;
            txtDebugText.Visible = display is ITextDisplay;

            if (display is IPixelDisplay pixelDisplay)
            {
                dispDebugOutput.SetPixelDisplay(pixelDisplay);
                dispDebugOutput.Visible = true;
                txtDebugText.Visible = false;
            }
            else if (display is ITextDisplay textDisplay)
            {
                dispDebugOutput.Visible = false;
                txtDebugText.Visible = true;
                txtDebugText.ForeColor = textDisplay.Color;

                display.FrameFinished += Display_FrameFinished;
            }
            
            ClientSize = new Size(display.Size.Width, display.Size.Height);
            if (!string.IsNullOrEmpty(display.Title))
                Text = display.Title;
        }

        public int ScaleFactor
        {
            set => ClientSize = new Size(display.Size.Width * value, display.Size.Height * value);
        }

        private void Display_FrameFinished()
        {
            BeginInvoke(new Action(UpdateDisplayData));
        }

        private void UpdateDisplayData()
        {
            if (display is ITextDisplay textDisplay)
                txtDebugText.Text = textDisplay.Text;
        }
    }
}
