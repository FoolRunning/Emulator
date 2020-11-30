using System.Collections.Generic;
using SystemBase;
using SystemBase.Bus;
using SystemBase.RAM;

namespace System_NES
{
    public sealed class NES : ISystem
    {
        private readonly CPU_2A03 cpu;
        private readonly Bus_16 bus;
        private readonly RAM_16 ram;
        private readonly PPU ppu;
        private readonly APU apu;
        private readonly IController[] controllers = new IController[2];
        private Cartridge cartridge;
        private bool running;

        public NES()
        {
            Clock = new SystemClock(100000);
            //Clock = new SystemClock(5369318); // 1/4 speed of real system, but emulate-able
            //Clock = new SystemClock(21477272); // True speed
            //Clock = new SystemClock(10000000);

            bus = new Bus_16();
            cpu = new CPU_2A03(new ClockDivider(Clock, 3), bus); // 3x slower than PPU
            ppu = new PPU(Clock, cpu);
            ram = new RAM_16(2048);
            apu = new APU();
            controllers[0] = new Controller(0x4016);
            controllers[1] = new Controller(0x4017);

            bus.AddComponent(ram, new BusAddressRange_16(0x0000, 0x1FFF));
            bus.AddComponent(ppu, new BusAddressRange_16(0x2000, 0x3FFF));
            bus.AddComponent(apu, new BusAddressRange_16(0x4000, 0x4013));
            bus.AddComponent(cpu, new BusAddressRange_16(0x4014, 0x4014));
            bus.AddComponent(apu, new BusAddressRange_16(0x4015, 0x4015));
            bus.AddComponent(apu, new BusAddressRange_16(0x4017, 0x4017));
            bus.AddComponent((Controller)controllers[0], new BusAddressRange_16(0x4016, 0x4016));
            bus.AddComponent((Controller)controllers[1], new BusAddressRange_16(0x4017, 0x4017));
        }

        #region ISystemInfo implementation
        public IEnumerable<IController> Controllers => controllers;

        public SystemClock Clock { get; }

        public ICPU CPU => cpu;

        public IBus Bus => bus;

        public IRAM RAM => ram;

        public IPixelDisplay MainDisplay => ppu;

        public ISoundGenerator SoundGenerator => apu;

        public IEnumerable<IDisplay> OtherDisplayableComponents
        {
            get
            {
                //foreach (IPixelDisplay display in ppu.PatternTableDisplays)
                //    yield return display;

                yield return new TickCountDisplay("Clock Speed", Clock, Clock);
                yield return new TickCountDisplay("CPU Speed", cpu, Clock);
            }
        }
        
        public string AcceptableFileExtensionsForPrograms => "NES Files (*.nes)|*.nes";

        public bool LoadProgramFile(string filePath)
        {
            cpu.Pause();
            if (cartridge != null)
                bus.RemoveComponent(cartridge);

            cartridge = new Cartridge(filePath);
            bus.AddComponent(cartridge, new BusAddressRange_16(0x4020, 0xFFFF));
            
            ppu.SetCartridge(cartridge);

            cpu.Resume();
            cpu.Reset();

            return true;
        }

        public void Start()
        {
            if (running)
                return;

            ppu.Start();
            cpu.Start();
            Clock.Start();
            running = true;
        }

        public void Stop()
        {
            Clock.Dispose();
            cpu.Dispose();
            ppu.Dispose();
            running = false;
        }
        #endregion
    }
}
