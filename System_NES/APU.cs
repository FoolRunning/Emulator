using System;
using System.Collections.Generic;
using System.Threading;
using SystemBase;

namespace System_NES
{
    internal sealed class APU : ClockListener, IBusComponent_16, ISoundProvider
    {
        #region EnabledFlag enumeration
        private static class EnabledFlag
        {
            public const byte Pulse1 = (1 << 0);
            public const byte Pulse2 = (1 << 1);
            public const byte Triangle = (1 << 2);
            public const byte Noise = (1 << 3);
            public const byte DMC = (1 << 4);
        }
        #endregion

        private static class EnvelopeFlag
        {
            public const byte ConstantVolume = (1 << 4);
            public const byte Loop = (1 << 5);
        }
        
        private const int CPUClockRate = 1789773;
        private const int SampleRate = 44100;
        private const double TimePerSample = 1.0 / SampleRate;
        private const double TimePerClock = 1.0 / (CPUClockRate / 2.0);

        private static readonly float[] pulseMixerLookup = new float[31];
        private static readonly float[] tndMixerLookup = new float[203];
        private static readonly int[] lengthLookup = 
        {
            10, 254, 20,  2, 40,  4, 80,  6, 160,  8, 60, 10, 14, 12, 26, 14,
            12,  16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
        };

        private readonly byte[] pulseSequence = { 0b01000000, 0b01100000, 0b01111000, 0b10011111 };
        //private readonly byte[] pulseSequence = { 0b10111111, 0b10011111, 0b10000111, 0b01100000 };
        //private readonly float[] pulseDutyCycle = { 0.125f, 0.250f, 0.500f, 0.750f };

        private readonly float[] buffer = new float[Utils.Kilo16];
        private double timeForNextSoundSample;
        private double totalTime;
        private long writeBufferOffset;
        private long readBufferOffset;

        private bool fiveStepFrameCounter;
        private IEnumerator<ClockTick> frameCounter;

        private readonly PulseSequencer pulse1Sequencer = new PulseSequencer();
        private readonly PulseSequencer pulse2Sequencer = new PulseSequencer();
        private readonly PulseSequencer triangleSequencer = new PulseSequencer();
        //private readonly SquareWaveGenerator pulse1Generator = new SquareWaveGenerator();
        //private readonly SquareWaveGenerator pulse2Generator = new SquareWaveGenerator();
        //private readonly TriangleWaveGenerator triangleGenerator = new TriangleWaveGenerator();

        private volatile bool dmcInterrupt;

        #region Constructors
        static APU()
        {
            for (int i = 1; i < pulseMixerLookup.Length; i++)
                pulseMixerLookup[i] = 95.52f / (8128.0f / i + 100.0f);

            for (int i = 1; i < tndMixerLookup.Length; i++)
                tndMixerLookup[i] = 163.67f / (24329.0f / i + 100.0f);
        }

        public APU(IClock clock) : base(clock, CPUClockRate, "APU")
        {
            frameCounter = FrameCounter4StepMode().GetEnumerator();
        }
        #endregion
        
        #region IBusComponent_16 implementation
        public void Reset()
        {
            pulse1Sequencer.Enabled = false;
            pulse2Sequencer.Enabled = false;
            triangleSequencer.Enabled = false;
            
            //pulse1Generator.Enabled = false;
            //pulse1Generator.Frequency = 0;
            //pulse1Generator.DutyCycle = 0.5f;
            
            //pulse2Generator.Enabled = false;
            //pulse2Generator.Frequency = 0;
            //pulse2Generator.DutyCycle = 0.5f;
            

            //triangleGenerator.Enabled = false;
            //triangleGenerator.Frequency = 0;
            //triangleGenerator.DutyCycle = 0.5f;
        }

