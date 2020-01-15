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

            Text += " v" + ProductVersion;

            dataGridView1.CellFormatting += DataGridView1_CellFormatting;
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;

            Load += Form1_Load;
        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex == _colUrl.Index)
            {
                var url = dataGridView1[e.ColumnIndex, e.RowIndex].Value.ToString();
                if (!url.StartsWith("https://") && !url.StartsWith("http://"))
                    Process.Start("https://" + url);
                else
                    Process.Start("chrome", url);
            }
        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var cell = dataGridView1[_colExpired.Index, e.RowIndex];
            if ((bool) cell.Value)
                e.CellStyle.BackColor = Color.Red;
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
            dataGridView1.Rows.Clear();

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
                            var r = t.Result;
                            dataGridView1.Rows.Add(new object[] { r.Url, r.ExpiresAt, r.ExpireDays, r.Expired, r.Timeout });

                        }, uiThread)
                        .Wait();
                }
            })
            .ContinueWith(t =>
            {
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
