using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbcReader
{
    public class DbcTable : IDisposable
    {
        protected const int HEADER_LENGTH_DBC = 20;
        protected const int HEADER_LENGTH_DB2 = 48;

        protected DbcFileFormat _format;
        protected int _count,
            /// <summary>Number of bytes per record</summary>
                        _recordLength,
            /// <summary>Number of entries per record</summary>
                        _perRecord,
                        _stringBlockLength,
                        _stringBlockOffset;
        protected bool _ownsStream;
        protected Stream _store;
        protected BinaryReader _reader;

        protected int _headerLength;
        protected int _idLookup;

        protected int _tableHash, _build, _lastWrittenTimestamp, _minId, _maxId, _locale;

        public DbcTable(Stream storage, bool ownsStorage = true)
        {
            if (storage == null)
                throw new ArgumentNullException("storage");
            if (!storage.CanSeek || !storage.CanRead)
                throw new ArgumentException("storage");

            _store = storage;
            _ownsStream = ownsStorage;

            _store.Seek(0, SeekOrigin.Begin);
            _reader = new BinaryReader(storage, Encoding.UTF8, true);
            int magic = _reader.ReadInt32();
            if (magic == 0x43424457)
            {
                _format = DbcFileFormat.Dbc;
                _headerLength = HEADER_LENGTH_DBC;
            }
            else if (magic == 0x32424457)
            {
                _format = DbcFileFormat.Db2;
                _headerLength = HEADER_LENGTH_DB2;
            }
            else if (magic == 0x32484357)
            {
                _format = DbcFileFormat.AdbCache;
                _headerLength = HEADER_LENGTH_DB2;
            }
            else
            {
                throw new InvalidDataException("Invalid header.");
            }
            _count = _reader.ReadInt32();
            _recordLength = _reader.ReadInt32();
            _perRecord = _reader.ReadInt32();
            _stringBlockLength = _reader.ReadInt32();

            if (_format != DbcFileFormat.Dbc)
            {
                _tableHash = _reader.ReadInt32();
                _build = _reader.ReadInt32();
                _lastWrittenTimestamp = _reader.ReadInt32();
                _minId = _reader.ReadInt32();
                _maxId = _reader.ReadInt32();
                _locale = _reader.ReadInt32();
                _reader.ReadInt32();

                if (_maxId != 0)
                {
                    _idLookup = _headerLength;
                    int numRows = _maxId - _minId + 1;
                    _headerLength = _headerLength + numRows * 6;
                }
            }
            else
            {
                _tableHash = 0;
                _build = -1;
                _lastWrittenTimestamp = 0;
                _minId = -1;
                _maxId = -1;
                _locale = 0;
            }

            _stringBlockOffset = _perRecord * _count + _headerLength;
        }

        public int StringBlockLength
        {
            get { return _stringBlockLength; }
        }

        #region Disposable implementation
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_ownsStream && _store != null)
                {
                    _store.Dispose();
                    _store = null;
                }

                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }

                _count = _recordLength = _perRecord = _stringBlockLength = _stringBlockOffset = 0;
            }
        }

        ~DbcTable()
        {
            Dispose(false);
        }
        #endregion

        public int Count
        {
            get { return _count; }
        }

        public int ColumnCount
        {
            get { return _recordLength; }
        }

        public DbcFileFormat FileFormat
        {
            get { return _format; }
        }

        public int DataPosition
        {
            get { return _headerLength; }
        }

        public string GetString(int stringTablePosition)
        {
            if (_store == null)
                throw new ObjectDisposedException("DbcTable");

            var curPos = _store.Position;

            _store.Seek(_stringBlockOffset + stringTablePosition, SeekOrigin.Begin);
            using (BinaryReader br = new BinaryReader(_store, Encoding.UTF8, true))
            {
                int len = 0;
                try
                {
                    while (br.ReadByte() != 0)
                        len++;
                }
                catch (Exception)
                {
                    //if (Debugger.IsAttached)
                    //{
                    //    Debugger.Break();
                    //}

                    return "<invalid string definition>";
                }

                _store.Seek(_stringBlockOffset + stringTablePosition, SeekOrigin.Begin);
                byte[] temp = br.ReadBytes(len);
                _store.Seek(curPos, SeekOrigin.Begin);
                return Encoding.UTF8.GetString(temp);
            }
        }

        internal int GetInt32Value(int record, int column)
        {
            _store.Seek(record * _perRecord + _headerLength + column * 4, SeekOrigin.Begin);
            return _reader.ReadInt32();
        }

        internal float GetSingleValue(int record, int column)
        {
            _store.Seek(record * _perRecord + _headerLength + column * 4, SeekOrigin.Begin);
            return _reader.ReadSingle();
        }

        internal string GetStringValue(int record, int column)
        {
            _store.Seek(record * _perRecord + _headerLength + column * 4, SeekOrigin.Begin);
            int offset = _reader.ReadInt32();
            return GetString(offset);
        }

        public IEnumerable<DbcRecord> GetRecords()
        {
            int curPos = _headerLength;
            for (int i = 0; i < _count; i++)
            {
                yield return new DbcRecord(curPos, i, this);
                curPos += _recordLength * 4;
            }
        }
    }

    internal delegate void DbcReaderProducer<T>(BinaryReader reader, int fieldsPerRecord, DbcTable table, T target)
        where T : class;

    public class DbcTable<T> : DbcTable, IEnumerable<T>
        where T : class, new()
    {
        private static Lazy<DbcReaderProducer<T>> Convert = new Lazy<DbcReaderProducer<T>>(() =>
        {
            if (Config.ForceSlowMode)
                return DbcTable<T>.ConvertSlow;

            try
            {
                return DbcTableCompiler.Compile<T>();
            }
            catch
            {
                if (Debugger.IsAttached && !Config.ForceSlowMode)
                    Debugger.Break();

                return DbcTable<T>.ConvertSlow;
            }
        });


        public DbcTable(Stream storage, bool ownsStorage = true)
            : base(storage, ownsStorage)
        {

        }

        public IEnumerator<T> GetEnumerator()
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                yield return GetAt(i);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T GetAt(int index)
        {
            if (_store == null)
                throw new ObjectDisposedException("DbcTable");

            _store.Seek(_perRecord * index + _headerLength, SeekOrigin.Begin);

            T target = new T();
            Convert.Value(_reader, _recordLength, this, target);
            return target;
        }

        private static void ConvertSlow(BinaryReader reader, int fieldsPerRecord, DbcTable table, T target)
        {
            int[] values = new int[fieldsPerRecord];
            for (int i = 0; i < fieldsPerRecord; i++)
            {
                values[i] = reader.ReadInt32();
            }

            Type t = typeof(T);
            foreach (var targetInfo in DbcTableCompiler.GetTargetInfoForType(t))
            {
                targetInfo.SetValue(target, values[targetInfo.Position], table);
            }
        }
    }

    public enum DbcFileFormat
    {
        Dbc,
        Db2,
        AdbCache,
    }
}
