// Full disclosure - I got a huge amount of help and code from Gbe-Plus when writing the CPU emulation

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;


namespace Gba.Core
{

	public partial class Cpu
    {
		// TODO: This will probably need to be unwound for speed!
		UInt32 ExtractValue(UInt32 value, int startBit, int bitCount)
        {
			UInt32 mask;
			mask = ((1U << bitCount) - 1U) << startBit;
			return (UInt32) ((value & mask) >> startBit);
		}


		void UpdateFlagsForLogicOps(UInt32 result, byte shift_out)
		{
			// Negative
			if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
			else ClearFlag(StatusFlag.Negative);

			// Zero
			if (result == 0) SetFlag(StatusFlag.Zero);
			else ClearFlag(StatusFlag.Zero);

			// Carry
			if (shift_out == 1) SetFlag(StatusFlag.Carry);
			else if (shift_out == 0) ClearFlag(StatusFlag.Carry);
		}


		void UpdateFlagsForArithmeticOps(UInt32 input, UInt32 operand, UInt32 result, bool addition)
		{
			// Negative
			if ((result & 0x80000000)!= 0) SetFlag(StatusFlag.Negative);
			else ClearFlag(StatusFlag.Negative);

			// Zero
			if (result == 0) SetFlag(StatusFlag.Zero);
			else ClearFlag(StatusFlag.Zero);

			//Carry - Addition
			if ((operand > (0xFFFFFFFF - input)) && (addition)) SetFlag(StatusFlag.Carry);

			//Carry - Subtraction
			else if ((operand <= input) && (!addition)) SetFlag(StatusFlag.Carry);

			else ClearFlag(StatusFlag.Carry);

			//Overflow
			byte inputMsb = (byte) (((input & 0x80000000)!=0) ? 1 : 0);
			byte operandMsb = (byte) (((operand & 0x80000000) != 0) ? 1 : 0);
			byte resultMsb = (byte) (((result & 0x80000000) != 0) ? 1 : 0);

			if (addition)
			{
				if (inputMsb != operandMsb) ClearFlag(StatusFlag.Overflow);

				else
				{
					if ((resultMsb == inputMsb) && (resultMsb == operandMsb)) ClearFlag(StatusFlag.Overflow);
					else SetFlag(StatusFlag.Overflow);
				}
			}
			else
			{
				if (inputMsb == operandMsb) ClearFlag(StatusFlag.Overflow);

				else
				{
					if (resultMsb == operandMsb) SetFlag(StatusFlag.Overflow);
					else ClearFlag(StatusFlag.Overflow);
				}
			}
		}



		// Performs 32-bit logical shift left - Returns Carry Out
		byte LogicalShiftLeft(ref UInt32 input, byte offset)
		{
			byte carryOut = 0;

			if (offset > 0)
			{
				//Test for carry
				//Perform LSL #(n-1), if Bit 31 is 1, we know it will carry out
				UInt32 carry_test = input << (offset - 1);
				carryOut = (byte) (((carry_test & 0x80000000) != 0) ? 1 : 0);

				if (offset >= 32) { input = 0; }
				else { input <<= offset; }
			}

			//LSL #0
			//No shift performed, carry flag not affected, set it to something not 0 or 1 to check!
			else { carryOut = 2; }

			return carryOut;
		}

		// Performs 32-bit logical shift right - Returns Carry Out
		byte LogicalShiftRight(ref UInt32 input, byte offset)
		{
			byte carryOut = 0;

			if (offset > 0)
			{
				//Test for carry
				//Perform LSR #(n-1), if Bit 0 is 1, we know it will carry out
				UInt32 carry_test = input >> (offset - 1);
				carryOut = (byte)(((carry_test & 0x1) != 0) ? 1 : 0);

				if (offset >= 32) { input = 0; }
				else { input >>= offset; }
			}

			//LSR #0
			//Same as LSR #32, input becomes zero, carry flag is Bit 31 of input
			else
			{
				carryOut = (byte) (((input & 0x80000000) != 0) ? 1 : 0);
				input = 0;
			}

			return carryOut;
		}

		// Performs 32-bit arithmetic shift right - Returns Carry Out
		byte ArithmeticShiftRight(ref UInt32 input, byte offset)
		{
			byte carryOut = 0;

			if (offset > 0)
			{
				byte high_bit = (byte) (((input & 0x80000000)!=0) ? 1 : 0);

				//Basically LSR, but bits become Bit 31
				for (int x = 0; x < offset; x++)
				{
					carryOut = (byte) (((input & 0x1) != 0) ? 1 : 0);
					input >>= 1;
					if (high_bit == 1) { input |= 0x80000000; }
				}
			}

			//ASR #0
			//Same as ASR #32, input becomes 0xFFFFFFFF or 0x0 depending on Bit 31 of input
			//Carry flag set to 0 or 1 depending on Bit 31 of input
			else
			{
				if ((input & 0x80000000) != 0) { input = 0xFFFFFFFF; carryOut = 1; }
				else { input = 0; carryOut = 0; }
			}

			return carryOut;
		}

		// Performs 32-bit rotate right
		public byte RotateRight(ref UInt32 input, byte offset)
		{
			byte carryOut = 0;

			if (offset > 0)
			{
				//Perform ROR shift on immediate
				for (int x = 0; x < offset; x++)
				{
					carryOut = (byte) (input & 0x1);
					input >>= 1;

					if (carryOut != 0) 
					{ 
						input |= 0x80000000; 
					}
				}
			}

			//ROR #0
			//Same as RRX #1, which is similar to ROR #1, except Bit 31 now becomes the old carry flag
			else
			{
				byte old_carry = (byte) (CarryFlag ? 1 : 0);
				carryOut = (byte) (input & 0x1);
				input >>= 1;

				if (old_carry != 0) { input |= 0x80000000; }
			}

			return carryOut;
		}


		// Performs 32-bit rotate right - For ARM.5 Data Processing when Bit 25 is 1
		void RotateRightSpecial(ref UInt32 input, byte offset)
		{
			if (offset > 0)
			{
				//Perform ROR shift on immediate
				for (int x = 0; x < (offset * 2); x++)
				{
					byte carry_out = (byte) (input & 0x1);
					input >>= 1;

					if (carry_out != 0) { input |= 0x80000000; }
				}
			}
		}

	}
}
