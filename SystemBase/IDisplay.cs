﻿using System;
using System.Drawing;

namespace SystemBase
{
    public enum PFormat
    {
        // Same values in .Net framework for easy conversion
        // TODO: Handle other pixel formats
        Format32bppRgb = 139273,
        Format8bppIndexed = 198659
    }

    public interface IDisplay
    {
        event Action FrameFinished;

        string Title { get; }

        Size Size { get; }
    }

    public interface ITextDisplay : IDisplay
    {
        string Text { get; }

        Color Color { get; }
    }

    public interface IPixelDisplay : IDisplay
    {
        void GetPixels(RgbColor[] pixelsReturn);
    }
}
