namespace W65C02S {
    public partial class CPU {
        internal void brk<B>(B bus) where B: Bus {
            var pc = ReadPCPostIncrement();
            bus.ReadOperandSpurious(this, pc);
            pc = GetPC();
            Push(bus, (byte)(pc >> 8));
            Push(bus, (byte)pc);
            Push(bus, (byte)(p | P_B));
            p &= (byte)(~P_D & 0xFF);
            p |= P_I;
            pc = (ushort)((pc & 0xFF00) | ((ushort)bus.ReadVector(this, IRQ_VECTOR)));
            pc = (ushort)((pc & 0x00FF) | (((ushort)bus.ReadVector(this, IRQ_VECTOR+1)) << 8));
        }
        internal void jsr<B>(B bus) where B: Bus {
            var pc = ReadPCPostIncrement();
            var target_lo = bus.ReadOperand(this, pc);
            SpuriousStackRead(bus);
            Push(bus, (byte)(pc >> 8));
            Push(bus, (byte)pc);
            CheckIRQEdge();
            var target_hi = bus.ReadOperand(this, pc);
            pc = (ushort)((((ushort)target_hi) << 8) | (ushort)target_lo);
        }
        internal void rts<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            SpuriousStackRead(bus);
            pc = (ushort)((pc & 0xFF00) | (ushort)Pop(bus));
            pc = (ushort)((pc & 0x00FF) | (((ushort)Pop(bus)) << 8));
            CheckIRQEdge();
            bus.ReadOperandSpurious(this, pc);
            ++pc;
        }
        internal void rti<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            SpuriousStackRead(bus);
            p = Pop(bus);
            pc = (ushort)((pc & 0xFF00) | (ushort)Pop(bus));
            CheckIRQEdge();
            pc = (ushort)((pc & 0x00FF) | (((ushort)Pop(bus)) << 8));
        }
        internal void jmp<R, AM, B>(B bus) where B: Bus where R: IHasEA where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            pc = am.GetEffectiveAddress();
        }
        internal void sta<R, AM, B>(B bus) where B: Bus where R: IWritable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            am.Write(bus, this, a);
        }
        internal void stx<R, AM, B>(B bus) where B: Bus where R: IWritable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            am.Write(bus, this, x);
        }
        internal void sty<R, AM, B>(B bus) where B: Bus where R: IWritable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            am.Write(bus, this, y);
        }
        internal void stz<R, AM, B>(B bus) where B: Bus where R: IWritable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            am.Write(bus, this, 0);
        }
        internal void lda<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            a = am.Read(bus, this);
            NZP(a);
        }
        internal void ldx<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            x = am.Read(bus, this);
            NZP(x);
        }
        internal void ldy<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            y = am.Read(bus, this);
            NZP(y);
        }
        internal void ora<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            CheckIRQEdge();
            a |= new AM().GetOperand(bus, this).Read(bus, this);
            NZP(a);
        }
        internal void and<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            CheckIRQEdge();
            a &= new AM().GetOperand(bus, this).Read(bus, this);
            NZP(a);
        }
        internal void bit<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            CheckIRQEdge();
            var data = new AM().GetOperand(bus, this).Read(bus, this);
            if((data & a) == 0) { p |= P_Z; }
            else { p &= (byte)(~P_Z & 0xFF); }
            p = (byte)((p & 0x3F) | (data & 0xC0));
        }
        internal void bit_i<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            CheckIRQEdge();
            var data = new AM().GetOperand(bus, this).Read(bus, this);
            if((data & a) == 0) { p |= P_Z; }
            else { p &= (byte)(~P_Z & 0xFF); }
        }
        internal void eor<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            CheckIRQEdge();
            a ^= new AM().GetOperand(bus, this).Read(bus, this);
            NZP(a);
        }
        internal void nop<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            am.ReadSpurious(bus, this);
        }
        // $5C is an especially weird one
        internal void nop_5c<R, AM, B>(B bus) where B: Bus where R: IHasEA where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            bus.ReadSpurious(this, (ushort)(am.GetEffectiveAddress() | 0xFF00));
            bus.ReadSpurious(this, 0xFFFF);
            bus.ReadSpurious(this, 0xFFFF);
            bus.ReadSpurious(this, 0xFFFF);
            CheckIRQEdge();
            bus.ReadSpurious(this, 0xFFFF);
        }
        internal void trb<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            CheckIRQEdge();
            am.WriteLocked(bus, this, (byte)(data & ~a));
            if((data & a) != 0) { p &= (byte)(~P_Z & 0xFF); }
            else { p |= P_Z; }
        }
        internal void tsb<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            CheckIRQEdge();
            am.WriteLocked(bus, this, (byte)(data | a));
            if((data & a) != 0) { p &= (byte)(~P_Z & 0xFF); }
            else { p |= P_Z; }
        }
        internal void asl<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            var result = (byte)(data << 1);
            CheckIRQEdge();
            am.WriteLocked(bus, this, result);
            CNZP((data & 0x80) != 0, result);
        }
        internal void lsr<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            var result = (byte)(data >> 1);
            CheckIRQEdge();
            am.WriteLocked(bus, this, result);
            CNZP((data & 0x01) != 0, result);
        }
        internal void rol<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            var result = (byte)(data << 1);
            if((p & P_C) != 0) result |= 1;
            CheckIRQEdge();
            am.WriteLocked(bus, this, result);
            CNZP((data & 0x80) != 0, result);
        }
        internal void ror<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            var result = (byte)(data >> 1);
            if((p & P_C) != 0) result |= 0x80;
            CheckIRQEdge();
            am.WriteLocked(bus, this, result);
            CNZP((data & 0x01) != 0, result);
        }
        internal void inc<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            CheckIRQEdge();
            var result = (byte)(data + 1);
            am.WriteLocked(bus, this, result);
            NZP(result);
        }
        internal void dec<R, AM, B>(B bus) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.Read(bus, this);
            am.ReadLockedSpurious(bus, this);
            CheckIRQEdge();
            var result = (byte)(data - 1);
            am.WriteLocked(bus, this, result);
            NZP(result);
        }
        // note that unlike the other RMW instructions, RMBx/SMBx have THREE
        // locked cycles, not two.
        internal void rmb<R, AM, B>(B bus, byte mask) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.ReadLocked(bus, this);
            am.ReadLockedSpurious(bus, this);
            var result = (byte)(data & mask);
            CheckIRQEdge();
            am.WriteLocked(bus, this, result);
        }
        internal void smb<R, AM, B>(B bus, byte mask) where B: Bus where R: IReadable, IWritable, IRMWable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            var data = am.ReadLocked(bus, this);
            am.ReadLockedSpurious(bus, this);
            var result = (byte)(data | mask);
            CheckIRQEdge();
            am.WriteLocked(bus, this, result);
        }
        internal void branch<R, AM, B>(B bus, bool should_branch) where B: Bus where R: IBranchable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            if(should_branch) {
                pc = am.GetBranchTarget(bus, this);
            }
        }
        internal void bbr<R, AM, B>(Bus bus, byte mask) where B: Bus where R: IReadable, IBranchable where AM: IAddressingMode<R>, new()  {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            if((am.Read(bus, this) & mask) == 0) {
                pc = am.GetBranchTarget(bus, this);
            }
        }
        internal void bbs<R, AM, B>(Bus bus, byte mask) where B: Bus where R: IReadable, IBranchable where AM: IAddressingMode<R>, new()  {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            if((am.Read(bus, this) & mask) == mask) {
                pc = am.GetBranchTarget(bus, this);
            }
        }
        internal void stp<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            state = State.Stopped;
        }
        internal void wai<B>(B bus) {
            state = State.AwaitingInterrupt;
        }
        internal void clc<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            p &= (byte)(~P_C & 0xFF);
        }
        internal void sec<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            p |= P_C;
        }
        internal void clv<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            p &= (byte)(~P_V & 0xFF);
        }
        internal void cld<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            p &= (byte)(~P_D & 0xFF);
        }
        internal void sed<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            p |= P_D;
        }
        internal void cli<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            p &= (byte)(~P_I & 0xFF);
        }
        internal void sei<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            p |= P_I;
        }
        internal void php<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            CheckIRQEdge();
            Push(bus, (byte)(p | (P_B | P_1)));
        }
        internal void plp<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            SpuriousStackRead(bus);
            CheckIRQEdge();
            p = Pop(bus);
        }
        internal void pha<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            CheckIRQEdge();
            Push(bus, a);
        }
        internal void pla<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            SpuriousStackRead(bus);
            CheckIRQEdge();
            a = Pop(bus);
        }
        internal void phx<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            CheckIRQEdge();
            Push(bus, x);
        }
        internal void plx<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            SpuriousStackRead(bus);
            CheckIRQEdge();
            x = Pop(bus);
        }
        internal void phy<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            CheckIRQEdge();
            Push(bus, y);
        }
        internal void ply<B>(B bus) where B: Bus {
            bus.ReadOperandSpurious(this, pc);
            SpuriousStackRead(bus);
            CheckIRQEdge();
            y = Pop(bus);
        }
        internal void tax<B>(B bus) where B: Bus {
            CheckIRQEdge();
            bus.ReadOperandSpurious(this, pc);
            x = a;
            NZP(x);
        }
        internal void tay<B>(B bus) where B: Bus {
            CheckIRQEdge();
            bus.ReadOperandSpurious(this, pc);
            y = a;
            NZP(y);
        }
        internal void txa<B>(B bus) where B: Bus {
            CheckIRQEdge();
            bus.ReadOperandSpurious(this, pc);
            a = x;
            NZP(a);
        }
        internal void tya<B>(B bus) where B: Bus {
            CheckIRQEdge();
            bus.ReadOperandSpurious(this, pc);
            a = y;
            NZP(a);
        }
        internal void txs<B>(B bus) where B: Bus {
            CheckIRQEdge();
            bus.ReadOperandSpurious(this, pc);
            s = x;
        }
        internal void tsx<B>(B bus) where B: Bus {
            CheckIRQEdge();
            bus.ReadOperandSpurious(this, pc);
            x = s;
            NZP(x);
        }
        internal void adc<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            var red = am.Read(bus, this);
            ushort val;
            if((p & P_D) != 0) {
                CheckIRQEdge();
                am.ReadSpurious(bus, this);
                var al = (a & 0xF) + (red & 0xF);
                if((p & P_C) != 0) { ++al; }
                if(al > 9) { al = ((al + 6) & 0xF) | 0x10; }
                val = (ushort)((((ushort)(sbyte)a) & 0xFFF0) + (((ushort)(sbyte)red) & 0xFFF0) + (ushort)al);
                if(val >= 0x80 && val < 0xFF80) { p |= P_V; }
                else { p &= (byte)(~P_V & 0xFF); }
                val = (ushort)((((ushort)a) & 0xF0) + (((ushort)red) & 0xF0) + (ushort)al);
                if(val > 0x9F) { val = (ushort)((val + 0x60) | 0x100); }
            }
            else {
                val = (ushort)((ushort)a + (ushort)red);
                if((p & P_C) != 0) { ++val; }
                if(((a ^ (byte)val) & (red ^ (byte)val) & 0x80) != 0) {
                    p |= P_V;
                }
                else { p &= (byte)(~P_V & 0xFF); }
            }
            a = (byte)val;
            CNZP(val >= 0x0100, a);
        }
        internal void sbc<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            var red = am.Read(bus, this);
            ushort val;
            if((p & P_D) != 0) {
                CheckIRQEdge();
                am.ReadSpurious(bus, this);
                var al = (short)((short)(a & 0xF) - (short)(red & 0xF));
                if((p & P_C) == 0) --al;
                val = (ushort)(((ushort)a) - ((ushort)red));
                if((p & P_C) == 0) --val;
                if((((ushort)a ^ val) & ((ushort)red ^ 0xFF ^ val) & 0x80)
                   != 0) { p |= P_V; }
                else { p &= (byte)(~P_V & 0xFF); }
                if((val & 0x8000) != 0) {
                    val -= 0x60;
                    p &= (byte)(~P_C & 0xFF);
                }
                else {
                    p |= P_C;
                }
                if(al >= 0x80) { val -= 0x06; }
                NZP((byte)val);
            }
            else {
                red ^= 0xFF;
                val = (ushort)((ushort)a + (ushort)red);
                if((p & P_C) != 0) { ++val; }
                if(((a ^ (byte)val) & (red ^ (byte)val) & 0x80) != 0) {
                    p |= P_V;
                }
                else { p &= (byte)(~P_V & 0xFF); }
                CNZP(val >= 0x0100, (byte)val);
            }
            a = (byte)val;
        }
        internal void cmp<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            var red = am.Read(bus, this);
            var val = (ushort)((ushort)a + (ushort)(red ^ 0xFF) + 1);
            CNZP(val >= 0x0100, (byte)val);
        }
        internal void cpx<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            var red = am.Read(bus, this);
            var val = (ushort)((ushort)x + (ushort)(red ^ 0xFF) + 1);
            CNZP(val >= 0x0100, (byte)val);
        }
        internal void cpy<R, AM, B>(B bus) where B: Bus where R: IReadable where AM: IAddressingMode<R>, new() {
            var am = new AM().GetOperand(bus, this);
            CheckIRQEdge();
            var red = am.Read(bus, this);
            var val = (ushort)((ushort)y + (ushort)(red ^ 0xFF) + 1);
            CNZP(val >= 0x0100, (byte)val);
        }
    }
}
