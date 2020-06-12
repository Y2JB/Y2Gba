using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Gba.Core;

namespace GbaDebugger
{
    public class GbaDebugConsole
    {
        List<Breakpoint> breakpoints = new List<Breakpoint>();
        List<Breakpoint> oneTimeBreakpoints = new List<Breakpoint>();

        // FIFO - previously executed instructions
        Queue<StoredInstruction> executionHistory = new Queue<StoredInstruction>();

        // Next sequential instructions. Forst will be next to be executed
        public List<StoredInstruction> NextInstructions { get; private set; }

        UInt32 lastTicks;

        public List<string> ConsoleText { get; private set; }
        public List<string> ConsoleCodeText { get; private set; }


        public enum Mode
        {
            Running,
            BreakPoint
        }

        enum ConsoleCommand
        {
            step,                           // intrinsically includes STEP n
            next,                           // step over 
            @continue,
            brk,
            breakpoint,
            list,                           // list breakpoints
            delete,
            mem,                            // mem 0 = read(0)   mem 0 10 = wrtie(0, 10)
            dump,
            help,
            set,                            // set (register) n/nn
            ticks,
            exit

        }

        public Mode EmulatorMode { get; private set; }

        public bool BreakpointStepAvailable { get; set; }

        private GameboyAdvance gba;


        public GbaDebugConsole(GameboyAdvance gba)
        {
            this.gba = gba;
            EmulatorMode = Mode.BreakPoint;

            // PPU profiler is expensive! Remember to disconnect the ppu profiler if you are not using it
            //ppuProfiler = new PpuProfiler(dmg);

            ConsoleText = new List<string>();
            ConsoleCodeText = new List<string>();

            NextInstructions = new List<StoredInstruction>();

            // SB : b $64 if [IO_LY] == 2

            //breakpoints.Add(new Breakpoint(0x8));
            //breakpoints.Add(new Breakpoint(0x18));
            breakpoints.Add(new Breakpoint(0x08011be4));

            //breakpoints.Add(new Breakpoint(0xB8E));

            //breakpoints.Add(new Breakpoint(0x64, new ConditionalExpression(snes.memory, 0xFF44, ConditionalExpression.EqualityCheck.Equal, 143)));


            BreakpointStepAvailable = false;

            PeekSequentialInstructions();
            UpdateCodeSnapshot();
        }


        public void RunCommand(string commandStr)
        {           
            // make lower case?


            string[] components = commandStr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            ConsoleCommand command;

            if (Enum.TryParse(components[0], out command) == false)
            {
                // Command Alias'
                if (commandStr.Equals("s")) command = ConsoleCommand.step;
                else if (commandStr.Equals("n")) command = ConsoleCommand.next;
                else if (commandStr.Equals("c")) command = ConsoleCommand.@continue;
                else if (commandStr.Equals("b")) command = ConsoleCommand.brk;
                else if (commandStr.Equals("x")) command = ConsoleCommand.exit;

                // error
                else
                {
                    ConsoleAddString(String.Format("Unknown command {0}", commandStr));
                    return;
                }
            }

            // Trim the first item
            var cmdParams = components.Where(w => w != components[0]).ToArray();

            ExecuteCommand(command, cmdParams);
        }


        bool ExecuteCommand(ConsoleCommand command, string[] parameters)
        {
            ConsoleAddString(String.Format("{0}", command.ToString()));
            switch (command)
            {
                case ConsoleCommand.step:
                    BreakpointStepAvailable = true;
                    return true;

                case ConsoleCommand.next:
                    return NextCommand();

                case ConsoleCommand.@continue:
                    EmulatorMode = Mode.Running;

                    executionHistory.Clear();
                    return true;

                case ConsoleCommand.mem:
                    return MemCommand(parameters);

                case ConsoleCommand.dump:
                    return DumpCommand(parameters);

                //case ConsoleCommand.set:
                //    return SetCommand(parameters);

                case ConsoleCommand.brk:
                    EmulatorMode = Mode.BreakPoint;
                    PeekSequentialInstructions();
                    UpdateCodeSnapshot();
                    return true;

                case ConsoleCommand.breakpoint:
                    return BreakpointCommand(parameters);

                case ConsoleCommand.list:
                    return ListCommand(parameters);

                case ConsoleCommand.delete:
                    breakpoints.Clear();
                    return true;

                case ConsoleCommand.ticks:
                    ConsoleAddString(String.Format("ticks - {0}", gba.Cpu.Cycles - lastTicks));
                    lastTicks = gba.Cpu.Cycles;
                    return true;
               
                case ConsoleCommand.exit:                    
                    return true;

                default:
                    return false;
            }
        }


