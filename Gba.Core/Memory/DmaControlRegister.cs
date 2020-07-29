using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class DmaControlRegister
    {
        DmaChannel channel;

        MemoryRegister16 register;

        public DmaControlRegister(GameboyAdvance gba, DmaChannel channel, UInt32 address)
        {
            this.channel = channel;

            MemoryRegister8 r0 = new MemoryRegister8(gba.Memory, address, true, true);
            MemoryRegister8WithSetHook r1 = new MemoryRegister8WithSetHook(gba.Memory, address + 1, true, true);
            register = new MemoryRegister16(gba.Memory, address, true, true, r0, r1);

            r1.OnSet = (oldValue, newValue) =>
            {
                bool oldEnable = ((oldValue & 0x80) != 0);
                bool newEnable = ((newValue & 0x80) != 0);
                if (oldEnable == false && newEnable == true)
                {
                    // Dma transfers take 2 cycles to start
                    channel.DelayTransfer = 2;
                }
            };
        }

        public bool Repeat { get { return ((register.HighByte.Value & 0x20) != 0); } }
        public bool IrqEnable { get { return ((register.HighByte.Value & 0x40) != 0); } }
        public bool ChannelEnabled { get { return (register.HighByte.Value & 0x80) != 0;  } }

        public void SetEnable(bool toggle)
        {
            if (toggle) register.HighByte.Value |= 0x80;
            else register.HighByte.Value &= (byte) 0x7F;
        }

        public enum AddressControl
        {
            Increment,
            Decrement,
            Fixed, 
            IncrementAndReload
        };
        public AddressControl DestinationAddressControl { get { return (AddressControl)((register.LowByte.Value & 0x60) >> 5); } }
        public AddressControl SourceAddressControl { get { return (AddressControl)(((register.LowByte.Value & 0x80) >> 7) + ((register.HighByte.Value & 0x1) * 2)); } }


        public int GamePakDrq { get { return ((register.HighByte.Value & 0x08) >> 3); } }


        public enum DmaTransferType
        {
            U16,
            U32
        };
        public DmaTransferType TransferType { get { return (DmaTransferType)((register.HighByte.Value & 0x4)>>2); } }


        public enum DmaStartTiming
        {
            Immediate,
            VBblank,
            HBlank,
            Special
        };
        public DmaStartTiming StartTiming { get { return (DmaStartTiming) ((register.HighByte.Value & 0x30)>>4); } }
    }


}
