using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using Microsoft.Win32;


namespace wildfire_MultiFuctionalSerial_assistant
{
    /// <summary>
    /// GSMAssistant.xaml 的交互逻辑
    /// </summary>
    public partial class GSMAssistant : UserControl
    {


        #region 常量定义
        //通用命令注释
        const string SIM900ANote_Head = "点击发送命令:\r\n";


        //常用SIM900A命令定义
        //响应测试
        const string SIM900ACommand_ReplyTest = "AT\r\n";
        const string SIM900AReply_ReplyTest = "AT\r\n命令响应:\r\nOK\r\n\r\n";
        const string SIM900ANote_ReplyTest = "命令说明：\r\n响应测试模块是否正常连接\r\n";
        //查询模块制造商
        const string SIM900ACommand_QueryManufacturer = "AT+GSV\r\n";
        const string SIM900AReply_QueryManufacturer = "AT+GSV\r\n命令响应:\r\nSIM900 R11.0 \r\n\r\n";
        const string SIM900ANote_QueryManufacturer = "命令说明：\r\n显示模块的制造商，名称和版本的信息。 \r\n";
        //查询模块型号
        const string SIM900ACommand_QueryVersion = "ATI\r\n";
        const string SIM900AReply_QueryVersion = "ATI\r\n命令响应:\r\nSIMCOM_Ltd\r\nSIMCOM_SIM900A\r\nRevision:1137B01SIM900A32_ST\r\nOK\r\n\r\n";
        const string SIM900ANote_QueryVersion = "命令说明：\r\n显示模块名称和版本信息 \r\n";
        //查询模块IMEI号
        const string SIM900ACommand_QueryIMEINumber = "AT+GSN\r\n";
        const string SIM900AReply_QueryIMEINumber = "AT+GSN\r\n命令响应:\r\n<sn>\r\nOK\r\n\r\n";
        const string SIM900ANote_QueryIMEINumber = "命令说明：\r\n上报设备的IMEI号(国际移动台设备识别码)。\r\n";
        //查询本机号码
        const string SIM900ACommand_QueryPhoneNumber = "AT+CNUM\r\n";
        const string SIM900AReply_QueryPhoneNumber = "AT+CNUM\r\n命令响应:\r\n+CNUM: 本机号码 \r\nOK\r\n\r\n";
        const string SIM900ANote_QueryPhoneNumber = "命令说明：\r\n返回本机的电话号码，(经测试某些电话卡不会返回号码，仅返回OK)\r\n";
        //查询运营商
        const string SIM900ACommand_QueryCarrieroperator = "AT+COPS?\r\n";
        const string SIM900AReply_QueryCarrieroperator = "AT+COPS?\r\n命令响应:\r\n+COPS: 0,0,,<name> \r\nOK\r\n\r\n";
        const string SIM900ANote_QueryCarrieroperator = "命令说明：\r\n显示模块当前注册的网络运营商。 \r\n";
        //查询模块温度
        const string SIM900ACommand_QueryTemperature = "AT+CMTE?\r\n";
        const string SIM900AReply_QueryTemperature = "AT+CMTE?\r\n命令响应:\r\n+CMTE: <mode><Temperature> \r\nOK\r\n\r\n";
        const string SIM900ANote_QueryTemperature = "命令说明：\r\n查询当前模块温度\r\n";
        //查询信号强度
        const string SIM900ACommand_QuerySignalIntensity = "AT+CSQ\r\n";
        const string SIM900AReply_QuerySignalIntensity = "AT+CSQ\r\n命令响应:\r\n+CSQ: <rssi>,<ber> \r\nOK\r\n\r\n";
        const string SIM900ANote_QuerySignalIntensity = "命令说明：\r\n查询当前信号强度\r\n<rssi>\r\n0  小于等于-115dBm \r\n1  -111dBm\r\n2...30  -110... -54dBm\r\n31  大于等于-52dBm\r\n99  未知或者不可测\r\n<ber> (百分比)：\r\n0...7  RXQUA 值\r\n99  未知或者不可测 \r\n";
        //打开侧音功能
        const string SIM900ACommand_TurnOnSidet = "AT+SIDET=0,16\r\n";
        const string SIM900AReply_TurnOnSidet = "AT+SIDET=0,16\r\n命令响应:\r\nAT+SIDET=0,16\r\nOK\r\n\r\n";
        const string SIM900ANote_TurnOnSidet = "命令说明：\r\n开启侧音功能，用于测试麦克风和耳机，拨打电话时麦克风的语音会回环输出到耳机\r\n";
        //关闭侧音功能
        const string SIM900ACommand_TurnOffSidet = "AT+SIDET=0,0\r\n";
        const string SIM900AReply_TurnOffSidet = "AT&F\r\n命令响应:\r\nAT+SIDET=0,0  \r\nOK\r\n\r\n";
        const string SIM900ANote_TurnOffSidet = "命令说明：\r\n关闭侧音功能，方便正常使用(如果在通话时设置关闭侧音，会延后至本次通话结束时关闭)\r\n";
        //拨打电话
        const string SIM900ACommand_Dial = "ATD";
        const string SIM900AReply_Dial = "ATDxxxxx\r\n命令响应:\r\nATDxxxxx\r\nOK\r\n\r\n";
        const string SIM900ANote_Dial = "命令说明：\r\n拨打电话,无电话卡时可拨打112测试(电话号码在号码框中输入)\r\n";
        //挂断电话
        const string SIM900ACommand_HangOff = "ATH\r\n";
        const string SIM900AReply_HangOff = "ATH\r\n命令响应:\r\nATH\r\nOK\r\n\r\n";
        const string SIM900ANote_HangOff = "命令说明：\r\n挂断电话\r\n";
        //接听电话
        const string SIM900ACommand_AnswerThePhone = "ATA\r\n";
        const string SIM900AReply_AnswerThePhone = "ATH\r\n命令响应:\r\nATA\r\nOK\r\n\r\n";
        const string SIM900ANote_AnswerThePhone = "命令说明：\r\n接听电话\r\n";
        //重拨电话
        const string SIM900ACommand_ReDial = "ATDL\r\n";
        const string SIM900AReply_ReDial = "ATDL\r\n命令响应:\r\nATDL\r\nOK\r\n\r\n";
        const string SIM900ANote_ReDial = "命令说明：\r\n接听电话\r\n";
        //发送短信        
        const string SIM900ACommand_SendMessageGSM = "AT+CSCS=\"GSM\"\r\n";
        const string SIM900ACommand_SendMessageCMGF = "AT+CMGF=1\r\n";
        const string SIM900ACommand_SendMessageCMGS = "AT+CMGS=";