        // This is 'step over'
        bool NextCommand()
        {
            UInt32 instrSize = (UInt32)(gba.Cpu.State == Cpu.CpuState.Arm ? 4 : 2);
            oneTimeBreakpoints.Add(new Breakpoint(gba.Cpu.PC_Adjusted + instrSize));
            EmulatorMode = Mode.Running;
            return true;
        }


        bool MemCommand(string[] parameters)
        {
            if (parameters.Length == 1 || parameters.Length == 2)
            {

                uint p1 = 0, p2 = 0;

                bool parsedParams;

                parsedParams = ParseUIntParameter(parameters[0], out p1);
                if (parsedParams && parameters.Length == 2)
                {
                    parsedParams = ParseUIntParameter(parameters[1], out p2);
                }


                if (parsedParams)
                {
                    if (parameters.Length == 1)
                    {
                        string memStr = String.Format("{0:X8} - {1:X2} {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2}", p1, gba.Memory.ReadByte(p1), gba.Memory.ReadByte(p1 + 1), gba.Memory.ReadByte(p1 + 2), gba.Memory.ReadByte(p1 + 3), gba.Memory.ReadByte(p1 + 4), gba.Memory.ReadByte(p1 + 5), gba.Memory.ReadByte(p1 + 6), gba.Memory.ReadByte(p1 + 7));
                        ConsoleAddString(memStr);
                        return true;
                    }
                    else
                    {
                        if (p2 > 0xFF)
                        {
                            ConsoleAddString(String.Format("mem write value must be 0 - 255"));
                            return false;
                        }
                        gba.Memory.WriteByte(p1, (byte)p2);
                        ConsoleAddString(String.Format("Written. Ram[0x{0:X4}] == 0x{1:X2}", p1, p2));
                        return true;
                    }
                }
                
            }
  
            // Fail
            ConsoleAddString(String.Format("mem usage: 'mem n' for read, 'mem n n' for write. n can be of the form 255 or 0xFF"));
            return false;
        }


        bool DumpCommand(string[] parameters)
        {
            if (parameters.Length == 1)
            {
                switch(parameters[0])
                {
                    case "palettes":
                        gba.LcdController.Palettes.DumpPaletteToPng(0);
                        gba.LcdController.Palettes.DumpPaletteToPng(1);
                        ConsoleAddString("Palettes dumped.");
                        return true;

                    case "tiles":
                        throw new NotImplementedException();
                    //ConsoleAddString("Tiles dumped.");
                    //return true;

                    case "tilemap":
                        gba.LcdController.Bg[0].TileMap.DumpTileMap();
                        ConsoleAddString("Tilemap dumped.");
                        return true;
                }
            }

            // Fail
            ConsoleAddString(String.Format("dump usage: 'dump <item>'. Supported items 'palettes', 'tiles', 'tilemap'"));
            return false;
        }

        // b 0xC06A if 0xff40 == 2
        bool BreakpointCommand(string[] parameters)
        {
            if (parameters.Length != 1 && parameters.Length != 5)
            {
                ConsoleAddString(String.Format("breakpoint: Invalid number of parameters. Usage:'breakpoint 0xC100'"));
                return false;
            }

            UInt32 p1 = 0;
            bool parsedParams;
            parsedParams = ParseU32Parameter(parameters[0], out p1);


            // Parse condtiion
           GbaDebugger.ConditionalExpression expression = null;

            /*
            if (parameters.Length > 1)
            {
                // Parse condition

                try
                {
                    expression = new SnesDebugger.ConditionalExpression(snes.memory, parameters.Skip(1).ToArray());
                }
                catch (ArgumentException ex)
                {
                    ConsoleAddString(String.Format("Error Adding breakpoint.\n{0}", ex.ToString()));
                }
            }
            */

            breakpoints.Add(new Breakpoint(p1, expression));

            ConsoleAddString(String.Format("breakpoint added at 0x{0:X4}", p1));
 
            return true;
        }

