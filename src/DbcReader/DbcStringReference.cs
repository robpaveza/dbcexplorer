using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbcReader
{
    public class DbcStringReference
    {
        private WeakReference<DbcTable> _owner;
        private int _pos;
        private Lazy<string> _val;

        internal DbcStringReference(DbcTable owner, int position)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
            if (position < 0)
                throw new ArgumentException("position");

            _owner = new WeakReference<DbcTable>(owner);
            _pos = position;
            _val = new Lazy<string>(() =>
            {
                DbcTable table;
                if (!_owner.TryGetTarget(out table))
                    throw new ObjectDisposedException("DbcTable");

                return table.GetString(_pos);
            });
        }

        public override string ToString()
        {
            return _val.Value;
        }
    }
}
