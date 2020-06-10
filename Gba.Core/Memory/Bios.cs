using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gba.Core
{
    public class Bios : IMemoryReader
    {
        public enum Status
        {
            STARTUP,
            IRQ_EXECUTE,
            IRQ_FINISH,
            SWI_FINISH
        };

        private byte[] biosData;

        public bool UseGbaBios { get; set; }

        public Status State { get; set; }

        GameboyAdvance gba;

        public Bios(GameboyAdvance gba, string fn)
        {
            this.gba = gba;
            State = Status.STARTUP;
            UseGbaBios = true;

            biosData = new MemoryStream(File.ReadAllBytes(fn)).ToArray();
        }


        // SWI's are the built in BIOS functions
        public void ProcessSwi(UInt32 comment)
        {
            // Emulate SWI using actual GBA BIOS
            if (gba.Bios.UseGbaBios)
            {
                // This is the same setup as when a 'standard' interrupt occurs
                gba.Cpu.SPSR_Svc = gba.Cpu.CPSR;

                gba.Cpu.SetFlag(Cpu.StatusFlag.IrqDisable);
                gba.Cpu.Mode = Cpu.CpuMode.Supervisor;
                UInt32 nextInstruction = (gba.Cpu.State == Cpu.CpuState.Arm ? 4u : 2u);
                gba.Cpu.LR = gba.Cpu.PC_Adjusted + nextInstruction;                       
                gba.Cpu.PC = 0x08;
                gba.Cpu.requestFlushPipeline = true;

                gba.Cpu.State = Cpu.CpuState.Arm;

                return;
            }

            // TODO: Process SWIs via High Level Emulation (HLE)??
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte(UInt32 address)
        {
            return biosData[address];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadHalfWord(UInt32 address)
        {
            // NB: Little Endian
            return (ushort)((ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32 ReadWord(UInt32 address)
        {
            // NB: Little Endian
            return (UInt32)((ReadByte((UInt32)(address + 3)) << 24) | (ReadByte((UInt32)(address + 2)) << 16) | (ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }
    }
}
