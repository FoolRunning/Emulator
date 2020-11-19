﻿using System.Collections.Generic;

namespace SystemBase
{
    public interface ISystem
    {
        IEnumerable<IController> Controllers { get; }

        SystemClock Clock { get; }

        ICPU CPU { get; }
        
        IBus Bus { get; }
        
        IRAM RAM { get; }

        IPixelDisplay MainDisplay { get; }

        IEnumerable<IDisplay> OtherDisplayableComponents { get; }

        /// <summary>
        /// In the form {description1}|*.{ext1}|{description2}|*.{ext2}|etc...
        /// </summary>
        string AcceptableFileExtensionsForPrograms { get; }

        bool LoadProgramFile(string filePath);

        void Start();

        void Stop();
    }
}