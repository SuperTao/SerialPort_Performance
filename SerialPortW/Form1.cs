using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Timers;

namespace SerialPortW
{
    public partial class SerialPortW : Form
    {

        SerialPort serialPort;
        long misMacthNum;
        long matchNum;
        long readLineNum;

        System.Timers.Timer tmr;
        public SerialPortW()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            //获取串口
            String[] serialPortArray = SerialPort.GetPortNames();
            if (serialPortArray != null && serialPortArray.Length != 0)
            {
                //对串口进行排序
                Array.Sort<String>(serialPortArray);
                foreach (String port in serialPortArray)
                {
                    //添加到combox的item
                    if (port != null && port.Length != 0)
                        serialPorts.Items.Add(port);
                }
            }
            //设置初始显示的值 
            serialPorts.SelectedIndex = 0;
            baudRate.SelectedIndex = 16;
            dataBits.SelectedIndex = 3;
            parity.SelectedIndex = 0;
            stop.SelectedIndex = 0;
            misMacthNum = 0;
            matchNum = 0;
            readLineNum = 0;
            sendText.AppendText("abcdefghijklmnopqrstuvwxyz0123456789");
            regexbox.AppendText("abcdefghijklmnopqrstuvwxyz0123456789");

            repeatTime_textBox.AppendText("500");
            send.BackColor = Color.Gray;
            //创建串口对象
            serialPort = new SerialPort();
        }

        private void dealReceive(object sender, SerialDataReceivedEventArgs e)
        {
            if (readLineNum % 300 == 0)
            {
                receive.Clear(); 
            }
            //除去空格
            String regex = regexbox.Text.ToString().Trim();
            if (!serialPort.IsOpen) return;
            try
            {
                String data = serialPort.ReadLine();
                readLineNum++;
                if (regex != null)
                {
                    //正则表达式进行比较
                    bool isMatch = Regex.IsMatch(data, regex);
                    if (!isMatch)
                    {
                        //不匹配
                        misMacthNum++;
                    }
                    else
                    {
                        matchNum++;
                    }
                }

                this.BeginInvoke((EventHandler)(delegate
                {
                    receive.AppendText(data + " " + readLineNum + '\n');
                    mismatch_num.Text = misMacthNum + "";
                    match_num.Text = matchNum + "";
                }));
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("read invalidoperationexception");
            }         
            catch (System.IO.IOException)
            {
                Console.WriteLine("read io exception");
            }
        }

        private void send_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
                serialPort.Write(sendText.Text.ToString() + "\n\r");
        }

        private void setPort()
        {
            try
            {
                serialPort.PortName = (String)(serialPorts.SelectedItem);
                serialPort.BaudRate = int.Parse((String)(baudRate.SelectedItem));
                serialPort.DataBits = int.Parse((String)(dataBits.SelectedItem));
                
                switch (stop.SelectedIndex)
                {
                    case 0:
                        serialPort.StopBits = StopBits.One;
                        break;
                    // 不支持 StopBits.None, 不知为何OnePointFive的时候出错,因此没有设置
                    //case 1:   
                    //    serialPort.StopBits = StopBits.OnePointFive;
                    //    break;
                    case 1:
                        serialPort.StopBits = StopBits.Two;
                        break;
                }
                
                switch (parity.SelectedIndex)
                {
                    case 0:
                        serialPort.Parity = Parity.None;
                        break;
                    case 1:
                        serialPort.Parity = Parity.Odd;
                        break;
                    case 2:
                        serialPort.Parity = Parity.Even;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        //open or close
        private void start_Click(object sender, EventArgs e)
        {
            if (start.Text == "Open")
            {
                if (serialPort.IsOpen)
                    serialPort.Close();

                readLineNum = 0;
                misMacthNum = 0;
                matchNum = 0;
                setPort();

                try 
                { 
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(dealReceive) ;
                    serialPort.Open();
                    if (repeat_checkBox.Checked == true)
                    {
                        tmr = new System.Timers.Timer();
                        tmr.Interval = (double)(int.Parse(repeatTime_textBox.Text));  //ms
                        tmr.Elapsed += new ElapsedEventHandler(tmr_timeout);
                        tmr.Enabled = true;
                        tmr.AutoReset = true;      //false:只定时一次, true：重复执行
                        tmr.Start();
                    }
                    start.Text = "Close";
                    send.BackColor = Color.GreenYellow;
                }
                catch (System.IO.IOException)
                {
                    Console.WriteLine("system io exception-----");
                }
                catch (Exception)
                {
                    MessageBox.Show("current port has been used by other application! \nor some other error may happened!\n");
                }
            }
            else
            {
                if (repeat_checkBox.Checked == true && tmr.Enabled == true)
                {
                    tmr.Enabled = false;
                    tmr.Stop();
                }

                try 
                {
                        serialPort.Close();
                }
                catch (System.InvalidOperationException)
                {
                    Console.WriteLine("invalid operation exception");
                }
                catch (System.IO.IOException)
                {
                    Console.WriteLine("Close io exception");
                }
                start.Text = "Open";
                send.BackColor = Color.Gray;
            }
        }

        //清除接收框中的内容
        private void cleanText_Click(object sender, EventArgs e)
        {
            receive.Clear();
            if (!serialPort.IsOpen)
            {
                readLineNum = 0;
                misMacthNum = 0;
                matchNum = 0;
                match_num.Text = "0";
                mismatch_num.Text = "0";
            }
        }

        void tmr_timeout(object sender, ElapsedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.Write(sendText.Text.ToString() + "\r\n");
                    //send.PerformClick();
                }
                catch (System.InvalidOperationException)
                {
                    Console.WriteLine("invalid exception ------");
                }
                catch (System.IO.IOException)
                {
                    Console.WriteLine("io exception ------");
                }
            }
        }
    }
}
