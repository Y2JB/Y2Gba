using System;
using System.Collections.Generic;
using System.Text;

namespace GbaDebugger
{
    public class Breakpoint
    {
        public UInt32 Address { get; set; }
        public ConditionalExpression Expression { get; set; }

        public Breakpoint(ushort address)
        {
            Address = address;
        }
        
        public Breakpoint(ushort address, ConditionalExpression expr)
        {
            Address = address;
            Expression = expr;
        }

        public bool ShouldBreak(UInt32 pc)
        {
            if(pc == Address)
            {
                if(Expression == null)
                {
                    return true;
                }
                return Expression.Evaluate();
            }
            return false;
        }


        public override string ToString()
        {
            string str = String.Format("Breakpoint {0:X4}", Address);
            if(Expression != null)
            {
                str = String.Format("{0} - {1}", str, Expression.ToString());
            }
            return str;
        }
    }
}
