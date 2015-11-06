using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbcReader
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class DbcRecordPositionAttribute : Attribute
    {
        public DbcRecordPositionAttribute(int position)
        {
            if (position < 0)
                throw new ArgumentException("position");

            Position = position;
        }

        public int Position
        {
            get;
            private set;
        }
    }
}
