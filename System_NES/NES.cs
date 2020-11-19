using System.Collections.Generic;
using SystemBase;
using SystemBase.Bus;
using SystemBase.CPUs;
using SystemBase.RAM;

namespace System_NES
{
    public sealed class NES : ISystem
    {
        private readonly CPU_6502 cpu;
        private readonly Bus_16 bus;
        private readonly RAM_16 ram;
        private readonly PPU ppu;
        private readonly IController[] controllers = new IController[2];
        private Cartridge cartridge;

        public NES()
        {
            //Clock = new SystemClock(2000000);
            Clock = new SystemClock(5369318); // 1/4 speed of real system, but emulate-able
            //Clock = new SystemClock(21477272);
            
            bus = new Bus_16();
            cpu = new CPU_6502(Clock, bus, 3); // 3x slower than PPU
            ppu = new PPU(Clock, cpu);
            ram = new RAM_16(2040);

            bus.AddComponent(cpu, new BusAddressRange_16(ushort.MaxValue, 0));
            bus.AddComponent(ram, new BusAddressRange_16(0x0000, 0x1FFF));
            bus.AddComponent(ppu, new BusAddressRange_16(0x2000, 0x3FFF));
            
            controllers[0] = new Controller(0x4016);
            controllers[1] = new Controller(0x4017);
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

        public IEnumerable<IDisplay> OtherDisplayableComponents
        {
            get
            {
                foreach (IPixelDisplay display in ppu.PatternTableDisplays)
                    yield return display;
            }
        }
        
        public string AcceptableFileExtensionsForPrograms => "NES Files (*.nes)|*.nes";

        public bool LoadProgramFile(string filePath)
        {
            cartridge = new Cartridge(filePath);
            bus.AddComponent(cartridge, new BusAddressRange_16(0x4020, 0xFFFF));
            
            ppu.SetCartridge(cartridge);

            return true;
        }

        public void Start()
        {
            ppu.Start();
            cpu.Start();
            Clock.Start();
            
            cpu.Reset();
        }

        public void Stop()
        {
            Clock.Dispose();
            cpu.Dispose();
            ppu.Dispose();
        }
        #endregion
    }
}