        const string SIM900AReply_SendMessage = "AT+CSCS=\"GSM\"\r\nAT+CMGF=1\r\n AT+CMGS=\"电话号码\"\r\n>短信内容\r\n命令响应:OK \r\n+CMGS:34\r\nOK\r\n\r\n";
        const string SIM900ANote_SendMessage = "命令说明：\r\n发送短信(暂不支持中文)\r\n";

        //读取短信
        const string SIM900ACommand_ReadMessage = "AT+CMGR=";
        const string SIM900AReply_ReadMessage = "AT+CMGR=<短信编号>\r\n命令响应:+CMGR: \"REC UNREAD\",\"xxxxx\", ,,\"02 /01/30,20:40:31+00\"\r\n<短信内容>\r\nOK\r\n\r\n";
        const string SIM900ANote_ReadMessage = "命令说明：\r\n查看短信(包含中文的短信是ucs2编码)\r\n";


        //手动发送框说明
        const string SIM900ANote_sendTextBox = "在输入SIM900A控制命令时，注意要添加回车结尾\r\n";

        #endregion

        #region 变量定义


        #region 内部变量
        private SerialPort serial = new SerialPort();

        private string receiveData;

        private DispatcherTimer autoSendTimer = new DispatcherTimer();
        private DispatcherTimer autoDetectionTimer = new DispatcherTimer();

        static UInt32 receiveBytesCount = 0;
        static UInt32 sendBytesCount = 0;

        #endregion

        #endregion
        public GSMAssistant()
        {
            InitializeComponent();

            GetValuablePortName();

            // 设置自动检测1秒1次
            autoDetectionTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            autoDetectionTimer.Tick += new EventHandler(AutoDectionTimer_Tick);
            //开启定时器
            autoDetectionTimer.Start();

            //设置状态栏提示
            statusTextBlock.Text = "准备就绪";
        }



