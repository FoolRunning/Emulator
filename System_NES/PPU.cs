using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Threading;
using SystemBase;
using System.Diagnostics;

namespace System_NES
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal sealed class PPU : IPixelDisplay, IBusComponent_16
    {
        #region Status enumeration
        private static class Status
        {
            public const byte SpriteOverFlow = 1 << 5;
            public const byte SpriteZeroHit  = 1 << 6;
            public const byte VerticalBlank  = 1 << 7;
        }
        #endregion

        #region Mask enumeration
        private static class Mask
        {
            public const byte GrayScale            = 1 << 0;
            public const byte RenderBackgroundLeft = 1 << 1;
            public const byte RenderSpritesLeft    = 1 << 2;
            public const byte RenderBackground     = 1 << 3;
            public const byte RenderSprites        = 1 << 4;
            public const byte EnhanceRed           = 1 << 5;
            public const byte EnhanceGreen         = 1 << 6;
            public const byte EnhanceBlue          = 1 << 7;
        }
        #endregion

        #region Control enumeration
        private static class Control
        {
            public const byte NameTableX        = 1 << 0;
            public const byte NameTableY        = 1 << 1;
            public const byte IncrementMode     = 1 << 2;
            public const byte PatternSprite     = 1 << 3;
            public const byte PatternBackground = 1 << 4;
            public const byte SpriteSize        = 1 << 5;
            public const byte SlaveMode         = 1 << 6;
            public const byte EnableNMI         = 1 << 7;
        }
        #endregion

        #region SpriteAttribute enumeration
        private static class SpriteAttribute
        {
            public const byte Pallete = 0x04; // First 2 bits
            public const byte Unused = 0x1C; // Next 3 bits
            public const byte Priority = 1 << 5;
            public const byte FlipHorizontally = 1 << 6;
            public const byte FlipVertically = 1 << 7;
        }
        #endregion

        #region Member variables
        private const int maxSprites = 8;

        public event Action FrameFinished;

        private readonly PatternTableDisp[] patternTableDisplay = new PatternTableDisp[2];
        private readonly ICPU cpu;
        private readonly IClock clock;
        private readonly Thread ppuThread;
        private long totalTicks;
        private volatile int ticksToRun;
        private volatile bool run;
        private IEnumerator<ClockTick> renderFrame;
#if DEBUG
        private int debugCycle = -2; // A couple ticks to get going
        private int debugScanLine;
#endif

        private ushort pixelByteOffset;
        private byte[] displayedBuffer;
        private byte[] notDisplayedBuffer;
        private Cartridge cartridge;

        private readonly byte[] palleteTable = new byte[32];
        private readonly byte[][] nameTable = new byte[2][];
        private readonly byte[][] patternTable = new byte[2][];
        private readonly byte[] mainOAM = new byte[64 * 4]; // 64 sprites = 256 bytes
        private readonly byte[] secondaryOAM = new byte[8 * 4]; // 8 sprites = 32 bytes
        private byte registerStatus;
        private byte registerMask;
        private byte registerControl;
        private byte oamAddress;
        private bool byteLatch;
        private byte dataBuffer;

        private byte fineX;
        private LoopyRegister registerT;
        private LoopyRegister registerV;

        private byte bgNextTileId;
        private byte bgNextTileAttribute;
        private byte bgNextTileLow;
        private byte bgNextTileHigh;

        private ushort bgShifterPatternLow;
        private ushort bgShifterPatternHigh;
        private ushort bgShifterAttributeLow;
        private ushort bgShifterAttributeHigh;

        private byte spriteEvalIndex;
        private byte spriteCountNextScanLine;
        private byte spriteCountThisScanLine;
        private bool spriteZeroHitPossibleNextScanLine;
        private bool spriteZeroHitPossibleThisScanLine;
        private byte spriteSize;

        private readonly byte[] fgShifterSpriteLow = new byte[maxSprites];
        private readonly byte[] fgShifterSpriteHigh = new byte[maxSprites];
        private readonly byte[] fgSpriteAttribute = new byte[maxSprites];
        private readonly byte[] fgSpriteCounter = new byte[maxSprites];
        private readonly object registerSyncLock = new object();
        #endregion
        
        #region Constructor
        public PPU(IClock clock, ICPU cpu)
        {
            this.clock = clock;
            this.cpu = cpu;

            nameTable[0] = new byte[1024];
            nameTable[1] = new byte[1024];
            patternTable[0] = new byte[4096];
            patternTable[1] = new byte[4096];

            Size = new Size(256, 240);
            Pallete = new[] // Pallete for NES is only 64 colors, but .Net needs 255 colors
            {
                Color.FromArgb(84 ,84 , 84),Color.FromArgb(0  ,30 ,116),Color.FromArgb(8  ,16 ,144),Color.FromArgb(48 ,0  ,136),Color.FromArgb(68 ,0  ,100),Color.FromArgb(92 ,0  , 48),Color.FromArgb(84 ,4  ,  0),Color.FromArgb(60 ,24 ,  0),Color.FromArgb(32 ,42 ,  0),Color.FromArgb(8  ,58 ,  0),Color.FromArgb(0  ,64 ,  0),Color.FromArgb(0  ,60 ,  0),Color.FromArgb(0  ,50 , 60),Color.FromArgb(0  , 0 ,  0),Color.FromArgb(0  , 0 ,  0),Color.FromArgb(0  , 0 ,  0),
                Color.FromArgb(152,150,152),Color.FromArgb(8  ,76 ,196),Color.FromArgb(48 ,50 ,236),Color.FromArgb(92 ,30 ,228),Color.FromArgb(136,20 ,176),Color.FromArgb(160,20 ,100),Color.FromArgb(152,34 , 32),Color.FromArgb(120,60 ,  0),Color.FromArgb(84 ,90 ,  0),Color.FromArgb(40 ,114,  0),Color.FromArgb(8  ,124,  0),Color.FromArgb(0  ,118, 40),Color.FromArgb(0  ,102,120),Color.FromArgb(0  , 0 ,  0),Color.FromArgb(0  , 0 ,  0),Color.FromArgb(0  , 0 ,  0),
                Color.FromArgb(236,238,236),Color.FromArgb(76 ,154,236),Color.FromArgb(120,124,236),Color.FromArgb(176,98 ,236),Color.FromArgb(228,84 ,236),Color.FromArgb(236,88 ,180),Color.FromArgb(236,106,100),Color.FromArgb(212,136, 32),Color.FromArgb(160,170,  0),Color.FromArgb(116,196,  0),Color.FromArgb(76 ,208, 32),Color.FromArgb(56 ,204,108),Color.FromArgb(56 ,180,204),Color.FromArgb(60 ,60 , 60),Color.FromArgb(0  , 0 ,  0),Color.FromArgb(0  , 0 ,  0),
                Color.FromArgb(236,238,236),Color.FromArgb(168,204,236),Color.FromArgb(188,188,236),Color.FromArgb(212,178,236),Color.FromArgb(236,174,236),Color.FromArgb(236,174,212),Color.FromArgb(236,180,176),Color.FromArgb(228,196,144),Color.FromArgb(204,210,120),Color.FromArgb(180,222,120),Color.FromArgb(168,226,144),Color.FromArgb(152,226,180),Color.FromArgb(160,214,228),Color.FromArgb(160,162,160),Color.FromArgb(0  , 0 ,  0),Color.FromArgb(0  , 0 ,  0),
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,
                Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black,Color.Black
            };
            
            displayedBuffer = new byte[Size.Width * Size.Height];
            notDisplayedBuffer = new byte[Size.Width * Size.Height];
            patternTableDisplay[0] = new PatternTableDisp(this, 0);
            patternTableDisplay[1] = new PatternTableDisp(this, 1);

            run = true;
            ppuThread = new Thread(PPULoop);
            ppuThread.IsBackground = true;
            ppuThread.Name = "PPU";

            clock.ClockTick += Clock_ClockTick;
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            clock.ClockTick -= Clock_ClockTick;
            run = false;
            if (ppuThread.IsAlive)
                ppuThread.Join();
        }
        #endregion

        #region IPixelDisplay implementation
        public string Title => "Main display";

        public PFormat PixelFormat => PFormat.Format8bppIndexed;
        
        public Size Size { get; }

        public Color[] Pallete { get; }

        public void GetPixels(byte[] pixelsReturn)
        {
            Array.Copy(displayedBuffer, 0, pixelsReturn, 0, displayedBuffer.Length);
        }
        #endregion

        #region IBusComponent_16 implementation
        public void WriteDataFromBus(ushort address, byte data)
        {
            switch (address)
            {
                case 0x2000: // Control
                    registerControl = data;
                    registerT.NameTableX = registerControl.HasFlag(Control.NameTableX) ? (byte)1 : (byte)0;
                    registerT.NameTableY = registerControl.HasFlag(Control.NameTableY) ? (byte)1 : (byte)0;
                    spriteSize = registerControl.HasFlag(Control.SpriteSize) ? (byte)16 : (byte)8;
                    break;
                case 0x2001: // Mask
                    registerMask = data; 
                    break;
                case 0x2002: // Status (read-only)
                    break;
                case 0x2003: // OAM Address
                    oamAddress = data;
                    break;
                case 0x2004: // OAM Data
                    mainOAM[oamAddress] = data;
                    break;
                case 0x2005: // Scroll
                    if (!byteLatch)
                    {
                        fineX = (byte)(data & 0x07);
                        registerT.CoarseX = (byte)(data >> 3);
                    }
                    else
                    {
                        registerT.FineY = (byte)(data & 0x07);
                        registerT.CoarseY = (byte)(data >> 3);
                    }
                    byteLatch = !byteLatch;
                    break;
                case 0x2006: // PPU Address
                    if (!byteLatch)
                        registerT.Reg = (ushort)((registerT.Reg & 0x00FF) | (data << 8));
                    else
                    {
                        registerT.Reg = (ushort)((registerT.Reg & 0xFF00) | data);
                        registerV = registerT;
                    }

                    byteLatch = !byteLatch;
                    break;
                case 0x2007: // PPU Data
                    WritePPUData(registerV.Reg, data);
                    if (registerV.Reg >= 0x3F00 && registerV.Reg <= 0x3F03)
                    {
                        patternTableDisplay[0].DataChanged();
                        patternTableDisplay[1].DataChanged();
                    }
                    registerV.Reg += (ushort)(registerControl.HasFlag(Control.IncrementMode) ? 32 : 1);
                    break;
            }
        }

        public byte ReadDataForBus(ushort address)
        {
            byte data = 0x00;
            switch (address)
            {
                case 0x2000: // Control
                    return registerControl;
                case 0x2001: // Mask
                    return registerMask;
                case 0x2002: // Status
                    lock (registerSyncLock)
                    {
                        data = (byte) ((registerStatus & 0xE0) | (dataBuffer & 0x1F));
                        registerStatus.ClearFlag(Status.VerticalBlank);
                    }
                    byteLatch = false;
                    break;
                case 0x2003: // OAM Address (write-only)
                    break;
                case 0x2004: // OAM Data
                    return mainOAM[oamAddress];
                case 0x2005: // Scroll
                    return 0;
                case 0x2006: // PPU Address (write-only)
                    break;
                case 0x2007: // PPU Data
                    data = dataBuffer;
                    dataBuffer = ReadPPUData(registerV.Reg);
                    if (registerV.Reg >= 0x3F00) // This range is instantaneous.
                        data = dataBuffer;
                    registerV.Reg += (ushort)(registerControl.HasFlag(Control.IncrementMode) ? 32 : 1);
                    break;
            }
            return data;
        }
        #endregion

        public IEnumerable<IPixelDisplay> PatternTableDisplays => patternTableDisplay;

        public void Start()
        {
            ppuThread.Start();
        }

        public void SetCartridge(Cartridge cart)
        {
            cartridge = cart;
            patternTableDisplay[0].DataChanged();
            patternTableDisplay[1].DataChanged();
        }

        public void WriteOAM(byte address, byte data)
        {
            mainOAM[address] = data;
        }

        #region Event handlers
        private void Clock_ClockTick()
        {
            Interlocked.Increment(ref ticksToRun);

            while (ticksToRun > 2) // Prefer slowdown versus getting overwhelmed with ticks
            {
            }
        }
        #endregion

        #region Main PPU loop
        private void PPULoop()
        {
            ticksToRun = 0;

            //DateTime then = DateTime.Now.AddSeconds(1);
            //int tickCount = 0;
            renderFrame = RenderFrame(false).GetEnumerator();
            bool oddFrame = false;

            while (run)
            {
                if (ticksToRun > 0)
                {
                    Interlocked.Decrement(ref ticksToRun);
                    totalTicks++;

                    if (!renderFrame.MoveNext())
                    {
                        oddFrame = !oddFrame;
                        renderFrame = RenderFrame(oddFrame).GetEnumerator();
                        renderFrame.MoveNext(); // Still want to consume a clock tick
#if DEBUG
                        if (!oddFrame)
                            debugCycle--; // Extra clock tick used
#endif
                    }
#if DEBUG
                    debugCycle++;
                    if (debugCycle >= 341)
                    {
                        debugCycle = 0;
                        debugScanLine++;
                        if (debugScanLine >= 262)
                        {
                            debugScanLine = 0;
                        }
                    }
#endif
                }

                // DEBUG CODE ********************************************************************************************
                //DateTime now = DateTime.Now;
                //if (now >= then)
                //{
                //    Console.WriteLine($"{tickCount} - {ticksToRun}");
                //    tickCount = 0;
                //    then = now.AddSeconds(1);
                //}
                // DEBUG CODE ********************************************************************************************
            }
        }

        private IEnumerable<ClockTick> RenderFrame(bool oddFrame)
        {
            if (!oddFrame)
                yield return new ClockTick(); // even frames have an extra tick

            // Scanlines 0 to 239 do the drawing of the visible portion of the screen
            pixelByteOffset = 0;
            for (int scanLine = 0; scanLine <= 239; scanLine++)
            {
                foreach (ClockTick tick in ScanLineVisible(scanLine))
                    yield return tick;
            }

            // The frame is effectively finished (240 scanlines are finished),
            // so let the emulator draw it during the blank period.
            SwapBuffers();
            FrameFinished?.Invoke();

            // Scanline 240 does nothing
            for (int cycle = 0; cycle <= 340; cycle++)
                yield return new ClockTick();

            // Scanline 241 sets the vertical-blank flag
            foreach (ClockTick tick in ScanLineSetVerticalBlank())
                yield return tick;

            // Scanlines 242 to 260 do nothing
            for (int scanLine = 242; scanLine <= 260; scanLine++)
            {
                for (int cycle = 0; cycle <= 340; cycle++)
                    yield return new ClockTick();
            }

            // Scanline 261 clears the vertical-blank flag
            foreach (ClockTick tick in ScanLineClearVerticalBlank())
                yield return tick;
        }

        /// <summary>
        /// Handles scanlines 0 to 239
        /// </summary>
        private IEnumerable<ClockTick> ScanLineVisible(int scanLine)
        {
            yield return new ClockTick(); // Cycle 0 does nothing
            spriteCountThisScanLine = spriteCountNextScanLine;
            spriteCountNextScanLine = 0;
            spriteZeroHitPossibleThisScanLine = spriteZeroHitPossibleNextScanLine;
            spriteZeroHitPossibleNextScanLine = false;
#if DEBUG
            Debug.Assert(debugCycle == 0);
#endif

            // Cycles 1 to 64 render pixels and clear the secondary OAM memory
            ushort address = 0;
            for (int c = 0; c <= 63; c++) // c = cycle - 1
            {
                yield return new ClockTick();
                HandleBackgroundDataReads(c, ref address);
                DrawPixel();
                secondaryOAM[c >> 1] = 0xFF;
                UpdateForegroundShifters();
            }

            // Cycles 65 to 256 render pixels and evaluate sprites for next scanline
            int nextScanLine = scanLine;
            spriteEvalIndex = 0;
            for (int c = 64; c <= 255; c++) // c = cycle - 1
            {
                yield return new ClockTick();
                HandleBackgroundDataReads(c, ref address);
                DrawPixel();
                HandleEvaluateSprite(c, nextScanLine);
                UpdateForegroundShifters();
            }
            lock (registerSyncLock)
                registerStatus.SetOrClearFlag(Status.SpriteOverFlow, spriteCountNextScanLine > 8);
            if (spriteCountNextScanLine > 8)
                spriteCountNextScanLine = 8;

            Debug.Assert(spriteEvalIndex == 64);
            
#if DEBUG
            Debug.Assert(debugCycle == 256);
#endif
            IncrementScrollY();

            yield return new ClockTick(); // Cycle 257 load sprite data for next scanline
            for (int i = 0; i < spriteCountThisScanLine; i++)
            {
                fgSpriteCounter[i] = 0xFF;
                fgShifterSpriteHigh[i] = 0;
                fgShifterSpriteLow[i] = 0;
                fgSpriteAttribute[i] = 0;
            }
            LoadBackgroundShifters();
            TransferAddressX();
            HandleSpriteReads(256, nextScanLine, ref address);
            
            // Cycles 258 to 320 load sprite data for next scanline
            for (int c = 257; c <= 319; c++) // c = cycle - 1
            {
                yield return new ClockTick();
                HandleSpriteReads(c, nextScanLine, ref address);
            }

#if DEBUG
            Debug.Assert(debugCycle == 320);
#endif

            // Cycles 321 to 336 do data-reads in preparation for the next scanline
            for (int c = 320; c <= 335; c++) // c = cycle - 1
            {
                yield return new ClockTick();
                HandleBackgroundDataReads(c, ref address);
            }

            yield return new ClockTick(); // Cycle 337 reads tile ID
            bgNextTileId = ReadPPUData((ushort)(0x2000 | (registerV.Reg & 0x0FFF)));

            yield return new ClockTick(); // Cycle 338 does nothing

            yield return new ClockTick(); // Cycle 339 reads tile ID
            bgNextTileId = ReadPPUData((ushort)(0x2000 | (registerV.Reg & 0x0FFF)));

            yield return new ClockTick(); // Cycle 340 does nothing
#if DEBUG
            Debug.Assert(debugCycle == 340);
#endif
        }

        /// <summary>
        /// Handles scanline 241
        /// </summary>
        private IEnumerable<ClockTick> ScanLineSetVerticalBlank()
        {
            yield return new ClockTick(); // Cycle 0 does nothing
            
            yield return new ClockTick(); // Cycle 1 sets VBlank flag and optionally triggers NMI
            lock (registerSyncLock)
                registerStatus.SetFlag(Status.VerticalBlank);

            if (registerControl.HasFlag(Control.EnableNMI))
                cpu.NMI();

            // Rest of the cycles do nothing
            for (int c = 2; c <= 340; c++)
                yield return new ClockTick();
        }

        /// <summary>
        /// Handles scanline 261
        /// </summary>
        private IEnumerable<ClockTick> ScanLineClearVerticalBlank()
        {
            yield return new ClockTick(); // Cycle 0 does nothing
            
            yield return new ClockTick(); // Cycle 1 clears vertical blank, sprite overflow, sprite zero hit
            lock (registerSyncLock)
            {
                registerStatus.ClearFlag(Status.VerticalBlank);
                registerStatus.ClearFlag(Status.SpriteOverFlow);
                registerStatus.ClearFlag(Status.SpriteZeroHit);
            }

            // Cycles 2 to 279 do nothing
            for (int c = 2; c <= 279; c++)
                yield return new ClockTick();

            // Cycles 280 to 304 transfer address
            for (int c = 280; c <= 304; c++)
            {
                yield return new ClockTick();
                TransferAddressY();
            }

            // Cycles 305 to 320 do nothing
            for (int c = 305; c <= 320; c++)
                yield return new ClockTick();

            // Cycles 321 to 336 do data-reads in preparation for the next frame
            ushort address = 0;
            for (int i = 0; i < 16; i++)
            {
                yield return new ClockTick();
                HandleBackgroundDataReads(i, ref address);
            }

            yield return new ClockTick(); // Cycle 337 reads tile ID
            bgNextTileId = ReadPPUData((ushort)(0x2000 | (registerV.Reg & 0x0FFF)));

            yield return new ClockTick(); // Cycle 338 does nothing

            yield return new ClockTick(); // Cycle 339 reads tile ID
            bgNextTileId = ReadPPUData((ushort)(0x2000 | (registerV.Reg & 0x0FFF)));

            yield return new ClockTick(); // Cycle 340 does nothing
        }
        #endregion

        #region PPU Memory read/write
        private byte ReadPPUData(ushort address)
        {
            address &= 0x3FFF;
            if (cartridge.ReadPPUData(address, out byte data))
                return data;

            if (address <= 0x1FFF)
                return patternTable[(address & 0x1000) >> 12][address & 0x0FFF];
            
            if (address <= 0x3EFF)
            {
                address &= 0x0FFF;
                if (cartridge.Mirror == MirrorMode.Vertical)
                {
                    if (address <= 0x03FF)
                        return nameTable[0][address & 0x03FF];
                    if (address <= 0x07FF)
                        return nameTable[1][address & 0x03FF];
                    if (address <= 0x0BFF)
                        return nameTable[0][address & 0x03FF];
                    if (address <= 0x0FFF)
                        return nameTable[1][address & 0x03FF];
                }
                else if (cartridge.Mirror == MirrorMode.Horizontal)
                {
                    if (address <= 0x03FF)
                        return nameTable[0][address & 0x03FF];
                    if (address <= 0x07FF)
                        return nameTable[0][address & 0x03FF];
                    if (address <= 0x0BFF)
                        return nameTable[1][address & 0x03FF];
                    if (address <= 0x0FFF)
                        return nameTable[1][address & 0x03FF];
                }
            }
            else if (address <= 0x3FFF)
                return palleteTable[HandlePalleteAddressMirror(address)];

            return 0;
        }

        private void WritePPUData(ushort address, byte data)
        {
            if (cartridge.WritePPUData(address, data))
                return;

            if (address <= 0x1FFF)
            {
                int index = (address & 0x1000) >> 12;
                patternTable[index][address & 0x0FFF] = data;
                patternTableDisplay[index].DataChanged();
            }
            else if (address <= 0x3EFF)
            {
                address &= 0x0FFF;
                if (cartridge.Mirror == MirrorMode.Vertical)
                {
                    if (address <= 0x03FF)
                        nameTable[0][address & 0x03FF] = data;
                    else if (address <= 0x07FF)
                        nameTable[1][address & 0x03FF] = data;
                    else if (address <= 0x0BFF)
                        nameTable[0][address & 0x03FF] = data;
                    else if (address <= 0x0FFF)
                        nameTable[1][address & 0x03FF] = data;
                }
                else if (cartridge.Mirror == MirrorMode.Horizontal)
                {
                    if (address <= 0x03FF)
                        nameTable[0][address & 0x03FF] = data;
                    else if (address <= 0x07FF)
                        nameTable[0][address & 0x03FF] = data;
                    else if (address <= 0x0BFF)
                        nameTable[1][address & 0x03FF] = data;
                    else if (address <= 0x0FFF)
                        nameTable[1][address & 0x03FF] = data;
                }
            }
            else if (address <= 0x3FFF)
            {
                palleteTable[HandlePalleteAddressMirror(address)] = data;
            }
        }
        #endregion

        #region Private helper methods
        private void DrawPixel()
        {
            byte bgPixel = 0x00;
            byte bgPallete = 0x00;

            bool renderBackground = registerMask.HasFlag(Mask.RenderBackground);
            bool renderSprites = registerMask.HasFlag(Mask.RenderSprites);

            if (renderBackground)
            {
                ushort bit = (ushort)(0x8000 >> fineX);

                bgPixel = (byte)((((bgShifterPatternHigh & bit) != 0 ? (byte)1 : (byte)0) << 1) |
                    ((bgShifterPatternLow & bit) != 0 ? (byte)1 : (byte)0));
                bgPallete = (byte)((((bgShifterAttributeHigh & bit) != 0 ? (byte)1 : (byte)0) << 1) |
                    ((bgShifterAttributeLow & bit) != 0 ? (byte)1 : (byte)0));
            }

            byte fgPixel = 0x00;
            byte fgPallete = 0x00;
            bool fgPriority = false;
            bool spriteZeroBeingRendered = false;
            if (renderSprites)
            {
                for (int i = 0; i < spriteCountThisScanLine; i++)
                {
                    if (fgSpriteCounter[i] == 0)
                    {
                        fgPixel = (byte)((((fgShifterSpriteHigh[i] & 0x80) != 0 ? (byte)1 : (byte)0) << 1) |
                                         ((fgShifterSpriteLow[i] & 0x80) != 0 ? (byte)1 : (byte)0));
                        fgPallete = (byte)((fgSpriteAttribute[i] & 0x03) + 0x04);
                        fgPriority = (fgSpriteAttribute[i] & 0x20) == 0;
                        if (fgPixel != 0)
                        {
                            if (i == 0)
                                spriteZeroBeingRendered = true;
                            break;
                        }
                    }
                }
            }

            byte finalPixel = 0x00;
            byte finalPallete = 0x00;
            if (bgPixel == 0 && fgPixel == 0)
            {
                // nothing to do
            }
            else if (bgPixel == 0 && fgPixel != 0)
            {
                finalPixel = fgPixel;
                finalPallete = fgPallete;
            }
            else if (bgPixel != 0 && fgPixel == 0)
            {
                finalPixel = bgPixel;
                finalPallete = bgPallete;
            }
            else if (bgPixel != 0 && fgPixel != 0)
            {
                if (fgPriority)
                {
                    finalPixel = fgPixel;
                    finalPallete = fgPallete;
                }
                else
                {
                    finalPixel = bgPixel;
                    finalPallete = bgPallete;
                }

                if (spriteZeroHitPossibleThisScanLine && spriteZeroBeingRendered)
                {
                    lock (registerSyncLock)
                        registerStatus.SetFlag(Status.SpriteZeroHit);
                }
            }

            notDisplayedBuffer[pixelByteOffset++] = GetColorFromPalleteRam(finalPallete, finalPixel);
        }

        private void HandleBackgroundDataReads(int count, ref ushort address)
        {
            UpdateBackgroundShifters();

            switch (count % 8)
            {
                case 0:
                    LoadBackgroundShifters();
                    bgNextTileId = ReadPPUData((ushort)(0x2000 | (registerV.Reg & 0x0FFF)));
                    break;
                case 2:
                    bgNextTileAttribute = ReadPPUData((ushort)(0x23C0 | 
                        (registerV.NameTableY << 11) | (registerV.NameTableX << 10) |
                        ((registerV.CoarseY >> 2) << 3) | (registerV.CoarseX >> 2)));

                    if ((registerV.CoarseY & 0x02) != 0)
                        bgNextTileAttribute >>= 4;
                    
                    if ((registerV.CoarseX & 0x02) != 0)
                        bgNextTileAttribute >>= 2;
                    bgNextTileAttribute &= 0x03;
                    break;
                case 4:
                    address = (ushort)((registerControl.HasFlag(Control.PatternBackground) ? (1 << 12) : 0) + (bgNextTileId << 4) + registerV.FineY);
                    bgNextTileLow = ReadPPUData(address);
                    break;
                case 6:
                    bgNextTileHigh = ReadPPUData((ushort)(address + 8));
                    break;
                case 7:
                    IncrementScrollX();
                    break;
            }
        }

        private void HandleEvaluateSprite(int count, int nextScanLine)
        {
            if (count % 3 != 0) 
                return;

            int byteOffset = spriteEvalIndex << 2;
            spriteEvalIndex++;
            int spOffsetY = nextScanLine - mainOAM[byteOffset];
            if (spOffsetY < 0 || spOffsetY >= spriteSize)
                return;

            if (spriteCountNextScanLine < 8)
            {
                spriteZeroHitPossibleNextScanLine |= (byteOffset == 0);
                Array.Copy(mainOAM, byteOffset, secondaryOAM, spriteCountNextScanLine << 2, 4);
            }

            spriteCountNextScanLine++;
        }

        private void HandleSpriteReads(int count, int nextScanLine, ref ushort address)
        {
            Debug.Assert(count >= 256 && count <= 319);
            int spriteIndex = (count - 256) / 8;
            int byteOffset = spriteIndex * 4;
            byte data;
            switch (count % 8)
            {
                case 0:
                    ReadPPUData((ushort)(0x2000 | (registerV.Reg & 0x0FFF)));
                    fgSpriteAttribute[spriteIndex] = secondaryOAM[byteOffset + 2];
                    break;
                case 2:
                    ReadPPUData((ushort)(0x23C0 | 
                        (registerV.NameTableY << 11) | (registerV.NameTableX << 10) |
                        ((registerV.CoarseY >> 2) << 3) | (registerV.CoarseX >> 2)));
                    fgSpriteCounter[spriteIndex] = secondaryOAM[byteOffset + 3];
                    break;
                case 4:
                    address = GetSpriteAddress(spriteIndex, nextScanLine, byteOffset);
                    data = ReadPPUData(address);
                    fgShifterSpriteLow[spriteIndex] = fgSpriteAttribute[spriteIndex].HasFlag(SpriteAttribute.FlipHorizontally) ?
                        data.ReverseBits() : data;
                    break;
                case 6:
                    data = ReadPPUData((ushort)(address + 8));
                    fgShifterSpriteHigh[spriteIndex] = fgSpriteAttribute[spriteIndex].HasFlag(SpriteAttribute.FlipHorizontally) ?
                        data.ReverseBits() : data;
                    break;
            }
        }

        private ushort GetSpriteAddress(int spriteIndex, int nextScanLine, int byteOffset)
        {
            int yOffset = nextScanLine - secondaryOAM[byteOffset];
            byte tileId = secondaryOAM[byteOffset + 1];
            if (spriteSize == 8)
            {
                if (fgSpriteAttribute[spriteIndex].HasFlag(SpriteAttribute.FlipVertically))
                {
                    return (ushort)((registerControl.HasFlag(Control.PatternSprite) ? 0x1000 : 0) | 
                                    (tileId << 4) |
                                    (7 - yOffset));
                }

                return (ushort)((registerControl.HasFlag(Control.PatternSprite) ? 0x1000 : 0) | 
                                (tileId << 4) | 
                                yOffset);
            }

            // spriteSize == 16
            if (fgSpriteAttribute[spriteIndex].HasFlag(SpriteAttribute.FlipVertically))
            {
                if (yOffset < 8)
                    return (ushort)(((tileId & 0x01) << 12) | 
                                    (((tileId & 0xFE) + 1) << 4) | 
                                    (7 -(yOffset & 0x07)));

                return (ushort)(((tileId & 0x01) << 12) | 
                                ((tileId & 0xFE) << 4) | 
                                (7 -(yOffset & 0x07)));
            }

            if (yOffset < 8)
            {
                return (ushort)(((tileId & 0x01) << 12) | 
                                ((tileId & 0xFE) << 4) | 
                                (yOffset & 0x07));
            }

            return (ushort)(((tileId & 0x01) << 12) | 
                            (((tileId & 0xFE) + 1) << 4) | 
                            (yOffset & 0x07));
        }

        private void LoadBackgroundShifters()
        {
            bgShifterPatternLow = (ushort)((bgShifterPatternLow & 0xFF00) | bgNextTileLow);
            bgShifterPatternHigh = (ushort)((bgShifterPatternHigh & 0xFF00) | bgNextTileHigh);
            bgShifterAttributeLow = (ushort)((bgShifterAttributeLow & 0xFF00) | ((bgNextTileAttribute & 0b01) != 0 ? 0xFF : 0x00));
            bgShifterAttributeHigh = (ushort)((bgShifterAttributeHigh & 0xFF00) | ((bgNextTileAttribute & 0b10) != 0 ? 0xFF : 0x00));
        }

        private void UpdateBackgroundShifters()
        {
            if (!registerMask.HasFlag(Mask.RenderBackground)) 
                return;

            bgShifterPatternLow <<= 1;
            bgShifterPatternHigh <<= 1;
            bgShifterAttributeLow <<= 1;
            bgShifterAttributeHigh <<= 1;
        }

        private void UpdateForegroundShifters()
        {
            if (!registerMask.HasFlag(Mask.RenderSprites)) 
                return;

            for (int i = 0; i < spriteCountThisScanLine; i++)
            {
                if (fgSpriteCounter[i] > 0)
                    fgSpriteCounter[i]--;
                else
                {
                    fgShifterSpriteLow[i] <<= 1;
                    fgShifterSpriteHigh[i] <<= 1;
                }
            }
        }
        
        private void IncrementScrollX()
        {
            if (!registerMask.HasFlag(Mask.RenderBackground) && !registerMask.HasFlag(Mask.RenderSprites))
                return;

            if (registerV.CoarseX < 31)
                registerV.CoarseX++;
            else
            {
                registerV.CoarseX = 0;
                registerV.NameTableX = (byte)~registerV.NameTableX; // switch horizontal nametable
            }
        }

        private void IncrementScrollY()
        {
            if (!registerMask.HasFlag(Mask.RenderBackground) && !registerMask.HasFlag(Mask.RenderSprites))
                return;

            if (registerV.FineY < 7)
                registerV.FineY++;
            else
            {
                registerV.FineY = 0;
                if (registerV.CoarseY == 29) // End of viewable area
                {
                    registerV.CoarseY = 0;
                    registerV.NameTableY = (byte)~registerV.NameTableY; // switch vertical nametable
                }
                else if (registerV.CoarseY == 31) // Into the attribute memory, loop around in same nametable
                {
                    registerV.CoarseY = 0;
                }
                else
                    registerV.CoarseY++;
            }
        }

        private void TransferAddressX()
        {
            if (!registerMask.HasFlag(Mask.RenderBackground) && !registerMask.HasFlag(Mask.RenderSprites))
                return;

            registerV.NameTableX = registerT.NameTableX;
            registerV.CoarseX = registerT.CoarseX;
        }

        private void TransferAddressY()
        {
            if (!registerMask.HasFlag(Mask.RenderBackground) && !registerMask.HasFlag(Mask.RenderSprites))
                return;

            registerV.NameTableY = registerT.NameTableY;
            registerV.CoarseY = registerT.CoarseY;
            registerV.FineY = registerT.FineY;
        }

        private static ushort HandlePalleteAddressMirror(ushort address)
        {
            address &= 0x001F;
            if (address == 0x0010)
                address = 0x0000;
            else if (address == 0x0014)
                address = 0x0004;
            else if (address == 0x0018)
                address = 0x0008;
            else if (address == 0x001C)
                address = 0x000C;
            return address;
        }

        private void SwapBuffers()
        {
            byte[] temp = displayedBuffer;
            displayedBuffer = notDisplayedBuffer;
            notDisplayedBuffer = temp;
        }

        private byte GetColorFromPalleteRam(byte palleteIndex, byte pixel)
        {
            return ReadPPUData((ushort)(0x3F00 + (palleteIndex << 2) + pixel));
        }
        #endregion
        
        #region LoopyRegister structure
        private struct LoopyRegister
        {
            private ushort data;

            public ushort Reg
            {
                get => data;
                set => data = value;
            }

            public byte CoarseX
            {
                get => (byte)(data & 0x001F);
                set => data = (ushort)((data & 0xFFE0) | value & 0x001F);
            }

            public byte CoarseY
            {
                get => (byte)((data >> 5) & 0x001F);
                set => data = (ushort)((data & 0xFC1F) | ((value & 0x001F) << 5));
            }

            public byte NameTableX
            {
                get => (byte)((data >> 10) & 0x0001);
                set => data = (ushort)((data & 0xFBFF) | ((value & 0x0001) << 10));
            }

            public byte NameTableY
            {
                get => (byte)((data >> 11) & 0x0001);
                set => data = (ushort)((data & 0xF7FF) | ((value & 0x0001) << 11));
            }

            public byte FineY
            {
                get => (byte)((data >> 12) & 0x0007);
                set => data = (ushort)((data & 0x8FFF) | ((value & 0x0007) << 12));
            }
        }
        #endregion
        
        #region PatternTableDisp class
        private sealed class PatternTableDisp : IPixelDisplay
        {
            private readonly byte patternTableIndex;
            private readonly Buffer2D patternTableDisplay;
            private readonly PPU ppu;

            public PatternTableDisp(PPU ppu, byte patternTableIndex)
            {
                this.ppu = ppu;
                this.patternTableIndex = patternTableIndex;
                patternTableDisplay = new Buffer2D(128, 128);
            }

            public event Action FrameFinished;

            public string Title => "Pattern Table " + patternTableIndex;

            public PFormat PixelFormat => PFormat.Format8bppIndexed;
            
            public Size Size => new Size(128, 128);
            
            public Color[] Pallete => ppu.Pallete;

            public void GetPixels(byte[] pixelsReturn)
            {
                if (ppu.cartridge != null)
                    UpdatePatternTable(patternTableIndex, 0);
                Array.Copy(patternTableDisplay.InternalBuffer, 0, pixelsReturn, 0, patternTableDisplay.InternalBuffer.Length);
            }

            public void DataChanged()
            {
                UpdatePatternTable(patternTableIndex, 0);
                FrameFinished?.Invoke();
            }

            private void UpdatePatternTable(byte tableIndex, byte palleteIndex)
            {
                Buffer2D pt = patternTableDisplay;
                for (int tileY = 0; tileY < 16; tileY++)
                {
                    for (int tileX = 0; tileX < 16; tileX++)
                    {
                        int offset = tileY * 256 + tileX * 16;
                        for (int row = 0; row < 8; row++)
                        {
                            byte low = ppu.ReadPPUData((ushort)(tableIndex * 0x1000 + offset + row + 0));
                            byte high = ppu.ReadPPUData((ushort)(tableIndex * 0x1000 + offset + row + 8));

                            for (int col = 0; col < 8; col++)
                            {
                                byte pixel = (byte)(((high & 0x01) << 1) | (low & 0x01));

                                low >>= 1;
                                high >>= 1;

                                pt[(ushort)(tileX * 8 + (7 - col)), (ushort)(tileY * 8 + row)] =
                                    ppu.GetColorFromPalleteRam(palleteIndex, pixel);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
