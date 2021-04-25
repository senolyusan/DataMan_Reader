
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using Ports = System.IO.Ports;
using SerialPort = System.IO.Ports.SerialPort;
using Cognex.DataMan.SDK;
using Cognex.DataMan.Discovery;

namespace DataMan_Scanner
{
    
    public partial class Form1 : Form
    {
        private const int WM_DEVICECHANGE= 0x0219;
        /// <summary>
        /// 扫描枪信息集合
        /// </summary>
        List<SerSystemDiscoverer.SystemInfo> systemInfos = new List<SerSystemDiscoverer.SystemInfo>();
        /// <summary>
        /// 获取扫描枪委托
        /// </summary>
        /// <param name="obj">扫描枪信息</param>
        public delegate void _getScanner(List<SerSystemDiscoverer.SystemInfo> obj);
        /// <summary>
        /// 获取扫描结果委托
        /// </summary>
        /// <param name="obj"></param>
        public delegate void _getData(String str);
        /// <summary>
        /// 设置文本委托
        /// </summary>
        /// <param name="text"></param>
        public delegate void _setText(String text);
        /// <summary>
        /// 显示报警图片
        /// </summary>
        /// <param name="flag"></param>
        public delegate void _displayAlert(Boolean flag);
        public delegate void _setFormTitle(string formTitle);