        public void WriteDataFromBus(ushort address, byte data)
        {
            switch (address)
            {
                case 0x4000:
                    pulse1Sequencer.Sequence = pulseSequence[(data & 0xC0) >> 6];
                    pulse1Sequencer.Volume = (byte)(data & 0xF);
                    pulse1Sequencer.ConstantVolume = data.HasFlag(EnvelopeFlag.ConstantVolume);
                    pulse1Sequencer.Loop = data.HasFlag(EnvelopeFlag.Loop);
                    //pulse1Generator.DutyCycle = pulseDutyCycle[(data & 0xC0) >> 6];
                    break;
                case 0x4001:
                    break;
                case 0x4002:
                    pulse1Sequencer.DutyCycle = (ushort)((pulse1Sequencer.DutyCycle & 0xFF00) | data);
                    break;
                case 0x4003:
                    pulse1Sequencer.Length = (ushort)lengthLookup[(data & 0xF8) >> 3];
                    pulse1Sequencer.DutyCycle = (ushort)(((data & 0x07) << 8) | (pulse1Sequencer.DutyCycle & 0x00FF));
                    pulse1Sequencer.Timer = pulse1Sequencer.DutyCycle;
                    pulse1Sequencer.Start = true;
                    //pulse1Generator.Frequency = CPUClockRate / (16.0f * pulse1Sequencer.DutyCycle + 1);
                    break;

                case 0x4004:
                    pulse2Sequencer.Sequence = pulseSequence[(data & 0xC0) >> 6];
                    pulse2Sequencer.Volume = (byte)(data & 0xF);
                    pulse2Sequencer.ConstantVolume = data.HasFlag(EnvelopeFlag.ConstantVolume);
                    pulse2Sequencer.Loop = data.HasFlag(EnvelopeFlag.Loop);
                    //pulse2Generator.DutyCycle = pulseDutyCycle[(data & 0xC0) >> 6];
                    break;
                case 0x4005:
                    break;
                case 0x4006:
                    pulse2Sequencer.DutyCycle = (ushort)((pulse2Sequencer.DutyCycle & 0xFF00) | data);
                    break;
                case 0x4007:
                    pulse2Sequencer.Length = (ushort)lengthLookup[(data & 0xF8) >> 3];
                    pulse2Sequencer.DutyCycle = (ushort)(((data & 0x07) << 8) | (pulse2Sequencer.DutyCycle & 0x00FF));
                    pulse2Sequencer.Timer = pulse2Sequencer.DutyCycle;
                    pulse2Sequencer.Start = true;
                    //pulse2Generator.Frequency = CPUClockRate / (16.0f * pulse2Sequencer.DutyCycle + 1);
                    break;

                case 0x4008:
                    break;
                case 0x4009:
                    break;
                case 0x400A:
                    triangleSequencer.DutyCycle = (ushort)((triangleSequencer.DutyCycle & 0xFF00) | data);
                    break;
                case 0x400B:
                    triangleSequencer.DutyCycle = (ushort)(((data & 0x07) << 8) | (triangleSequencer.DutyCycle & 0x00FF));
                    triangleSequencer.Timer = triangleSequencer.DutyCycle;
                    //triangleGenerator.Frequency = CPUClockRate / (32.0f * triangleSequencer.DutyCycle + 1);
                    break;

                case 0x400C:
                    break;
                case 0x400D:
                    break;
                case 0x400E:
                    break;
                case 0x400F:
                    break;

                case 0x4010:
                    break;
                case 0x4011:
                    break;
                case 0x4012:
                    break;
                case 0x4013:
                    break;

                case 0x4015: 
                    dmcInterrupt = false;
                    pulse1Sequencer.Enabled = data.HasFlag(EnabledFlag.Pulse1);
                    pulse2Sequencer.Enabled = data.HasFlag(EnabledFlag.Pulse2);
                    triangleSequencer.Enabled = data.HasFlag(EnabledFlag.Triangle);

                    //pulse1Generator.Enabled = data.HasFlag(EnabledFlag.Pulse1);
                    //pulse2Generator.Enabled = data.HasFlag(EnabledFlag.Pulse2);
                    //triangleGenerator.Enabled = data.HasFlag(EnabledFlag.Triangle);
                    break;
                case 0x4017: // Frame counter
                    fiveStepFrameCounter = (data & 0b10000000) != 0;
                    break;
            }
        }

        public byte ReadDataForBus(ushort address)
        {
            if (address == 0x4015)
                return 0;

            return 0;
        }
        #endregion

        #region ISoundProvider implementation
        public int ChannelCount => 1;

        public float GetSample(int channel, float globalTime, float timeStep)
        {
            long writeOffset = Interlocked.Read(ref writeBufferOffset);
            if (readBufferOffset >= writeOffset)
                readBufferOffset = Math.Max(writeOffset - 100, 0);

            float data = buffer[readBufferOffset % buffer.Length];
            readBufferOffset++;
            return data;
        }
        #endregion

        #region ClockListener implementation
        private float prevSampleValue;
        private bool isEvenTick;

