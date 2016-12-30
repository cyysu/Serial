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


using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;


namespace wildfire_MultiFuctionalSerial_assistant
{
    /// <summary>
    /// GPS_map.xaml 的交互逻辑
    /// </summary>
    public partial class GPSMap : UserControl
    {
        #region 引用nmeaLib
           
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct nmeaPARSER
            {
            public unsafe void* top_node;
            public unsafe void* end_node;
            public unsafe char* buffer;
            public int buff_size;
            public int buff_use;

            }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct nmeaTIME
            {
                public int year;       /**< Years since 1900 */
                public int mon;        /**< Months since January - [0,11] */
                public int day;        /**< Day of the month - [1,31] */
                public int hour;       /**< Hours since midnight - [0,23] */
                public int min;        /**< Minutes after the hour - [0,59] */
                public int sec;        /**< Seconds after the minute - [0,59] */
                public int hsec;       /**< Hundredth part of second - [0,99] */

            }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct nmeaSATELLITE
        {
            public int id;         /**< Satellite PRN number */
            public int in_use;     /**< Used in position fix */
            public int elv;        /**< Elevation in degrees, 90 maximum */
            public int azimuth;    /**< Azimuth, degrees from true north, 000 to 359 */
            public int sig;        /**< Signal, 00-99 dB */

        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct nmeaSATINFO
        {
            public int inuse;      /**< Number of satellites in use (not those in view) */
            public int inview;     /**< Total number of satellites in view */

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public nmeaSATELLITE[] sat; /**< Satellites information */

        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct nmeaINFO
        {
            public int smask;      /**< Mask specifying types of packages from which data have been obtained */

            public nmeaTIME utc;       /**< UTC of position */

            public int sig;        /**< GPS quality indicator (0 = Invalid; 1 = Fix; 2 = Differential, 3 = Sensitive) */
            public int fix;        /**< Operating mode, used for navigation (1 = Fix not available; 2 = 2D; 3 = 3D) */

            public double PDOP;       /**< Position Dilution Of Precision */
            public double HDOP;       /**< Horizontal Dilution Of Precision */
            public double VDOP;       /**< Vertical Dilution Of Precision */

            public double lat;        /**< Latitude in NDEG - +/-[degree][min].[sec/60] */
            public double lon;        /**< Longitude in NDEG - +/-[degree][min].[sec/60] */
            public double elv;        /**< Antenna altitude above/below mean sea level (geoid) in meters */
            public double speed;      /**< Speed over the ground in kilometers/hour */
            public double direction;  /**< Track angle in degrees True */
            public double declination; /**< Magnetic variation degrees (Easterly var. subtracts from true course) */

            public nmeaSATINFO satinfo; /**< Satellites information */

        }





        [DllImport("nmeaLib.dll", EntryPoint = "nmea_zero_INFO", CallingConvention = CallingConvention.Cdecl)]
        static extern void nmea_zero_INFO(ref nmeaINFO info);

        [DllImport("nmeaLib.dll", EntryPoint = "nmea_parser_init", CallingConvention = CallingConvention.Cdecl)]
        static extern void nmea_parser_init(ref nmeaPARSER parser);

        [DllImport("nmeaLib.dll", EntryPoint = "nmea_parse", CallingConvention = CallingConvention.Cdecl)]
        static extern int nmea_parse(ref nmeaPARSER parser, string buff, int buff_sz,ref nmeaINFO info);
        
        [DllImport("nmeaLib.dll", EntryPoint = "GMTconvert", CallingConvention = CallingConvention.Cdecl)]
        static extern void GMTconvert(ref nmeaTIME SourceTime, ref nmeaTIME ConvertTime, int GMT, int AREA);

        [DllImport("nmeaLib.dll", EntryPoint = "DegreeConvert", CallingConvention = CallingConvention.Cdecl)]
        static extern double DegreeConvert(double sDegree);

        #endregion





        nmeaINFO gpsInfo = new nmeaINFO();
        nmeaPARSER gpsParser = new nmeaPARSER();


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
        public  GPSMap()
        {
           
            InitializeComponent();

            //serialSettingBorder.IsEnabled = false;
            //serialPortControlBorder.IsEnabled = false;


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

                gpsInfoTabControl.SelectedIndex = 1;


                //使能发送面板
                //  sendControlTab.IsEnabled = true;
                //  sendControlTooltip.IsEnabled = false;
                try
                {
                    nmea_zero_INFO(ref gpsInfo);

                    nmea_parser_init(ref gpsParser);
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }




            }
            catch(Exception exc)
            {
                statusTextBlock.Text = "配置串口出错！请检查串口是否被占用。";
               // MessageBox.Show(exc.Message);
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
        private delegate void UpdateTextDelegate(string text);
        private void ReceiveData(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            receiveData = serial.ReadExisting();
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateTextDelegate(ShowData), receiveData);
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateTextDelegate(NMEADecodeGPS), receiveData);
        }

        //显示数据
        private void ShowData(string text)
        {
            string receiveText = text;

            try
            {
                    //更新接收字节数
                    receiveBytesCount += (UInt32)receiveText.Length;
                    statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();


                    //没有关闭数据显示
                    if (stopShowingButton.IsChecked == false)
                    {
                        //字符串显示
                        if (hexadecimalDisplayCheckBox.IsChecked == false)
                        {
                            receiveTextBox.AppendText( receiveText);

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
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
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

        private void FileOpen(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.FileName = "gpslog.txt";
            openFile.DefaultExt = ".txt";
            openFile.Filter = "TXT文本|*.txt";
            if (openFile.ShowDialog() == true)
            {
                receiveTextBox.Text = File.ReadAllText(openFile.FileName, System.Text.Encoding.Default);


                NMEADecodeGPSLog(openFile.FileName);
                //fileNameTextBox.Text = openFile.FileName;
            }
        }

        #endregion



        private void WindowClosed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        #region 状态栏

        //清空计数
        private void countClearButton_Click(object sender, RoutedEventArgs e)
        {
            //接收、发送计数清零
            receiveBytesCount = 0;

            //更新数据显示
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();

        }



        #endregion



        #region 地图相关


        //用于屏蔽webbrowser脚本错误
        public void SuppressScriptErrors(object webBrowser, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(object).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;

            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null) return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }


        private void CommondFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            SuppressScriptErrors(sender, true);
        }

        private void CommondFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (e.Exception is System.Net.WebException)
            {
                // MessageBox.Show("\r\n站点不可达\r\n");
                e.Handled = true;
            }

        }
        #endregion


        #region nmea解码

        //根据解码结果更新gps信息
        private void UpdateInfo(ref nmeaINFO gpsInfo)
        {
            nmeaTIME beiJingTime = new nmeaTIME();

            //转换成北京时间
            GMTconvert(ref gpsInfo.utc,ref beiJingTime,8,1);
            timeLabel.Content = Convert.ToString(beiJingTime.year) + "年" + Convert.ToString(beiJingTime.mon) + "月" + Convert.ToString(beiJingTime.day) + "日" + Convert.ToString(beiJingTime.hour) + ":" + Convert.ToString(beiJingTime.min) + ":" + Convert.ToString(beiJingTime.sec);
            //经度
            longitudeLabel.Content = Convert.ToString(DegreeConvert(gpsInfo.lon))+" E";
            //纬度
            latitudeLabel.Content = Convert.ToString(DegreeConvert(gpsInfo.lat))+" N";
            

            elevatiionLabel.Content = Convert.ToString(gpsInfo.elv)+" 米";
            speedLabel.Content = Convert.ToString(gpsInfo.speed)+" km/h";
            directionLabel.Content = Convert.ToString(gpsInfo.declination);

            switch (gpsInfo.sig)
            {
                case 0:
                    signalLevalLabel.Content = "未定位";
                    break;
                case 1:
                    signalLevalLabel.Content = "标准GPS定位";
                    break;
                case 2:
                    signalLevalLabel.Content = "差分GPS定位";
                    break;
                default:
                    break; 
            }

            switch (gpsInfo.fix)
            {
                case 1:
                    operationModeLabel.Content = "未定位";
                    break;
                case 2:
                    operationModeLabel.Content = "2D定位";
                    break;
                case 3:
                    operationModeLabel.Content = "3D定位";
                    break;
                default:
                    break;
            }

            sateliteInView.Content = Convert.ToString(gpsInfo.satinfo.inview)+" 颗";
            sateliteInUse.Content = Convert.ToString(gpsInfo.satinfo.inuse) + " 颗";

            positionPrecisionLabel.Content = Convert.ToString(gpsInfo.PDOP);
            horizontalPrecisionLabel.Content = Convert.ToString(gpsInfo.HDOP);
            verticalPrecisionLabel.Content = Convert.ToString(gpsInfo.VDOP);

            if (baiDuMap.IsLoaded)
            {
                //非零时才作标注
                if(gpsInfo.lon!=0 &&gpsInfo.lat!=0)
                {
                    //调用javascritpt函数标注地图
                    WebBrowser mapWB = (WebBrowser)baiDuMap.Content;
                    mapWB.InvokeScript("theLocation", new object[] { DegreeConvert(gpsInfo.lon) , DegreeConvert(gpsInfo.lat) });

                }
            }
        
        
        }

        //对gps日志文件进行解码
        public void NMEADecodeGPSLog(string path)
        {
            string readbuf  = "";
            try
            {

                nmea_zero_INFO(ref gpsInfo);

                nmea_parser_init(ref gpsParser);

                gpsInfoTabControl.SelectedIndex = 1;


                foreach (string line in File.ReadLines(path))
                {

                    readbuf = line + "\r\n";
                    nmea_parse(ref gpsParser, readbuf, readbuf.Length, ref gpsInfo);

                    UpdateInfo(ref gpsInfo);

                }   

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
           }

        
        }


        //供串口接收到数据后调用,对接收到的品数据进行解码
        private void NMEADecodeGPS(string gpsData)
        {
         
            nmea_parse(ref gpsParser, gpsData, gpsData.Length, ref gpsInfo);

            UpdateInfo(ref gpsInfo);
        
        
        }
        #endregion

        private void GPSFrame_Navigated(object sender, NavigationEventArgs e)
        {
            //serialSettingBorder.IsEnabled = true;
            //serialPortControlBorder.IsEnabled = true;
        }



    }
    
}



