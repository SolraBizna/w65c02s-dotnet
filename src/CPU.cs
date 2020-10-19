using System;

namespace W65C02S {
    /// <summary>An instance of a W65C02S, encapsulating the entire runtime
    /// state of the processor itself. Not very useful without a
    /// <tt>System</tt> to go with it.</summary>
    public partial class CPU {
        /// <summary>The CPU is in one of the given states between
        /// steps.</summary>
        public enum State {
            /// <summary>The CPU has just been reset. It will execute the reset
            /// sequence at the next step. RDY is HIGH.</summary>
            HasBeenReset,
            /// <summary>The CPU is in its normal operating state. It will
            /// fetch an instruction at the next step, and possibly handle an
            /// interrupt. RDY is HIGH.</summary>
            Running,
            /// <summary>The CPU has executed a <tt>WAI</tt> instruction and
            /// has not yet detected an interrupt. <tt>RDY</tt> is
            /// LOW.</summary>
            AwaitingInterrupt,
            /// <summary>The CPU has executed a <tt>STP</tt> instruction.
            /// Only a reset will return it to an executing state.
            /// <tt>RDY</tt> is LOW.</summary>
            Stopped,
        }

        /// <summary>Status register flag corresponding to the <b>C</b>arry
        /// bit.</summary>
        public const byte P_C = 0x01;
        /// <summary>Status register flag corresponding to the <b>Z</b>ero
        /// bit.</summary>
        public const byte P_Z = 0x02;
        /// <summary>Status register flag corresponding to the <b>I</b>nterrupt
        /// mask bit.</summary>
        public const byte P_I = 0x04;
        /// <summary>Status register flag corresponding to the <b>D</b>ecimal
        /// mode bit.</summary>
        public const byte P_D = 0x08;
        /// <summary>Status register flag corresponding to the <b>B</b>reak
        /// bit.</summary>
        public const byte P_B = 0x10;
        /// <summary>Status register flag that is hardwired to 1, in spite of
        /// what the datasheet says.</summary>
        public const byte P_1 = 0x20;
        /// <summary>Status register flag corresponding to the o<b>V</b>erflow
        /// bit.</summary>
        public const byte P_V = 0x40;
        /// <summary>Status register flag corresponding to the <b>N</b>egative
        /// bit.</summary>
        public const byte P_N = 0x80;
        /// <summary>Address of the IRQ/BRK interrupt vector.</summary>
        public const ushort IRQ_VECTOR = 0xfffe;
        /// <summary>Address of the Reset interrupt vector.</summary>
        public const ushort RESET_VECTOR = 0xfffc;
        /// <summary>Address of the NMI interrupt vector.</summary>
        public const ushort NMI_VECTOR = 0xfffa;

