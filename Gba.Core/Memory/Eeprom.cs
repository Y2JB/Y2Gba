using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gba.Core
{
	public class Eeprom
	{
		public int Size { get; set; }
		public bool SizeLocked { get; set; }
		UInt32 eepromReadAddress;
		UInt32 eepromWriteAddress;

		private const int ReadBitCounterReadReset = 68;
		int numberOfBitsProcessedForWrite;
		int numberOfBitsToProcessForRead;
		// The value we are building bit by bit
		int currentValue;

		enum AccessType : byte
		{
			Read = 0b11,
			Write = 0b10
		}
		AccessType Access;

		public const int Max_Eeprom_Size = 0x2000;
		private byte[] data;
		public byte[] Data { get { return data; } }


		Rom rom;

		public Eeprom(Rom rom)
		{
			this.rom = rom;

			// 512 bytes default but can be 8K for some games
			Size = 0x200;
			data = new byte[Max_Eeprom_Size];
			for(int i=0; i < data.Length; i++)
            {
				data[i] = 0xFF;
            }
			SizeLocked = false;
		}


		public void WriteData(ushort bit)
		{
			// Read 6 or 14 bits from the bitstream (MSB 1st)
			int eepromBusSize = 6;
			if (Size == 0x2000) 
			{ 
				eepromBusSize = 14; 
			}

			currentValue <<= 1;
			currentValue |= (bit & 1);
			numberOfBitsProcessedForWrite++;

			if (numberOfBitsProcessedForWrite == 2)
			{
				// This is the read / write command 
				Access = (AccessType)(currentValue & 3);
			}
			else if (numberOfBitsProcessedForWrite == 2 + eepromBusSize)
			{
				// Eeprom address write 
				if (Access == AccessType.Read)
				{
					eepromReadAddress = (UInt32) ((currentValue << 3) & (Size - 1));
				}
				else
				{
					eepromWriteAddress = (UInt32) ((currentValue << 3) & (Size - 1));
				}
			}
			else if (numberOfBitsProcessedForWrite > 2 + eepromBusSize)
			{
				// Data 
				if (Access == AccessType.Write)
				{
					// first time we get here, WriteBitCounter will be 63 (== 7 mod 8) and counting down
					int bitCounter = (64 + eepromBusSize + 2 - numberOfBitsProcessedForWrite);

					// Last bit is a 0 and ignored 
					if (bitCounter < 0)
					{
						
						// reset buffer values
						currentValue = 0; 
						numberOfBitsProcessedForWrite = 0;
						return;
					}

					// Byte Complete 
					if ((bitCounter & 7) == 0)  
					{
						Data[eepromWriteAddress] = (byte)currentValue;
						eepromWriteAddress++;
						return;
					}
				}
				// This write call was to set up the read address only. There is a final 0 to indicate the end but we don't care about that
				else if (Access == AccessType.Read)
				{
					// Setup read values 
					numberOfBitsToProcessForRead = ReadBitCounterReadReset;
					numberOfBitsProcessedForWrite = 0;
					currentValue = 0;
				}
				else
				{
					throw new ArgumentException("Bad Eeprom Write");
				}
			}
		}


		// Read data from EEPROM and write to GBA memory
		public byte ReadData()
		{
			if (this.Access == AccessType.Write)
			{
				/*
                 GBATek: After the DMA, keep reading from the chip,
                         by normal LDRH [DFFFF00h], until Bit 0 of the returned data becomes "1" (Ready).    
                */
				throw new Exception("Bad eeprom read");
				return 1;
			}
			else if (this.Access == AccessType.Read)
			{
				/*
                    GBATek:
                Read a stream of 68 bits from EEPROM by using DMA,
                then decipher the received data as follows:
                    4 bits - ignore these
                    64 bits - data (conventionally MSB first)
                 */
				numberOfBitsToProcessForRead--;
				if (numberOfBitsToProcessForRead > 63)
				{
					// Ignore
					return 1;
				}

				// MSB first (BitCounter == 7 (mod 8) when we first get here)
				byte value = (byte)((Data[eepromReadAddress] >> (numberOfBitsToProcessForRead & 7)) & 1);
				// Console.WriteLine($"Read Access {EEPROMReadBitCounter}, got {value} from {EEPROMReadAddress.ToString("x4")} ({this.Storage[EEPROMReadAddress].ToString("x2")})");

				if ((numberOfBitsToProcessForRead & 7) == 0)
				{
					eepromReadAddress++;  // move to next byte if BitCounter == 0 mod 8

					if (numberOfBitsToProcessForRead == 0)
					{
						// read done
						numberOfBitsToProcessForRead = ReadBitCounterReadReset;
					}
				}

				return value;
			}
			else
			{
				throw new ArgumentException("Bad Eeprom read access");
			}
		}


		public void Load()
		{
			try
			{
				using (FileStream fs = File.Open(Path.ChangeExtension(rom.RomFileName, "sav"), FileMode.Open))
				{
					using (BinaryReader bw = new BinaryReader(fs))
					{
						bw.Read(data, 0, (int) fs.Length);
					}
				}
			}
			catch (FileNotFoundException)
			{
			}		
		}


		public void Save()
		{
			using (FileStream fs = File.Open(Path.ChangeExtension(rom.RomFileName, "sav"), FileMode.Create))
			{
				using (BinaryWriter bw = new BinaryWriter(fs))
				{
					bw.Write(data, 0, data.Length);
				}
			}
		}
	
	}	
}
