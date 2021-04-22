using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Ports = System.IO.Ports;
using SerialPort = System.IO.Ports.SerialPort;
using Cognex.DataMan.SDK;
using Cognex.DataMan.Discovery;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            GetScanner = new _getScanner(addListItem);
            GetData = new _getData(DataGridAddRows);
        }
        List<SerSystemDiscoverer.SystemInfo> systemInfos = new List<SerSystemDiscoverer.SystemInfo>();
        public delegate void _getScanner(object obj);
        public delegate void _getData(object obj);
        public _getScanner GetScanner;
        public _getData GetData;
        private void Form1_Load(object sender, EventArgs e)
        {            
            SerSystemDiscoverer serSystemDiscoverer = new SerSystemDiscoverer();
            serSystemDiscoverer.SystemDiscovered += SerSystemDiscoverer_SystemDiscovered;
            serSystemDiscoverer.Discover();
            
            
        }
        public void addListItem(object obj)
        {
            
            comboBox1.Items.Add(obj);
        }

        public void DataGridAddRows(object obj)
        {

            dataGridView1.Rows.Add(obj,DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
        }
        private void SerSystemDiscoverer_SystemDiscovered(SerSystemDiscoverer.SystemInfo systemInfo)
        {
            systemInfos.Add(systemInfo);
            this.Invoke(GetScanner, systemInfo);
        }

        private void cOMToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SerSystemConnector serSystemConnector = new SerSystemConnector(comboBox1.SelectedItem.ToString());
            DataManSystem dataManSystem = new DataManSystem(serSystemConnector);
            try {
                dataManSystem.ReadStringArrived -= DataManSystem_ReadStringArrived; 
            }
            finally { 
                dataManSystem.ReadStringArrived += DataManSystem_ReadStringArrived;
                dataManSystem.Connect();
            }
            
        }

        private void DataManSystem_ReadStringArrived(object sender, ReadStringArrivedEventArgs args)
        {
            this.Invoke(GetData, args.ReadString);
            //throw new NotImplementedException();
        }
    }
}
