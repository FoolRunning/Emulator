using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SystemBase.Bus;

namespace SystemBase.CPUs
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CPU_6502 : ClockListener, ICPU, IBusComponent_16
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
        private readonly Bus_16 bus;
        private volatile InterruptType interruptRequested = InterruptType.None;
        private volatile bool resetRequested;

        /// <summary>The current instruction being run.</summary>
        private Op currentInstruction;
        private IEnumerator<ClockTick> currentMicroInstruction;
        private IEnumerator<ushort> currentAddressMicroInstruction;

        private ushort registerProgramCounter;
        private byte registerAccumulator;
        private byte registerX;
        private byte registerY;
        private byte registerStatus;
        private byte registerStackPointer;
        #endregion

        #region Constructor
        public CPU_6502(IClock clock, Bus_16 bus) : base(clock, "CPU_6502")
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));

            resetOp = new Op(Reset, IMP);
            interruptOp = new Op(Interrupt, IMP);
            interruptNMIOp = new Op(InterruptNMI, IMP);

            opCodes = new[]
            {
                new Op(BRK, IMP), new Op(ORA, IZX), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(ORA, ZP0), new Op(ASL, ZP0), new Op(X  , IMP), new Op(PHP, IMP), new Op(ORA, IMM), new Op(ASL, ACC), new Op(X  , IMP), new Op(X  , IMP), new Op(ORA, ABS), new Op(ASL, ABS), new Op(X  , IMP), 
                new Op(BPL, REL), new Op(ORA, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(ORA, ZPX), new Op(ASL, ZPX), new Op(X  , IMP), new Op(CLC, IMP), new Op(ORA, ABY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(ORA, ABX), new Op(ASL, ABX), new Op(X  , IMP), 
                new Op(JSR, ABS), new Op(AND, IZX), new Op(X  , IMP), new Op(X  , IMP), new Op(BIT, ZP0), new Op(AND, ZP0), new Op(ROL, ZP0), new Op(X  , IMP), new Op(PLP, IMP), new Op(AND, IMM), new Op(ROL, ACC), new Op(X  , IMP), new Op(BIT, ABS), new Op(AND, ABS), new Op(ROL, ABS), new Op(X  , IMP), 
                new Op(BMI, REL), new Op(AND, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(AND, ZPX), new Op(ROL, ZPX), new Op(X  , IMP), new Op(SEC, IMP), new Op(AND, ABY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(AND, ABX), new Op(ROL, ABX), new Op(X  , IMP), 
                new Op(RTI, IMP), new Op(EOR, IZX), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(EOR, ZP0), new Op(LSR, ZP0), new Op(X  , IMP), new Op(PHA, IMP), new Op(EOR, IMM), new Op(LSR, ACC), new Op(X  , IMP), new Op(JMP, ABS), new Op(EOR, ABS), new Op(LSR, ABS), new Op(X  , IMP), 
                new Op(BVC, REL), new Op(EOR, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(EOR, ZPX), new Op(LSR, ZPX), new Op(X  , IMP), new Op(CLI, IMP), new Op(EOR, ABY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(EOR, ABX), new Op(LSR, ABX), new Op(X  , IMP), 
                new Op(RTS, IMP), new Op(ADC, IZX), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(ADC, ZP0), new Op(ROR, ZP0), new Op(X  , IMP), new Op(PLA, IMP), new Op(ADC, IMM), new Op(ROR, ACC), new Op(X  , IMP), new Op(JMP, IND), new Op(ADC, ABS), new Op(ROR, ABS), new Op(X  , IMP), 
                new Op(BVS, REL), new Op(ADC, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(ADC, ZPX), new Op(ROR, ZPX), new Op(X  , IMP), new Op(SEI, IMP), new Op(ADC, ABY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(ADC, ABX), new Op(ROR, ABX), new Op(X  , IMP), 
                new Op(X  , IMP), new Op(STA, IZX), new Op(X  , IMP), new Op(X  , IMP), new Op(STY, ZP0), new Op(STA, ZP0), new Op(STX, ZP0), new Op(X  , IMP), new Op(DEY, IMP), new Op(X  , IMP), new Op(TXA, IMP), new Op(X  , IMP), new Op(STY, ABS), new Op(STA, ABS), new Op(STX, ABS), new Op(X  , IMP), 
                new Op(BCC, REL), new Op(STA, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(STY, ZPX), new Op(STA, ZPX), new Op(STX, ZPY), new Op(X  , IMP), new Op(TYA, IMP), new Op(STA, ABY), new Op(TXS, IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(STA, ABX), new Op(X  , IMP), new Op(X  , IMP), 
                new Op(LDY, IMM), new Op(LDA, IZX), new Op(LDX, IMM), new Op(X  , IMP), new Op(LDY, ZP0), new Op(LDA, ZP0), new Op(LDX, ZP0), new Op(X  , IMP), new Op(TAY, IMP), new Op(LDA, IMM), new Op(TAX, IMP), new Op(X  , IMP), new Op(LDY, ABS), new Op(LDA, ABS), new Op(LDX, ABS), new Op(X  , IMP), 
                new Op(BCS, REL), new Op(LDA, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(LDY, ZPX), new Op(LDA, ZPX), new Op(LDX, ZPY), new Op(X  , IMP), new Op(CLV, IMP), new Op(LDA, ABY), new Op(TSX, IMP), new Op(X  , IMP), new Op(LDY, ABX), new Op(LDA, ABX), new Op(LDX, ABY), new Op(X  , IMP), 
                new Op(CPY, IMM), new Op(CMP, IZX), new Op(X  , IMP), new Op(X  , IMP), new Op(CPY, ZP0), new Op(CMP, ZP0), new Op(DEC, ZP0), new Op(X  , IMP), new Op(INY, IMP), new Op(CMP, IMM), new Op(DEX, IMP), new Op(X  , IMP), new Op(CPY, ABS), new Op(CMP, ABS), new Op(DEC, ABS), new Op(X  , IMP), 
                new Op(BNE, REL), new Op(CMP, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(CMP, ZPX), new Op(DEC, ZPX), new Op(X  , IMP), new Op(CLD, IMP), new Op(CMP, ABY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(CMP, ABX), new Op(DEC, ABX), new Op(X  , IMP), 
                new Op(CPX, IMM), new Op(SBC, IZX), new Op(X  , IMP), new Op(X  , IMP), new Op(CPX, ZP0), new Op(SBC, ZP0), new Op(INC, ZP0), new Op(X  , IMP), new Op(INX, IMP), new Op(SBC, IMM), new Op(NOP, IMP), new Op(X  , IMP), new Op(CPX, ABS), new Op(SBC, ABS), new Op(INC, ABS), new Op(X  , IMP), 
                new Op(BEQ, REL), new Op(SBC, IZY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(SBC, ZPX), new Op(INC, ZPX), new Op(X  , IMP), new Op(SED, IMP), new Op(SBC, ABY), new Op(X  , IMP), new Op(X  , IMP), new Op(X  , IMP), new Op(SBC, ABX), new Op(INC, ABX), new Op(X  , IMP), 
            };
            
            currentInstruction = opCodes[0xEA]; // NOP
        }
        #endregion
        
        #region ICPU implementation
        public void Reset()
        {
            resetRequested = true;
        }

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

        #region IBusComponent_16 implementation
        public virtual void WriteDataFromBus(ushort address, byte data)
        {
            throw new NotImplementedException("CPU does not accept reads/writes from the bus");
        }

        public virtual byte ReadDataForBus(ushort address)
        {
            throw new NotImplementedException("CPU does not accept reads/writes from the bus");
        }
        #endregion

        #region CPU main loop
        protected override void HandleSingleTick()
        {
            registerStatus.SetFlag(Status.Not_Used);

            if (currentAddressMicroInstruction != null)
            {
                // Only the last enumeration contains the data so just keep reading it
                ushort dataAddress = currentAddressMicroInstruction.Current;
                if (currentAddressMicroInstruction.MoveNext())
                    return;

                currentMicroInstruction = currentInstruction.Instructions(dataAddress).GetEnumerator();
                currentAddressMicroInstruction = null;
            }
            
            if (currentMicroInstruction == null || !currentMicroInstruction.MoveNext())
            {
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
        }
        #endregion

        #region Addressing modes
        /// <summary>
        /// Address is the accumulator
        /// </summary>
        private IEnumerable<ushort> ACC()
        {
            // Takes 1 cycle to get address
            yield return 0;
        }

        /// <summary>
        /// Address is where the program counter is
        /// </summary>
        private IEnumerable<ushort> IMM()
        {
            // Takes 1 cycle to get address
            yield return registerProgramCounter++;
        }
        
        /// <summary>
        /// Address is specified by next two bytes
        /// </summary>
        private IEnumerable<ushort> ABS()
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
        private IEnumerable<ushort> ZP0()
        {
            // Takes 2 cycles to get address
            yield return 0;
            byte address = bus.ReadData(registerProgramCounter++);

            yield return address;
        }
        
        /// <summary>
        /// Address is specified by the next byte added to the x register (high byte assumed to be zero)
        /// </summary>
        private IEnumerable<ushort> ZPX()
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
        private IEnumerable<ushort> ZPY()
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
        private IEnumerable<ushort> ABX()
        {
            // Takes 3 or 4 cycles to get address
            yield return 0;
            byte low = bus.ReadData(registerProgramCounter++);
            
            yield return 0;
            byte high = bus.ReadData(registerProgramCounter++);
            
            ushort address = (ushort)((high << 8) | low);
            address += registerX;
            if (DifferentPages(address, (ushort)(high << 8)))
                yield return 0;

            yield return address;
        }
        
        /// <summary>
        /// Address is specified by next two bytes added to the y register
        /// </summary>
        private IEnumerable<ushort> ABY()
        {
            // Takes 3 or 4 cycles to get address
            yield return 0;
            byte low = bus.ReadData(registerProgramCounter++);
            
            yield return 0;
            byte high = bus.ReadData(registerProgramCounter++);
            
            ushort address = (ushort)((high << 8) | low);
            address += registerY;
            if (DifferentPages(address, (ushort)(high << 8)))
                yield return 0;

            yield return address;
        }
        
        /// <summary>
        /// Address doesn't matter as nothing needs to be read
        /// </summary>
        private IEnumerable<ushort> IMP()
        {
            // Takes 1 cycle to process
            yield return 0;
        }
        
        /// <summary>
        /// Address is specified by the next byte as an offset from the current program counter (-128 to 127)
        /// </summary>
        private IEnumerable<ushort> REL()
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
        private IEnumerable<ushort> IZX()
        {
            // Takes 4 cycles to get address
            yield return 0;
            ushort address = bus.ReadData(registerProgramCounter++);
            address += registerX;

            yield return 0;
            byte low2 = bus.ReadData((ushort)(address & 0x00FF));
            
            yield return 0;
            byte high2 = bus.ReadData((ushort)((address + 1) & 0x00FF));

            yield return (ushort)((high2 << 8) | low2);
        }
        
        /// <summary>
        /// Address is specified by the two bytes at the location specified by the next byte added to the y register (high byte assumed to be zero)
        /// </summary>
        private IEnumerable<ushort> IZY()
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
            if (DifferentPages(address, (ushort)(high << 8)));
                yield return 0;

            yield return address;
        }
        
        /// <summary>
        /// Address is specified by the two bytes at the location specified by the next two bytes
        /// </summary>
        private IEnumerable<ushort> IND()
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
            StackPush((byte)registerStatus);

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
        private IEnumerable<ClockTick> NOP(ushort address)
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
            //SetStatus(Status.BrkCommand, false);
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
            currentAddressMicroInstruction = operation.AddressMode().GetEnumerator();
            currentAddressMicroInstruction.MoveNext();
            currentMicroInstruction = null;
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
                .Append(' ').Append("P:").Append(((byte)registerStatus).ToString("X2"))
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
            /// Actions are tasks to be performed with each tick of the clock.
            /// </summary>
            public readonly Func<ushort, IEnumerable<ClockTick>> Instructions;
            /// <summary>
            /// Enumerable is to mimic the time for each tick of the clock.
            /// </summary>
            public readonly Func<IEnumerable<ushort>> AddressMode;

            public Op(Func<ushort, IEnumerable<ClockTick>> instructions, Func<IEnumerable<ushort>> addressMode)
            {
                Instructions = instructions;
                AddressMode = addressMode;
            }
        }
        #endregion
    }
}
