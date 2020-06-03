using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    // 0     V-Blank flag(Read only) (1=VBlank) (set in line 160..226; not 227)
    // 1     H-Blank flag(Read only) (1=HBlank) (toggled in all lines, 0..227)
    // 2     V-Counter flag(Read only) (1=Match)  (set in selected line)    (R)
    // 3     V-Blank IRQ Enable(1=Enable)                                   (R/W)
    // 4     H-Blank IRQ Enable(1=Enable)                                   (R/W)
    // 5     V-Counter IRQ Enable(1=Enable)                                 (R/W)
    // 6     Not used(0) / DSi: LCD Initialization Ready(0=Busy, 1=Ready)   (R)
    // 7     Not used(0) / NDS: MSB of V-Vcount Setting(LYC.Bit8) (0..262)  (R/W)
    // 8-15  V-Count Setting(LYC)      (0..227)                             (R/W)
    public class DisplayStatusRegister
    {
        // 0x4000005
        public byte Register1 { get; set; }


        // 0x4000004
        byte reg0;
        public byte Register0
        {
            get
            {
                // Dynamically set the first 3 flag bits 
                if (lcd.Mode == LcdController.LcdMode.VBlank) reg0 |= 0x01;
                else reg0 &= 0xFE;

                if (lcd.Mode == LcdController.LcdMode.HBlank)
                {
                    reg0 |= 0x02;
                }
                else reg0 &= 0xFD;

                if (Register1 == lcd.CurrentScanline) reg0 |= 0x04;
                else reg0 &= 0xFB;

                return reg0;
            }
            set
            {
                reg0 = value;
            }
        }
        
        LcdController lcd;
        
        
        public DisplayStatusRegister(LcdController lcd)
        {
            this.lcd = lcd;
        }

        public bool VBlankFLag { get { return (Register0 & 0x01) != 0; } }
        public bool HBlankFLag { get { return (Register0 & 0x01) != 0; } }

    }

/*
    // Bit 6 - LYC=LY Coincidence Interrupt(1=Enable) (Read/Write)
    // Bit 5 - Mode 2 OAM Interrupt(1=Enable) (Read/Write)
    // Bit 4 - Mode 1 V-Blank Interrupt(1=Enable) (Read/Write)
    // Bit 3 - Mode 0 H-Blank Interrupt(1=Enable) (Read/Write)
    // Bit 2 - Coincidence Flag(0:LYC<> LY, 1:LYC= LY) (Read Only)
    // Bit 1-0 - Mode Flag(Mode 0-3) (Read Only)
    //       0: During H-Blank
    //       1: During V-Blank
    //       2: During Searching OAM
    //       3: During Transferring Data to LCD Driver
    // 0xFF41
    public class LcdStatusRegister
    {
        byte register;
        public byte Register
        {
            get
            {
                byte low3Bits = 0;

                // See the ppu.Enable function for some details on this
                byte mode = (byte)(ppu.Mode == PpuMode.Glitched_OAM ? PpuMode.HBlank : ppu.Mode);
                byte ppuMode = mode;
                low3Bits |= ppuMode;

                // Bit 2 (Coincidence Flag) is set to 1 if register(0xFF44) is the same value as (0xFF45) otherwise it is set to 0
                if (ppu.CurrentScanline == LYC)
                {
                    low3Bits |= 0x04;
                }

                return (byte)(register | low3Bits);
            }

            set
            {
                // Mask off the read only bits
                register = (byte)(value & 0xF8);

                // Set the unused bit
                register |= 0x80;
            }

        }

        // Set at 0xFF45 by the program to drive the coincidence interrupt
        public byte LYC { get; set; }


        IPpu ppu;

        public LcdStatusRegister(IPpu ppu)
        {
            this.ppu = ppu;
        }

        public bool LycLyCoincidenceInterruptEnable { get { return (Register & (byte)(1 << 6)) != 0; } }
        public bool OamInterruptEnable { get { return (Register & (byte)(1 << 5)) != 0; } }
        public bool VBlankInterruptEnable { get { return (Register & (byte)(1 << 4)) != 0; } }
        public bool HBlankInterruptEnable { get { return (Register & (byte)(1 << 3)) != 0; } }

        public byte CoincidenceFlag { get { return (byte)((Register & (byte)(1 << 2)) == 0 ? 0 : 1); } }

        public byte ModeFlag { get { return (byte)(Register & (byte)(0x3)); } }


        public override string ToString()
        {
            // See the ppu.Enable function for some details on this
            string mode = (ppu.Mode == PpuMode.Glitched_OAM ? PpuMode.HBlank.ToString() : ppu.Mode.ToString());

            return String.Format("STAT:{0}Current Mode: {1}{2}LYC Flag: {3}{4}HBlank IRQ: {5}{6}VBlank IRQ: {7}{8}OAM IRQ: {9}{10}LYC IRQ: {11}{12}LYC: {13}{14}",
                Environment.NewLine, mode, Environment.NewLine, CoincidenceFlag.ToString(), Environment.NewLine, HBlankInterruptEnable.ToString(),
                Environment.NewLine, VBlankInterruptEnable.ToString(), Environment.NewLine, OamInterruptEnable.ToString(), Environment.NewLine,
                LycLyCoincidenceInterruptEnable.ToString(), Environment.NewLine, LYC.ToString(), Environment.NewLine);
        }
    }
*/


}
