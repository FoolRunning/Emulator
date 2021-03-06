﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SystemBase;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System_NES
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal sealed class PPU : ClockListener, IBusComponent, IPixelDisplay
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

        private static readonly byte[] finalPxDataLookup = new byte[512];
        private static RgbColor[] pallete = 
        {
            new RgbColor(84 ,84 , 84),new RgbColor(0  ,30 ,116),new RgbColor(8  ,16 ,144),new RgbColor(48 ,0  ,136),new RgbColor(68 ,0  ,100),new RgbColor(92 ,0  , 48),new RgbColor(84 ,4  ,  0),new RgbColor(60 ,24 ,  0),new RgbColor(32 ,42 ,  0),new RgbColor(8  ,58 ,  0),new RgbColor(0  ,64 ,  0),new RgbColor(0  ,60 ,  0),new RgbColor(0  ,50 , 60),new RgbColor(0  , 0 ,  0),new RgbColor(0  , 0 ,  0),new RgbColor(0  , 0 ,  0),
            new RgbColor(152,150,152),new RgbColor(8  ,76 ,196),new RgbColor(48 ,50 ,236),new RgbColor(92 ,30 ,228),new RgbColor(136,20 ,176),new RgbColor(160,20 ,100),new RgbColor(152,34 , 32),new RgbColor(120,60 ,  0),new RgbColor(84 ,90 ,  0),new RgbColor(40 ,114,  0),new RgbColor(8  ,124,  0),new RgbColor(0  ,118, 40),new RgbColor(0  ,102,120),new RgbColor(0  , 0 ,  0),new RgbColor(0  , 0 ,  0),new RgbColor(0  , 0 ,  0),
            new RgbColor(236,238,236),new RgbColor(76 ,154,236),new RgbColor(120,124,236),new RgbColor(176,98 ,236),new RgbColor(228,84 ,236),new RgbColor(236,88 ,180),new RgbColor(236,106,100),new RgbColor(212,136, 32),new RgbColor(160,170,  0),new RgbColor(116,196,  0),new RgbColor(76 ,208, 32),new RgbColor(56 ,204,108),new RgbColor(56 ,180,204),new RgbColor(60 ,60 , 60),new RgbColor(0  , 0 ,  0),new RgbColor(0  , 0 ,  0),
            new RgbColor(236,238,236),new RgbColor(168,204,236),new RgbColor(188,188,236),new RgbColor(212,178,236),new RgbColor(236,174,236),new RgbColor(236,174,212),new RgbColor(236,180,176),new RgbColor(228,196,144),new RgbColor(204,210,120),new RgbColor(180,222,120),new RgbColor(168,226,144),new RgbColor(152,226,180),new RgbColor(160,214,228),new RgbColor(160,162,160),new RgbColor(0  , 0 ,  0),new RgbColor(0  , 0 ,  0),
        };

        private readonly PatternTableDisp[] patternTableDisplay = new PatternTableDisp[2];
        private readonly SystemBus bus;
        private IEnumerator<ClockTick> renderFrame;
#if DEBUG
        private int debugCycle = -2; // A couple ticks to get going
        private int debugScanLine;
