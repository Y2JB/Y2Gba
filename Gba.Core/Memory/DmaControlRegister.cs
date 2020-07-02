using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class DmaControlRegister
    {
        DmaChannel channel;
        public DmaControlRegister(DmaChannel channel)
        {
            this.channel = channel;
        }


        public byte dmaCntRegister0 { get; set; }

        byte reg1;
        public byte dmaCntRegister1 { 
            get 
            { 
                return reg1; 
            } 
            set
            {
                bool oldEnable = ChannelEnabled;
                reg1 = value;

                if(oldEnable == false && ChannelEnabled)
                {
                    // Dma transfers take 2 cycles to start
                    channel.DelayTransfer = 2;
                }
            }
        }


        public ushort DmaCntRegister
        {
            get { return (ushort)((dmaCntRegister1 << 8) | dmaCntRegister0); }
            set { dmaCntRegister0 = (byte)(value & 0x00FF); dmaCntRegister1 = (byte)((value & 0xFF00) >> 8); }
        }

        public bool Repeat { get { return ((dmaCntRegister1 & 0x20) != 0); } }
        public bool IrqEnable { get { return ((dmaCntRegister1 & 0x40) != 0); } }
        public bool ChannelEnabled { get { return (dmaCntRegister1 & 0x80) != 0;  } }

        public void SetEnable(bool toggle)
        {
            if (toggle) dmaCntRegister1 |= 0x80;
            else dmaCntRegister1 &= (byte) 0x7F;
        }

        public enum AddressControl
        {
            Increment,
            Decrement,
            Fixed, 
            IncrementAndReload
        };
        public AddressControl DestinationAddressControl { get { return (AddressControl)((dmaCntRegister0 & 0x60) >> 5); } }
        public AddressControl SourceAddressControl { get { return (AddressControl)(((dmaCntRegister0 & 0x80) >> 7) + ((dmaCntRegister1 & 0x1) * 2)); } }


        public int GamePakDrq { get { return ((dmaCntRegister1 & 0x08) >> 3); } }


        public enum DmaTransferType
        {
            U16,
            U32
        };
        public DmaTransferType TransferType { get { return (DmaTransferType)((dmaCntRegister1 & 0x40)>>6); } }


        public enum DmaStartTiming
        {
            Immediate,
            VBblank,
            HBlank,
            Special
        };
        public DmaStartTiming StartTiming { get { return (DmaStartTiming) ((dmaCntRegister1 & 0x30)>>4); } }
    }


}