        #region 自动更新串口号
        //自动检测串口名
        private void GetValuablePortName()
        {
            //检测有效的串口并添加到combobox
            string[] serialPortName = System.IO.Ports.SerialPort.GetPortNames();

            foreach (string name in serialPortName)
            {
                portNamesCombobox.Items.Add(name);
            }
        }

        //自动检测串口时间到
        private void AutoDectionTimer_Tick(object sender, EventArgs e)
        {

            string[] serialPortName = System.IO.Ports.SerialPort.GetPortNames();

            if (turnOnButton.IsChecked == true)
            {
                //在找到的有效串口号中遍历当前打开的串口号
                foreach (string name in serialPortName)
                {
                    if (serial.PortName == name)
                        return;                 //找到，则返回，不操作               
                }

                //若找不到已打开的串口:表示当前打开的串口已失效
                //按钮回弹
                turnOnButton.IsChecked = false;
                //删除combobox中的名字
                portNamesCombobox.Items.Remove(serial.PortName);
                portNamesCombobox.SelectedIndex = 0;
                //提示消息
                statusTextBlock.Text = "串口已失效！";
            }
            else
            {
                //检查有效串口和combobox中的串口号个数是否不同
                if (portNamesCombobox.Items.Count != serialPortName.Length)
                {
                    //串口数不同，清空combobox
                    portNamesCombobox.Items.Clear();

                    //重新添加有效串口
                    foreach (string name in serialPortName)
                    {
                        portNamesCombobox.Items.Add(name);
                    }
                    portNamesCombobox.SelectedIndex = 0;

                    statusTextBlock.Text = "串口列表已更新！";

                }
            }
        }
        #endregion

        #region 串口配置面板

        //使能或关闭串口配置相关的控件
        private void serialSettingControlState(bool state)
        {
            portNamesCombobox.IsEnabled = state;
            baudRateCombobox.IsEnabled = state;
            parityCombobox.IsEnabled = state;
            dataBitsCombobox.IsEnabled = state;
            stopBitsCombobox.IsEnabled = state;
        }

        //打开串口
        private void TurnOnButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                //配置串口
                serial.PortName = portNamesCombobox.Text;
                serial.BaudRate = Convert.ToInt32(baudRateCombobox.Text);
                serial.Parity = (System.IO.Ports.Parity)Enum.Parse(typeof(System.IO.Ports.Parity), parityCombobox.Text);
                serial.DataBits = Convert.ToInt16(dataBitsCombobox.Text);
                serial.StopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(System.IO.Ports.StopBits), stopBitsCombobox.Text);

                //设置串口编码为default：获取操作系统的当前 ANSI 代码页的编码。
                serial.Encoding = Encoding.Default;

                //添加串口事件处理
                serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(ReceiveData);

                //开启串口
                serial.Open();

                //关闭串口配置面板
                serialSettingControlState(false);

                statusTextBlock.Text = "串口已开启";

                //显示提示文字
                turnOnButton.Content = "关闭串口";

                serialPortStatusEllipse.Fill = Brushes.Red;

