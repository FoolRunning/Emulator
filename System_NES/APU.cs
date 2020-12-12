using System;
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

        private const int CPUClockRate = 1789773;
        private const int SampleRate = 44100;
        private const double TimePerSample = 1.0 / SampleRate;
        private const double TimePerClock = 1.0 / (CPUClockRate / 2.0);

        private readonly byte[] pulseSequence = { 0b00000001, 0b00000011, 0b00001111, 0b11111100 };
        private readonly float[] pulseDutyCycle = { 0.125f, 0.250f, 0.500f, 0.750f };

        private readonly float[] buffer = new float[8192];
        private double timeForNextSoundSample;
        private double totalTime;
        private ulong writeBufferOffset;
        private ulong readBufferOffset;

        private uint frameClockCounter;
        private float pulse1Sample;

        private readonly Sequencer pulse1Sequencer = new Sequencer();
        private readonly Sequencer pulse2Sequencer = new Sequencer();
        private readonly Sequencer triangleSequencer = new Sequencer();
        private readonly SquareWaveGenerator pulse1Generator = new SquareWaveGenerator();
        private readonly SquareWaveGenerator pulse2Generator = new SquareWaveGenerator();
        private readonly TriangleWaveGenerator triangleGenerator = new TriangleWaveGenerator();

        private volatile byte registerPulse1Duty;
        private volatile byte registerPulse2Duty;
        private volatile byte registerEnabled;
        private volatile bool dmcInterrupt;

        private static readonly float[] pulseMixerLookup = new float[31];
        private static readonly float[] tndMixerLookup = new float[203];

        static APU()
        {
            for (int i = 1; i < pulseMixerLookup.Length; i++)
                pulseMixerLookup[i] = 95.52f / (8128.0f / i + 100.0f);

            for (int i = 1; i < tndMixerLookup.Length; i++)
                tndMixerLookup[i] = 163.67f / (24329.0f / i + 100.0f);
        }

        public APU(IClock clock) : base(clock, 894886, "APU")
        {
        }
        
        #region IBusComponent_16 implementation
        public void Reset()
        {
            registerEnabled = 0x00;
            registerPulse1Duty = 0x00;
            registerPulse2Duty = 0x00;

            pulse1Sequencer.Enabled = false;
            pulse2Sequencer.Enabled = false;
            triangleSequencer.Enabled = false;
            
            pulse1Generator.Enabled = false;
            pulse1Generator.Frequency = 0;
            pulse1Generator.DutyCycle = 0.5f;
            
            pulse2Generator.Enabled = false;
            pulse2Generator.Frequency = 0;
            pulse2Generator.DutyCycle = 0.5f;
            

            triangleGenerator.Enabled = false;
            triangleGenerator.Frequency = 0;
            triangleGenerator.DutyCycle = 0.5f;
        }

        public void WriteDataFromBus(ushort address, byte data)
        {
            switch (address)
            {
                case 0x4000:
                    registerPulse1Duty = data;
                    pulse1Sequencer.Sequence = pulseSequence[(data & 0xC0) >> 6];
                    pulse1Generator.DutyCycle = pulseDutyCycle[(data & 0xC0) >> 6];
                    break;
                case 0x4001:
                    break;
                case 0x4002:
                    pulse1Sequencer.Reload = (ushort)((pulse1Sequencer.Reload & 0xFF00) | data);
                    break;
                case 0x4003:
                    pulse1Sequencer.Reload = (ushort)(((data & 0x07) << 8) | (pulse1Sequencer.Reload & 0x00FF));
                    pulse1Sequencer.Timer = pulse1Sequencer.Reload;
                    pulse1Generator.Frequency = CPUClockRate / (16.0f * pulse1Sequencer.Reload + 1);
                    break;

                case 0x4004:
                    registerPulse2Duty = data;
                    pulse2Sequencer.Sequence = pulseSequence[(data & 0xC0) >> 6];
                    pulse2Generator.DutyCycle = pulseDutyCycle[(data & 0xC0) >> 6];
                    break;
                case 0x4005:
                    break;
                case 0x4006:
                    pulse2Sequencer.Reload = (ushort)((pulse2Sequencer.Reload & 0xFF00) | data);
                    break;
                case 0x4007:
                    pulse2Sequencer.Reload = (ushort)(((data & 0x07) << 8) | (pulse2Sequencer.Reload & 0x00FF));
                    pulse2Sequencer.Timer = pulse2Sequencer.Reload;
                    pulse2Generator.Frequency = CPUClockRate / (16.0f * pulse2Sequencer.Reload + 1);
                    break;

                case 0x4008:
                    break;
                case 0x4009:
                    break;
                case 0x400A:
                    triangleSequencer.Reload = (ushort)((triangleSequencer.Reload & 0xFF00) | data);
                    break;
                case 0x400B:
                    triangleSequencer.Reload = (ushort)(((data & 0x07) << 8) | (triangleSequencer.Reload & 0x00FF));
                    triangleSequencer.Timer = triangleSequencer.Reload;
                    triangleGenerator.Frequency = CPUClockRate / (32.0f * triangleSequencer.Reload + 1);
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
                    registerEnabled = data;
                    dmcInterrupt = false;
                    pulse1Sequencer.Enabled = data.HasFlag(EnabledFlag.Pulse1);
                    pulse2Sequencer.Enabled = data.HasFlag(EnabledFlag.Pulse2);
                    triangleSequencer.Enabled = data.HasFlag(EnabledFlag.Triangle);

                    pulse1Generator.Enabled = data.HasFlag(EnabledFlag.Pulse1);
                    pulse2Generator.Enabled = data.HasFlag(EnabledFlag.Pulse2);
                    triangleGenerator.Enabled = data.HasFlag(EnabledFlag.Triangle);
                    break;
                case 0x4017: // Frame counter
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
            if (readBufferOffset >= writeBufferOffset)
                readBufferOffset = writeBufferOffset - 100;

            float data = buffer[readBufferOffset % (ulong)buffer.Length];
            readBufferOffset++;
            return data;
        }

        //public IEnumerable<ISoundChannelGenerator> InputChannels
        //{
        //    get
        //    {
        //        yield return pulse1Generator;
        //        yield return pulse2Generator;
        //        yield return triangleGenerator;
        //    }
        //}
        #endregion

        #region ClockListener implementation
        protected override void HandleSingleTick()
        {
            //bool quarterFrameClock = false;
            //bool halfFrameClock = false;

            //frameClockCounter++;

            //if (frameClockCounter == 3729)
            //    quarterFrameClock = true;
            //else if (frameClockCounter == 7457)
            //{
            //    quarterFrameClock = true;
            //    halfFrameClock = true;
            //}
            //else if (frameClockCounter == 11186)
            //    quarterFrameClock = true;
            //else if (frameClockCounter == 14915)
            //{
            //    quarterFrameClock = true;
            //    halfFrameClock = true;
            //    frameClockCounter = 0;
            //}

            //if (quarterFrameClock)
            //{

            //}

            //if (halfFrameClock)
            //{

            //}

            pulse1Sequencer.Clock((ref uint sequence) =>
            {
                sequence = ((sequence & 0x0001) << 7) | ((sequence & 0x00FE) >> 1);
            });

            totalTime += TimePerClock;
            if (totalTime >= timeForNextSoundSample)
            {
                float newSampleValue = GetMixedSoundSample((float)totalTime, (float)TimePerSample);
                buffer[writeBufferOffset % (ulong)buffer.Length] = (newSampleValue + prevSampleValue) / 2.0f;
                prevSampleValue = newSampleValue;
                writeBufferOffset++;
                timeForNextSoundSample += TimePerSample;
            }
        }
        #endregion

        private float prevSampleValue;

        private float GetMixedSoundSample(float globalTime, float timeStep)
        {
            int pulse1 = (int)((pulse1Generator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            int pulse2 = (int)((pulse2Generator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            int triangle = (int)((triangleGenerator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            //int pulse1 = (pulse1Sequencer.Output & 0x01) * 15;
            //int pulse2 = 0; //(int)((pulse2Generator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            //int triangle = 0; //(int)((triangleGenerator.GetSample(0, globalTime, timeStep) + 1.0f) * 7.5f);
            const int noise = 0; // TODO
            const int dmc = 0; // TODO

            float pulseOut = pulse1 == 0 && pulse2 == 0 ? 0.0f :
                95.88f / (8128.0f / (pulse1 + pulse2) + 100.0f);
            float tndOut = triangle == 0 && noise == 0 && dmc == 0 ? 0.0f :
                159.79f / (1.0f / (triangle / 8227.0f + noise / 12241.0f + dmc / 22638.0f));
            return pulseOut + tndOut;
            //return pulseMixerLookup[pulse1 + pulse2] + tndMixerLookup[3 * triangle + 2 * noise + dmc];
        }

        private delegate void SequencerAction(ref uint sequence);

        private sealed class Sequencer
        {
            public bool Enabled;
            public uint Sequence;
            public ushort Timer;
            public ushort Reload;
            public volatile byte Output;

            public void Clock(SequencerAction function)
            {
                if (!Enabled || Reload < 8)
                {
                    Output = 0;
                    return;
                }

                Timer--;
                if (Timer == 0xFFFF)
                {
                    Timer = (ushort)(Reload + 1);
                    function(ref Sequence);
                    Output = (byte)(Sequence & 0x01);
                }
            }
        }
        
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
    }
}
