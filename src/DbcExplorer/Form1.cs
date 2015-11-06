using DbcReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DbcExplorer
{
    public partial class frmMain : Form
    {
        private DbcTable _table;
        private DbcSchema _schema;
        private string _schemaFileName;
        private string _dbcFileName;
        private string _dbcFilePath;

        public frmMain()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdOpenDbc.ShowDialog() == DialogResult.OK)
            {
                if (_table != null)
                {
                    dgv.Rows.Clear();
                    dgv.Columns.Clear();

                    _table.Dispose();
                    _table = null;
                }

                FileStream fs = File.OpenRead(ofdOpenDbc.FileName);
                _table = new DbcTable(fs, true);
                _dbcFileName = Path.GetFileName(ofdOpenDbc.FileName);
                _dbcFilePath = Path.GetDirectoryName(ofdOpenDbc.FileName);

                var testSchema = Path.Combine(_dbcFilePath, _dbcFileName + "schema");
                if (File.Exists(testSchema))
                {
                    using (var file = File.OpenRead(testSchema))
                    {
                        _schema = DbcSchema.Load(file, _table.ColumnCount);
                        _schemaFileName = testSchema;
                    }

                    BindSchema();
                }
                else
                {
                    CreateDefaultSchema();
                    _schemaFileName = null;
                }
                PopulateDbc();
            }
        }

        private void CreateDefaultSchema()
        {
            _schema = new DbcSchema();
            for (int i = 0; i < _table.ColumnCount; i++)
            {
                var col = new DbcColumnSchema()
                {
                    Name = "Column" + i,
                    Position = i,
                    Type = ColumnType.Int32,
                    Width = 100,
                };
                _schema.AddColumn(col);
            }

            BindSchema();
        }

        private void BindSchema()
        {
            lbColumns.Items.Clear();
            foreach (var col in _schema.Columns)
            {
                lbColumns.Items.Add(col.Name);
            }

            lbColumns.SelectedItem = null;

        }

        private void PopulateDbc()
        {
            dgv.Columns.Clear();

            for (int i = 0; i < _table.ColumnCount; i++)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = _schema[i].Name, Width = _schema[i].Width, });
            }

            foreach (var item in _table.GetRecords())
            {
                DataGridViewRow row = new DataGridViewRow();

                foreach (var col in _schema.Columns)
                {
                    DataGridViewCell cell = new DataGridViewTextBoxCell();
                    switch (col.Type)
                    {
                        case ColumnType.Int32:
                            cell.Value = item.GetInt32Value(col.Position);
                            break;
                        case ColumnType.Single:
                            cell.Value = item.GetSingleValue(col.Position);
                            break;
                        case ColumnType.String:
                            cell.Value = item.GetStringValue(col.Position);
                            break;
                        case ColumnType.Int32Flags:
                            cell.Value = item.GetInt32Value(col.Position).ToString("x8");
                            break;
                        case ColumnType.Boolean:
                            cell.Value = item.GetInt32Value(col.Position) == 1;
                            break;
                    }
                    row.Cells.Add(cell);
                }

                dgv.Rows.Add(row);
            }

            status.Text = string.Format("{0} rows; string block length {1}, file format {2}; data position 0x{3:x8}", _table.Count, _table.StringBlockLength, _table.FileFormat, _table.DataPosition);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_table != null)
                _table.Dispose();
            Close();
        }

        private void lbColumns_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_updatingColumnsSet)
                return;

            _changingColumnSelection = true;
            if (lbColumns.SelectedIndex == -1)
            {
                textBox1.Text = "";
                textBox1.Enabled = false;
                comboBox1.SelectedItem = null;
                comboBox1.Enabled = false;
            }
            else
            {
                var col = _schema[lbColumns.SelectedIndex];

                textBox1.Text = col.Name;
                textBox1.Enabled = true;
                comboBox1.SelectedIndex = (int)col.Type;
                comboBox1.Enabled = true;
            }
            _changingColumnSelection = false;
        }

        private bool _changingColumnSelection;

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_changingColumnSelection)
                return;

            var colIndex = lbColumns.SelectedIndex;
            if (colIndex == -1 || comboBox1.SelectedIndex == -1)
                return;

            var col = _schema[colIndex];
            col.Type = (ColumnType)comboBox1.SelectedIndex;

            RebindColumn(colIndex);
        }

        private void RebindColumn(int colIndex)
        {
            int curRow = 0;
            foreach (var item in _table.GetRecords())
            {
                DataGridViewRow row = dgv.Rows[curRow++];

                DataGridViewCell cell = row.Cells[colIndex];
                switch (_schema[colIndex].Type)
                {
                    case ColumnType.Int32:
                        cell.Value = item.GetInt32Value(colIndex);
                        break;
                    case ColumnType.Single:
                        cell.Value = item.GetSingleValue(colIndex);
                        break;
                    case ColumnType.String:
                        cell.Value = item.GetStringValue(colIndex);
                        break;
                    case ColumnType.Int32Flags:
                        cell.Value = item.GetInt32Value(colIndex).ToString("x8");
                        break;
                    case ColumnType.Boolean:
                        cell.Value = item.GetInt32Value(colIndex) == 1;
                        break;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (lbColumns.SelectedIndex == -1)
                return;

            var col = _schema[lbColumns.SelectedIndex];
            col.Name = textBox1.Text;
            dgv.Columns[lbColumns.SelectedIndex].HeaderText = textBox1.Text;
            
        }

        private void dgv_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            int index = dgv.Columns.IndexOf(e.Column);
            if (index == -1)
                return;

            var col = _schema[index];
            col.Width = e.Column.Width;
        }

        private void saveSchemaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_schemaFileName))
            {
                using (FileStream fs = new FileStream(_schemaFileName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.SetLength(0);

                    _schema.Save(fs, _dbcFileName);
                }
            }
            else
            {
                sfdSaveSchema.FileName = _dbcFileName + "schema";
                if (sfdSaveSchema.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (FileStream fs = new FileStream(sfdSaveSchema.FileName, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        fs.SetLength(0);

                        _schema.Save(fs, _dbcFileName);
                    }
                }
            }
        }

        private bool _updatingColumnsSet;
        private void textBox1_Leave(object sender, EventArgs e)
        {
            _updatingColumnsSet = true;
            lbColumns.Items[lbColumns.SelectedIndex] = textBox1.Text;
            _updatingColumnsSet = false;
        }
    }
}
