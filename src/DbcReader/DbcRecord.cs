using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbcReader
{
    public class DbcRecord
    {
        private WeakReference<DbcTable> _owner;
        private int _position, _index;

        internal DbcRecord(int position, int index, DbcTable owner)
        {
            _position = position;
            _index = index;
            _owner = new WeakReference<DbcTable>(owner);
        }

        public int GetInt32Value(int column)
        {
            DbcTable owner;
            if (!_owner.TryGetTarget(out owner))
                throw new InvalidOperationException();

            return owner.GetInt32Value(_index, column);
        }

        public float GetSingleValue(int column)
        {
            DbcTable owner;
            if (!_owner.TryGetTarget(out owner))
                throw new InvalidOperationException();

            return owner.GetSingleValue(_index, column);
        }

        public string GetStringValue(int column)
        {
            DbcTable owner;
            if (!_owner.TryGetTarget(out owner))
                throw new InvalidOperationException();

            return owner.GetStringValue(_index, column);
        }
    }
}