        /*
                bool SetCommand(string[] parameters)
                {
                    if (parameters.Length != 2)
                    {
                        // Fail
                        ConsoleAddString(String.Format("set usgage: set a 10, ser HL 0xFF..."));
                        return false;
                    }

                    parameters[0] = parameters[0].ToUpper();
                    // Param 1 is the register id
                    string[] registers = new string[] { "A", "B", "C", "D", "E", "F", "H", "L", "AF", "BC", "DE", "HL", "SP", "PC" };
                    bool match = false;
                    foreach (var s in registers)
                    {
                        if (String.Equals(parameters[0], s, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }

                    if (match == false)
                    {
                        ConsoleAddString(String.Format("set command: invalid register"));
                    }

                    //Param 2 is the value
                    bool parsedParams;
                    ushort value;
                    parsedParams = ParseUShortParameter(parameters[1], out value);

                    if (parsedParams == false)
                    {
                        ConsoleAddString(String.Format("set command: parameter 2 must be a number"));
                    }

                    // This isn't pretty but it's UI code and the debugger just needs to work.
                    switch (parameters[0])
                    {
                        case "A":
                            snes.cpu.A = (byte)value;
                            break;

                        case "B":
                            snes.cpu.B = (byte)value;
                            break;

                        case "C":
                            snes.cpu.C = (byte)value;
                            break;

                        case "D":
                            snes.cpu.D = (byte)value;
                            break;

                        case "E":
                            snes.cpu.E = (byte)value;
                            break;

                        case "F":
                            snes.cpu.F = (byte)value;
                            break;

                        case "H":
                            snes.cpu.H = (byte)value;
                            break;

                        case "L":
                            snes.cpu.L = (byte)value;
                            break;

                        case "AF":
                            snes.cpu.AF = value;
                            break;

                        case "BC":
                            snes.cpu.BC = value;
                            break;

                        case "DE":
                            snes.cpu.DE = value;
                            break;

                        case "HL":
                            snes.cpu.HL = value;
                            break;

                        case "SP":
                            snes.cpu.SP = value;
                            break;

                        case "PC":
                            snes.cpu.PC = value;
                            break;
                    }
                    return true;
                }
        */


        bool ListCommand(string[] parameters)
        {
            if (breakpoints.Count == 0)
            {
                ConsoleAddString("No Breakpoints set.");
            }
            else
            {
                ConsoleAddString("Breakpoint List:");
                foreach (var bp in breakpoints)
                {
                    ConsoleAddString(String.Format("0x{0:X8}", bp.Address));
                }
            }
            return true;
        }


        // Try to parse a base 10 or base 16 number from string
        bool ParseUIntParameter(string p, out uint value)
        {
            if (uint.TryParse(p, out value) == false)
            {
                // Is it hex?
                if (p.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    p = p.Substring(2);
                }
                return uint.TryParse(p, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out value);
            }
            return true;
        }


        // Try to parse a base 10 or base 16 number from string
        bool ParseU32Parameter(string p, out UInt32 value)
        {
            if (UInt32.TryParse(p, out value) == false)
            {
                // Is it hex?
                if (p.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    p = p.Substring(2);
                }
                return UInt32.TryParse(p, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out value);
            }
            return true;
        }


        public bool CheckForBreakpoints()
        {
            foreach (var bp in breakpoints)
            {
                if (bp.ShouldBreak(gba.Cpu.PC_Adjusted))
                {
                    EmulatorMode = Mode.BreakPoint;

                    PeekSequentialInstructions();
                    UpdateCodeSnapshot();

                    ConsoleAddString(String.Format("BREAK"));
                    ConsoleAddString(bp.ToString());

                    return true;
                }
            }

            // 'step over' breakpoints
            foreach (var bp in oneTimeBreakpoints)
            {
                if (bp.ShouldBreak(gba.Cpu.PC_Adjusted))
                {
                    EmulatorMode = Mode.BreakPoint;
                    oneTimeBreakpoints.Remove(bp);

                    PeekSequentialInstructions();
                    UpdateCodeSnapshot();

                    return true;
                }
            }

            return false;
        }