        protected override void HandleSingleTick()
        {
            isEvenTick = !isEvenTick;
            if (!isEvenTick) // Most of the APU operates 1/2 the speed of the CPU
                return;

            if (!frameCounter.MoveNext())
            {
                frameCounter = fiveStepFrameCounter
                    ? FrameCounter5StepMode().GetEnumerator()
                    : FrameCounter4StepMode().GetEnumerator();
                frameCounter.MoveNext(); // Still want to consume a clock tick
            }

            pulse1Sequencer.MainTick();
            pulse2Sequencer.MainTick();

            totalTime += TimePerClock;
            if (totalTime >= timeForNextSoundSample)
            {
                float newSampleValue = GetMixedSoundSample((float)totalTime, (float)TimePerSample);

                //buffer[Interlocked.Read(ref writeBufferOffset) % buffer.Length] = (newSampleValue + prevSampleValue) / 2.0f;
                //prevSampleValue = newSampleValue;
                buffer[Interlocked.Read(ref writeBufferOffset) % buffer.Length] = newSampleValue;
                Interlocked.Increment(ref writeBufferOffset);
                timeForNextSoundSample += TimePerSample;
            }
        }

        private IEnumerable<ClockTick> FrameCounter4StepMode()
        {
            for (int c = 1; c <= 3728; c++)
                yield return new ClockTick();

            // cycle 3728.5 (3729) fires quarter-frame tick
            yield return new ClockTick();
            HandleQuarterFrameTick();

            for (int c = 3730; c <= 7456; c++)
                yield return new ClockTick();

            // cycle 7456.5 (7457) fires quarter-frame and half-frame ticks
            yield return new ClockTick();
            HandleQuarterFrameTick();
            HandleHalfFrameTick();

            for (int c = 7458; c <= 11185; c++)
                yield return new ClockTick();

            // cycle 11185.5 (11186) fires quarter-frame tick
            yield return new ClockTick();
            HandleQuarterFrameTick();

            for (int c = 11187; c <= 14914; c++)
                yield return new ClockTick();

            // cycle 14914.5 (14915) fires quarter-frame and half-frame ticks
            yield return new ClockTick();
            HandleQuarterFrameTick();
            HandleHalfFrameTick();
        }

        private IEnumerable<ClockTick> FrameCounter5StepMode()
        {
            for (int c = 1; c <= 3728; c++)
                yield return new ClockTick();

            // cycle 3728.5 (3729) fires quarter-frame tick
            yield return new ClockTick();
            HandleQuarterFrameTick();

            for (int c = 3730; c <= 7456; c++)
                yield return new ClockTick();

            // cycle 7456.5 (7457) fires quarter-frame and half-frame ticks
            yield return new ClockTick();
            HandleQuarterFrameTick();
            HandleHalfFrameTick();

            for (int c = 7458; c <= 11185; c++)
                yield return new ClockTick();

            // cycle 11185.5 (11186) fires quarter-frame tick
            yield return new ClockTick();
            HandleQuarterFrameTick();

            for (int c = 11187; c <= 18640; c++)
                yield return new ClockTick();

            // cycle 18640.5 (18641) fires quarter-frame and half-frame ticks
            yield return new ClockTick();
            HandleQuarterFrameTick();
            HandleHalfFrameTick();
        }

        private void HandleQuarterFrameTick()
        {
            pulse1Sequencer.QuarterTick();
            pulse2Sequencer.QuarterTick();
            triangleSequencer.QuarterTick();
        }

        private void HandleHalfFrameTick()
        {
            pulse1Sequencer.HalfTick();
            pulse2Sequencer.HalfTick();
            triangleSequencer.HalfTick();
        }
        #endregion

        private float GetMixedSoundSample(float globalTime, float timeStep)
        {
            //int pulse1 = (int)((pulse1Generator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            //int pulse2 = (int)((pulse2Generator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            //int triangle = (int)((triangleGenerator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            int pulse1 = pulse1Sequencer.FinalOutput();
            int pulse2 = pulse2Sequencer.FinalOutput();
            int triangle = 0; //(int)((triangleGenerator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            const int noise = 0; // TODO
            const int dmc = 0; // TODO

            //float pulseOut = pulse1 == 0 && pulse2 == 0 ? 0.0f :
            //    95.88f / (8128.0f / (pulse1 + pulse2) + 100.0f);
            //float tndOut = triangle == 0 && noise == 0 && dmc == 0 ? 0.0f :
            //    159.79f / (1.0f / (triangle / 8227.0f + noise / 12241.0f + dmc / 22638.0f));
            //return pulseOut + tndOut;
            return pulseMixerLookup[pulse1 + pulse2] + tndMixerLookup[3 * triangle + 2 * noise + dmc];
        }

