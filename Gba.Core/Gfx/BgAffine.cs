using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class BgAffineMatrix
    {
        // These are 8.8 fixed point values. They must be sigend to work correctly.
        // |Pa Pb|
        // |Pc Pd|
      
        MemoryRegister16 pa;
        MemoryRegister16 pb;
        MemoryRegister16 pc;
        MemoryRegister16 pd;

        public short Pa { get { return (short)pa.Value; } }
        public short Pb { get { return (short)pb.Value; } }
        public short Pc { get { return (short)pc.Value; } }
        public short Pd { get { return (short)pd.Value; } }

        public BgAffineMatrix(GameboyAdvance gba, UInt32 address)
        {
            pa = new MemoryRegister16(gba.Memory, address, false, true);
            pb = new MemoryRegister16(gba.Memory, address + 2, false, true);
            pc = new MemoryRegister16(gba.Memory, address + 4, false, true);
            pd = new MemoryRegister16(gba.Memory, address + 6, false, true);
        }
   
    }


    public class AffineNegRegister : MemoryRegister8WithSetHook
    {
        public AffineNegRegister(Memory memory, UInt32 address, bool readable, bool writeable) :
        base(memory, address, readable, writeable)
        {
        }

        public override byte Value
        {
            get
            {
                // Negative
                if ((reg & 0x08) > 0)
                {
                    return (byte)((reg & 0x07) | 0xF8);
                }
                else
                {
                    return (byte)(reg & 0x07);
                }
            }

            set
            {
                base.Value = value;
            }
        }
    }


    public class AffineScrollRegister : MemoryRegister32
    {
        public int CachedValue { get; set; }

        public AffineScrollRegister(Memory memory, UInt32 address, bool readable, bool writeable) :
            base()
        {
            MemoryRegister8WithSetHook r0 = new MemoryRegister8WithSetHook(memory, address, false, true);
            MemoryRegister8WithSetHook r1 = new MemoryRegister8WithSetHook(memory, address + 1, false, true);
            MemoryRegister8WithSetHook r2 = new MemoryRegister8WithSetHook(memory, address + 2, false, true);
            AffineNegRegister r3 = new AffineNegRegister(memory, address + 3, false, true);

            MemoryRegister16 loWord = new MemoryRegister16(memory, address, false, true, r0, r1);
            MemoryRegister16 hiWord = new MemoryRegister16(memory, address + 2, false, true, r2, r3);

            // Whenever a byte of this register is changed, we update the cached value 
            // TODO: I don't understand the performance cost of 'capturing' the Value call here....
            Action<byte, byte> updateAction = (oldValue, newValue) => { CachedValue = (int)Value; };

            r0.OnSet = updateAction;
            r1.OnSet = updateAction;
            r2.OnSet = updateAction;
            r3.OnSet = updateAction;

            LoWord = loWord;
            HiWord = hiWord;
        }
    }

}
