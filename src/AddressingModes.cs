namespace W65C02S {
    interface IAddressingMode<Result> {
        /// <summary>Fetch and process the operand, if any. (Do a spurious
        /// fetch if not.)</summary>
        Result GetOperand<B>(B bus, CPU cpu) where B: Bus;
    }
    interface IReadable {
        /// <summary>Read the addressed data.</summary>
        byte Read<B>(B bus, CPU cpu) where B: Bus;
        /// <summary>Spuriously read the addressed data.</summary>
        void ReadSpurious<B>(B bus, CPU cpu) where B: Bus;
    }
    interface IWritable {
        /// <summary>Write the addressed data.</summary>
        void Write<B>(B bus, CPU cpu, byte data) where B: Bus;
    }
    interface IRMWable {
        /// <summary>Perform the first read of a RMBx/SMBx
        /// instruction.</summary>
        byte ReadLocked<B>(B bus, CPU cpu) where B: Bus;
        /// <summary>Perform the second read of a RMW instruction.</summary>
        void ReadLockedSpurious<B>(B bus, CPU cpu) where B: Bus;
        /// <summary>Perform the write of a RMW instruction.</summary>
        void WriteLocked<B>(B bus, CPU cpu, byte data) where B: Bus;
    }
    interface IHasEA {
        /// <summary>Return the effective address of this addressing
        /// mode.</summary>
        ushort GetEffectiveAddress();
    }
    interface IBranchable {
        /// <summary>Return the target address to branch to, if the branch is
        /// taken.</summary>
        ushort GetBranchTarget<B>(B bus, CPU cpu) where B: Bus;
    }
    struct SimpleEA : IReadable, IWritable, IRMWable, IHasEA {
        ushort ea;
        public SimpleEA(ushort ea) { this.ea = ea; }
        public SimpleEA(byte ea_high, byte ea_low) {
            ea = (ushort)((((ushort)ea_high) << 8) | ((ushort)ea_low));
        }
        public byte Read<B>(B bus, CPU cpu) where B: Bus
            => bus.Read(cpu, ea);
        public void ReadSpurious<B>(B bus, CPU cpu) where B: Bus
            => bus.ReadSpurious(cpu, ea);
        public void Write<B>(B bus, CPU cpu, byte data) where B: Bus
            => bus.Write(cpu, ea, data);
        public byte ReadLocked<B>(B bus, CPU cpu) where B: Bus
            => bus.ReadLocked(cpu, ea);
        public void ReadLockedSpurious<B>(B bus, CPU cpu) where B: Bus
            => bus.ReadLockedSpurious(cpu, ea);
        public void WriteLocked<B>(B bus, CPU cpu, byte data) where B: Bus
            => bus.WriteLocked(cpu, ea, data);
        public ushort GetEffectiveAddress() => ea;
    }
    struct Implied : IAddressingMode<ImmediateValue> {
        public ImmediateValue GetOperand<B>(B bus, CPU cpu) where B: Bus {
            bus.ReadOperandSpurious(cpu, cpu.GetPC());
            return new ImmediateValue(0);
        }
    }
    struct FastImplied : IAddressingMode<ImmediateValue> {
        public ImmediateValue GetOperand<B>(B bus, CPU cpu) where B: Bus {
            return new ImmediateValue(0);
        }
    }
    struct Absolute : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var ea_low = bus.ReadOperand(cpu, pc);
            pc = cpu.ReadPCPostIncrement();
            var ea_high = bus.ReadOperand(cpu, pc);
            return new SimpleEA(ea_high, ea_low);
        }
    }
    struct AbsoluteIndirect : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var addr_low = bus.ReadOperand(cpu, pc);
            pc = cpu.GetPC();
            var addr_high = bus.ReadOperand(cpu, pc);
            var addr = (ushort)(((ushort)addr_high) << 8)
                | ((ushort)addr_low);
            pc = cpu.ReadPCPostIncrement();
            bus.ReadSpurious(cpu, pc);
            var ea_low = bus.ReadPointer(cpu, (ushort)addr);
            var ea_high = bus.ReadPointer(cpu, (ushort)(addr + 1));
            return new SimpleEA(ea_high, ea_low);
        }
    }
    struct AbsoluteX : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato_low = bus.ReadOperand(cpu, pc);
            pc = cpu.ReadPCPostIncrement();
            var staato_high = bus.ReadOperand(cpu, pc);
            var staato = (ushort)((((ushort)staato_high) << 8)
                                  | ((ushort)staato_low));
            var ea = (ushort)(staato + (ushort)cpu.GetX());
            if((ea & 0xFF00) != (staato & 0xFF00)) {
                pc = (ushort)(cpu.GetPC() - 1);
                bus.ReadSpurious(cpu, pc);
            }
            return new SimpleEA(ea);
        }
    }
    struct AbsoluteXSlower : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato_low = bus.ReadOperand(cpu, pc);
            pc = cpu.ReadPCPostIncrement();
            var staato_high = bus.ReadOperand(cpu, pc);
            var staato = (ushort)((((ushort)staato_high) << 8)
                                  | (ushort)staato_low);
            var ea = (ushort)(staato + (ushort)cpu.GetX());
            if((ea & 0xFF00) != (staato & 0xFF00)) {
                pc = (ushort)(cpu.GetPC() - 1);
                bus.ReadSpurious(cpu, pc);
            }
            else {
                bus.ReadSpurious(cpu, ea);
            }
            return new SimpleEA(ea);
        }
    }

    struct AbsoluteXIndirect : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato_low = bus.ReadOperand(cpu, pc);
            pc = cpu.GetPC();
            var staato_high = bus.ReadOperand(cpu, pc);
            var staato = (ushort)((((ushort)staato_high) << 8)
                                  | (ushort)staato_low);
            var addr = (ushort)(staato + (ushort)cpu.GetX());
            pc = cpu.ReadPCPostIncrement();
            bus.ReadSpurious(cpu, pc);
            var ea_low = bus.ReadPointer(cpu, addr);
            var ea_high = bus.ReadPointer(cpu, (ushort)(addr+1));
            return new SimpleEA(ea_high, ea_low);
        }
    }
    struct AbsoluteY : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato_low = bus.ReadOperand(cpu, pc);
            pc = cpu.ReadPCPostIncrement();
            var staato_high = bus.ReadOperand(cpu, pc);
            var staato = (ushort)((((ushort)staato_high) << 8)
                                  | (ushort)staato_low);
            var ea = (ushort)(staato + (ushort)cpu.GetY());
            if((ea & 0xFF00) != (staato & 0xFF00)) {
                pc = (ushort)(cpu.GetPC() - 1);
                bus.ReadSpurious(cpu, pc);
            }
            return new SimpleEA(ea);
        }
    }
    struct AbsoluteYSlower : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato_low = bus.ReadOperand(cpu, pc);
            pc = cpu.ReadPCPostIncrement();
            var staato_high = bus.ReadOperand(cpu, pc);
            var staato = (ushort)((((ushort)staato_high) << 8)
                                  | (ushort)staato_low);
            var ea = (ushort)(staato + (ushort)cpu.GetY());
            if((ea & 0xFF00) != (staato & 0xFF00)) {
                pc = (ushort)(cpu.GetPC() - 1);
                bus.ReadSpurious(cpu, pc);
            }
            else {
                bus.ReadSpurious(cpu, ea);
            }
            return new SimpleEA(ea);
        }
    }
    struct ZeroPage : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var ea = (ushort)bus.ReadOperand(cpu, pc);
            return new SimpleEA(ea);
        }
    }
    struct ZeroPageIndirect : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var addr = bus.ReadOperand(cpu, pc);
            var ea_low = bus.ReadPointer(cpu, (ushort)addr);
            var ea_high = bus.ReadPointer(cpu, (ushort)(byte)(addr+1));
            return new SimpleEA(ea_high, ea_low);
        }
    }
    struct ZeroPageX : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato = bus.ReadOperand(cpu, pc);
            var ea = (ushort)(byte)(staato + (byte)cpu.GetX());
            bus.ReadSpurious(cpu, (ushort)(cpu.GetPC()-1));
            return new SimpleEA(ea);
        }
    }
    struct ZeroPageXIndirect : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato = bus.ReadOperand(cpu, pc);
            var addr = (ushort)(byte)(staato + (ushort)cpu.GetX());
            bus.ReadSpurious(cpu, (ushort)(cpu.GetPC()-1));
            var ea_low = bus.ReadPointer(cpu, (ushort)addr);
            var ea_high = bus.ReadPointer(cpu, (ushort)(byte)(addr + 1));
            return new SimpleEA(ea_high, ea_low);
        }
    }
    struct ZeroPageY : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato = bus.ReadOperand(cpu, pc);
            var ea = (ushort)(byte)(staato + (byte)cpu.GetY());
            bus.ReadSpurious(cpu, (ushort)(cpu.GetPC()-1));
            return new SimpleEA(ea);
        }
    }
    struct ZeroPageIndirectY : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato = bus.ReadOperand(cpu, pc);
            var addr_low = bus.ReadPointer(cpu, (ushort)staato);
            var addr_high = bus.ReadPointer(cpu, (ushort)((staato + 1) & 0xFF));
            var addr = (ushort)((((ushort)addr_high) << 8) | (ushort)addr_low);
            var ea = (ushort)(addr + (ushort)cpu.GetY());
            if((ea & 0xFF00) != (addr & 0xFF00)) {
                bus.ReadSpurious(cpu, (ushort)(byte)(staato + 1));
            }
            return new SimpleEA(ea);
        }
    }
    struct ZeroPageIndirectYSlower : IAddressingMode<SimpleEA> {
        public SimpleEA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var staato = bus.ReadOperand(cpu, pc);
            var addr_low = bus.ReadPointer(cpu, (ushort)staato);
            var addr_high = bus.ReadPointer(cpu, (ushort)((staato + 1) & 0xFF));
            var addr = (ushort)((((ushort)addr_high) << 8) | (ushort)addr_low);
            var ea = (ushort)(addr + (short)cpu.GetY());
            bus.ReadSpurious(cpu, (ushort)(byte)(staato + 1));
            return new SimpleEA(ea);
        }
    }
    struct ImpliedA : IAddressingMode<ImpliedA>, IReadable, IWritable, IRMWable {
        public ImpliedA GetOperand<B>(B bus, CPU cpu) where B: Bus {
            bus.ReadOperandSpurious(cpu, cpu.GetPC());
            return this;
        }
        public byte Read<B>(B bus, CPU cpu) where B: Bus => cpu.GetA();
        public void ReadSpurious<B>(B bus, CPU cpu) where B: Bus {}
        public byte ReadLocked<B>(B bus, CPU cpu) where B: Bus => cpu.GetA();
        public void ReadLockedSpurious<B>(B bus, CPU cpu) where B: Bus {}
        public void Write<B>(B bus, CPU cpu, byte data) where B: Bus
            => cpu.SetA(data);
        public void WriteLocked<B>(B bus, CPU cpu, byte data) where B: Bus
            => cpu.SetA(data);
    }
    struct ImpliedX : IAddressingMode<ImpliedX>, IReadable, IWritable, IRMWable {
        public ImpliedX GetOperand<B>(B bus, CPU cpu) where B: Bus {
            bus.ReadOperandSpurious(cpu, cpu.GetPC());
            return this;
        }
        public byte Read<B>(B bus, CPU cpu) where B: Bus => cpu.GetX();
        public void ReadSpurious<B>(B bus, CPU cpu) where B: Bus {}
        public byte ReadLocked<B>(B bus, CPU cpu) where B: Bus => cpu.GetX();
        public void ReadLockedSpurious<B>(B bus, CPU cpu) where B: Bus {}
        public void Write<B>(B bus, CPU cpu, byte data) where B: Bus
            => cpu.SetX(data);
        public void WriteLocked<B>(B bus, CPU cpu, byte data) where B: Bus
            => cpu.SetX(data);
    }
    struct ImpliedY : IAddressingMode<ImpliedY>, IReadable, IWritable, IRMWable {
        public ImpliedY GetOperand<B>(B bus, CPU cpu) where B: Bus {
            bus.ReadOperandSpurious(cpu, cpu.GetPC());
            return this;
        }
        public byte Read<B>(B bus, CPU cpu) where B: Bus => cpu.GetY();
        public void ReadSpurious<B>(B bus, CPU cpu) where B: Bus {}
        public byte ReadLocked<B>(B bus, CPU cpu) where B: Bus => cpu.GetY();
        public void ReadLockedSpurious<B>(B bus, CPU cpu) where B: Bus {}
        public void Write<B>(B bus, CPU cpu, byte data) where B: Bus
            => cpu.SetY(data);
        public void WriteLocked<B>(B bus, CPU cpu, byte data) where B: Bus
            => cpu.SetY(data);
    }
    struct ImmediateValue : IReadable {
        byte val;
        public ImmediateValue(byte val) { this.val = val; }
        public byte Read<B>(B bus, CPU cpu) where B: Bus => val;
        public void ReadSpurious<B>(B bus, CPU cpu) where B: Bus {}
    }
    struct Immediate : IAddressingMode<ImmediateValue> {
        public ImmediateValue GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var val = bus.ReadOperand(cpu, pc);
            return new ImmediateValue(val);
        }
    }
    struct RelativeTarget : IBranchable {
        ushort target;
        public RelativeTarget(ushort target) { this.target = target; }
        public ushort GetBranchTarget<B>(B bus, CPU cpu) where B: Bus {
            // always burn one cycle
            bus.ReadSpurious(cpu, cpu.GetPC());
            if((cpu.GetPC() & 0xFF00) != (target & 0xFF00)) {
                var old_irq_pending = cpu.irq_pending;
                cpu.CheckIRQEdge();
                cpu.irq_pending = cpu.irq_pending | old_irq_pending;
                // another cycle burns!
                bus.ReadSpurious(cpu, cpu.GetPC());
            }
            return target;
        }
    }
    struct Relative : IAddressingMode<RelativeTarget> {
        public RelativeTarget GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var val = (sbyte)bus.ReadOperand(cpu, pc);
            var target = (ushort)(cpu.GetPC() + val);
            return new RelativeTarget(target);
        }
    }
    struct RelativeBitBranchTarget : IBranchable, IReadable {
        byte data;
        ushort target;
        public RelativeBitBranchTarget(byte data, ushort target) {
            this.data = data;
            this.target = target;
        }
        public byte Read<B>(B bus, CPU cpu) where B: Bus => data;
        public void ReadSpurious<B>(B bus, CPU cpu) where B: Bus {}
        public ushort GetBranchTarget<B>(B bus, CPU cpu) where B: Bus {
            // always burn one cycle
            bus.ReadSpurious(cpu, cpu.GetPC());
            if((cpu.GetPC() & 0xFF00) != (target & 0xFF00)) {
                var old_irq_pending = cpu.irq_pending;
                cpu.CheckIRQEdge();
                cpu.irq_pending = cpu.irq_pending | old_irq_pending;
                // another cycle burns!
                bus.ReadSpurious(cpu, cpu.GetPC());
            }
            return target;
        }
    }
    struct RelativeBitBranch : IAddressingMode<RelativeBitBranchTarget> {
        public RelativeBitBranchTarget GetOperand<B>(B bus, CPU cpu) where B: Bus {
            var pc = cpu.ReadPCPostIncrement();
            var addr = bus.ReadOperand(cpu, pc);
            var data = bus.Read(cpu, (ushort)addr);
            bus.ReadSpurious(cpu, (ushort)addr);
            pc = cpu.ReadPCPostIncrement();
            var value = (sbyte)bus.ReadOperand(cpu, pc);
            var target = (ushort)(cpu.GetPC() + (ushort)value);
            return new RelativeBitBranchTarget(data, target);
        }
    }
}