        public void UpdateCodeSnapshot()
        {
            ConsoleCodeText.Clear();
           
            // Show previous instructions
            foreach (var instruction in executionHistory)
            {
                ConsoleCodeText.Add("--- " + instruction.ToString());
            }

            // Next instruction
            ConsoleCodeText.Add(">>> " + NextInstructions[0].ToString());

            // Future instructions
            for (int i = 1; i < NextInstructions.Count; i++)
            {
                if (NextInstructions[i] != null)
                {
                    ConsoleCodeText.Add("+++ " + NextInstructions[i].ToString());
                }
            }
        }


        void ConsoleAddString(string str)
        {
            ConsoleText.Add(str);
        }


        // We are about to step
        public void OnPreBreakpointStep()
        {
            // Pop the current instruction and store it in our history 
            executionHistory.Enqueue(new StoredInstruction(NextInstructions[0].RawInstruction, NextInstructions[0].FriendlyInstruction, NextInstructions[0].PC));
            NextInstructions.RemoveAt(0);

            // Only show last x instructions
            if (executionHistory.Count == Cpu.Pipeline_Size) executionHistory.Dequeue();
        }


        // We have just completed a step, PeekSequentialInstructions will have been called
        public void OnPostBreakpointStep()
        {
            BreakpointStepAvailable = false;            

            UpdateCodeSnapshot();
        }     


        public void PeekSequentialInstructions()
        {
            NextInstructions.Clear();

            string instructionText;
            UInt32 rawInstr;
            StoredInstruction newInstruction;

            UInt32 instrSize = (UInt32) (gba.Cpu.State == Cpu.CpuState.Arm ? 4 : 2);
            UInt32 adjust = 0;

            int instructionPtr = gba.Cpu.NextPipelineInsturction;

            // Inefficient but we are not running at this point so what the hell
            rawInstr = gba.Cpu.InstructionPipeline[instructionPtr];
            instructionText = gba.Cpu.State == Cpu.CpuState.Arm ? gba.Cpu.PeekArmInstruction(rawInstr) : gba.Cpu.PeekThumbInstruction((ushort)rawInstr);
            newInstruction = new StoredInstruction(rawInstr, instructionText, gba.Cpu.PC_Adjusted);
            NextInstructions.Add(newInstruction);

            instructionPtr++;
            if (instructionPtr >= Cpu.Pipeline_Size) instructionPtr = 0;
            adjust += instrSize;

            rawInstr = gba.Cpu.InstructionPipeline[instructionPtr];
            instructionText = gba.Cpu.State == Cpu.CpuState.Arm ? gba.Cpu.PeekArmInstruction(rawInstr) : gba.Cpu.PeekThumbInstruction((ushort)rawInstr);
            newInstruction = new StoredInstruction(rawInstr, instructionText, (UInt32) (gba.Cpu.PC_Adjusted + adjust));
            NextInstructions.Add(newInstruction);

            instructionPtr++;
            if (instructionPtr >= Cpu.Pipeline_Size) instructionPtr = 0;
            adjust += instrSize;

            rawInstr = gba.Cpu.InstructionPipeline[instructionPtr];
            instructionText = gba.Cpu.State == Cpu.CpuState.Arm ? gba.Cpu.PeekArmInstruction(rawInstr) : gba.Cpu.PeekThumbInstruction((ushort)rawInstr);
            newInstruction = new StoredInstruction(rawInstr, instructionText, (UInt32)(gba.Cpu.PC_Adjusted + adjust));
            NextInstructions.Add(newInstruction);
            /*
            UInt32 lookAheadBytes = 0;
            for (int i = 0; i < Cpu.Pipeline_Size; i++)
            {
                UInt32 pc = (UInt32)(gba.Cpu.PC_Adjusted + lookAheadBytes);
                UInt32 rawInstr = gba.Memory.ReadWord(pc);
                lookAheadBytes+= 4;

                string instructionText;
                try
                {
                    instructionText = gba.Cpu.PeekArmInstruction(rawInstr);
                    var newInstruction = new StoredInstruction(instructionText, pc);
                    NextInstructions.Add(newInstruction);
                }
                catch (ArgumentException) { }
            }
            */
        }
    }

}
