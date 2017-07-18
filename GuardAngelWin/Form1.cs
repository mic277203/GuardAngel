using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace GuardAngelWin
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ServiceBind();

        }

        private void ServiceBind()
        {
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            dataGridView1.Columns.Add(chk);
            dataGridView1.Columns[0].Width = 30;
            var allServices = GetCurrentServices();
            dataGridView1.DataSource = allServices;
            dataGridView1.Columns[1].HeaderText = "名称";
            dataGridView1.Columns[1].Width = 150;
            dataGridView1.Columns[2].HeaderText = "描述";
            dataGridView1.Columns[2].Width = 100;
        }

        private List<ServiceInfo> GetCurrentServices()
        {
            var allServices = ServiceController.GetServices();

            List<ServiceInfo> listSI = new List<ServiceInfo>();

            foreach (var s in allServices)
            {
                listSI.Add(new ServiceInfo()
                {
                    Name = s.ServiceName,
                    Desc = s.DisplayName
                });
            }

            return listSI;
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            ServiceBind();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<ServiceInfo> listSi = new List<ServiceInfo>();

            foreach (var d in dataGridView1.Rows)
            {
                var cells = ((DataGridViewRow)d).Cells;

                if (Convert.ToBoolean(cells[0].Value))
                {
                    listSi.Add(new ServiceInfo()
                    {
                        Name = cells[1].Value.ToString(),
                        Desc = cells[2].Value.ToString()
                    });
                }
            }

            dataGridView2.DataSource = listSi;

            dataGridView2.Columns[0].HeaderText = "名称";
            dataGridView2.Columns[0].Width = 150;
            dataGridView2.Columns[1].HeaderText = "描述";
            dataGridView2.Columns[1].Width = 100;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dataGridView2.Columns.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<ServiceInfo> listSi = new List<ServiceInfo>();

            foreach (var d in dataGridView2.Rows)
            {
                var cells = ((DataGridViewRow)d).Cells;

                listSi.Add(new ServiceInfo()
                {
                    Name = cells[0].Value.ToString(),
                    Desc = cells[1].Value.ToString()
                });
            }

            string content = JsonConvert.SerializeObject(listSi);
            string jsonPath = AppDomain.CurrentDomain.BaseDirectory + "Service.json";

            File.WriteAllText(jsonPath, content);

            MessageBox.Show("配置成功");
        }
    }

    public class ServiceInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
    }
}
