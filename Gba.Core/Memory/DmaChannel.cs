using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Gba.Core
{
    public class DmaChannel
    {
        public DmaControlRegister DmaCnt { get; set; }

        // The most significant address bits are ignored, only the least significant 27 or 28 bits are used(max 07FFFFFFh internal memory, or max 0FFFFFFFh any memory
        public byte sAddr0 { get; set; }
        public byte sAddr1 { get; set; }
        public byte sAddr2 { get; set; }
        public byte sAddr3 { get; set; }
        public UInt32 SourceAddress
        {
            get { return (UInt32)((sAddr3 << 24) | (sAddr2 << 16) |(sAddr1 << 8) | sAddr0); }
            set { sAddr0 = (byte)(value & 0xFF); sAddr1 = (byte)((value & 0xFF00) >> 8); sAddr2 = (byte)((value & 0xFF0000) >> 16); sAddr3 = (byte)((value & 0xFF000000) >> 24);  }
        }

        // The most significant address bits are ignored, only the least significant 27 or 28 bits are used (max 07FFFFFFh internal memory, or max 0FFFFFFFh any memory
        public byte dAddr0 { get; set; }
        public byte dAddr1 { get; set; }
        public byte dAddr2 { get; set; }
        public byte dAddr3 { get; set; }
        public UInt32 DestAddress
        {
            get { return (UInt32)((dAddr3 << 24) | (dAddr2 << 16) | (dAddr1 << 8) | dAddr0); }
            set { dAddr0 = (byte)(value & 0xFF); dAddr1 = (byte)((value & 0xFF00) >> 8); dAddr2 = (byte)((value & 0xFF0000) >> 16); dAddr3 = (byte)((value & 0xFF000000) >> 24); }
        }


        // Specifies the number of data units to be transferred, each unit is 16bit or 32bit depending on the transfer type, a value of zero is treated 
        // as max length (ie. 4000h, or 10000h for DMA3).
        public byte wordCount0 { get; set; }
        public byte wordCount1 { get; set; }
        public ushort WordCount
        {
            get { return (ushort)((wordCount1 << 8) | wordCount0); }
            set { wordCount0 = (byte)(value & 0xFF); wordCount1 = (byte)((value & 0xFF00) >> 8); }
        }

        // Dma starts 2 cycles after enable flag is flipped
        public int DelayTransfer { get; set; }

        // Set on hblank/vblank etc ti start the dma at the right time
        public bool Started { get; set; }

        int channelNumber;

        GameboyAdvance gba;

        public DmaChannel(GameboyAdvance gba, int channelNumber)
        {
            this.gba = gba;
            this.channelNumber = channelNumber;

            DmaCnt = new DmaControlRegister(this);
        }


        public void Step()
        {
            // You cannot support this until you are cycle accurate otherwise dma's mess up
            if (DelayTransfer > 0)
            {
                DelayTransfer--;
                return;
            }

            if(DmaCnt.ChannelEnabled == false)
            {
                return;
            }  
            
            //if(DmaCnt.StartTiming != DmaControlRegister.DmaStartTiming.Special)
            //{
            //    gba.LogMessage(String.Format("DMA: Ch: {0} Source 0x{1:X} Dest 0x{2:X} Size 0x{3:X}", channelNumber, SourceAddress, DestAddress, WordCount));
            //}

            switch(DmaCnt.StartTiming)
            {
                case DmaControlRegister.DmaStartTiming.Immediate:
                    Transfer();
                    break;

                case DmaControlRegister.DmaStartTiming.VBblank:
                    throw new ArgumentException("vblank dma");

                case DmaControlRegister.DmaStartTiming.HBlank:
                    if (Started)
                    {
                        Transfer();
                        Started = false;
                    }
                    break;

                case DmaControlRegister.DmaStartTiming.Special:

                    if (channelNumber == 1 || channelNumber == 2)
                    {
                        //Transfer();
                    }
                    else
                    {
                        gba.LogMessage("WARNING: Invalid DmaStartTiming.Special");
                    }
                    break;

                default:
                    throw new ArgumentException("Bad Dma start timing");
            }

        }


        void Transfer()
        {
            // unit == 2 or 4 bytes depending on transfer type
            
            int unitsToTransfer = WordCount;
            if (unitsToTransfer == 0)
            {
                if (channelNumber == 3) unitsToTransfer = 0x10000;
                else unitsToTransfer = 0x4000;
            }

            bool eepromDma = false;
            bool eepromRead = false; 

            if (channelNumber == 3)
            {
                if (DmaCnt.GamePakDrq != 0)
                {
                    throw new NotImplementedException("DMA: GamePakDrq");
                }



                // Read data from EEPROM and write to GBA memory
                if ((SourceAddress >= 0xD000000) && (SourceAddress <= 0xDFFFFFF))
                {
                    eepromDma = true;
                    eepromRead = true;
                }                   
                else if ((DestAddress >= 0xD000000) && (DestAddress <= 0xDFFFFFF))
                {
                    eepromDma = true;
                    eepromRead = false;

                    // This is a 'trick' to guess the eeprom size. It may not be 100% reliable. The alternative is to use a game database.
                    // Determine EEPROM size based on bitstream length. Default is 512B, here it is set to 8KB if necessary
                    if ((!gba.Rom.Eeprom.SizeLocked) && ((unitsToTransfer == 0x11) || (unitsToTransfer == 0x51)))
                    {
                        gba.Rom.Eeprom.Size = Eeprom.Max_Eeprom_Size;                       
                        gba.Rom.Eeprom.SizeLocked = true;
                    }                   
                }
            }

            DmaControlRegister.DmaTransferType transferType = DmaCnt.TransferType;
            UInt32 unitSize;

            
            // TODO: this is different for each channel, also check the 'checked' memory fucntions which is doing stuff
            //SourceAddress &= 0xFFFFFFF;
            //DestAddress &= 0xFFFFFFF;


            UInt32 sourceAddress = SourceAddress;
            UInt32 destinationAddress = DestAddress;

            if (transferType == DmaControlRegister.DmaTransferType.U16)
            {
                unitSize = 2u;

                // Align addresses to half word 
                sourceAddress &= ~0x1u;
                destinationAddress &= ~0x1u;
            }
            else
            {
                unitSize = 4u;

                // Align addresses to word
                sourceAddress &= ~0x3u;
                destinationAddress &= ~0x3u;
            }

            while (unitsToTransfer > 0)
            {
                // Copy 1 unit
                if (transferType == DmaControlRegister.DmaTransferType.U16)
                {                               
                    // Eeprom will always be 16bit units
                    if (eepromDma)
                    {
                        if(eepromRead)
                        {
                            byte value = gba.Rom.Eeprom.ReadData();
                            gba.Memory.WriteByte(destinationAddress, value);
                        }
                        else
                        {
                            ushort value = gba.Memory.ReadHalfWord(sourceAddress);
                            gba.Rom.Eeprom.WriteData(value);
                        }
                    }
                    else
                    {
                        ushort value = gba.Memory.ReadHalfWord(sourceAddress);
                        gba.Memory.WriteHalfWord(destinationAddress, value);
                    }
                }
                else
                {
                    UInt32 value = gba.Memory.ReadWord(sourceAddress);
                    gba.Memory.WriteWord(destinationAddress, value);
                }

                // Address control
                if (DmaCnt.SourceAddressControl == DmaControlRegister.AddressControl.Increment) sourceAddress += unitSize;
                else if (DmaCnt.SourceAddressControl == DmaControlRegister.AddressControl.Decrement) sourceAddress -= unitSize;
                // This is not allowed but it does happen. Treat it as Increment
                else if (DmaCnt.SourceAddressControl == DmaControlRegister.AddressControl.IncrementAndReload) sourceAddress += unitSize;

                if (DmaCnt.DestinationAddressControl == DmaControlRegister.AddressControl.Increment) destinationAddress += unitSize;
                else if (DmaCnt.DestinationAddressControl == DmaControlRegister.AddressControl.Decrement) destinationAddress -= unitSize;
                else if (DmaCnt.DestinationAddressControl == DmaControlRegister.AddressControl.IncrementAndReload) destinationAddress += unitSize;               

                unitsToTransfer--;

                // TODO: Run LCD, timers etc 

                // TODO: Check for DMA being interrupted by a higher priority DMA (probably make this recursive)
            }

            
            if (DmaCnt.DestinationAddressControl != DmaControlRegister.AddressControl.IncrementAndReload)
            {
                // We are doing things a little backward here. We never actually updated our register values during the transfer so the reload is to do nothing
                // Here we update the actual register with the final value after the transfer is done
                DestAddress = destinationAddress;
            }

            // NB: SourceAddress cannot have IncrementAndReload
            SourceAddress = sourceAddress;

      
            if(DmaCnt.IrqEnable)
            {
                int flag = (int)(Interrupts.InterruptType.Dma0) + channelNumber;
                gba.Interrupts.RequestInterrupt((Interrupts.InterruptType)flag);
            }

            if (DmaCnt.Repeat == false)
            {
                DmaCnt.SetEnable(false);
            }
        }

    }
}