#endif

        private ushort pixelByteOffset;
        private RgbColor[] displayedBuffer;
        private RgbColor[] notDisplayedBuffer;
        private bool oddFrame;
        private Cartridge cartridge;

        private readonly byte[] palleteTable = new byte[32];
        private readonly byte[][] nameTable = new byte[2][];
        private readonly byte[] patternTable = new byte[Utils.Kilo8];
        private readonly byte[] mainOAM = new byte[64 * 4]; // 64 sprites = 256 bytes
        private readonly byte[] secondaryOAM = new byte[8 * 4]; // 8 sprites = 32 bytes
        private byte registerStatus;
        private volatile byte registerControl;
        private volatile byte registerMask;
        private byte oamAddress;
        private bool byteLatch;
        private byte dataBuffer;

        private volatile byte fineX;
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

        private readonly object registerStatusSyncLock = new object();
        #endregion
        
        #region Constructors
        static PPU()
        {
            for (int bgPx = 0; bgPx < 4; bgPx++)
            {
                for (int bgPal = 0; bgPal < 4; bgPal++)
                {
                    for (int fgPx = 0; fgPx < 4; fgPx++)
                    {
                        for (int fgPal = 0; fgPal < 4; fgPal++)
                        {
                            for (int pri = 0; pri < 2; pri++)
                            {
                                int finalPixel = bgPx | fgPx;
                                int finalPallete = 0x00;
                                int allowSprZ = 0;
                                if (bgPx == 0 && fgPx != 0)
                                    finalPallete = fgPal + 0x04;
                                else if (bgPx != 0 && fgPx == 0)
                                    finalPallete = bgPal;
                                else if (bgPx != 0 && fgPx != 0)
                                {
                                    if (pri == 0)
                                    {
                                        finalPixel = fgPx;
                                        finalPallete = fgPal + 0x04;
                                    }
                                    else
                                    {
                                        finalPixel = bgPx;
                                        finalPallete = bgPal;
                                    }
                                    allowSprZ = 1;
                                }

                                int index = (pri << 8) | (fgPal << 6) | (fgPx << 4) | (bgPal << 2) | bgPx;
                                finalPxDataLookup[index] = (byte)((allowSprZ << 6) | (finalPallete << 3) | finalPixel);
                            }
                        }
                    }
                }
            }
        }

        public PPU(IClock clock, SystemBus bus) : base(clock)
        {
            this.bus = bus;

            nameTable[0] = new byte[1024];
            nameTable[1] = new byte[1024];

            Size = new Size(256, 240);
            
            displayedBuffer = new RgbColor[Size.Width * Size.Height];
            notDisplayedBuffer = new RgbColor[Size.Width * Size.Height];
            patternTableDisplay[0] = new PatternTableDisp(this, 0);
            patternTableDisplay[1] = new PatternTableDisp(this, 1);
            
            renderFrame = RenderFrame(false).GetEnumerator();
        }
        #endregion
        
        #region IPixelDisplay implementation
        public string Title => "Main display";

        public Size Size { get; }

        public void GetPixels(RgbColor[] pixelsReturn)
        {
            Array.Copy(displayedBuffer, 0, pixelsReturn, 0, displayedBuffer.Length);
        }
        #endregion

        #region IBusComponent implementation
        public void Reset()
        {
            Array.Clear(nameTable[0], 0, nameTable[0].Length);
            Array.Clear(nameTable[1], 0, nameTable[1].Length);
            Array.Clear(patternTable, 0, patternTable.Length);
            Array.Clear(mainOAM, 0, mainOAM.Length);
            Array.Clear(secondaryOAM, 0, secondaryOAM.Length);
            
            registerControl = 0x00;
            registerMask = 0x00;
            lock (registerStatusSyncLock)
                registerStatus = 0x00;

            oamAddress = 0x00;
            byteLatch = false;
            dataBuffer = 0x00;
            
            fineX = 0;
            registerT.Reg = 0;
            registerV.Reg = 0;
        }

        public void WriteDataFromBus(uint address, byte data)
        {
            switch (address)
            {
                case 0x2000: // Control
                    registerControl = data;
                    registerT.NameTableX = HasControlFlag(Control.NameTableX) ? (byte)1 : (byte)0;
                    registerT.NameTableY = HasControlFlag(Control.NameTableY) ? (byte)1 : (byte)0;
                    spriteSize = HasControlFlag(Control.SpriteSize) ? (byte)16 : (byte)8;
                    return;
                case 0x2001: // Mask
                    registerMask = data; 
                    return;
                case 0x2002: // Status (read-only)
                    return;
                case 0x2003: // OAM Address
                    oamAddress = data;
                    return;
                case 0x2004: // OAM Data
                    mainOAM[oamAddress++] = data;
                    return;
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
                    return;
                case 0x2006: // PPU Address
                    if (!byteLatch)
                        registerT.Reg = (ushort)((registerT.Reg & 0x00FF) | (data << 8));
                    else
                    {
                        registerT.Reg = (ushort)((registerT.Reg & 0xFF00) | data);
                        registerV = registerT;
                    }

                    byteLatch = !byteLatch;
                    return;
                case 0x2007: // PPU Data
                    WritePPUData(registerV.Reg, data);
                    if (registerV.Reg >= 0x3F00 && registerV.Reg <= 0x3F03)
                    {
                        patternTableDisplay[0].DataChanged();
                        patternTableDisplay[1].DataChanged();
                    }
                    registerV.Reg += (ushort)(HasControlFlag(Control.IncrementMode) ? 32 : 1);
                    return;
            }
        }

        public byte ReadDataForBus(uint address)
        {
            byte data;
            switch (address)
            {
                case 0x2000: // Control
                    return registerControl;
                case 0x2001: // Mask
                    return registerMask;
                case 0x2002: // Status
                    data = dataBuffer;
                    byteLatch = false;
                    lock (registerStatusSyncLock)
                    {
                        data = (byte)((registerStatus & 0xE0) | (data & 0x1F));
                        registerStatus.ClearFlag(Status.VerticalBlank);
                    }
                    return data;
                case 0x2003: // OAM Address (write-only)
                    return 0;
                case 0x2004: // OAM Data
                    return mainOAM[oamAddress];
                case 0x2005: // Scroll (write-only)
                    return 0;
                case 0x2006: // PPU Address (write-only)
                    return 0;
                case 0x2007: // PPU Data
                    data = dataBuffer;
                    dataBuffer = ReadPPUData(registerV.Reg);
                    if (registerV.Reg >= 0x3F00) // This range is instantaneous.
                        data = dataBuffer;
                    registerV.Reg += (ushort)(HasControlFlag(Control.IncrementMode) ? 32 : 1);
                    return data;
            }
            return 0;
        }
        #endregion

        public IEnumerable<IPixelDisplay> PatternTableDisplays => patternTableDisplay;

        public void SetCartridge(Cartridge cart)
        {
            cartridge = cart;
            patternTableDisplay[0].DataChanged();
            patternTableDisplay[1].DataChanged();
        }
        
        #region Main PPU loop
        protected override void HandleSingleTick()
        {
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

        private IEnumerable<ClockTick> RenderFrame(bool isOddFrame)
        {
            if (!isOddFrame)
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

            if (spriteCountNextScanLine > 8)
            {
                lock (registerStatusSyncLock)
                    registerStatus.SetFlag(Status.SpriteOverFlow);
                spriteCountNextScanLine = 8;
            }

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
            lock (registerStatusSyncLock)
                registerStatus.SetFlag(Status.VerticalBlank);

            if (HasControlFlag(Control.EnableNMI))
                bus.NMI<ICPU>();

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
            lock (registerStatusSyncLock)
                registerStatus.ClearFlag(Status.VerticalBlank | Status.SpriteOverFlow | Status.SpriteZeroHit);

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
                return patternTable[address];
            
            if (address <= 0x3EFF)
            {
                address &= 0x0FFF;
                if (cartridge.Mirror == MirrorMode.OneScreenLo)
                {
                    return nameTable[0][address & 0x03FF];
                }
                if (cartridge.Mirror == MirrorMode.OneScreenHigh)
                {
                    return nameTable[1][address & 0x03FF];
                }
                
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
                patternTable[address] = data;
                patternTableDisplay[(address & 0x1000) >> 12].DataChanged();
            }
            else if (address <= 0x3EFF)
            {
                address &= 0x0FFF;
                if (cartridge.Mirror == MirrorMode.OneScreenLo)
                {
                    nameTable[0][address & 0x03FF] = data;
                }
                else if (cartridge.Mirror == MirrorMode.OneScreenHigh)
                {
                    nameTable[1][address & 0x03FF] = data;
                }
                else if (cartridge.Mirror == MirrorMode.Vertical)
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
            byte xOffset = fineX; // for thread safety
            ushort bit = (ushort)(0x8000 >> xOffset);
            ushort shift = (byte)(15 - xOffset);
            byte bgPixel = (byte)(((bgShifterPatternHigh & bit) >> (shift - 1)) | ((bgShifterPatternLow & bit) >> shift));
            byte bgPallete = (byte)(((bgShifterAttributeHigh & bit) >> (shift - 1)) | ((bgShifterAttributeLow & bit) >> shift));
            bgPixel = (byte)(bgPixel & MaskMask(Mask.RenderBackground));

            byte fgPixel = 0x00;
            byte fgPallete = 0x00;
            byte fgPriority = 0x00;
            bool spriteZeroBeingRendered = false;
            for (int i = 0; i < spriteCountThisScanLine; i++)
            {
                if (fgSpriteCounter[i] != 0)
                    continue;

                fgPixel = (byte)(((fgShifterSpriteHigh[i] & 0x80) >> 6) | ((fgShifterSpriteLow[i] & 0x80) >> 7));
                fgPallete = (byte)(fgSpriteAttribute[i] & 0x03);
                fgPriority = (byte)((fgSpriteAttribute[i] & 0x20) >> 5);
                if (fgPixel != 0)
                {
                    spriteZeroBeingRendered = (i == 0);
                    break;
                }
            }
            fgPixel = (byte)(fgPixel & MaskMask(Mask.RenderSprites));

            ushort index = (ushort)((fgPriority << 8) | (fgPallete << 6) | (fgPixel << 4) | (bgPallete << 2) | bgPixel);
            byte pxData = finalPxDataLookup[index];
            byte palleteIndex = (byte)((pxData >> 3) & 0x07);
            byte pixel = (byte)(pxData & 0x03);
            notDisplayedBuffer[pixelByteOffset++] = GetColorFromPalleteRam(palleteIndex, pixel);

            if ((pxData & 0b1000000) != 0 && spriteZeroBeingRendered && spriteZeroHitPossibleThisScanLine)
            {
                lock (registerStatusSyncLock)
                    registerStatus.SetFlag(Status.SpriteZeroHit);
            }

            //notDisplayedBuffer[pixelByteOffset++] = GetColorFromPalleteRam(finalPallete, pixelByteOffset % 5 == 0 ? (byte)1 : (byte)0); // finalPixel);
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
                    address = (ushort)((HasControlFlag(Control.PatternBackground) ? (1 << 12) : 0) + (bgNextTileId << 4) + registerV.FineY);
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
                ushort attributeAddress = HasControlFlag(Control.PatternSprite) ? (ushort)0x1000 : (ushort)0;   
                if (fgSpriteAttribute[spriteIndex].HasFlag(SpriteAttribute.FlipVertically))
                    return (ushort)(attributeAddress | (tileId << 4) | (7 - yOffset));

                return (ushort)(attributeAddress | (tileId << 4) | yOffset);
            }

            // spriteSize == 16
            if (fgSpriteAttribute[spriteIndex].HasFlag(SpriteAttribute.FlipVertically))
            {
                if (yOffset < 8)
                    return (ushort)(((tileId & 0x01) << 12) | (((tileId & 0xFE) + 1) << 4) | (7 -(yOffset & 0x07)));

                return (ushort)(((tileId & 0x01) << 12) | ((tileId & 0xFE) << 4) | (7 -(yOffset & 0x07)));
            }

            if (yOffset < 8)
                return (ushort)(((tileId & 0x01) << 12) | ((tileId & 0xFE) << 4) | (yOffset & 0x07));

            return (ushort)(((tileId & 0x01) << 12) | (((tileId & 0xFE) + 1) << 4) | (yOffset & 0x07));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadBackgroundShifters()
        {
            bgShifterPatternLow = (ushort)((bgShifterPatternLow & 0xFF00) | bgNextTileLow);
            bgShifterPatternHigh = (ushort)((bgShifterPatternHigh & 0xFF00) | bgNextTileHigh);
            bgShifterAttributeLow = (ushort)((bgShifterAttributeLow & 0xFF00) | ((bgNextTileAttribute & 0b01) != 0 ? 0xFF : 0x00));
            bgShifterAttributeHigh = (ushort)((bgShifterAttributeHigh & 0xFF00) | ((bgNextTileAttribute & 0b10) != 0 ? 0xFF : 0x00));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateBackgroundShifters()
        {
            //if (!HasMaskFlag(Mask.RenderBackground)) 
            //    return;

            bgShifterPatternLow <<= 1;
            bgShifterPatternHigh <<= 1;
            bgShifterAttributeLow <<= 1;
            bgShifterAttributeHigh <<= 1;
        }

        private void UpdateForegroundShifters()
        {
            //if (!HasMaskFlag(Mask.RenderSprites))
            //    return;

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
            if (!HasMaskFlag(Mask.RenderBackground | Mask.RenderSprites))
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
            if (!HasMaskFlag(Mask.RenderBackground | Mask.RenderSprites))
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
            if (!HasMaskFlag(Mask.RenderBackground | Mask.RenderSprites))
                return;

            registerV.NameTableX = registerT.NameTableX;
            registerV.CoarseX = registerT.CoarseX;
        }

        private void TransferAddressY()
        {
            if (!HasMaskFlag(Mask.RenderBackground | Mask.RenderSprites))
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
            RgbColor[] temp = displayedBuffer;
            displayedBuffer = notDisplayedBuffer;
            notDisplayedBuffer = temp;
        }

        private RgbColor GetColorFromPalleteRam(byte palleteIndex, byte pixel)
        {
            return pallete[ReadPPUData((ushort)(0x3F00 + (palleteIndex << 2) + pixel)) & 0x3F];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte MaskMask(int mask)
        {
            byte val = (byte)(registerMask & mask);
            val = (byte)(val | (val >> 1) | (val << 7));
            val = (byte)(val | (val >> 1) | (val << 7));
            val = (byte)(val | (val >> 1) | (val << 7));
            val = (byte)(val | (val >> 1) | (val << 7));
            val = (byte)(val | (val >> 1) | (val << 7));
            val = (byte)(val | (val >> 1) | (val << 7));
            val = (byte)(val | (val >> 1) | (val << 7));
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasMaskFlag(byte maskValue)
        {
            return (registerMask & maskValue) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasControlFlag(byte maskValue)
        {
            return (registerControl & maskValue) != 0;
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
            private readonly Buffer2D patternTableBuffer;
            private readonly PPU ppu;

            public PatternTableDisp(PPU ppu, byte patternTableIndex)
            {
                this.ppu = ppu;
                this.patternTableIndex = patternTableIndex;
                patternTableBuffer = new Buffer2D(128, 128);
            }

            public event Action FrameFinished;

            public string Title => "Pattern Table " + patternTableIndex;

            public Size Size => new Size(128, 128);

            public void GetPixels(RgbColor[] pixelsReturn)
            {
                if (ppu.cartridge != null)
                    UpdatePatternTable(patternTableIndex, 0);
                Array.Copy(patternTableBuffer.InternalBuffer, 0, pixelsReturn, 0, patternTableBuffer.InternalBuffer.Length);
            }

            public void DataChanged()
            {
                UpdatePatternTable(patternTableIndex, 0);
                FrameFinished?.Invoke();
            }

            private void UpdatePatternTable(byte tableIndex, byte palleteIndex)
            {
                Buffer2D pt = patternTableBuffer;
                int memOffset = tableIndex * 0x1000;
                for (int tileY = 0; tileY < 16; tileY++)
                {
                    for (int tileX = 0; tileX < 16; tileX++)
                    {
                        int offset = memOffset + tileY * 256 + tileX * 16;
                        for (int row = 0; row < 8; row++)
                        {
                            byte low = ppu.ReadPPUData((ushort)(offset + row));
                            byte high = ppu.ReadPPUData((ushort)(offset + row + 8));

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
