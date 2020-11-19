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

                display.FrameFinished += Display_FrameFinished;
            }
            
            ClientSize = new Size(display.Size.Width * 2, display.Size.Height * 2);
            if (!string.IsNullOrEmpty(display.Title))
                Text = display.Title;
        }

        private void Display_FrameFinished()
        {
            if (display is ITextDisplay textDisplay)
                txtDebugText.Text = textDisplay.Text;
        }
    }
}
