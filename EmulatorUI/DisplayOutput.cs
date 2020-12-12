using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SystemBase;

namespace EmulatorUI
{
    public class DisplayOutput : Control
    {
        #region Member variables
        private Size size;
        private Bitmap pixels;
        private byte[] pixelData;
        private GCHandle pixelDataHandle;
        private IPixelDisplay display;
        private InterpolationMode smoothingMode = InterpolationMode.NearestNeighbor;

        private int paintCount;
        private string fpsStr;
        #endregion

        public DisplayOutput()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.Opaque | 
                     ControlStyles.ResizeRedraw | 
                     ControlStyles.DoubleBuffer |
                     ControlStyles.Selectable, true);

            size = new Size(2, 2);
            pixelData = new byte[16];
            pixelDataHandle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
            pixels = new Bitmap(2, 2, PixelFormat.Format32bppRgb);
            Timer timer = new Timer();
            timer.Tick += Timer_Tick;
            timer.Interval = 1000;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            fpsStr = "FPS: " + paintCount;
            paintCount = 0;
        }

        public bool ShowFPS { get; set; }

        public bool SmoothPixels
        {
            set => smoothingMode = value ? InterpolationMode.HighQualityBilinear : InterpolationMode.NearestNeighbor;
        }

        public void SetPixelDisplay(IPixelDisplay newDisplay)
        {
            pixels.Dispose();
            pixelDataHandle.Free();

            if (display != null)
                display.FrameFinished -= Display_FrameFinished;

            display = newDisplay;
            size = display.Size;

            switch (display.PixelFormat)
            {
                case PFormat.Format8bppIndexed:
                    pixelData = new byte[size.Width * size.Height];
                    pixelDataHandle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
                    pixels = new Bitmap(size.Width, size.Height, size.Width, PixelFormat.Format8bppIndexed, 
                        pixelDataHandle.AddrOfPinnedObject());
                
                    ColorPalette pal = pixels.Palette;
                    Color[] colors = display.Pallete;
                    for (int i = 0; i < pal.Entries.Length; i++)
                        pal.Entries[i] = colors[i];
                    pixels.Palette = pal; // Must be set to take effect
                    break;
                case PFormat.Format32bppRgb:
                    pixelData = new byte[size.Width * size.Height * 4];
                    pixelDataHandle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
                    pixels = new Bitmap(size.Width, size.Height, size.Width, PixelFormat.Format32bppRgb, 
                        pixelDataHandle.AddrOfPinnedObject());
                    break;
                default:
                    Trace.Fail("Not implemented");
                    break;
            }

            display.FrameFinished += Display_FrameFinished;
        }

        #region Overridden methods
        protected override void Dispose(bool disposing)
        {
            pixels.Dispose();
            pixelDataHandle.Free();

            base.Dispose(disposing);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = true;
            base.OnPreviewKeyDown(e);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return display?.Size ?? base.GetPreferredSize(proposedSize);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.DarkSlateGray);
 
            //float ratioX = Width / (float)size.Width;
            //float ratioY = Height / (float)size.Height;
            float ratioX = Width / size.Width;
            float ratioY = Height / size.Height;
            float scale = ratioX > ratioY ? ratioY : ratioX;
            int outputWidth = (int)(size.Width * scale);
            int outputHeight = (int)(size.Height * scale);

            e.Graphics.CompositingMode = CompositingMode.SourceCopy;
            e.Graphics.InterpolationMode = smoothingMode;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            Rectangle imageRect = new Rectangle((Width - outputWidth) / 2, (Height - outputHeight) / 2, outputWidth, outputHeight);
            lock (pixels)
                e.Graphics.DrawImage(pixels, imageRect);
            
            using (Pen p = new Pen(Color.FloralWhite))
                e.Graphics.DrawRectangle(p, imageRect.X, imageRect.Y, imageRect.Width + 1, imageRect.Height + 1);

            if (ShowFPS)
            {
                e.Graphics.CompositingMode = CompositingMode.SourceOver;
                using (Brush b = new SolidBrush(Color.White))
                    e.Graphics.DrawString(fpsStr, Font, b, 5, 5);
                e.Graphics.CompositingMode = CompositingMode.SourceCopy;
            }
        }
        #endregion

        #region Event handlers
        private void Display_FrameFinished()
        {
            BeginInvoke(new Action(UpdateDisplayData));
        }
        #endregion
        
        #region Private helper methods
        private void UpdateDisplayData()
        {
            if (display == null)
                return;

            lock (pixels)
                display.GetPixels(pixelData);

            paintCount++;
            Invalidate(false);
        }
        #endregion
    }
}