        private _setFormTitle SetFormTitle;
        private  _getScanner GetScanner;
        private _getData GetData;
        private _setText SetText;
        private _displayAlert DisplayAlert;
        public Dictionary<string, string> ScanData = new Dictionary<string, string>();
        SerSystemConnector serSystemConnector = null;
        DataManSystem dataManSystem = null;
        private string storeFile = null;
        public delegate void FreshScannerList();
        public event FreshScannerList FreshEvent;
        public delegate void _discoverScanner();
        private _discoverScanner DiscoverScanner;
        SerSystemDiscoverer serSystemDiscoverer =null;
        bool AutoLoad = false;
        List<String> autoLoadDevices = new List<string>();
        protected virtual void OnFreshScannerList()
        {
            FreshScannerList eventHandler = FreshEvent;
            if (eventHandler != null)
            {
                eventHandler();
            }
        }
        public Form1()
        {
            InitializeComponent();
            GetScanner = new _getScanner(ComBoBoxaddListItem);
            GetData = new _getData(DataGridAddRows);
            SetText = new _setText(TextBoxSetText);
            DisplayAlert = new _displayAlert(displayAlert);
            DiscoverScanner = new _discoverScanner(discoverScanner);
            SetFormTitle = new _setFormTitle(setFormTitle);
        }
        /// <summary>
        /// 重复扫码报警处理函数
        /// </summary>
        /// <param name="flag"></param>
        private void displayAlert(Boolean flag)
        {
            if (flag) 
            { 
                this.pic_NG.Visible = true;
                this.pic_OK.Visible = false;
                try
                {
                    dataManSystem.SendCommand("OUTPUT.DATAVALID-FAIL", 100);
                }
                catch (Exception)
                {

                }
            }
            else
            {                
                this.pic_NG.Visible = false;
                this.pic_OK.Visible = true;
            }

        }
        /// <summary>
        /// 窗体文本设置委托函数
        /// </summary>
        /// <param name="title"></param>
        private void setFormTitle(string title)
        {
            this.Text = title;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            this.pic_NG.Visible = false;
            this.pic_OK.Visible = false;
            this.textBox1.Enabled = false;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.RowHeadersVisible = false;
            serSystemDiscoverer = new SerSystemDiscoverer();
            serSystemDiscoverer.SystemDiscovered += SerSystemDiscoverer_SystemDiscovered;
            timer1.Interval = 1000;
            timer1.Tick += Timer1_Tick;
            timer1.Enabled = true;
            MessageBox.Show("使用时必须指定【存储文件】的路径！");
            timer1.Start();
            this.Invoke(DiscoverScanner);
            if(Boolean.TryParse(Properties.Settings.Default.Properties["AutoLoad"].DefaultValue.ToString(), out this.AutoLoad))
            {
                if (this.AutoLoad)
                {
                    this.checkBox1.Checked = true;
                }
                else
                {
                    this.checkBox1.Checked = false;
                }
            }
            
        }
        /// <summary>
        /// 扫码枪发现处理函数
        /// </summary>
        private void discoverScanner()
        {
            if(dataManSystem == null && serSystemDiscoverer !=null)
            {
                systemInfos.Clear();
                serSystemDiscoverer.Discover();
            }
        }
        /// <summary>
        /// 定时器周期触发事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (dataManSystem == null)
            {
                this.Invoke(DiscoverScanner);
            }
            else
            {
                try
                {
                    ///关断定时器，防止重复触发
                    this.timer1.Stop();
                    ///使用“GET DEVICE.NAME”命令，检测扫码枪是否掉线
                    DmccResponse dmccResponse = dataManSystem.SendCommand("GET DEVICE.NAME",1000);
                }                
                catch(Exception)
                {   
                    dataManSystem.Disconnect();
                    dataManSystem = null;
                    this.comboBox1.Items.Clear();
                    systemInfos.Clear();
                }
                this.timer1.Start();
            }
        }


/*        protected override void WndProc(ref Message m)
        {
            if(m.Msg ==WM_DEVICECHANGE)
            {
                ;
                ;
            }
            base.WndProc(ref m);
        }*/
        /// <summary>
        /// 扫描枪下拉列表项添加处理函数
        /// </summary>
        /// <param name="obj"></param>
        public void ComBoBoxaddListItem(List<SerSystemDiscoverer.SystemInfo> obj)
        {
            ComboBox.ObjectCollection objectCollection = comboBox1.Items;
            
                obj.ForEach(o => {
                    if (!objectCollection.Contains(o.PortName)) 
                    {
                        comboBox1.Items.Add(o.PortName);
                    };
                });
        }
        /// <summary>
        /// 扫码结果处理函数
        /// </summary>
        /// <param name="str"></param>
        public void DataGridAddRows(string str)
        {
            ///文本框显示最新扫码内容
            this.Invoke(SetText, str);
            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (ScanData.ContainsKey(str))
            {                
                this.Invoke(DisplayAlert,true);
            }
            else
            {
                this.Invoke(DisplayAlert, false);
                ScanData.Add(str, timeStr);
                dataGridView1.Rows.Insert(0, str, timeStr);
                str.Replace("\"", "\"\"");
                if(str.Contains(",") || str.Contains("\r") || str.Contains("\n") || str.Contains("\t") || str.Contains(" ") || str.Contains("\""))
                {
                    str =$"\"{str}\"";
                }
                if (storeFile == null)
                {

                    SaveFileDialog openFile = new SaveFileDialog();
                    openFile.Filter = "CSV文件|*.csv";
                    openFile.FilterIndex = 0;
                    if (DialogResult.OK == openFile.ShowDialog())
                    {
                        storeFile = openFile.FileName;
                    }
                    else
                    {
                        MessageBox.Show("必须指定存储文件");
                        return;
                    }
                }
                System.IO.FileStream fs = new System.IO.FileStream(storeFile, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(fs, Encoding.UTF8);
                
                streamWriter.WriteLine($"{str}\t{timeStr}");
                streamWriter.Close();
                fs.Close();
            }
            
        }
        public void TextBoxSetText(string text)
        {
            this.textBox1.Text = text;
        }
        /// <summary>
        /// 扫码枪发现事件处理函数
        /// </summary>
        /// <param name="systemInfo"></param>
        private void SerSystemDiscoverer_SystemDiscovered(SerSystemDiscoverer.SystemInfo systemInfo)
        {
            if (this.AutoLoad)
            {
                selectScanner(systemInfo.PortName);
            }
            else
            {
                if (!systemInfos.Contains(systemInfo))
                {
                    systemInfos.Add(systemInfo);
                    this.Invoke(GetScanner, systemInfos);
                    this.OnFreshScannerList();
                }
            }
        }
        /// <summary>
        /// 扫码枪列表框选择项更改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectScanner(comboBox1.SelectedItem.ToString());
        }
        private void selectScanner(string scanner)
        {

            if (serSystemConnector != null || dataManSystem != null)
            {
                if (dataManSystem == null)
                {
                    serSystemConnector.Disconnect();
                    serSystemConnector.Dispose();
                    serSystemConnector = null;
                }
                else if (serSystemConnector == null)
                {
                    dataManSystem.Disconnect();
                    dataManSystem.Dispose();
                    dataManSystem = null;
                }
                else
                {
                    try
                    {
                        dataManSystem.ReadStringArrived -= DataManSystem_ReadStringArrived;
                    }
                    catch
                    {

                    }
                    dataManSystem.Disconnect();
                    dataManSystem = null;
                    serSystemConnector.Disconnect();
                    serSystemConnector = null;
                }
            }
            try
            {
                serSystemConnector = new SerSystemConnector(scanner);
                dataManSystem = new DataManSystem(serSystemConnector);
            }
            catch (Exception)
            {

                throw;
            }
            try
            {
                dataManSystem.ReadStringArrived -= DataManSystem_ReadStringArrived;
            }
            finally
            {
                dataManSystem.ReadStringArrived += DataManSystem_ReadStringArrived;
            }
            try
            {
                dataManSystem.SystemDisconnected -= DataManSystem_SystemDisconnected;
            }
            finally
            {
                dataManSystem.SystemDisconnected += DataManSystem_SystemDisconnected;
                if (serSystemConnector.IsKeepAliveSupported)
                {
                    serSystemConnector.SetKeepAliveOptions(true, 1000, 2000);
                }

            }
            try
            {
                dataManSystem.SystemWentOffline -= DataManSystem_SystemWentOffline;
            }
            finally
            {
                dataManSystem.SystemWentOffline += DataManSystem_SystemWentOffline;
            }
            dataManSystem.Connect();
            this.Invoke(SetFormTitle,$"扫码校验【{serSystemConnector.PortName},{serSystemConnector.Baudrate},{serSystemConnector.Parity},{serSystemConnector.DataBits},{serSystemConnector.StopBits}】");

        }
        private void DataManSystem_SystemWentOffline(object sender, EventArgs args)
        {

        }