        private State state = State.HasBeenReset;
        private ushort pc = 0xFFFF;
        private byte a = 0xFF, x = 0xFF, y = 0xFF, s = 0xFF, p = 0xFF;
        private bool irq = false;
        internal bool irq_pending = false;
        private bool nmi = false, nmi_edge = false, nmi_pending = false;
        /// <summary>Resets the CPU. Execution will flounder for a few cycles,
        /// then fetch the reset vector and "start over".</summary>
        public void Reset() {
            state = State.HasBeenReset;
            s = 0;
            p = (byte)(P_1|P_I);
        }
        /// <summary>Get the current value of the <b>P</b>rogram
        /// <b>C</b>ounter, i.e. the next instruction that will (probably) be
        /// executed.</summary>
        public ushort GetPC() => pc;
        /// <summary>Overwrite the current value of the <b>P</b>rogram
        /// <b>C</b>ounter.</summary>
        public void SetPC(ushort pc) => this.pc = pc;
        /// <summary>Internal function. Get the current value of the
        /// <b>P</b>rogram <b>C</b>ounter. Increment the underlying value by
        /// one <em>after</em> reading it.</summary>
        internal ushort ReadPCPostIncrement() {
            ushort ret = pc;
            pc += 1;
            return ret;
        }
        /// <summary>Get the current value of the <b>A</b>ccumulator.</summary>
        public byte GetA() => a;
        /// <summary>Overwrite the current value of the
        /// <b>A</b>ccumulator.</summary>
        public void SetA(byte a) => this.a = a;
        /// <summary>Get the current value of index register
        /// <b>X</b>.</summary>
        public byte GetX() => x;
        /// <summary>Overwrite the current value of index register
        /// <b>X</b>.</summary>
        public void SetX(byte x) => this.x = x;
        /// <summary>Get the current value of indey register
        /// <b>Y</b>.</summary>
        public byte GetY() => y;
        /// <summary>Overwrite the current value of indey register
        /// <b>Y</b>.</summary>
        public void SetY(byte y) => this.y = y;
        /// <summary>Get the current value of the <b>S</b>tack
        /// pointer.</summary>
        public byte GetS() => s;
        /// <summary>Overwrite the current value of the <b>S</b>tack
        /// pointer.</summary>
        public void SetS(byte s) => this.s = s;
        /// <summary>Get the current value of the <b>P</b>rocessor status
        /// register.</summary>
        /// <remarks>
        ///   <para>
        ///     Use the <tt>P_*</tt> constants to interpret the value.
        ///   </para>
        /// </remarks>
        public byte GetP() => p;
        /// <summary>Overwrite the current value of the <b>P</b>rocessor
        /// status register.</summary>
        public void SetP(byte p) => this.p = p;
        /// <summary>Get the current operating state of the CPU. May return a
        /// stale value if called during a <tt>step</tt>.</summary>
        public State GetState() => state;
        /// <summary>Push a value onto the stack using the given
        /// <cref>Bus</cref>.</summary>
        public void Push<B>(B bus, byte val) where B: Bus {
            bus.WriteStack(this, (ushort)(0x0100 | s--), val);
        }
        /// <summary>Perform a spurious push during reset.</summary>
        internal void SpuriousPush<B>(B bus) where B: Bus {
            bus.ReadStackSpurious(this, (ushort)(0x0100 | s--));
        }
        /// <summary>Pop a value from the stack using the given
        /// <cref>Bus</cref></summary>
        public byte Pop<B>(B bus) where B: Bus {
            return bus.ReadStack(this, (ushort)(0x0100 | ++s));
        }
        /// <summary>Spuriously read a value from the next stack slot, like
        /// happens during a JSR or RTS or most pulls.</summary>
        internal void SpuriousStackRead<B>(B bus) where B: Bus {
            bus.ReadSpurious(this, (ushort)(0x0100 | s));
        }
        /// <summary>Change the input on the <tt>IRQB</tt> pin. <tt>false</tt>
        /// means no interrupt pending. <tt>true</tt> means some interrupt is
        /// pending.</summary>
        /// <remarks>
        ///   <para>
        ///     Note that <tt>IRQB</tt> is an active-low pin and that the value
        ///     you pass to this function is the <em>logical</em> value and not
        ///     the <em>electrical</em> one.
        ///   </para>
        /// </remarks>
        public void SetIRQ(bool irq) => this.irq = irq;
        /// <summary>Change the input on the <tt>NMIB</tt> pin. <tt>false</tt>
        /// means no NMI pending. A transition from <tt>false</tt> to
        /// <tt>true</tt> triggers an NMI at the next step.</summary>
        /// <remarks>
        ///   <para>
        ///     Note that <tt>NMIB</tt> is an active-low pin and that the value
        ///     you pass to this function is the <em>logical</em> value and not
        ///     the <em>electrical</em> one.
        ///   </para>
        ///   <para>
        ///     This library does not accurately simulate extremely short NMI
        ///     pulses, or extremely rapid ones. If these conditions arise on
        ///     real hardware, chaos will ensue anyway.
        ///   </para>
        /// </remarks>
        public void SetNMI(bool nmi) {
            if(!this.nmi) {
                nmi_edge = nmi_edge || (!nmi_edge && nmi);
            }
            this.nmi = nmi;
        }
        /// <summary>Internal function. Updates the IRQ and NMI edge
        /// flags.</summary>
        internal void CheckIRQEdge() {
            irq_pending = irq && (p & P_I) == 0;
            nmi_pending = nmi_edge;
        }
        /// <summary>Set N and Z flags according to the argument
        /// value.</summary>
        internal void NZP(byte v) {
            p = (byte)((p & 0x7F) | (v & 0x80));
            if(v == 0) p |= P_Z;
            else p &= (byte)(~P_Z & 0xFF);
        }
        /// <summary>Set N and Z flags according to the argument
        /// value, and the C flag according to the argument..</summary>
        internal void CNZP(bool c, byte v) {
            p = (byte)((p & 0x7F) | (v & 0x80));
            if(v == 0) p |= P_Z;
            else p &= (byte)(~P_Z & 0xFF);
            if(c) p |= P_C;
            else p &= (byte)(~P_C & 0xFF);
        }
        /// <summary>Step the processor once.</summary>
        /// <remarks>
        ///   <para>
        ///     This means executing an interrupt sequence, fetching an
        ///     instruction, or doing a spurious read, depending on the current
        ///     state of the processor. Returns the new state.
        ///   </para>
        ///   <para>
        ///     Always executes at least one bus cycle. May execute more.
        ///   </para>
        /// </remarks>
        public State Step<B>(B bus) where B: Bus {
            switch(state) {
                case State.Stopped: bus.ReadOperandSpurious(this, pc); break;
                case State.AwaitingInterrupt:
                    if(irq || nmi_edge) {
                        state = State.Running;
                        bus.ReadOperandSpurious(this, pc);
                    }
                    CheckIRQEdge();
                    bus.ReadOperandSpurious(this, pc);
                    break;
                case State.HasBeenReset:
                    // first, we spuriously read an opcode
                    bus.ReadOpcodeSpurious(this, pc);
                    // second, we read ... the same byte, but with SYNC low
                    bus.ReadOperandSpurious(this, pc);
                    // three spurious pushes...
                    SpuriousPush(bus);
                    SpuriousPush(bus);
                    SpuriousPush(bus);
                    // clear the D flag, set the I flag
                    p &= (byte)(~P_D & 0xFF);
                    p |= P_I;
                    // read the reset vector, non-spuriously
                    pc = (ushort)((pc & 0xFF00)
                                  | ((ushort)bus.ReadVector(this,
                                                            RESET_VECTOR)));
                    pc = (ushort)((pc & 0x00FF)
                                  | ((ushort)(bus.ReadVector(this,
                                                             RESET_VECTOR+1))
                                     << 8));
                    // we are ready to be actually running!
                    state = State.Running;
                    break;
                case State.Running:
                    if(nmi_pending) {
                        nmi_pending = false;
                        nmi_edge = false;
                        var opcode_addr = GetPC();
                        bus.ReadOpcodeSpurious(this, opcode_addr);
                        bus.ReadSpurious(this, opcode_addr);
                        Push(bus, (byte)(opcode_addr >> 8));
                        Push(bus, (byte)(opcode_addr));
                        Push(bus, (byte)(p & ((byte)(~P_B & 0xFF)) | P_1));
                        p &= (byte)(~P_D & 0xFF);
                        p |= P_I;
                        pc = (ushort)((pc & 0xFF00)
                                      | (ushort)(bus.ReadVector(this,
                                                                NMI_VECTOR)));
                        pc = (ushort)((pc & 0x00FF)
                                      | (ushort)((bus.ReadVector(this,
                                                                 NMI_VECTOR+1))
                                                 << 8));
                    }
                    else if(irq_pending) {
                        irq_pending = false;
                        var opcode_addr = GetPC();
                        bus.ReadOpcodeSpurious(this, opcode_addr);
                        bus.ReadSpurious(this, opcode_addr);
                        Push(bus, (byte)(opcode_addr >> 8));
                        Push(bus, (byte)(opcode_addr));
                        Push(bus, (byte)(p | P_1));
                        p &= (byte)(~P_D & 0xFF);
                        p |= P_I;
                        pc = (ushort)((pc & 0xFF00)
                                      | (ushort)(bus.ReadVector(this,
                                                                IRQ_VECTOR)));
                        pc = (ushort)((pc & 0x00FF)
                                      | (ushort)((bus.ReadVector(this,
                                                                 IRQ_VECTOR+1))
                                                 << 8));
                    }
                    else {
                        // oh boy, we're running! oh boy oh boy!
                        // hey, wait, didn't I read that exact comment in the
                        // Rust version's code? I'm plagiarizing myself!
                        var opcode_addr = ReadPCPostIncrement();
                        var opcode = bus.ReadOpcode(this, opcode_addr);
                        switch(opcode) {
                            case 0x00: brk(bus); break;
                            case 0x01: ora<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0x02: nop<ImmediateValue, Immediate, B>(bus); break;
                            case 0x03: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x04: tsb<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x05: ora<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x06: asl<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x07: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x01 & 0xFF)); break;
                            case 0x08: php(bus); break;
                            case 0x09: ora<ImmediateValue, Immediate, B>(bus); break;
                            case 0x0A: asl<ImpliedA, ImpliedA, B>(bus); break;
                            case 0x0B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x0C: tsb<SimpleEA, Absolute, B>(bus); break;
                            case 0x0D: ora<SimpleEA, Absolute, B>(bus); break;
                            case 0x0E: asl<SimpleEA, Absolute, B>(bus); break;
                            case 0x0F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x01); break;
                            case 0x10: branch<RelativeTarget, Relative, B>(bus, (p & P_N) == 0); break;
                            case 0x11: ora<SimpleEA, ZeroPageIndirectY, B>(bus); break;
                            case 0x12: ora<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0x13: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x14: trb<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x15: ora<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x16: asl<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x17: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x02 & 0xFF)); break;
                            case 0x18: clc(bus); break;
                            case 0x19: ora<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0x1A: inc<ImpliedA, ImpliedA, B>(bus); break;
                            case 0x1B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x1C: trb<SimpleEA, Absolute, B>(bus); break;
                            case 0x1D: ora<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x1E: asl<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x1F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x02); break;
                            case 0x20: jsr(bus); break;
                            case 0x21: and<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0x22: nop<ImmediateValue, Immediate, B>(bus); break;
                            case 0x23: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x24: bit<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x25: and<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x26: rol<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x27: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x04 & 0xFF)); break;
                            case 0x28: plp(bus); break;
                            case 0x29: and<ImmediateValue, Immediate, B>(bus); break;
                            case 0x2A: rol<ImpliedA, ImpliedA, B>(bus); break;
                            case 0x2B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x2C: bit<SimpleEA, Absolute, B>(bus); break;
                            case 0x2D: and<SimpleEA, Absolute, B>(bus); break;
                            case 0x2E: rol<SimpleEA, Absolute, B>(bus); break;
                            case 0x2F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x04); break;
                            case 0x30: branch<RelativeTarget, Relative, B>(bus, (p & P_N) == P_N); break;
                            case 0x31: and<SimpleEA, ZeroPageIndirectY, B>(bus); break;
                            case 0x32: and<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0x33: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x34: bit<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x35: and<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x36: rol<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x37: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x08 & 0xFF)); break;
                            case 0x38: sec(bus); break;
                            case 0x39: and<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0x3A: dec<ImpliedA, ImpliedA, B>(bus); break;
                            case 0x3B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x3C: bit<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x3D: and<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x3E: rol<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x3F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x08); break;
                            case 0x40: rti(bus); break;
                            case 0x41: eor<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0x42: nop<ImmediateValue, Immediate, B>(bus); break;
                            case 0x43: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x44: nop<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x45: eor<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x46: lsr<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x47: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x10 & 0xFF)); break;
                            case 0x48: pha(bus); break;
                            case 0x49: eor<ImmediateValue, Immediate, B>(bus); break;
                            case 0x4A: lsr<ImpliedA, ImpliedA, B>(bus); break;
                            case 0x4B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x4C: jmp<SimpleEA, Absolute, B>(bus); break;
                            case 0x4D: eor<SimpleEA, Absolute, B>(bus); break;
                            case 0x4E: lsr<SimpleEA, Absolute, B>(bus); break;
                            case 0x4F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x10); break;
                            case 0x50: branch<RelativeTarget, Relative, B>(bus, (p & P_V) == 0); break;
                            case 0x51: eor<SimpleEA, ZeroPageIndirectY, B>(bus); break;
                            case 0x52: eor<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0x53: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x54: nop<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x55: eor<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x56: lsr<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x57: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x20 & 0xFF)); break;
                            case 0x58: cli(bus); break;
                            case 0x59: eor<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0x5A: phy(bus); break;
                            case 0x5B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x5C: nop_5c<SimpleEA, Absolute, B>(bus); break;
                            case 0x5D: eor<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x5E: lsr<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x5F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x20); break;
                            case 0x60: rts(bus); break;
                            case 0x61: adc<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0x62: nop<ImmediateValue, Immediate, B>(bus); break;
                            case 0x63: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x64: stz<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x65: adc<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x66: ror<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x67: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x40 & 0xFF)); break;
                            case 0x68: pla(bus); break;
                            case 0x69: adc<ImmediateValue, Immediate, B>(bus); break;
                            case 0x6A: ror<ImpliedA, ImpliedA, B>(bus); break;
                            case 0x6B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x6C: jmp<SimpleEA, AbsoluteIndirect, B>(bus); break;
                            case 0x6D: adc<SimpleEA, Absolute, B>(bus); break;
                            case 0x6E: ror<SimpleEA, Absolute, B>(bus); break;
                            case 0x6F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x40); break;
                            case 0x70: branch<RelativeTarget, Relative, B>(bus, (p & P_V) == P_V); break;
                            case 0x71: adc<SimpleEA, ZeroPageIndirectY, B>(bus); break;
                            case 0x72: adc<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0x73: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x74: stz<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x75: adc<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x76: ror<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x77: rmb<SimpleEA, ZeroPage, B>(bus, (byte)(~0x80 & 0xFF)); break;
                            case 0x78: sei(bus); break;
                            case 0x79: adc<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0x7A: ply(bus); break;
                            case 0x7B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x7C: jmp<SimpleEA, AbsoluteXIndirect, B>(bus); break;
                            case 0x7D: adc<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x7E: ror<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0x7F: bbr<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x80); break;
                            case 0x80: branch<RelativeTarget, Relative, B>(bus, true); break;
                            case 0x81: sta<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0x82: nop<ImmediateValue, Immediate, B>(bus); break;
                            case 0x83: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x84: sty<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x85: sta<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x86: stx<SimpleEA, ZeroPage, B>(bus); break;
                            case 0x87: smb<SimpleEA, ZeroPage, B>(bus, 0x01); break;
                            case 0x88: dec<ImpliedY, ImpliedY, B>(bus); break;
                            case 0x89: bit_i<ImmediateValue, Immediate, B>(bus); break;
                            case 0x8A: txa(bus); break;
                            case 0x8B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x8C: sty<SimpleEA, Absolute, B>(bus); break;
                            case 0x8D: sta<SimpleEA, Absolute, B>(bus); break;
                            case 0x8E: stx<SimpleEA, Absolute, B>(bus); break;
                            case 0x8F: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x01); break;
                            case 0x90: branch<RelativeTarget, Relative, B>(bus, (p & P_C) == 0); break;
                            case 0x91: sta<SimpleEA, ZeroPageIndirectYSlower, B>(bus); break;
                            case 0x92: sta<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0x93: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x94: sty<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x95: sta<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0x96: stx<SimpleEA, ZeroPageY, B>(bus); break;
                            case 0x97: smb<SimpleEA, ZeroPage, B>(bus, 0x02); break;
                            case 0x98: tya(bus); break;
                            case 0x99: sta<SimpleEA, AbsoluteYSlower, B>(bus); break;
                            case 0x9A: txs(bus); break;
                            case 0x9B: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0x9C: stz<SimpleEA, Absolute, B>(bus); break;
                            case 0x9D: sta<SimpleEA, AbsoluteXSlower, B>(bus); break;
                            case 0x9E: stz<SimpleEA, AbsoluteXSlower, B>(bus); break;
                            case 0x9F: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x02); break;
                            case 0xA0: ldy<ImmediateValue, Immediate, B>(bus); break;
                            case 0xA1: lda<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0xA2: ldx<ImmediateValue, Immediate, B>(bus); break;
                            case 0xA3: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xA4: ldy<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xA5: lda<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xA6: ldx<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xA7: smb<SimpleEA, ZeroPage, B>(bus, 0x04); break;
                            case 0xA8: tay(bus); break;
                            case 0xA9: lda<ImmediateValue, Immediate, B>(bus); break;
                            case 0xAA: tax(bus); break;
                            case 0xAB: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xAC: ldy<SimpleEA, Absolute, B>(bus); break;
                            case 0xAD: lda<SimpleEA, Absolute, B>(bus); break;
                            case 0xAE: ldx<SimpleEA, Absolute, B>(bus); break;
                            case 0xAF: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x04); break;
                            case 0xB0: branch<RelativeTarget, Relative, B>(bus, (p & P_C) == P_C); break;
                            case 0xB1: lda<SimpleEA, ZeroPageIndirectY, B>(bus); break;
                            case 0xB2: lda<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0xB3: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xB4: ldy<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xB5: lda<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xB6: ldx<SimpleEA, ZeroPageY, B>(bus); break;
                            case 0xB7: smb<SimpleEA, ZeroPage, B>(bus, 0x08); break;
                            case 0xB8: clv(bus); break;
                            case 0xB9: lda<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0xBA: tsx(bus); break;
                            case 0xBB: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xBC: ldy<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0xBD: lda<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0xBE: ldx<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0xBF: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x08); break;
                            case 0xC0: cpy<ImmediateValue, Immediate, B>(bus); break;
                            case 0xC1: cmp<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0xC2: nop<ImmediateValue, Immediate, B>(bus); break;
                            case 0xC3: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xC4: cpy<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xC5: cmp<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xC6: dec<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xC7: smb<SimpleEA, ZeroPage, B>(bus, 0x10); break;
                            case 0xC8: inc<ImpliedY, ImpliedY, B>(bus); break;
                            case 0xC9: cmp<ImmediateValue, Immediate, B>(bus); break;
                            case 0xCA: dec<ImpliedX, ImpliedX, B>(bus); break;
                            case 0xCB: wai(bus); break;
                            case 0xCC: cpy<SimpleEA, Absolute, B>(bus); break;
                            case 0xCD: cmp<SimpleEA, Absolute, B>(bus); break;
                            case 0xCE: dec<SimpleEA, Absolute, B>(bus); break;
                            case 0xCF: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x10); break;
                            case 0xD0: branch<RelativeTarget, Relative, B>(bus, (p & P_Z) == 0); break;
                            case 0xD1: cmp<SimpleEA, ZeroPageIndirectY, B>(bus); break;
                            case 0xD2: cmp<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0xD3: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xD4: nop<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xD5: cmp<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xD6: dec<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xD7: smb<SimpleEA, ZeroPage, B>(bus, 0x20); break;
                            case 0xD8: cld(bus); break;
                            case 0xD9: cmp<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0xDA: phx(bus); break;
                            case 0xDB: stp(bus); break;
                            case 0xDC: nop<SimpleEA, Absolute, B>(bus); break;
                            case 0xDD: cmp<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0xDE: dec<SimpleEA, AbsoluteXSlower, B>(bus); break;
                            case 0xDF: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x20); break;
                            case 0xE0: cpx<ImmediateValue, Immediate, B>(bus); break;
                            case 0xE1: sbc<SimpleEA, ZeroPageXIndirect, B>(bus); break;
                            case 0xE2: nop<ImmediateValue, Immediate, B>(bus); break;
                            case 0xE3: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xE4: cpx<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xE5: sbc<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xE6: inc<SimpleEA, ZeroPage, B>(bus); break;
                            case 0xE7: smb<SimpleEA, ZeroPage, B>(bus, 0x40); break;
                            case 0xE8: inc<ImpliedX, ImpliedX, B>(bus); break;
                            case 0xE9: sbc<ImmediateValue, Immediate, B>(bus); break;
                            case 0xEA: nop<ImmediateValue, Implied, B>(bus); break;
                            case 0xEB: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xEC: cpx<SimpleEA, Absolute, B>(bus); break;
                            case 0xED: sbc<SimpleEA, Absolute, B>(bus); break;
                            case 0xEE: inc<SimpleEA, Absolute, B>(bus); break;
                            case 0xEF: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x40); break;
                            case 0xF0: branch<RelativeTarget, Relative, B>(bus, (p & P_Z) == P_Z); break;
                            case 0xF1: sbc<SimpleEA, ZeroPageIndirectY, B>(bus); break;
                            case 0xF2: sbc<SimpleEA, ZeroPageIndirect, B>(bus); break;
                            case 0xF3: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xF4: nop<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xF5: sbc<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xF6: inc<SimpleEA, ZeroPageX, B>(bus); break;
                            case 0xF7: smb<SimpleEA, ZeroPage, B>(bus, 0x80); break;
                            case 0xF8: sed(bus); break;
                            case 0xF9: sbc<SimpleEA, AbsoluteY, B>(bus); break;
                            case 0xFA: plx(bus); break;
                            case 0xFB: nop<ImmediateValue, FastImplied, B>(bus); break;
                            case 0xFC: nop<SimpleEA, Absolute, B>(bus); break;
                            case 0xFD: sbc<SimpleEA, AbsoluteX, B>(bus); break;
                            case 0xFE: inc<SimpleEA, AbsoluteXSlower, B>(bus); break;
                            case 0xFF: bbs<RelativeBitBranchTarget, RelativeBitBranch, B>(bus, 0x80); break;
                        }
                    }
                    break;
            }
            return state;
        }
    }
}
