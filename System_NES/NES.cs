using System.Collections.Generic;
using SystemBase;

namespace System_NES
{
    public sealed class NES : ISystem
    {
        #region Member variables
        private readonly CPU_2A03 cpu;
        private readonly SystemBus bus;
        private readonly PPU ppu;
        private readonly APU apu;
        private readonly IController[] controllers = new IController[2];
        private Cartridge cartridge;
        private bool running;
        private readonly SystemClock clock;
        #endregion

        #region Constructor
        public NES()
        {
            //clock = new SystemClock(200000);
            clock = new SystemClock(5369318); // 1/4 speed of real system clock, but emulate-able
            //clock = new SystemClock(21477272); // True speed

            bus = new SystemBus(0xFFFF);
            RAM ram = new RAM(2048);
            ppu = new PPU(clock, bus);
            cpu = new CPU_2A03(new ClockDivider(clock, 3), bus); // 3x slower than PPU
            apu = new APU(new ClockDivider(clock, 3)); // 3x slower than PPU
            controllers[0] = new Controller(0x4016);
            controllers[1] = new Controller(0x4017);

            bus.AddComponent(ram, new BusAddressRange(0x0000, 0x1FFF));
            bus.AddComponent(ppu, new BusAddressRange(0x2000, 0x3FFF));
            bus.AddComponent(apu, new BusAddressRange(0x4000, 0x4013));
            bus.AddComponent(cpu, new BusAddressRange(0x4014, 0x4014));
            bus.AddComponent(apu, new BusAddressRange(0x4015, 0x4015));
            bus.AddComponent((Controller)controllers[0], new BusAddressRange(0x4016, 0x4016));
            bus.AddComponent((Controller)controllers[1], new BusAddressRange(0x4017, 0x4017));
            bus.AddComponent(apu, new BusAddressRange(0x4017, 0x4017));
        }
        #endregion

        #region ISystemInfo implementation
        public IEnumerable<IController> Controllers => controllers;

        public ICPU CPU => cpu;

        public SystemBus Bus => bus;

        public IPixelDisplay MainDisplay => ppu;

        public ISoundProvider SoundGenerator => apu;

        public IEnumerable<IDisplay> OtherDisplayableComponents
        {
            get
            {
                foreach (IPixelDisplay display in ppu.PatternTableDisplays)
                    yield return display;

                yield return new TickCountDisplay(clock,  
                    new[] { "Main", "PPU", "CPU", "APU" }, 
                    new ITickProvider[] { clock, ppu, cpu, apu });
            }
        }
        
        public string AcceptableFileExtensionsForPrograms => "NES Files (*.nes)|*.nes";

        public bool LoadProgramFile(string filePath)
        {
            if (cartridge != null)
                bus.RemoveComponent(cartridge);

            cartridge = new Cartridge(filePath, bus);
            bus.AddComponent(cartridge, new BusAddressRange(0x4020, 0xFFFF));
            
            ppu.SetCartridge(cartridge);

            return true;
        }

        public void Start()
        {
            if (running)
                return;

            apu.Start();
            ppu.Start();
            cpu.Start();
            clock.Start();
            running = true;
        }

        public void Stop()
        {
            clock.Dispose();
            cpu.Dispose();
            ppu.Dispose();
            apu.Dispose();
            running = false;
        }
        #endregion
    }
}