        private void DataManSystem_SystemDisconnected(object sender, EventArgs args)
        {
            dataManSystem.Disconnect();
            dataManSystem.Dispose();
            dataManSystem = null;
            this.Invoke(SetFormTitle, $"扫码校验");
            this.OnFreshScannerList();
        }

        private void DataManSystem_ReadStringArrived(object sender, ReadStringArrivedEventArgs args)
        {
            this.Invoke(GetData, args.ReadString);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "CSV文件|*.csv";
            openFile.FilterIndex = 0;
            openFile.Multiselect = true;
            openFile.ShowDialog();
            List<string> files = new List<string>();
            files.AddRange(openFile.FileNames);
            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            bool ignoreAll = false;
            foreach(string file in files)
            {
                Microsoft.VisualBasic.FileIO.TextFieldParser textFieldParser = new TextFieldParser(file);                
                while (!textFieldParser.EndOfData) 
                {
                    textFieldParser.SetDelimiters("\t");
                    string[] arr = textFieldParser.ReadFields();
                    if (!keyValues.ContainsKey(arr[0]))
                    {
                        keyValues.Add(arr[0], arr[1]);
                    }
                    else
                    {
                        if (  ignoreAll || MessageBox.Show("发现冲突数据，是否继续？\n继续将会忽略冲突数据！！","警告",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                        {
                            ignoreAll = true;
                            continue;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            foreach(KeyValuePair<string,string> keyValuePair in keyValues)
            {
                if (!ScanData.ContainsKey(keyValuePair.Key))
                {
                    ScanData.Add(keyValuePair.Key, keyValuePair.Value);
                    this.dataGridView1.Rows.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog openFile = new SaveFileDialog();
            openFile.Filter = "CSV文件|*.csv";
            openFile.FilterIndex = 0;
            if(DialogResult.OK == openFile.ShowDialog())
            {
                storeFile = openFile.FileName;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoLoad = checkBox1.Checked;
            this.AutoLoad = checkBox1.Checked;
        }
    }
}
