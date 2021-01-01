using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SystemBase;
using EmulatorUI.Scalers;

namespace EmulatorUI
{
    public enum ScalingMode
    {
        Nearest,
        CRT,
        Hqx2x
    }

    public class DisplayOutput : Control
    {
        #region Member variables
        private const int outputScaleFactor = 2;

        private IDisplayScaler scaler;
        private Size size;
        private Bitmap outputPixels;
        private RgbColor[] outputPixelData;
        private RgbColor[] inputPixelData;
        private GCHandle pixelDataHandle;
        private IPixelDisplay display;
        private InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor;
        private SmoothingMode smoothingMode = SmoothingMode.None;

        private int paintCount;
        private string fpsStr;

        private readonly object imageSyncObj = new object();
        #endregion

        #region Constructor
        public DisplayOutput()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.Opaque | 
                     ControlStyles.ResizeRedraw | 
                     ControlStyles.DoubleBuffer |
                     ControlStyles.Selectable, true);

            scaler = new NearestScaler();
            size = new Size(2, 2);
            outputPixelData = new RgbColor[16];
            pixelDataHandle = GCHandle.Alloc(outputPixelData, GCHandleType.Pinned);
            outputPixels = new Bitmap(2, 2, PixelFormat.Format32bppRgb);
            Timer timer = new Timer();
            timer.Tick += Timer_Tick;
            timer.Interval = 1000;
            timer.Start();
        }
        #endregion

        public bool ShowFPS { get; set; }

        public bool ExactScaling { get; set; }

        public bool SmoothPixels
        {
            set
            {
                interpolationMode = value ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
                smoothingMode = value ? SmoothingMode.AntiAlias : SmoothingMode.None;
            }
        }

        public ScalingMode ScalingMode
        {
            set
            {
                switch (value)
                {
                    case ScalingMode.CRT: scaler = new CRTScaler(); break;
                    case ScalingMode.Hqx2x: scaler = new Hqx2xScaler(); break;
                    default: scaler = new NearestScaler(); break;
                }
            }
        }

        public void SetPixelDisplay(IPixelDisplay newDisplay)
        {
            if (display != null)
                display.FrameFinished -= Display_FrameFinished;

            display = newDisplay;
            size = display.Size;

            lock (imageSyncObj)
            {
                outputPixels.Dispose();
                pixelDataHandle.Free();

                outputPixelData = new RgbColor[(size.Width * outputScaleFactor) * (size.Height * outputScaleFactor)];
                pixelDataHandle = GCHandle.Alloc(outputPixelData, GCHandleType.Pinned);
                outputPixels = new Bitmap(size.Width * outputScaleFactor, size.Height * outputScaleFactor, size.Width * outputScaleFactor * 4, PixelFormat.Format32bppRgb,
                    pixelDataHandle.AddrOfPinnedObject());

                inputPixelData = new RgbColor[size.Width * size.Height];
            }

            display.FrameFinished += Display_FrameFinished;
        }

        public void RefreshDisplay()
        {
            if (display == null)
                return;

            lock (imageSyncObj)
                display.GetPixels(inputPixelData);

            scaler.Scale(inputPixelData, size, outputPixelData, new Size(size.Width * outputScaleFactor, size.Height * outputScaleFactor));

            paintCount++;
            Invalidate(false);
        }

        #region Overridden methods
        protected override void Dispose(bool disposing)
        {
            lock (imageSyncObj)
            {
                outputPixels.Dispose();
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
            e.Graphics.Clear(Color.DarkSlateGray);

            float ratioX = 0.0f;
            float ratioY = 0.0f;
            if (ExactScaling)
            {
                ratioX = Width / (size.Width * outputScaleFactor);
                ratioY = Height / (size.Height * outputScaleFactor);
            }
            
            if (ratioX == 0.0f || ratioY == 0.0f)
            {
                ratioX = Width / (float)(size.Width * outputScaleFactor);
                ratioY = Height / (float)(size.Height * outputScaleFactor);
            }

            float scale = ratioX > ratioY ? ratioY : ratioX;
            int outputWidth = (int)(size.Width * scale * outputScaleFactor);
            int outputHeight = (int)(size.Height * scale * outputScaleFactor);

            e.Graphics.CompositingMode = CompositingMode.SourceCopy;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.InterpolationMode = interpolationMode;
            e.Graphics.SmoothingMode = smoothingMode;
            Rectangle imageRect = new Rectangle((Width - outputWidth) / 2, (Height - outputHeight) / 2, outputWidth, outputHeight);
            lock (imageSyncObj)
                e.Graphics.DrawImage(outputPixels, imageRect);
            
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.SmoothingMode = SmoothingMode.None;
            using (Pen p = new Pen(Color.FloralWhite))
                e.Graphics.DrawRectangle(p, imageRect.X, imageRect.Y, imageRect.Width + 1, imageRect.Height + 1);

            if (ShowFPS)
            {
                e.Graphics.CompositingMode = CompositingMode.SourceOver;
                using (Brush b = new SolidBrush(Color.White))
                    e.Graphics.DrawString(fpsStr, Font, b, 5, 5);
            }
        }
        #endregion

        #region Event handlers
        private void Display_FrameFinished()
        {
            BeginInvoke(new Action(RefreshDisplay));
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            fpsStr = "FPS: " + paintCount;
            paintCount = 0;
        }
        #endregion
    }
}
