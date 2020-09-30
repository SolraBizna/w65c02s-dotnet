using System;

namespace W65C02S {
    /// <summary>Implements a system connected to a W65C02S's bus. (This
    /// corresponds to the <tt>System</tt> type of my other W65C02S
    /// simulators.)</summary>
    /// <remarks>
    ///   <para>
    ///     Only <tt>Read</tt> and <tt>Write</tt> need to be implemented for a
    ///     simple system, but other systems may be more complicated; for
    ///     instance, many 65C02-based microcontrollers have advanced interrupt
    ///     vectoring logic that would require implementing
    ///     <tt>ReadVector</tt>.
    ///   </para>
    /// </remarks>
    public abstract class Bus {
        /// <summary>Read a byte of data from the given address.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH.
        ///   </para>
        /// </remarks>
        public abstract byte Read(CPU cpu, ushort addr);
        /// <summary>Read data from the given address as part of a
        /// Read-Modify-Write instruction.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC and MLB are LOW, VPB is HIGH.
        ///   </para>
        /// </remarks>
        public virtual byte ReadLocked(CPU cpu, ushort addr)
            => Read(cpu, addr);
        /// <summary>Second data read from the given address as part of a
        /// Read-Modify-Write instruction. This data is ignored; this is an
        /// "idle cycle".</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC and MLB are LOW, VPB is HIGH. Indistinguishable from a
        ///     locked data read on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual byte ReadLockedSpurious(CPU cpu, ushort addr)
            => ReadLocked(cpu, addr);
        /// <summary>Read an instruction opcode from the given
        /// address.</summary>
        /// <remarks>
        ///   <para>
        ///     VPB, MLB, and SYNC are all HIGH.
        ///   </para>
        /// </remarks>
        public virtual byte ReadOpcode(CPU cpu, ushort addr) => Read(cpu, addr);
        /// <summary>Read an instruction opcode whose execution will be
        /// preempted by an interrupt or a reset, or which follows a
        /// <tt>WAI</tt> or <tt>STP</tt> instruction that has not yet been
        /// broken out of.</summary>
        /// <remarks>
        ///   <para>
        ///     VPB, MLB, and SYNC are all HIGH. Indistinguishable from a
        ///     normal opcode fetch on real hardware, but the distinction may
        ///     be useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual byte ReadOpcodeSpurious(CPU cpu, ushort addr)
            => ReadOpcode(cpu, addr);
        /// <summary>Read an instruction operand from the given
        /// address.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH. Indistinguishable from an
        ///     ordinary data read on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual byte ReadOperand(CPU cpu, ushort addr)
            => Read(cpu, addr);
        /// <summary>Read an instruction operand from the given
        /// address, except that the instruction had an implied operand or was
        /// preempted by a reset..</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH. Indistinguishable from an
        ///     ordinary data read on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual void ReadOperandSpurious(CPU cpu, ushort addr)
            => Read(cpu, addr);
        /// <summary>Read part of a pointer from the given address.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH. Indistinguishable from an
        ///     ordinary data read on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual byte ReadPointer(CPU cpu, ushort addr)
            => Read(cpu, addr);
        /// <summary>Read a byte of data from the given address during an
        /// "internal operation" cycle.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH. Indistinguishable from an
        ///     ordinary data read on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual byte ReadSpurious(CPU cpu, ushort addr)
            => Read(cpu, addr);
        /// <summary>Pop a value from the stack at the given address.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH. Indistinguishable from an
        ///     ordinary data read on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual byte ReadStack(CPU cpu, ushort addr) => Read(cpu, addr);
        /// <summary>Spurious stack "read" that occurs during reset.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH. Indistinguishable from an
        ///     ordinary data read on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual byte ReadStackSpurious(CPU cpu, ushort addr)
            => ReadStack(cpu, addr);
        /// <summary>Read part of an interrupt vector from the given
        /// address.</summary>
        /// <remarks>
        ///   <para>
        ///     VPB is LOW, and SYNC and MLB are HIGH.
        ///   </para>
        /// </remarks>
        public virtual byte ReadVector(CPU cpu, ushort addr)
            => Read(cpu, addr);
        /// <summary>Write a byte of data to the given address.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH.
        ///   </para>
        /// </remarks>
        public abstract void Write(CPU cpu, ushort addr, byte data);
        /// <summary>Write a byte of data to the given address as the
        /// conclusion of a Read- Modify-Write instruction.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC and MLB are LOW, VPB is HIGH.
        ///   </para>
        /// </remarks>
        public virtual void WriteLocked(CPU cpu, ushort addr, byte data)
            => Write(cpu, addr, data);
        /// <summary>Push a byte of data onto the stack at the given
        /// address.</summary>
        /// <remarks>
        ///   <para>
        ///     SYNC is LOW and VPB and MLB are HIGH. Indistinguishable from a
        ///     normal data write on real hardware, but the distinction may be
        ///     useful for simulation.
        ///   </para>
        /// </remarks>
        public virtual void WriteStack(CPU cpu, ushort addr, byte data)
            => Write(cpu, addr, data);
    }
}