        private sealed class PulseSequencer
        {
            public uint Sequence;
            public ushort Timer;
            public ushort DutyCycle;
            public bool Enabled;
            
            public byte Volume;
            public bool Start;
            public bool Loop;
            public bool ConstantVolume;

            public ushort Length;

            public bool SweepEnabled;
            public byte SweepDivider;
            public byte SweepBitShift;

            private byte volumeDivider;
            private byte decayCounter;
            
            private byte outputVolume;
            private byte sequencerOutput;
            
            public void MainTick()
            {
                Timer--;
                if (Timer == 0xFFFF)
                {
                    Timer = DutyCycle;
                    Sequence = ((Sequence & 0x0001) << 7) | ((Sequence & 0x00FE) >> 1);
                    sequencerOutput = (byte)(Sequence & 0x01);
                }
            }
            
            public void QuarterTick()
            {
                if (Start)
                {
                    Start = false;
                    decayCounter = 15;
                    volumeDivider = Volume;
                }
                else
                {
                    if (volumeDivider > 0)
                        volumeDivider--;
                    else
                    {
                        volumeDivider = Volume;

                        if (decayCounter > 0)
                            decayCounter--;
                        else if (Loop)
                            decayCounter = 15;
                    }
                }

                CalculateVolume();
            }

            public void HalfTick()
            {
                if (!Enabled)
                    Length = 0;

                if (Length > 0 && !Loop)
                    Length--;

                CalculateVolume();

                //ushort targetCycle = DutyCycle >> SweepBitShift;
            }

            public int FinalOutput()
            {
                return sequencerOutput * outputVolume;
            }

            private void CalculateVolume()
            {
                if (!Enabled || DutyCycle < 8 || DutyCycle > 0x7FF)
                    outputVolume = 0;
                else if (!Loop && Length == 0)
                    outputVolume = 0;
                else
                    outputVolume = ConstantVolume ? Volume : decayCounter;
            }
        }
        
        #region SquareWaveGenerator class
        private sealed class SquareWaveGenerator : ISoundChannelGenerator
        {
            private const float PI = (float)Math.PI;
            private const int Harmonics = 20;
            public volatile bool Enabled;
            public volatile float Frequency;
            public volatile float DutyCycle = 0.5f;
            public volatile float Amplitude = 1.0f;

            #region ISoundChannelGenerator implementation
            public float GetSample(int channel, float globalTime, float timeStep)
            {
                if (!Enabled || Frequency > 23000)
                    return 0.0f;

                float a = 0.0f;
                float b = 0.0f;
                float p = DutyCycle * 2.0f * PI;
                float offset = Frequency * 2.0f * PI * globalTime;

                for (int n = 1; n <= Harmonics; n++)
                {
                    float c = n * offset;
                    a += Utils.FastSine(c) / n;
                    b += Utils.FastSine(c - p * n) / n;
                }

                return 2.0f * Amplitude * (a - b) / PI;
            }
            #endregion
        }
        #endregion

        #region TriangleWaveGenerator class
        private sealed class TriangleWaveGenerator : ISoundChannelGenerator
        {
            private const float PI = (float)Math.PI;
            private const int Harmonics = 20;
            public volatile bool Enabled;
            public volatile float Frequency;
            public volatile float DutyCycle = 0.5f;
            public volatile float Amplitude = 1.0f;

            #region ISoundChannelGenerator implementation
            public float GetSample(int channel, float globalTime, float timeStep)
            {
                if (!Enabled || Frequency > 23000)
                    return 0.0f;

                float a = 0.0f;
                float b = 0.0f;
                float p = DutyCycle * 2.0f * PI;
                float offset = Frequency * 2.0f * PI * globalTime;

                for (int n = 1; n <= Harmonics; n++)
                {
                    float c = n * offset;
                    a += Utils.FastSine(c) / n;
                    b += Utils.FastSine(c - p * n) / n;
                }

                return 2.0f * Amplitude * (a - b) / PI;
            }
            #endregion
        }
        #endregion
    }
}
