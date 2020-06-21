using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gba.Core;

namespace GbaDebugger
{
    // Breaks if (PC == X) && (Mem[lhs] == rhs)
    public class MemConditionalExpression : IBreakpoint
    {
        public enum EqualityCheck
        {
            Equal,
            NotEqual,
            GtEqual,
            LtEqual,

            Invalid
        }

        public UInt32 Address { get; set; }
        UInt32 lhs, rhs;
        EqualityCheck equalitycheck;

        IMemoryReaderWriter memory;

        public MemConditionalExpression(UInt32 address, IMemoryReaderWriter memory, UInt32 lhs, EqualityCheck op, UInt32 rhs)
        {
            Address = address;
            this.memory = memory;
            this.lhs = lhs;
            this.rhs = rhs;
            this.equalitycheck = op;
        }

        public MemConditionalExpression(UInt32 address, IMemoryReaderWriter memory, string[] terms)
        {
            Address = address;
            this.memory = memory;

            if (terms.Length != 4)
            {
                throw new ArgumentException("ConditionalExpression arguments wrong. Form must be 'if <x> <==> <y>");
            }

            if (terms[0].Equals("if", StringComparison.OrdinalIgnoreCase) == false) throw new ArgumentException("missing if");

            if (ParseU32Parameter(terms[1], out lhs) == false ||
                ParseU32Parameter(terms[3], out rhs) == false)
            {
                throw new ArgumentException("ConditionalExpression arguments: params incorrect");
            }

            if (ParseEqualityParameter(terms[2], out equalitycheck) == false)
            {
                throw new ArgumentException("ConditionalExpression arguments: Invalid equality check");
            }

        }


        public bool ShouldBreak(UInt32 pc)
        {
            if (pc == Address && 
                EvaluateExpression())
            {
                return true;
            }
            return false;
        }


        private bool EvaluateExpression()
        {
            switch (equalitycheck)
            {
                case EqualityCheck.Equal:
                    return (memory.ReadWord(lhs) == rhs);

                case EqualityCheck.NotEqual:
                    return (memory.ReadWord(lhs) != rhs);

                case EqualityCheck.GtEqual:
                    return (memory.ReadWord(lhs) >= rhs);

                case EqualityCheck.LtEqual:
                    return (memory.ReadWord(lhs) <= rhs);
            }
            return false;
        }


        protected bool ParseU32Parameter(string p, out UInt32 value)
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

        bool ParseEqualityParameter(string p, out EqualityCheck value)
        {
            if (p.Equals("=="))
            {
                value = EqualityCheck.Equal;
                return true;
            }

            if (p.Equals("!="))
            {
                value = EqualityCheck.NotEqual;
                return true;
            }

            if (p.Equals(">="))
            {
                value = EqualityCheck.GtEqual;
                return true;
            }

            if (p.Equals("<="))
            {
                value = EqualityCheck.LtEqual;
                return true;
            }

            value = EqualityCheck.Invalid;
            return false;
        }


        public override string ToString()
        {
            return String.Format("{0} {1} {2}", lhs, equalitycheck.ToString(), rhs);
        }
    }
}
