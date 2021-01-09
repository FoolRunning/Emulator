using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SystemBase.CPUs
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CPU_6502 : ClockListener, ICPU, IBusComponent
    {
        #region Status enumeration
        private static class Status
        {
            public const byte Carry =       1 << 0;
            public const byte Zero =        1 << 1;
            public const byte IrqDisable =  1 << 2;
            public const byte DecimalMode = 1 << 3;
            public const byte BrkCommand =  1 << 4;
            public const byte Not_Used =    1 << 5;
            public const byte Overflow =    1 << 6;
            public const byte Negative =    1 << 7;
        }
        #endregion

        #region InterruptType enumeration
        private enum InterruptType
        {
            None,
            Normal,
            NonMaskable
        }
        #endregion

        #region Member variables
        private readonly Op resetOp;
        private readonly Op interruptOp;
        private readonly Op interruptNMIOp;
        private readonly Op[] opCodes;
        private readonly SystemBus bus;
        private volatile InterruptType interruptRequested = InterruptType.None;
        private volatile bool resetRequested;

        /// <summary>The current instruction being run.</summary>
        private Op currentInstruction;
        private IEnumerator<ClockTick> currentMicroInstruction;

        private ushort registerProgramCounter;
        private byte registerAccumulator;
        private byte registerX;
        private byte registerY;
        private byte registerStatus;
        private byte registerStackPointer;
        #endregion

        #region Constructor
        public CPU_6502(IClock clock, SystemBus bus) : base(clock)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));

            resetOp = new Op(Reset, IMP, false);
            interruptOp = new Op(Interrupt, IMP, false);
            interruptNMIOp = new Op(InterruptNMI, IMP, false);

            opCodes = new[]
            {
                new Op(BRK, IMP, false), new Op(ORA, IZX, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(ORA, ZP0, false), new Op(ASL, ZP0, false), new Op(X  , IMP, false), new Op(PHP, IMP, false), new Op(ORA, IMM, false), new Op(ASL, ACC, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(ORA, ABS, false), new Op(ASL, ABS, false), new Op(X  , IMP, false), 
                new Op(BPL, REL, false), new Op(ORA, IZY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(ORA, ZPX, false), new Op(ASL, ZPX, false), new Op(X  , IMP, false), new Op(CLC, IMP, false), new Op(ORA, ABY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(ORA, ABX, false), new Op(ASL, ABX, true ), new Op(X  , IMP, false), 
                new Op(JSR, ABS, false), new Op(AND, IZX, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(BIT, ZP0, false), new Op(AND, ZP0, false), new Op(ROL, ZP0, false), new Op(X  , IMP, false), new Op(PLP, IMP, false), new Op(AND, IMM, false), new Op(ROL, ACC, false), new Op(X  , IMP, false), new Op(BIT, ABS, false), new Op(AND, ABS, false), new Op(ROL, ABS, false), new Op(X  , IMP, false), 
                new Op(BMI, REL, false), new Op(AND, IZY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(AND, ZPX, false), new Op(ROL, ZPX, false), new Op(X  , IMP, false), new Op(SEC, IMP, false), new Op(AND, ABY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(AND, ABX, false), new Op(ROL, ABX, true ), new Op(X  , IMP, false), 
                new Op(RTI, IMP, false), new Op(EOR, IZX, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(EOR, ZP0, false), new Op(LSR, ZP0, false), new Op(X  , IMP, false), new Op(PHA, IMP, false), new Op(EOR, IMM, false), new Op(LSR, ACC, false), new Op(X  , IMP, false), new Op(JMP, ABS, false), new Op(EOR, ABS, false), new Op(LSR, ABS, false), new Op(X  , IMP, false), 
                new Op(BVC, REL, false), new Op(EOR, IZY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(EOR, ZPX, false), new Op(LSR, ZPX, false), new Op(X  , IMP, false), new Op(CLI, IMP, false), new Op(EOR, ABY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(EOR, ABX, false), new Op(LSR, ABX, true ), new Op(X  , IMP, false), 
                new Op(RTS, IMP, false), new Op(ADC, IZX, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(ADC, ZP0, false), new Op(ROR, ZP0, false), new Op(X  , IMP, false), new Op(PLA, IMP, false), new Op(ADC, IMM, false), new Op(ROR, ACC, false), new Op(X  , IMP, false), new Op(JMP, IND, false), new Op(ADC, ABS, false), new Op(ROR, ABS, false), new Op(X  , IMP, false), 
                new Op(BVS, REL, false), new Op(ADC, IZY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(ADC, ZPX, false), new Op(ROR, ZPX, false), new Op(X  , IMP, false), new Op(SEI, IMP, false), new Op(ADC, ABY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(ADC, ABX, false), new Op(ROR, ABX, true ), new Op(X  , IMP, false), 
                new Op(X  , IMP, false), new Op(STA, IZX, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(STY, ZP0, false), new Op(STA, ZP0, false), new Op(STX, ZP0, false), new Op(X  , IMP, false), new Op(DEY, IMP, false), new Op(X  , IMP, false), new Op(TXA, IMP, false), new Op(X  , IMP, false), new Op(STY, ABS, false), new Op(STA, ABS, false), new Op(STX, ABS, false), new Op(X  , IMP, false), 
                new Op(BCC, REL, false), new Op(STA, IZY, true ), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(STY, ZPX, false), new Op(STA, ZPX, false), new Op(STX, ZPY, false), new Op(X  , IMP, false), new Op(TYA, IMP, false), new Op(STA, ABY, true ), new Op(TXS, IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(STA, ABX, true ), new Op(X  , IMP, false), new Op(X  , IMP, false), 
                new Op(LDY, IMM, false), new Op(LDA, IZX, false), new Op(LDX, IMM, false), new Op(X  , IMP, false), new Op(LDY, ZP0, false), new Op(LDA, ZP0, false), new Op(LDX, ZP0, false), new Op(X  , IMP, false), new Op(TAY, IMP, false), new Op(LDA, IMM, false), new Op(TAX, IMP, false), new Op(X  , IMP, false), new Op(LDY, ABS, false), new Op(LDA, ABS, false), new Op(LDX, ABS, false), new Op(X  , IMP, false), 
                new Op(BCS, REL, false), new Op(LDA, IZY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(LDY, ZPX, false), new Op(LDA, ZPX, false), new Op(LDX, ZPY, false), new Op(X  , IMP, false), new Op(CLV, IMP, false), new Op(LDA, ABY, false), new Op(TSX, IMP, false), new Op(X  , IMP, false), new Op(LDY, ABX, false), new Op(LDA, ABX, false), new Op(LDX, ABY, false), new Op(X  , IMP, false), 
                new Op(CPY, IMM, false), new Op(CMP, IZX, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(CPY, ZP0, false), new Op(CMP, ZP0, false), new Op(DEC, ZP0, false), new Op(X  , IMP, false), new Op(INY, IMP, false), new Op(CMP, IMM, false), new Op(DEX, IMP, false), new Op(X  , IMP, false), new Op(CPY, ABS, false), new Op(CMP, ABS, false), new Op(DEC, ABS, false), new Op(X  , IMP, false), 
                new Op(BNE, REL, false), new Op(CMP, IZY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(CMP, ZPX, false), new Op(DEC, ZPX, false), new Op(X  , IMP, false), new Op(CLD, IMP, false), new Op(CMP, ABY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(CMP, ABX, false), new Op(DEC, ABX, true ), new Op(X  , IMP, false), 
                new Op(CPX, IMM, false), new Op(SBC, IZX, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(CPX, ZP0, false), new Op(SBC, ZP0, false), new Op(INC, ZP0, false), new Op(X  , IMP, false), new Op(INX, IMP, false), new Op(SBC, IMM, false), new Op(NOP, IMP, false), new Op(X  , IMP, false), new Op(CPX, ABS, false), new Op(SBC, ABS, false), new Op(INC, ABS, false), new Op(X  , IMP, false), 
                new Op(BEQ, REL, false), new Op(SBC, IZY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(SBC, ZPX, false), new Op(INC, ZPX, false), new Op(X  , IMP, false), new Op(SED, IMP, false), new Op(SBC, ABY, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(X  , IMP, false), new Op(SBC, ABX, false), new Op(INC, ABX, true ), new Op(X  , IMP, false) 
            };
            
            StartOp(opCodes[0xEA]); // NOP
        }
        #endregion
        
        #region ICPU implementation
        /// <summary>
        /// Interrupt request
        /// </summary>
        public void IRQ()
        {
            if (!registerStatus.HasFlag(Status.IrqDisable))
                interruptRequested = InterruptType.Normal;
        }

        /// <summary>
        /// Non-maskable interrupt request
        /// </summary>
        public void NMI()
        {
            interruptRequested = InterruptType.NonMaskable;
        }
        #endregion

        #region IBusComponent implementation
        public void Reset()
        {
            //Console.WriteLine();
            //Console.WriteLine("**************************************************************************");
            //Console.WriteLine("Ticks per instruction: ");
            //for (int i = 0; i < opCodes.Length; i++)
            //{
            //    currentInstruction = opCodes[i];
            //    Console.WriteLine($"{i:X2}: {opCodes[i].RunOp().Count()}");
            //}
            //Console.WriteLine("**************************************************************************");
            //Console.WriteLine();

            resetRequested = true;
        }

        public virtual void WriteDataFromBus(uint address, byte data)
        {
            throw new NotImplementedException("CPU does not accept reads/writes from the bus");
        }

        public virtual byte ReadDataForBus(uint address)
        {
            throw new NotImplementedException("CPU does not accept reads/writes from the bus");
        }
        #endregion

        #region CPU main loop
        protected override void HandleSingleTick()
        {
            registerStatus.SetFlag(Status.Not_Used);

            if (currentMicroInstruction.MoveNext()) 
                return;

            // Current instruction finished running
            if (interruptRequested != InterruptType.None)
            {
                StartOp(interruptRequested == InterruptType.Normal ? interruptOp : interruptNMIOp);
                interruptRequested = InterruptType.None;
            }
            else if (resetRequested)
            {
                resetRequested = false;
                StartOp(resetOp);
            }
            else
            {
                byte opCode = bus.ReadData(registerProgramCounter++);
                StartOp(opCodes[opCode]);
            }
        }
        #endregion

        #region Addressing modes
        /// <summary>
        /// Address is the accumulator
        /// </summary>
        private static IEnumerable<ushort> ACC(bool assumePageCrossed)
        {
            // Takes 1 cycle to get address
            yield return 0;
        }

        /// <summary>
        /// Address is where the program counter is
        /// </summary>
        private IEnumerable<ushort> IMM(bool assumePageCrossed)
        {
            // Takes 1 cycle to get address
            yield return registerProgramCounter++;
        }
        
        /// <summary>
        /// Address is specified by next two bytes
        /// </summary>
        private IEnumerable<ushort> ABS(bool assumePageCrossed)
        {
            // Takes 3 cycles to get address
            yield return 0;
            byte low = bus.ReadData(registerProgramCounter++);
            
            yield return 0;
            byte high = bus.ReadData(registerProgramCounter++);
            
            yield return (ushort)((high << 8) | low);
        }
        
        /// <summary>
        /// Address is specified by the next byte (high byte assumed to be zero)
        /// </summary>
        private IEnumerable<ushort> ZP0(bool assumePageCrossed)
        {
            // Takes 2 cycles to get address
            yield return 0;
            byte address = bus.ReadData(registerProgramCounter++);

            yield return address;
        }
        
        /// <summary>
        /// Address is specified by the next byte added to the x register (high byte assumed to be zero)
        /// </summary>
        private IEnumerable<ushort> ZPX(bool assumePageCrossed)
        {
            // Takes 3 cycles to get address
            yield return 0;
            ushort address = bus.ReadData(registerProgramCounter++);
         
            yield return 0;
            address += registerX;

            yield return (ushort)(address & 0x00FF);
        }
        
        /// <summary>
        /// Address is specified by the next byte added to the y register (high byte assumed to be zero)
        /// </summary>
        private IEnumerable<ushort> ZPY(bool assumePageCrossed)
        {
            // Takes 3 cycles to get address
            yield return 0;
            ushort address = bus.ReadData(registerProgramCounter++);
         
            yield return 0;
            address += registerY;

            yield return (ushort)(address & 0x00FF);
        }
        
        /// <summary>
        /// Address is specified by next two bytes added to the x register
        /// </summary>
        private IEnumerable<ushort> ABX(bool assumePageCrossed)
        {
            // Takes 3 or 4 cycles to get address
            yield return 0;
            byte low = bus.ReadData(registerProgramCounter++);
            
            yield return 0;
            byte high = bus.ReadData(registerProgramCounter++);
            
            ushort address = (ushort)((high << 8) | low);
            address += registerX;
            if (assumePageCrossed || DifferentPages(address, (ushort)(high << 8)))
                yield return 0;

            yield return address;
        }
        
        /// <summary>
        /// Address is specified by next two bytes added to the y register
        /// </summary>
        private IEnumerable<ushort> ABY(bool assumePageCrossed)
        {
            // Takes 3 or 4 cycles to get address
            yield return 0;
            byte low = bus.ReadData(registerProgramCounter++);
            
            yield return 0;
            byte high = bus.ReadData(registerProgramCounter++);
            
            ushort address = (ushort)((high << 8) | low);
            address += registerY;
            if (assumePageCrossed || DifferentPages(address, (ushort)(high << 8)))
                yield return 0;

            yield return address;
        }
        
        /// <summary>
        /// Address doesn't matter as nothing needs to be read
        /// </summary>
        private static IEnumerable<ushort> IMP(bool assumePageCrossed)
        {
            // Takes 1 cycle to process
            yield return 0;
        }
        
        /// <summary>
        /// Address is specified by the next byte as an offset from the current program counter (-128 to 127)
        /// </summary>
        private IEnumerable<ushort> REL(bool assumePageCrossed)
        {
            // Takes 1 cycle to get data
            ushort offset = bus.ReadData(registerProgramCounter++);
            if ((offset & 0x80) != 0)
                offset |= 0xFF00;

            yield return (ushort)(registerProgramCounter + offset);
        }
        
        /// <summary>
        /// Address is specified by the two bytes at the location specified by the next byte added to the x register (high byte assumed to be zero)
        /// </summary>
        private IEnumerable<ushort> IZX(bool assumePageCrossed)
        {
            // Takes 5 cycles to get address
            yield return 0;
            ushort address = bus.ReadData(registerProgramCounter++);

            yield return 0;
            address += registerX;

            yield return 0;
            byte low = bus.ReadData((ushort)(address & 0x00FF));
            
            yield return 0;
            byte high = bus.ReadData((ushort)((address + 1) & 0x00FF));

            yield return (ushort)((high << 8) | low);
        }
        
        /// <summary>
        /// Address is specified by the two bytes at the location specified by the next byte added to the y register (high byte assumed to be zero)
        /// </summary>
        private IEnumerable<ushort> IZY(bool assumePageCrossed)
        {
            // Takes 4 or 5 cycles to get address
            yield return 0;
            byte data = bus.ReadData(registerProgramCounter++);

            yield return 0;
            byte low = bus.ReadData(data);

            yield return 0;
            byte high = bus.ReadData((byte)(data + 1));

            ushort address = (ushort)((high << 8) | low);
            address += registerY;
            if (assumePageCrossed || DifferentPages(address, (ushort)(high << 8)))
                yield return 0;

            yield return address;
        }
        
        /// <summary>
        /// Address is specified by the two bytes at the location specified by the next two bytes
        /// </summary>
        private IEnumerable<ushort> IND(bool assumePageCrossed)
        {
            // Takes 5 cycles to get address
            yield return 0;
            byte low = bus.ReadData(registerProgramCounter++);
            
            yield return 0;
            byte high = bus.ReadData(registerProgramCounter++);
            
            ushort address = (ushort)((high << 8) | low);
            yield return 0;

            byte high2, low2;
            if (low == 0xFF) // Simulate page boundary hardware bug
            {
                low2 = bus.ReadData(address);
                yield return 0;

                high2 = bus.ReadData((ushort)(address & 0xFF00));
            }
            else
            {
                low2 = bus.ReadData(address);
                yield return 0;

                high2 = bus.ReadData((ushort)(address + 1));
            }

            yield return (ushort)((high2 << 8) | low2);
        }
        #endregion

        #region Instruction handlers
        /// <summary>
        /// Special instruction set for a reset
        /// </summary>
        private IEnumerable<ClockTick> Reset(ushort address)
        {
            yield return new ClockTick();
            registerAccumulator = 0x00;

            yield return new ClockTick();
            registerX = 0x00;
            
            yield return new ClockTick();
            registerY = 0x00;

            yield return new ClockTick();
            registerStatus = 0x00;
            registerStatus.SetFlag(Status.Not_Used);
            registerStatus.SetFlag(Status.IrqDisable);

            yield return new ClockTick();
            registerStackPointer = 0xFD;

            yield return new ClockTick();
            byte low = bus.ReadData(0xFFFC);

            yield return new ClockTick();
            byte high = bus.ReadData(0xFFFD);
            registerProgramCounter = (ushort)((high << 8) | low);
        }

        /// <summary>
        /// Special instruction set for a interrupt
        /// </summary>
        private IEnumerable<ClockTick> Interrupt(ushort address)
        {
            return Interrupt(0xFFFE, InterruptType.Normal);
        }

        /// <summary>
        /// Special instruction set for a non-maskable interrupt
        /// </summary>
        private IEnumerable<ClockTick> InterruptNMI(ushort address)
        {
            return Interrupt(0xFFFA, InterruptType.NonMaskable);
        }

        private IEnumerable<ClockTick> Interrupt(ushort startingAddress, InterruptType type)
        {
            yield return new ClockTick();
            StackPush((byte)((registerProgramCounter >> 8) & 0xFF));

            yield return new ClockTick();
            StackPush((byte)(registerProgramCounter & 0xFF));
            
            yield return new ClockTick();
            registerStatus.ClearFlag(Status.BrkCommand);
            registerStatus.SetFlag(Status.IrqDisable);
            registerStatus.SetFlag(Status.Not_Used);

            yield return new ClockTick();
            StackPush(registerStatus);

            yield return new ClockTick();
            byte low = bus.ReadData(startingAddress);

            yield return new ClockTick();
            byte high = bus.ReadData((ushort)(startingAddress + 1));

            if (type == InterruptType.NonMaskable)
                yield return new ClockTick();
            registerProgramCounter = (ushort)((high << 8) | low);
        }

        /// <summary>
        /// Add memory to accumulator with carry
        /// </summary>
        private IEnumerable<ClockTick> ADC(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);

            int carry = (registerStatus & Status.Carry) != 0 ? 1 : 0;
            ushort result = (ushort)(registerAccumulator + data + carry);

            SetStatusBasedOnResult(result);
            registerStatus.SetOrClearFlag(Status.Overflow, ((~(registerAccumulator ^ data) & (registerAccumulator ^ result)) & 0x0080) != 0);
            registerStatus.SetOrClearFlag(Status.Carry, result > 0xFF);
            registerAccumulator = (byte)(result & 0xFF);
        }

        /// <summary>
        /// "And" memory with accumulator
        /// </summary>
        private IEnumerable<ClockTick> AND(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);
            registerAccumulator &= data;
            SetStatusBasedOnResult(registerAccumulator);
        }

        /// <summary>
        /// Shift left one bit (memory or accumulator)
        /// </summary>
        private IEnumerable<ClockTick> ASL(ushort address)
        {
            if (currentInstruction.AddressMode == ACC)
            {
                yield return new ClockTick();
                registerStatus.SetOrClearFlag(Status.Carry, (registerAccumulator & 0x80) != 0);
                registerAccumulator = (byte)(registerAccumulator << 1);
                SetStatusBasedOnResult(registerAccumulator);
            }
            else
            {
                yield return new ClockTick();
                byte data = bus.ReadData(address);

                yield return new ClockTick();
                registerStatus.SetOrClearFlag(Status.Carry, (data & 0x80) != 0);
                data = (byte)(data << 1);
                SetStatusBasedOnResult(data);

                yield return new ClockTick();
                bus.WriteData(address, data);
            }
        }

        /// <summary>
        /// Branch on carry clear
        /// </summary>
        private IEnumerable<ClockTick> BCC(ushort address)
        {
            yield return new ClockTick();
            bool hadCarry = registerStatus.HasFlag(Status.Carry);

            if (!hadCarry)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Branch on carry set
        /// </summary>
        private IEnumerable<ClockTick> BCS(ushort address)
        {
            yield return new ClockTick();
            bool hadCarry = registerStatus.HasFlag(Status.Carry);

            if (hadCarry)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Branch on result zero
        /// </summary>
        private IEnumerable<ClockTick> BEQ(ushort address)
        {
            yield return new ClockTick();
            bool isZero = registerStatus.HasFlag(Status.Zero);

            if (isZero)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Test bits in memory with accumulator
        /// </summary>
        private IEnumerable<ClockTick> BIT(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);
            byte result = (byte)(registerAccumulator & data);
            registerStatus.SetOrClearFlag(Status.Zero, result == 0);
            registerStatus.SetOrClearFlag(Status.Negative, (data & 0x80) != 0);
            registerStatus.SetOrClearFlag(Status.Overflow, (data & 0x40) != 0);
        }

        /// <summary>
        /// Branch on result minus
        /// </summary>
        private IEnumerable<ClockTick> BMI(ushort address)
        {
            yield return new ClockTick();
            bool wasNegative = registerStatus.HasFlag(Status.Negative);

            if (wasNegative)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Branch on result not zero
        /// </summary>
        private IEnumerable<ClockTick> BNE(ushort address)
        {
            yield return new ClockTick();
            bool isZero = registerStatus.HasFlag(Status.Zero);

            if (!isZero)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Branch on result plus
        /// </summary>
        private IEnumerable<ClockTick> BPL(ushort address)
        {
            yield return new ClockTick();
            bool wasNegative = registerStatus.HasFlag(Status.Negative);

            if (!wasNegative)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Force break
        /// </summary>
        private IEnumerable<ClockTick> BRK(ushort address)
        {
            yield return new ClockTick();
            registerStatus.SetFlag(Status.IrqDisable);

            yield return new ClockTick();
            StackPush((byte)((registerProgramCounter >> 8) & 0xFF));

            yield return new ClockTick();
            StackPush((byte)(registerProgramCounter & 0xFF));
            
            yield return new ClockTick();
            StackPush((byte)(registerStatus & Status.BrkCommand));

            yield return new ClockTick();
            byte low = bus.ReadData(0xFFFE);

            yield return new ClockTick();
            byte high = bus.ReadData(0xFFFF);
            registerProgramCounter = (ushort)((high << 8) | low);
        }

        /// <summary>
        /// Branch on overflow clear
        /// </summary>
        private IEnumerable<ClockTick> BVC(ushort address)
        {
            yield return new ClockTick();
            bool overflowed = registerStatus.HasFlag(Status.Overflow);

            if (!overflowed)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Branch on overflow set
        /// </summary>
        private IEnumerable<ClockTick> BVS(ushort address)
        {
            yield return new ClockTick();
            bool overflowed = registerStatus.HasFlag(Status.Overflow);

            if (overflowed)
            {
                if (DifferentPages(address, registerProgramCounter))
                    yield return new ClockTick();

                yield return new ClockTick();
                registerProgramCounter = address;
            }
        }

        /// <summary>
        /// Clear carry flag
        /// </summary>
        private IEnumerable<ClockTick> CLC(ushort address)
        {
            yield return new ClockTick();
            registerStatus.ClearFlag(Status.Carry);
        }

        /// <summary>
        /// Clear decimal mode
        /// </summary>
        private IEnumerable<ClockTick> CLD(ushort address)
        {
            yield return new ClockTick();
            registerStatus.ClearFlag(Status.DecimalMode);
        }

        /// <summary>
        /// Clear interrupt disable bit
        /// </summary>
        private IEnumerable<ClockTick> CLI(ushort address)
        {
            yield return new ClockTick();
            registerStatus.ClearFlag(Status.IrqDisable);
        }

        /// <summary>
        /// Clear overflow flag
        /// </summary>
        private IEnumerable<ClockTick> CLV(ushort address)
        {
            yield return new ClockTick();
            registerStatus.ClearFlag(Status.Overflow);
        }

        /// <summary>
        /// Compare memory and accumulator
        /// </summary>
        private IEnumerable<ClockTick> CMP(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);

            ushort result = (ushort)(registerAccumulator - data);
            SetStatusBasedOnResult(result);
            registerStatus.SetOrClearFlag(Status.Carry, registerAccumulator >= data);
        }

        /// <summary>
        /// Compare memory and index x
        /// </summary>
        private IEnumerable<ClockTick> CPX(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);

            ushort result = (ushort)(registerX - data);
            SetStatusBasedOnResult(result);
            registerStatus.SetOrClearFlag(Status.Carry, registerX >= data);
        }

        /// <summary>
        /// Compare memory and index y
        /// </summary>
        private IEnumerable<ClockTick> CPY(ushort address)
        {
            byte data = bus.ReadData(address);
            yield return new ClockTick();

            ushort result = (ushort)(registerY - data);
            SetStatusBasedOnResult(result);
            registerStatus.SetOrClearFlag(Status.Carry, registerY >= data);
        }

        /// <summary>
        /// Decrement memory by one
        /// </summary>
        private IEnumerable<ClockTick> DEC(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);

            yield return new ClockTick();
            data--;
            SetStatusBasedOnResult(data);

            yield return new ClockTick();
            bus.WriteData(address, data);
        }

        /// <summary>
        /// Decrement index x by one
        /// </summary>
        private IEnumerable<ClockTick> DEX(ushort address)
        {
            yield return new ClockTick();
            registerX--;
            SetStatusBasedOnResult(registerX);
        }

        /// <summary>
        /// Decrement index y by one
        /// </summary>
        private IEnumerable<ClockTick> DEY(ushort address)
        {
            yield return new ClockTick();
            registerY--;
            SetStatusBasedOnResult(registerY);
        }

        /// <summary>
        /// Exclusive-or memory with accumulator
        /// </summary>
        private IEnumerable<ClockTick> EOR(ushort address)
        {
            byte data = bus.ReadData(address);
            yield return new ClockTick();

            registerAccumulator ^= data;
            SetStatusBasedOnResult(registerAccumulator);
        }

        /// <summary>
        /// Increment memory by one
        /// </summary>
        private IEnumerable<ClockTick> INC(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);

            yield return new ClockTick();
            data++;
            SetStatusBasedOnResult(data);

            yield return new ClockTick();
            bus.WriteData(address, data);
        }

        /// <summary>
        /// Increment index x by one
        /// </summary>
        private IEnumerable<ClockTick> INX(ushort address)
        {
            yield return new ClockTick();
            registerX++;
            SetStatusBasedOnResult(registerX);
        }

        /// <summary>
        /// Increment index y by one
        /// </summary>
        private IEnumerable<ClockTick> INY(ushort address)
        {
            yield return new ClockTick();
            registerY++;
            SetStatusBasedOnResult(registerY);
        }

        /// <summary>
        /// Jump to new location
        /// </summary>
        private IEnumerable<ClockTick> JMP(ushort address)
        {
            registerProgramCounter = address;
            yield break; // JMP seems to take no clock cycles according to my calculations
        }

        /// <summary>
        /// Jump to new location saving return address
        /// </summary>
        private IEnumerable<ClockTick> JSR(ushort address)
        {
            registerProgramCounter--;

            yield return new ClockTick();
            StackPush((byte)((registerProgramCounter >> 8) & 0xFF));

            yield return new ClockTick();
            StackPush((byte)(registerProgramCounter & 0xFF));

            yield return new ClockTick();
            registerProgramCounter = address;
        }

        /// <summary>
        /// Load accumulator with memory
        /// </summary>
        private IEnumerable<ClockTick> LDA(ushort address)
        {
            yield return new ClockTick();
            registerAccumulator = bus.ReadData(address);
            SetStatusBasedOnResult(registerAccumulator);
        }

        /// <summary>
        /// Load index x with memory
        /// </summary>
        private IEnumerable<ClockTick> LDX(ushort address)
        {
            yield return new ClockTick();
            registerX = bus.ReadData(address);
            SetStatusBasedOnResult(registerX);
        }

        /// <summary>
        /// Load index y with memory
        /// </summary>
        private IEnumerable<ClockTick> LDY(ushort address)
        {
            yield return new ClockTick();
            registerY = bus.ReadData(address);
            SetStatusBasedOnResult(registerY);
        }

        /// <summary>
        /// Shift one bit right (memory or accumulator)
        /// </summary>
        private IEnumerable<ClockTick> LSR(ushort address)
        {
            if (currentInstruction.AddressMode == ACC)
            {
                yield return new ClockTick();
                registerStatus.SetOrClearFlag(Status.Carry, (registerAccumulator & 0x0001) != 0);
                registerAccumulator = (byte)(registerAccumulator >> 1);
                SetStatusBasedOnResult(registerAccumulator);
            }
            else
            {
                yield return new ClockTick();
                byte data = bus.ReadData(address);

                yield return new ClockTick();
                registerStatus.SetOrClearFlag(Status.Carry, (data & 0x0001) != 0);
                data = (byte)(data >> 1);
                SetStatusBasedOnResult(data);

                yield return new ClockTick();
                bus.WriteData(address, data);
            }
        }

        /// <summary>
        /// No operation
        /// </summary>
        private static IEnumerable<ClockTick> NOP(ushort address)
        {
            yield return new ClockTick();
        }

        /// <summary>
        /// "Or" memory with accumulator
        /// </summary>
        private IEnumerable<ClockTick> ORA(ushort address)
        {
            yield return new ClockTick();
            byte data = bus.ReadData(address);

            registerAccumulator |= data;
            SetStatusBasedOnResult(registerAccumulator);
        }

        /// <summary>
        /// Push accumulator on stack
        /// </summary>
        private IEnumerable<ClockTick> PHA(ushort address)
        {
            yield return new ClockTick();
            byte data = registerAccumulator;

            yield return new ClockTick();
            StackPush(data);
        }

        /// <summary>
        /// Push processor status on stack
        /// </summary>
        private IEnumerable<ClockTick> PHP(ushort address)
        {
            yield return new ClockTick();
            byte data = (byte)(registerStatus | Status.BrkCommand | Status.Not_Used);

            yield return new ClockTick();
            StackPush(data);
            registerStatus.ClearFlag(Status.BrkCommand);
            registerStatus.ClearFlag(Status.Not_Used);
        }

        /// <summary>
        /// Pull accumulator from stack
        /// </summary>
        private IEnumerable<ClockTick> PLA(ushort address)
        {
            yield return new ClockTick();
            byte data = StackPop();

            yield return new ClockTick();
            registerAccumulator = data;

            yield return new ClockTick();
            SetStatusBasedOnResult(registerAccumulator);
        }

        /// <summary>
        /// Pull processor status from stack
        /// </summary>
        private IEnumerable<ClockTick> PLP(ushort address)
        {
            yield return new ClockTick();
            byte data = StackPop();

            yield return new ClockTick();
            registerStatus = data;

            yield return new ClockTick();
            registerStatus.SetFlag(Status.Not_Used);
        }

        /// <summary>
        /// Rotate one bit left (memory or accumulator)
        /// </summary>
        private IEnumerable<ClockTick> ROL(ushort address)
        {
            if (currentInstruction.AddressMode == ACC)
            {
                yield return new ClockTick();
                ushort result = (ushort)(registerAccumulator << 1 | (registerStatus.HasFlag(Status.Carry) ? 1 : 0));
                SetStatusBasedOnResult(result);
                registerStatus.SetOrClearFlag(Status.Carry, (result & 0xFF00) != 0);
                registerAccumulator = (byte)result;
            }
            else
            {
                yield return new ClockTick();
                byte data = bus.ReadData(address);

                yield return new ClockTick();
                ushort result = (ushort)(data << 1 | (registerStatus.HasFlag(Status.Carry) ? 1 : 0));
                SetStatusBasedOnResult(result);
                registerStatus.SetOrClearFlag(Status.Carry, (result & 0xFF00) != 0);

                yield return new ClockTick();
                bus.WriteData(address, (byte)result);
            }
        }

        /// <summary>
        /// Rotate one bit right (memory or accumulator)
        /// </summary>
        private IEnumerable<ClockTick> ROR(ushort address)
        {
            if (currentInstruction.AddressMode == ACC)
            {
                yield return new ClockTick();
                ushort result = (ushort)((registerStatus.HasFlag(Status.Carry) ? 1 : 0) << 7 | registerAccumulator >> 1);
                SetStatusBasedOnResult(result);
                registerStatus.SetOrClearFlag(Status.Carry, (registerAccumulator & 0x01) != 0);
                registerAccumulator = (byte)result;
            }
            else
            {
                yield return new ClockTick();
                byte data = bus.ReadData(address);

                yield return new ClockTick();
                ushort result = (ushort)((registerStatus.HasFlag(Status.Carry) ? 1 : 0) << 7 | data >> 1);
                SetStatusBasedOnResult(result);
                registerStatus.SetOrClearFlag(Status.Carry, (data & 0x01) != 0);

                yield return new ClockTick();
                bus.WriteData(address, (byte)result);
            }
        }

        /// <summary>
        /// Return from interrupt
        /// </summary>
        private IEnumerable<ClockTick> RTI(ushort address)
        {
            yield return new ClockTick();
            registerStatus = StackPop();

            yield return new ClockTick();
            registerStatus.ClearFlag(Status.BrkCommand);
            registerStatus.ClearFlag(Status.Not_Used);

            yield return new ClockTick();
            byte low = StackPop();
            
            yield return new ClockTick();
            byte high = StackPop();
            
            yield return new ClockTick();
            registerProgramCounter = (ushort)((high << 8) | low);
        }

        /// <summary>
        /// Return from subroutine
        /// </summary>
        private IEnumerable<ClockTick> RTS(ushort address)
        {
            yield return new ClockTick();
            byte low = StackPop();

            yield return new ClockTick();
            byte high = StackPop();

            yield return new ClockTick();

            yield return new ClockTick();
            registerProgramCounter = (ushort)((high << 8) | low);
            
            yield return new ClockTick();
            registerProgramCounter++;
        }

        /// <summary>
        /// Subtract memory from accumulator with borrow
        /// </summary>
        private IEnumerable<ClockTick> SBC(ushort address)
        {
            yield return new ClockTick();
            ushort data = bus.ReadData(address);
            data ^= 0x00FF;

            int carry = (registerStatus & Status.Carry) != 0 ? 1 : 0;
            ushort result = (ushort)(registerAccumulator + data + carry);
            
            SetStatusBasedOnResult(result);
            registerStatus.SetOrClearFlag(Status.Carry, (result & 0xFF00) != 0);
            registerStatus.SetOrClearFlag(Status.Overflow, ((result ^ registerAccumulator) & (result ^ data) & 0x0080) != 0);
            registerAccumulator = (byte)(result & 0xFF);
        }

        /// <summary>
        /// Set carry flag
        /// </summary>
        private IEnumerable<ClockTick> SEC(ushort address)
        {
            yield return new ClockTick();
            registerStatus.SetFlag(Status.Carry);
        }

        /// <summary>
        /// Set decimal mode
        /// </summary>
        private IEnumerable<ClockTick> SED(ushort address)
        {
            yield return new ClockTick();
            registerStatus.SetFlag(Status.DecimalMode);
        }

        /// <summary>
        /// Set interrupt disable status
        /// </summary>
        private IEnumerable<ClockTick> SEI(ushort address)
        {
            yield return new ClockTick();
            registerStatus.SetFlag(Status.IrqDisable);
        }

        /// <summary>
        /// Store accumulator in memory
        /// </summary>
        private IEnumerable<ClockTick> STA(ushort address)
        {
            yield return new ClockTick();
            bus.WriteData(address, registerAccumulator);
        }

        /// <summary>
        /// Store index x in memory
        /// </summary>
        private IEnumerable<ClockTick> STX(ushort address)
        {
            yield return new ClockTick();
            bus.WriteData(address, registerX);
        }

        /// <summary>
        /// Store index y in memory
        /// </summary>
        private IEnumerable<ClockTick> STY(ushort address)
        {
            yield return new ClockTick();
            bus.WriteData(address, registerY);
        }

        /// <summary>
        /// Transfer accumulator to index x
        /// </summary>
        private IEnumerable<ClockTick> TAX(ushort address)
        {
            yield return new ClockTick();
            registerX = registerAccumulator;
            SetStatusBasedOnResult(registerX);
        }

        /// <summary>
        /// Transfer accumulator to index y
        /// </summary>
        private IEnumerable<ClockTick> TAY(ushort address)
        {
            yield return new ClockTick();
            registerY = registerAccumulator;
            SetStatusBasedOnResult(registerY);
        }

        /// <summary>
        /// Transfer stack pointer to index x
        /// </summary>
        private IEnumerable<ClockTick> TSX(ushort address)
        {
            yield return new ClockTick();
            registerX = registerStackPointer;
            SetStatusBasedOnResult(registerX);
        }

        /// <summary>
        /// Transfer index x to accumulator
        /// </summary>
        private IEnumerable<ClockTick> TXA(ushort address)
        {
            yield return new ClockTick();
            registerAccumulator = registerX;
            SetStatusBasedOnResult(registerAccumulator);
        }

        /// <summary>
        /// Transfer index x to stack register
        /// </summary>
        private IEnumerable<ClockTick> TXS(ushort address)
        {
            yield return new ClockTick();
            registerStackPointer = registerX;
        }

        /// <summary>
        /// Transfer index y to accumulator
        /// </summary>
        private IEnumerable<ClockTick> TYA(ushort address)
        {
            yield return new ClockTick();
            registerAccumulator = registerY;
            SetStatusBasedOnResult(registerAccumulator);
        }

        /// <summary>
        /// Invalid instruction
        /// </summary>
        private static IEnumerable<ClockTick> X(ushort address)
        {
            yield return new ClockTick();
        }
        #endregion

        #region Private helper methods
        private void StartOp(Op operation)
        {
            //Console.WriteLine($"{registerProgramCounter - 1:X4} {OperationInfo(operation)}");

            currentInstruction = operation;
            currentMicroInstruction = operation.RunOp().GetEnumerator();
            currentMicroInstruction.MoveNext();
        }

        private string OperationInfo(Op op)
        {
            StringBuilder bldr = new StringBuilder();
            bldr.Append(op.Instructions.Method.Name);
            bldr.Append(' ');
            if (op.AddressMode == ABS || op.AddressMode == ABX || op.AddressMode == ABY || op.AddressMode == IND)
                bldr.Append('$').Append(bus.ReadData((ushort)(registerProgramCounter + 1)).ToString("X2")).Append(bus.ReadData(registerProgramCounter).ToString("X2"));
            else if (op.AddressMode == REL)
                bldr.Append('$').Append((registerProgramCounter + bus.ReadData(registerProgramCounter)).ToString("X4"));
            else if (op.AddressMode == IMM)
                bldr.Append("#$").Append(bus.ReadData(registerProgramCounter).ToString("X2"));
            else if (op.AddressMode != IMP)
                bldr.Append(bus.ReadData(registerProgramCounter).ToString("X4"));
            else
                bldr.Append("     ");
            //bldr.Append('{').Append(op.AddressMode.Method.Name).Append('}');

            bldr.Append('\t').Append("A:").Append(registerAccumulator.ToString("X2"))
                .Append(' ').Append("X:").Append(registerX.ToString("X2"))
                .Append(' ').Append("Y:").Append(registerY.ToString("X2"))
                .Append(' ').Append("P:").Append(registerStatus.ToString("X2"))
                .Append(' ').Append("SP:").Append(registerStackPointer.ToString("X2"));
            return bldr.ToString();
        }

        private void SetStatusBasedOnResult(ushort result)
        {
            registerStatus.SetOrClearFlag(Status.Negative, (result & 0x80) != 0);
            registerStatus.SetOrClearFlag(Status.Zero, (result & 0xFF) == 0);
        }

        private static bool DifferentPages(ushort add1, ushort add2)
        {
            return (add1 & 0xFF00) != (add2 & 0xFF00);
        }

        private void StackPush(byte b)
        {
            bus.WriteData((ushort)(0x0100 + registerStackPointer--), b);
        }

        private byte StackPop()
        {
            return bus.ReadData((ushort)(0x0100 + ++registerStackPointer));
        }
        #endregion

        #region Op structure
        private sealed class Op
        {
            /// <summary>
            /// Enumerable is to mimic the time for each tick of the clock.
            /// </summary>
            public readonly Func<bool, IEnumerable<ushort>> AddressMode;

            /// <summary>
            /// Actions are tasks to be performed with each tick of the clock.
            /// </summary>
            public readonly Func<ushort, IEnumerable<ClockTick>> Instructions;

            private readonly bool assumePageBoundaryCrossed;

            public Op(Func<ushort, IEnumerable<ClockTick>> instructions, Func<bool, IEnumerable<ushort>> addressMode, bool assumePageBoundaryCrossed)
            {
                Instructions = instructions;
                AddressMode = addressMode;
                this.assumePageBoundaryCrossed = assumePageBoundaryCrossed;
            }

            public IEnumerable<ClockTick> RunOp()
            {
                ushort dataAddress = 0xFFFF;
                foreach (ushort address in AddressMode(assumePageBoundaryCrossed))
                {
                    yield return new ClockTick();
                    dataAddress = address;
                }

                foreach (ClockTick tick in Instructions(dataAddress))
                    yield return tick;
            }
        }
        #endregion
    }
}
