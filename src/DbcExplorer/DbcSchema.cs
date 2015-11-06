using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DbcExplorer
{
    public class DbcSchema
    {
        private List<DbcColumnSchema> _columns = new List<DbcColumnSchema>();
        public int ColumnCount { get; set; }

        public IEnumerable<DbcColumnSchema> Columns
        {
            get { return _columns.AsEnumerable(); }
        }

        public void AddColumn(DbcColumnSchema column)
        {
            _columns.Add(column);
        }

        public DbcColumnSchema this[int index]
        {
            get { return _columns[index]; }
        }

        public void Save(Stream outputStream, string associatedDbc)
        {
            foreach (var col in _columns)
            {
                if (col.Width == 0)
                    col.Width = 100;
            }

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("DbcSchema",
                    new XAttribute("Version", "1.0"),
                    new XAttribute("Target", associatedDbc),
                    _columns.Select(c => new XElement("Column",
                        new XAttribute("Name", c.Name),
                        new XAttribute("Position", c.Position),
                        new XAttribute("Type", c.Type),
                        new XAttribute("Width", c.Width)
                    ))
                )
            );

            doc.Save(outputStream);
        }

        public static DbcSchema Load(Stream source, int specifiedColumns)
        {
            XDocument doc = XDocument.Load(source);
            DbcSchema result = new DbcSchema();

            foreach (var e in doc.Root.Elements("Column"))
            {
                string name = e.Attribute("Name").Value;
                int pos = int.Parse(e.Attribute("Position").Value);
                ColumnType type = (ColumnType)Enum.Parse(typeof(ColumnType), e.Attribute("Type").Value, true);
                int width = int.Parse(e.Attribute("Width").Value);

                var col = new DbcColumnSchema()
                {
                    Position = pos,
                    Name = name,
                    Type = type,
                    Width = width,
                };
                result.AddColumn(col);
            }

            while (result.ColumnCount > specifiedColumns)
                result._columns.RemoveAt(result.ColumnCount - 1);

            while (result._columns.Count < specifiedColumns)
            {
                var col = new DbcColumnSchema()
                {
                    Position = result.ColumnCount,
                    Name = "Column" + result.ColumnCount,
                    Type = ColumnType.Int32,
                    Width = 100,
                };
                result.AddColumn(col);
            }

            return result;
        }
    }

    public class DbcColumnSchema
    {
        public int Position { get; set; }
        public string Name { get; set; }
        public ColumnType Type { get; set; }
        public int Width { get; set; }
    }

    public enum ColumnType
    {
        Int32 = 0,
        Single = 1,
        String = 2,
        Int32Flags = 3,
        Boolean = 4,
    }
}