                //使能发送面板
                //  sendControlTab.IsEnabled = true;
                //  sendControlTooltip.IsEnabled = false;


            }
            catch
            {
                statusTextBlock.Text = "配置串口出错！请检查串口是否被占用。";
            }

        }


        //关闭串口
        private void TurnOnButton_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                serial.Close();

                //关闭定时器
                autoSendTimer.Stop();

                //使能串口配置面板
                serialSettingControlState(true);

                statusTextBlock.Text = "串口已关闭";

                //显示提示文字
                turnOnButton.Content = "打开串口";

                serialPortStatusEllipse.Fill = Brushes.Gray;

                //使能发送面板
                //   sendControlTab.IsEnabled = false;
                //   sendControlTooltip.IsEnabled = true;
            }
            catch
            {

            }

        }

        #endregion

        #region 接收显示窗口

        //接收数据
        private delegate void UpdateUiTextDelegate(string text);
        private void ReceiveData(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            receiveData = serial.ReadExisting();
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ShowData), receiveData);
        }

        //显示数据
        private void ShowData(string text)
        {
            string receiveText = text;

            //更新接收字节数
            receiveBytesCount += (UInt32)receiveText.Length;
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();


            //没有关闭数据显示
            if (stopShowingButton.IsChecked == false)
            {
                //字符串显示
                if (hexadecimalDisplayCheckBox.IsChecked == false)
                {
                    receiveTextBox.AppendText(receiveText);

                }
                else //16进制显示
                {
                    foreach (byte str in receiveText)
                    {
                        receiveTextBox.AppendText(string.Format("{0:X2} ", str));
                    }
                }
            }

        }

        //设置滚动条显示到末尾
        private void ReceiveTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (receiveTextBox.LineCount >= 50 && autoClearCheckBox.IsChecked == true)
            {

                receiveTextBox.Clear();
            }
            else
            {
                try
                {
                    receiveScrollViewer.ScrollToEnd();
                }
                catch
                {
                }
            }
        }

        #endregion



        #region 接收设置面板

        //清空接收数据
        private void ClearReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            receiveTextBox.Clear();
        }


        //停止显示按钮
        private void StopShowingButton_Checked(object sender, RoutedEventArgs e)
        {
            stopShowingButton.Content = "恢复显示";
        }

        private void StopShowingButton_Unchecked(object sender, RoutedEventArgs e)
        {
            stopShowingButton.Content = "停止显示";
        }



        private void FileSave(object sender, ExecutedRoutedEventArgs e)
        {

            if (receiveTextBox.Text == string.Empty)
            {
                statusTextBlock.Text = "接收区为空，不保存！";
            }
            else
            {
                SaveFileDialog saveFile = new SaveFileDialog();
                saveFile.Filter = "TXT文本|*.txt";
                if (saveFile.ShowDialog() == true)
                {
                    File.AppendAllText(saveFile.FileName, "\r\n******" + DateTime.Now.ToString() + "******\r\n");
                    File.AppendAllText(saveFile.FileName, receiveTextBox.Text);
                    statusTextBlock.Text = "保存成功！";

                }


            }

        }
        #endregion


        #region 发送控制面板

        //发送数据
        private void SerialPortSend(string stringToSend, bool hexdecimalSend)
        {
            if (!serial.IsOpen)
            {

                statusTextBlock.Text = "请先打开串口！";
                return;

            }
            try
            {
                string sendData = stringToSend;    //复制发送数据

                //字符串发送
                if (hexdecimalSend == false)
                {
                    serial.Write(sendData);

                    //更新发送数据计数
                    sendBytesCount += (UInt32)sendData.Length;
                    statusSendByteTextBlock.Text = sendBytesCount.ToString();

                }
                else //十六进制发送
                {
                    try
                    {
                        sendData.Replace("0x", "");   //去掉0x
                        sendData.Replace("0X", "");   //去掉0X
                        //  sendData.


                        string[] strArray = sendData.Split(new char[] { ',', '，', '\r', '\n', ' ', '\t' });
                        int decNum = 0;
                        int i = 0;
                        byte[] sendBuffer = new byte[strArray.Length];  //发送数据缓冲区

                        foreach (string str in strArray)
                        {
                            try
                            {
                                decNum = Convert.ToInt16(str, 16);
                                sendBuffer[i] = Convert.ToByte(decNum);
                                i++;
                            }
                            catch
                            {
                                //MessageBox.Show("字节越界，请逐个字节输入！", "Error");                          
                            }
                        }

                        serial.Write(sendBuffer, 0, sendBuffer.Length);

                        //更新发送数据计数
                        sendBytesCount += (UInt32)sendBuffer.Length;
                        statusSendByteTextBlock.Text = sendBytesCount.ToString();

                    }
                    catch //无法转为16进制时
                    {
                        //autoSendCheckBox.IsChecked = false;//关闭自动发送
                        statusTextBlock.Text = "当前为16进制发送模式，请输入16进制数据";
                        return;
                    }

                }

            }
            catch
            {

            }

        }

        //手动发送数据
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(sendTextBox.Text, (bool)hexadecimalSendCheckBox.IsChecked);
        }

        #region 已删除的功能
        //设置自动发送定时器
        private void AutoSendCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //创建定时器
            autoSendTimer.Tick += new EventHandler(AutoSendTimer_Tick);

            //设置定时时间，开启定时器
            // autoSendTimer.Interval =new TimeSpan(0,0,0,0, Convert.ToInt32(autoSendCycleTextBox.Text));
            autoSendTimer.Start();
        }

        //关闭自动发送定时器
        private void AutoSendCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoSendTimer.Stop();
        }


        //自动发送时间到
        void AutoSendTimer_Tick(object sender, EventArgs e)
        {
            //发送数据
            SerialPortSend(sendTextBox.Text, (bool)hexadecimalSendCheckBox.IsChecked);

            //设置新的定时时间           
            // autoSendTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(autoSendCycleTextBox.Text));

        }

        #endregion

        //清空发送区
        private void ClearSendButton_Click(object sender, RoutedEventArgs e)
        {
            sendTextBox.Clear();
        }



        private void SendTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (hexadecimalSendCheckBox.IsChecked == true)
            {
                MatchCollection hexadecimalCollection = Regex.Matches(e.Text, @"[\da-fA-F]");

                foreach (Match mat in hexadecimalCollection)
                {
#if true
                    sendTextBox.AppendText(mat.Value);
#else
                     sendTextBox.Text += mat;
#endif
                }

                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }


        private void SendTextBox2_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (hexadecimalSendCheckBox2.IsChecked == true)
            {
                MatchCollection hexadecimalCollection = Regex.Matches(e.Text, @"[\da-fA-F]");

                foreach (Match mat in hexadecimalCollection)
                {
#if true
                    sendTextBox2.AppendText(mat.Value);
#else
                     sendTextBox.Text += mat;
#endif
                }

                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void SendButton2_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(sendTextBox2.Text, (bool)hexadecimalSendCheckBox2.IsChecked);
        }

        private void ClearSendButton2_Click(object sender, RoutedEventArgs e)
        {
            sendTextBox2.Clear();
        }

        private void FileOpen(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.FileName = "serialCom";
            openFile.DefaultExt = ".txt";
            openFile.Filter = "TXT文本|*.txt";
            if (openFile.ShowDialog() == true)
            {
                sendTextBox.Text = File.ReadAllText(openFile.FileName, System.Text.Encoding.Default);

                //fileNameTextBox.Text = openFile.FileName;
            }
        }



        #endregion





        private void WindowClosed(object sender, ExecutedRoutedEventArgs e)
        {

        }


        //清空计数
        private void countClearButton_Click(object sender, RoutedEventArgs e)
        {
            //接收、发送计数清零
            receiveBytesCount = 0;
            sendBytesCount = 0;

            //更新数据显示
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();
            statusSendByteTextBlock.Text = sendBytesCount.ToString();

        }


        #region 常用命令
        //响应测试
        private void ModulReplyTestButton_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_ReplyTest, false);
        }

        //查询模块制造商
        private void ModulQueryManufacturer_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_QueryManufacturer, false);
        }

        //查询模块型号
        private void ModulQueryVersion_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_QueryVersion, false);
        }

        //查询模块IMEI号
        private void ModulQueryIMEINumber_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_QueryIMEINumber, false);
        }
        //查询本机号码
        private void ModulQueryPhoneNumber_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_QueryPhoneNumber, false);
        }

        //查询运营商
        private void ModulQueryCarrieroperator_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_QueryCarrieroperator, false);
        }

        //查询信号强度
        private void ModulQuerySignalIntensity_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_QuerySignalIntensity, false);
        }

        //打开侧音功能
        private void ModulTurnOnSidet_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_TurnOnSidet, false);
        }

        //关闭侧音功能
        private void ModulTurnOffSidet_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_TurnOffSidet, false);
        }

        //查询模块温度
        private void ModulQueryTemperature_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_QueryTemperature, false);
        }
        #endregion

        #region 电话功能
        //拨打电话
        private void ModulDial_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_Dial + telephoneNubberTextBox.Text + ";\r\n", false);
        }

        //挂断电话
        private void ModulHangOff_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_HangOff, false);
        }

        //接听电话
        private void ModulAnswerThePhone_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_AnswerThePhone, false);
        }

        //重拨电话
        private void ModulReDial_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_ReDial, false);
        }

        #region 短信格式说明
        //1 普通短信
        // 在开发短信应用时，需要注意几个方面的事项：

        //1.        首先需要使用指令“AT+CREG?”确认模块已经注册入网络再开始发送短信（或者模块启动15秒后）。

        //2.        执行AT+CMGS指令时，只需要带一个’\r’作为命令介绍，很多用户用’\r\n’作为结束，导致接收短信为乱码。

        //3.        在调用AT+CMGS指令后，上位机需要在出现“>”后等待20毫秒后再发送文本数据。

        //4.        在发送完文本数据后，必须发送CTRL+Z(0x1A)控制字符作为结束符号，该符号不会出现在接收方的短信内容里。

        //2 中文短信

        //GU900模块可以兼容GSM、UCS2和GB2312编码方式收发短信。在GSM模式下，一个短信最大可以发送160个字节，其它两种编码可以发送70个字节。GU900D可以支持中文收发，可以完全使用原来的标准指令来实现中文收发，前提就是用AT+CSCS="GB2312"。

        // 每次发送短信文本，一次可以容纳最大320个字节的数据缓冲。因此，如果发送中文短信，如果用户发送了320个字节的汉字英文混合的短信，短信将被分割为多个短信发出。发中文短信的示例命令如下：

        //AT+CMGF=1

        // OK
        // AT+CSCS="GB2312"

        // OK
        // AT+CNMI=2,1

        // OK

        // AT+CMGS=”13913883990”
        //>你好啊                       ‘敲入CTRL+Z

        // +CMGS: 1

        // OK
        #endregion
        //发送短信
        private void ModulSendMessage_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_SendMessageGSM, false);
            Thread.Sleep(10);
            SerialPortSend(SIM900ACommand_SendMessageCMGF, false);
            Thread.Sleep(10);
            SerialPortSend(SIM900ACommand_SendMessageCMGS + "\"" + telephoneNubberTextBox.Text + "\"\r\n", false);

            Thread.Sleep(100);
            SerialPortSend(sendMessageContentTextBox.Text, false);
            byte[] ctrl = { 0x1A };

            try
            {
                serial.Write(ctrl, 0, 1);
            }
            catch
            {
                return;
            }


        }
        //读取短信
        private void ModulReadMessage_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSend(SIM900ACommand_ReadMessage + unReadMessageNumber.Text + "\r\n", false);
        }

        #endregion
        //模块响应测试说明
        private void ModulReplyTestButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_ReplyTest + SIM900ANote_ReplyTest;

        }
        //查询模块制造商说明
        private void ModulQueryManufacturerButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_QueryManufacturer + SIM900ANote_QueryManufacturer;
        }
        //查询模块温度说明
        private void ModulQueryVersionButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_QueryVersion + SIM900ANote_QueryVersion;
        }
        //查询模块IMEI号说明
        private void ModulQueryIMEINumberButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_QueryIMEINumber + SIM900ANote_QueryIMEINumber;
        }
        //查询本机号码说明
        private void ModulQueryPhoneNumberButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_QueryPhoneNumber + SIM900ANote_QueryPhoneNumber;
        }
        //查询运营商说明
        private void ModulQueryCarrieroperatorButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_QueryCarrieroperator + SIM900ANote_QueryCarrieroperator;
        }
        //查询模块温度说明
        private void ModulQueryTemperatureButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_QueryTemperature + SIM900ANote_QueryTemperature;
        }
        //查询信号强度说明
        private void ModulQuerySignalIntensityButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_QuerySignalIntensity + SIM900ANote_QuerySignalIntensity;
        }
        //打开侧音功能说明
        private void ModulTurnOnSidetButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_TurnOnSidet + SIM900ANote_TurnOnSidet;
        }
        //关闭侧音功能说明
        private void ModulTurnOffSidetButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_TurnOffSidet + SIM900ANote_TurnOffSidet;
        }
        //拨打电话说明
        private void ModulDialButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_Dial + SIM900ANote_Dial;
        }
        //挂断电话说明
        private void ModulHangOffButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_HangOff + SIM900ANote_HangOff;
        }
        //接听电话说明
        private void ModulAnswerThePhoneButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_AnswerThePhone + SIM900ANote_AnswerThePhone;
        }
        //发送短信说明
        private void ModulSendMessageButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_SendMessage + SIM900ANote_SendMessage;
        }
        //读取短信说明
        private void ModulReadMessageButton_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_Head + SIM900AReply_ReadMessage + SIM900ANote_ReadMessage;
        }
        //手动发送框说明
        private void SendTextBox_MouseEnter(object sender, MouseEventArgs e)
        {
            commandNoteTextBlock.Text = SIM900ANote_sendTextBox;

        }
         
    }
}
