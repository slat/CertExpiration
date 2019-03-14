using System;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace CertExpiration
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            dataGridView1.CellFormatting += DataGridView1_CellFormatting;
            dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;
            dataGridView1.CellClick += DataGridView1_CellClick;

            Load += Form1_Load;
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == 0)
                Process.Start("chrome", dataGridView1[e.ColumnIndex, e.RowIndex].Value.ToString());
        }

        private void DataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
                r.Cells[0] = new DataGridViewLinkCell();
        }

        private void DataGridView1_CellFormatting (object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.DataSource == null)
                return;

            var list = (List<CertificateValidationResult>)dataGridView1.DataSource;
            if (list[e.RowIndex].Expired)
                e.CellStyle.ForeColor = Color.Red;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _rtbUrls.Text = string.IsNullOrWhiteSpace(StartUrls) ? Config.Urls : StartUrls;

            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                if (Config.GridColumnWidths.ContainsKey(c.DataPropertyName))
                    c.Width = Config.GridColumnWidths[c.DataPropertyName];
            }

            if (Config.SplitPanelDistance.HasValue)
                splitContainer1.SplitterDistance = Config.SplitPanelDistance.Value;
        }

        public string StartUrls { get; set; }
        public AppConfig Config { get; set; }

        private void _btnCheck_Click(object sender, EventArgs e)
        {
            var urls = _rtbUrls.Text.Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            if (urls.Length == 0)
                return;

            _btnCheck.Enabled = false;
            dataGridView1.DataSource = null;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = urls.Length;
            progressBar1.Value = 0;

            var results = new List<CertificateValidationResult>();
            var uiThread = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Factory.StartNew(() =>
            {
                foreach (string url in urls)
                {
                    Task.Factory.StartNew(() => CertificateValidation.Validate(url))
                        .ContinueWith(t =>
                        {
                            progressBar1.Value++;
                            results.Add(t.Result);

                        }, uiThread)
                        .Wait();
                }
            })
            .ContinueWith(t =>
            {
                dataGridView1.DataSource = results;
                _btnCheck.Enabled = true;

            }, uiThread);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Config.Urls = _rtbUrls.Text;
            Config.SplitPanelDistance = splitContainer1.SplitterDistance;
            Config.GridColumnWidths = dataGridView1.Columns.Cast<DataGridViewColumn>()
                .Select(x => new { x.DataPropertyName, x.Width })
                .ToDictionary(x => x.DataPropertyName, x => x.Width);

            Config.Save();
        }
    }
}
