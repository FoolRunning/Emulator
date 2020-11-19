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

        private int paintCount;
        private int currentDraws;
        private DateTime then;
        #endregion

        public DisplayOutput()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.Opaque | 
                     ControlStyles.ResizeRedraw | 
                     ControlStyles.DoubleBuffer |
                     ControlStyles.Selectable, true);
        }

        public bool ShowFPS { get; set; }

        public void SetPixelDisplay(IPixelDisplay newDisplay)
        {
            if (pixels != null)
            {
                pixels.Dispose();
                pixelDataHandle.Free();
            }

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
            if (pixels != null)
            {
                pixels.Dispose();
                pixelDataHandle.Free();
            }
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
            e.Graphics.CompositingMode = CompositingMode.SourceCopy;
            //using (Brush b = new SolidBrush(Color.Black))
            //    e.Graphics.FillRectangle(b, 0, 0, Width, Height);
 
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            //e.Graphics.SmoothingMode = SmoothingMode.None;

            if (ShowFPS)
            {
                e.Graphics.CompositingMode = CompositingMode.SourceOver;
                using (Brush b = new SolidBrush(Color.White))
                    e.Graphics.DrawString("FPS: " + currentDraws, Font, b, 5, 5);
                e.Graphics.CompositingMode = CompositingMode.SourceCopy;
            }

            if (pixels == null) 
                return;

            //e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            //e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float ratioX = Width / (float)size.Width;
            float ratioY = Height / (float)size.Height;
            float scale = ratioX > ratioY ? ratioY : ratioX;

            int outputWidth = (int)(size.Width * scale);
            int outputHeight = (int)(size.Height * scale);
            e.Graphics.DrawImage(pixels, (Width - outputWidth) / 2, (Height - outputHeight) / 2, outputWidth, outputHeight);

            paintCount++;
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

            display.GetPixels(pixelData);

            DateTime now = DateTime.Now;
            if (then <= now)
            {
                then = now.AddSeconds(1);
                currentDraws = paintCount;
                paintCount = 0;
            }

            Invalidate(false);
        }
        #endregion
    }
}
