using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Collections;





//TODO: 串口接收到文件
//TODO: 自动获取IP更新服务器
//TODO: 异步打开的文件是否要关闭
//TODO: 接收摄像头数据加载到文本框的时候会出错



namespace wildfire_MultiFuctionalSerial_assistant
{
    /// <summary>
    /// network.xaml 的交互逻辑
    /// </summary>
    public partial class Network : UserControl
    {

        Thread threadWatch = null;
        Thread threadClient = null;
        Thread threadUDPWatch = null;
        Socket socketWatch = null;
        Socket socketClient= null;
        Socket socketUDP = null;

        static UInt32 receiveBytesCount = 0;
        static UInt32 sendBytesCount = 0;
        static UInt32 receiveFrameCount = 0;

        string selectedMode;

        UInt16 connectionNum=0;
        UInt16 udpNum=0;
        EndPoint lastRemotePoint = new IPEndPoint(0,0); 

        Dictionary<int, Socket> dictSock = new Dictionary<int, Socket>();
        Dictionary<int, EndPoint> dictEndPoint = new Dictionary<int, EndPoint>();
        Dictionary<int, string> dictConnectInfo = new Dictionary<int, string>()
        {
            {0,"没有监听到连接。"}
        };
        Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();

        private DispatcherTimer autoSendTimer = new DispatcherTimer();
        private DispatcherTimer secondTimer = new DispatcherTimer();


        string recText="";

        bool saveToFile_IsEnable = false;
        string saveToFile_FileName = "";
        FileStream saveFileHandle;

        private BitmapImage image;
        Stream recStream;
        
        DynamicBufferManager recDynBuffer = new DynamicBufferManager(10*1024*1024);

        static Semaphore writeProtect = new Semaphore(1, 1);



        public Network()
        {
            InitializeComponent();
            

            connectObjectComboBox.ItemsSource = dictConnectInfo;
            connectObjectComboBox.SelectedValuePath = "Key";
            connectObjectComboBox.DisplayMemberPath = "Value";
            connectObjectComboBox.SelectedIndex = 0;

            secondTimer.Tick += new EventHandler(SecondTimer_Tick);
            secondTimer.Interval = new  TimeSpan(0, 0, 1);
            secondTimer.Start();
        }

        void SecondTimer_Tick(object sender, EventArgs e)
        {
            
            statusFrameTextBlock.Text = receiveFrameCount.ToString();
            receiveFrameCount = 0;
            //设置新的定时时间           
            secondTimer.Interval = new TimeSpan(0, 0, 1);


        }

        private void Connect()
        {

            IPAddress address = IPAddress.Parse(IPAddressTextBox.Text.Trim());
            int portNum = int.Parse(portNumTextBox.Text.Trim());
            IPEndPoint endPoint = new IPEndPoint(address, portNum);

            if (selectedMode == "TCP Client")
            {
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    statusTextBlock.Text = ("与服务器连接中...");
                    socketClient.Connect(endPoint);
                    LocalNetworkInfo(socketClient, true);
                }
                catch (SocketException se)
                {
                    statusTextBlock.Text = ("与服务器连接失败！" + se.Message);
                    connectButton.IsChecked = false;
                    return;
                }
                statusTextBlock.Text = ("与服务器连接成功！");

                //显示提示文字
                connectButton.IsChecked = true;
                networkStatusEllipse.Fill = Brushes.Red;


                threadClient = new Thread(RecMsg);
                threadClient.IsBackground = true;
                threadClient.Start(socketClient);
            }
            else if (selectedMode == "TCP Server")
            {

                // 创建负责监听的套接字，注意其中的参数；  
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // 将负责监听的套接字绑定到唯一的ip和端口上；  
                    socketWatch.Bind(endPoint);
                }
                catch (SocketException se)
                {
                    statusTextBlock.Text = ("创建服务器失败！" + se.Message);
                    return;
                }
                // 设置监听队列的长度；  
                socketWatch.Listen(10);
                // 创建负责监听的线程；  
                threadWatch = new Thread(WatchConnecting);
                threadWatch.IsBackground = true;
                threadWatch.Start();

                statusTextBlock.Text = ("创建服务器成功！");

                //显示提示文字
                connectButton.IsChecked = true;
                networkStatusEllipse.Fill = Brushes.Red;
            }
            else if (selectedMode == "UDP")
            {
                // 创建负责监听的套接字，注意其中的参数；  
                socketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    socketUDP.Bind(endPoint);
                }
                catch (SocketException se)
                {
                    statusTextBlock.Text = ("创建UDP服务器失败！" + se.Message);
                    return;
                }

                threadUDPWatch = new Thread(RecMsg);
                threadUDPWatch.Start(socketUDP);


                statusTextBlock.Text = ("创建服务器成功！");

                //显示提示文字
                connectButton.IsChecked = true;
                networkStatusEllipse.Fill = Brushes.Red;
            }
        }

        //打开连接
        private void ConnectButton_Checked(object sender, RoutedEventArgs e)
        {

            ConnectSetingEnable(false);
            Connect();

         }

        private void ConnectSetingEnable(bool isEnable)
        {
            if (isEnable)
            {
                protocalCombobox.IsEnabled = true;
                IPAddressTextBox.IsEnabled = true;
                portNumTextBox.IsEnabled = true;

            }
            else
            {
                protocalCombobox.IsEnabled = false;
                IPAddressTextBox.IsEnabled = false;
                portNumTextBox.IsEnabled = false;

            }
        }



        //显示或隐藏本地网络信息
        void LocalNetworkInfo(Socket sock,Boolean show)
        {
            if (show == true)
            {
                IPEndPoint localEndPoint;
                localEndPoint = (IPEndPoint)sock.LocalEndPoint;
                localIPLabel.Content = localEndPoint.Address.ToString();
                localPortLabel.Content = localEndPoint.Port.ToString();

                localIPInfoLabel.Visibility = Visibility.Visible;
                localPortInfoLabel.Visibility = Visibility.Visible;
                localIPLabel.Visibility = Visibility.Visible;
                localPortLabel.Visibility = Visibility.Visible;
            }
            else
            {
                localIPInfoLabel.Visibility = Visibility.Hidden;
                localPortInfoLabel.Visibility = Visibility.Hidden;
                localIPLabel.Visibility = Visibility.Hidden;
                localPortLabel.Visibility = Visibility.Hidden;
            }

        
        }
        void WatchConnecting()
        {


            while (true)
            {

                try
                {
                    // 开始监听客户端连接请求，Accept方法会阻断当前的线程；  
                    Socket sokConnection = socketWatch.Accept(); // 一旦监听到一个客户端的请求，就返回一个与该客户端通信的 套接字；  
                    connectionNum++;
                    // 将与客户端连接的 套接字 对象添加到集合中；  
                    dictSock.Add(connectionNum - 1, sokConnection);

                    IPEndPoint remoteEndPoint;
                    remoteEndPoint = (IPEndPoint)sokConnection.RemoteEndPoint;

                    if (connectionNum == 1)
                    {
                        dictConnectInfo[0] = remoteEndPoint.ToString();
                    }
                    else
                    {
                        dictConnectInfo.Add(connectionNum - 1, remoteEndPoint.ToString());
                    }

                    this.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        statusTextBlock.Text = ("已与"+dictConnectInfo[connectionNum-1]+"建立连接");
                  
                        connectObjectComboBox.Items.Refresh(); 
                        connectObjectComboBox.SelectedIndex = connectionNum - 1;
                        
                    }));

                    Thread threadServerRec = new Thread(RecMsg);
                    threadServerRec.IsBackground = true;
                    threadServerRec.Start(sokConnection);
                    //dictThread.Add(sokConnection.RemoteEndPoint.ToString(), thr);  //  将新建的线程 添加 到线程的集合中去。  
                }
                catch (SocketException se)
                {
                    this.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        statusTextBlock.Text = ("服务器已失效！");
                        connectButton.IsChecked =false ;
                    }));

                    return;
                }
                catch
                { return; }
            }
        }

        //处理tcp接收的数据。
        void RecMsg(object sockConnectionparn)
        {
            Socket sockConnection = sockConnectionparn as Socket;
            EndPoint remoteEndPoint = new IPEndPoint(0, 0); ;  
            byte[] buffMsgRec = new byte[1024 * 1024 *10];

            int length = -1;

            while (true)
            {

                try
                {
                    if(selectedMode =="UDP")
                   {

                       length = sockConnection.ReceiveFrom(buffMsgRec, ref remoteEndPoint);
                       if (!dictConnectInfo.Values.Contains(remoteEndPoint.ToString()))
                       {
                           udpNum++;
                           if (udpNum == 1)
                               dictConnectInfo[0] = remoteEndPoint.ToString();
                           else
                               dictConnectInfo.Add(udpNum - 1, remoteEndPoint.ToString());


                           this.Dispatcher.BeginInvoke(new Action(delegate
                           {                     
                               connectObjectComboBox.Items.Refresh();
                               connectObjectComboBox.SelectedIndex = udpNum - 1;
                           }));

                       }
                         

                    }
                    else if (selectedMode == "TCP Server")
                    {
                        length = sockConnection.Receive(buffMsgRec);
                    
                    }
                    else if(selectedMode == "TCP Client")
                    {
                        length = sockConnection.Receive(buffMsgRec);


                        //this.Dispatcher.Invoke(new Action(delegate
                        //{
                        //                       //更新接收字节数
                        //receiveBytesCount += (UInt32)length;
                        //statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();

                        //}));

                        recDynBuffer.WriteBuffer(buffMsgRec, 0, length); 

                        //length = sockConnection.Receive(recDynBuffer.Buffer, recDynBuffer.DataCount ,10*1024*1024, SocketFlags.None);
                        //recDynBuffer.DataCount += length;
                        
                    }
                   
                }

                catch (SocketException se)
                {
                    this.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        statusTextBlock.Text = ("接收数据出错！" + se.Message);
                        
                    }));
                    return;

                }
                catch (ThreadAbortException te)
                {
                    //this.Dispatcher.BeginInvoke(new Action(delegate
                    //{
                    //    statusTextBlock.Text = (te.Message);

                    //}));
                    return;

                }
                catch (Exception e)
                {
                    this.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        statusTextBlock.Text = ("异常！" + e.Message);

                    }));
                    return;

                }

                               
                this.Dispatcher.BeginInvoke(new Action(delegate {
                    if (selectedMode == "UDP"  &&  remoteEndPoint != lastRemotePoint)
                        {
                            receiveTextBox.Text += "\r\nreceive from[" + remoteEndPoint.ToString() + "]\r\n";
                            lastRemotePoint = remoteEndPoint;
                        }

                        ShowData(buffMsgRec,length);
                        //ShowData(recDynBuffer.Buffer, recDynBuffer.DataCount); 

                     }));

                Thread.Sleep(10);
            
            
            }
        
        
        }

        int lastStartPosition = -1;
        int lastEndPosition = -1;
        //显示数据
        private void ShowData(byte[] recData ,int dataLength)
        {
            //更新接收字节数
            receiveBytesCount += (UInt32)dataLength;
            statusReceiveByteTextBlock.Text = receiveBytesCount.ToString();

            if (saveToFile_IsEnable)
            {
                try
                { 
                    saveFileHandle.Seek(0, SeekOrigin.End);
                    saveFileHandle.WriteAsync(recData, 0, dataLength);
                }
                catch 
                { return; }

            }
            else
            {
                int startPosition = -1; 
                int endPosition = -1;

#if false

                if (lastStartPosition != -1 && lastEndPosition != -1)
                {
                    recArrary.RemoveRange(lastStartPosition, lastEndPosition - lastStartPosition);
                    lastStartPosition = lastEndPosition = -1;

                }


                foreach(byte b in recSegment)
                    recArrary.Add(b);


                startPosition = IndexOf(recArrary, startCode, 0, recArrary.Count);
                if (startPosition != -1)
                {
                    endPosition = IndexOf(recArrary, endCode, startPosition, recArrary.Count);
                     if (endPosition != -1)
                     {

                         lastStartPosition = startPosition;
                         lastEndPosition = endPosition;
                         recStream = new MemoryStream((byte[])recArrary.ToArray(typeof(byte)), (int)startPosition, (int)(endPosition - startPosition));
                         image = new BitmapImage();

                         image.BeginInit();
                         image.StreamSource = recStream;
                         image.EndInit();
                         picImage.Source = image;

                         picImage.Height = image.PixelHeight;
                         picImage.Width = image.PixelWidth;

                        
                         //recArrary.Clear();
                     }
                     else 
                     {                           
                         //Array.Copy(recData,startPosition,tempData,0,dataLength);
                         return;                     
                     }                
                }                
                else
                { 
                    return;
                }

#else

                if (lastStartPosition != -1 && lastEndPosition != -1)
                {
                    recDynBuffer.Clear(lastEndPosition);
                    lastStartPosition = lastEndPosition = -1;

                }


                //recDynBuffer.WriteBuffer(recData,0,dataLength);


                //try
                //{
                //    saveFileHandle.Seek(0, SeekOrigin.End);
                //    saveFileHandle.WriteAsync(recData, 0, dataLength);
                //}
                //catch
                //{ return; }

                //if (lastStartPosition == -1)
                    startPosition = IndexOf(recDynBuffer.Buffer, startCode, 0, recDynBuffer.DataCount);
                //else
                //    startPosition = lastStartPosition;

                if (startPosition != -1)
                {    
                    lastStartPosition = startPosition;
                    endPosition = IndexOf(recDynBuffer.Buffer, endCode, startPosition, recDynBuffer.DataCount);
                    if (endPosition != -1)
                    {

                  
                        lastEndPosition = endPosition;
                        recStream = new MemoryStream(recDynBuffer.Buffer, startPosition, endPosition - startPosition);
                        image = new BitmapImage();

                        //image.BeginInit();
                        //image.StreamSource = recStream;
                        //image.EndInit();
                        //picImage.Source = image;

                        //picImage.Height = image.PixelHeight;
                        //picImage.Width = image.PixelWidth;


                        ImageSourceConverter converterS = new ImageSourceConverter();

                        picImage.Source = converterS.ConvertFrom(recStream) as BitmapFrame;
                        picImage.Height = picScrollViewer.Height;
                        picImage.Width = picScrollViewer.Width;
                        //recArrary.Clear();

                        //更新帧数
                        receiveFrameCount ++;
                        
                    }
                    else
                    {
                        //Array.Copy(recData,startPosition,tempData,0,dataLength);            

                        return;
                    }
                }
                else
                {

                    return;
                }


#endif


#if false
                //没有关闭数据显示
                if (stopShowingButton.IsChecked == false)
                {
                    try
                    {
                        //字符串显示
                        if (hexadecimalDisplayCheckBox.IsChecked == false)
                        {
                            string receiveText = System.Text.Encoding.Default.GetString(recData, 0, dataLength);// 将接受到的字节数据转化成字符串；  
                            receiveTextBox.AppendText(receiveText);

                        }
                        else //16进制显示
                        {
                            for (UInt32 i = 0; i < dataLength; i++)
                                receiveTextBox.AppendText(string.Format("{0:X2} ", recData[i]));

                        }
                    }
                    catch (SocketException se)
                    {
                        this.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            statusTextBlock.Text = ("发送数据出错！" + se.Message);
                        }));
                        return;

                    }
                    catch (ThreadAbortException te)
                    { return; }
                    catch (Exception e)
                    {
                        this.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            statusTextBlock.Text = ("异常！" + e.Message);

                        }));
                        return;

                    }

                }
#endif
            }
        }


        //断开连接
        private void ConnectButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ConnectSetingEnable(true);
            if (selectedMode == "TCP Server")
            {
                try
                {
                    for (UInt16 i = 0; i < connectionNum; i++)
                        dictSock[i].Close();

                    socketWatch.Close();
                    threadWatch.Abort();

                }
                catch 
                {
                    return;
                }

                dictConnectInfo.Clear();
                connectObjectComboBox.Items.Refresh();
                connectObjectComboBox.SelectedIndex = 0;
                //显示提示文字
                connectButton.Content = "监听";
            }
            else if(selectedMode == "TCP Client")
            {

                try
                {
                    threadClient.Abort();
                    socketClient.Close();
                }
                catch
                {
                    return;
                }

                //隐藏本地网络信息
                LocalNetworkInfo(null, false);
                 //显示提示文字
                connectButton.Content = "连接";
            }

            else if (selectedMode == "UDP")
            {

                try
                {
                    threadUDPWatch.Abort();
                    socketUDP.Close();
                }
                catch
                {
                    return;
                }

                //隐藏本地网络信息
                LocalNetworkInfo(null, false);
                //显示提示文字
                connectButton.Content = "连接";
            }

            networkStatusEllipse.Fill = Brushes.Gray;

            statusTextBlock.Text = ("连接已关闭！");
            ConnectSetingEnable(true);


        }



        //设置滚动条显示到末尾
        private void ReceiveTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (autoClearCheckBox.IsChecked == true && receiveTextBox.LineCount >= 50)
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

        //清空接收数据
        private void ClearReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            receiveTextBox.Clear();
        }



        private void StopShowingButton_Checked(object sender, RoutedEventArgs e)
        {
            stopShowingButton.Content = "恢复显示";
        }

        private void StopShowingButton_Unchecked(object sender, RoutedEventArgs e)
        {
            stopShowingButton.Content = "停止显示";
        }





        private void WindowClosed(object sender, ExecutedRoutedEventArgs e)
        {

            try
            {
                socketUDP.Close();
                socketClient.Close();
                for (UInt16 i = 0; i < connectionNum; i++)
                    dictSock[i].Close();
            }
            catch (Exception)
            {

                return;
            }

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


        //发送数据
        private void SendMsg(object sockConnectionparn)
        {                   
            Socket sockConnection = sockConnectionparn as Socket;

            if (!sockConnection.IsBound)
            {
                statusTextBlock.Text = "请先建立连接";
                return;
            }
            try
            {
                string sendData = sendTextBox.Text;    //复制发送数据

                //字符串发送
                if (hexadecimalSendCheckBox.IsChecked == false)
                {
                    //serial.Write(sendData);
                    byte[] arrData = System.Text.Encoding.Default.GetBytes(sendData);
                    sockConnection.Send(arrData);
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

                        sockConnection.Send(sendBuffer);

                        //更新发送数据计数
                        sendBytesCount += (UInt32)sendBuffer.Length;
                        statusSendByteTextBlock.Text = sendBytesCount.ToString();

                    }
                    catch //无法转为16进制时
                    {
                        autoSendCheckBox.IsChecked = false;//关闭自动发送
                        statusTextBlock.Text = "当前为16进制发送模式，请输入16进制数据";
                        return;
                    }

                }

            }
            catch
            {

            }

        }

        private void SendMsg(EndPoint remoteEP )
        {
            //Socket sockConnection = sockConnectionparn as Socket;

            if (!socketUDP.IsBound)
            {
                statusTextBlock.Text = "请先建立连接";
                return;
            }
            try
            {
                string sendData = sendTextBox.Text;    //复制发送数据

                //字符串发送
                if (hexadecimalSendCheckBox.IsChecked == false)
                {
                    //serial.Write(sendData);
                    byte[] arrData = System.Text.Encoding.Default.GetBytes(sendData);
                    socketUDP.SendTo(arrData,remoteEP);
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

                        socketUDP.SendTo(sendBuffer, remoteEP);

                        //更新发送数据计数
                        sendBytesCount += (UInt32)sendBuffer.Length;
                        statusSendByteTextBlock.Text = sendBytesCount.ToString();

                    }
                    catch //无法转为16进制时
                    {
                        autoSendCheckBox.IsChecked = false;//关闭自动发送
                        statusTextBlock.Text = "当前为16进制发送模式，请输入16进制数据";
                        return;
                    }

                }

            }
            catch
            {

            }

        }


        private void SendMsg_AllProtocal()
        {
            if (selectedMode == "TCP Client")
                SendMsg(socketClient);
            else if (selectedMode == "TCP Server")
            {
                //发送数据到选择的客户端
                SendMsg(dictSock[connectObjectComboBox.SelectedIndex]);
            }
            else if (selectedMode == "UDP")
            {
                EndPoint remoteEndPoint = new IPEndPoint(0, 0);
                string[] addr = connectObjectComboBox.Text.Split(':', '：');
                try
                {
                    remoteEndPoint = new IPEndPoint(IPAddress.Parse(addr[0]), Convert.ToInt32(addr[1]));
                }
                catch
                {
                    statusTextBlock.Text = "输入的IP地址格式不对,示例 192.168.1.11:5000";
                    return;
                }

                SendMsg(remoteEndPoint);
            }
        }
        //手动发送数据
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMsg_AllProtocal();
        }

        //设置自动发送定时器
        private void AutoSendCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //创建定时器
            autoSendTimer.Tick += new EventHandler(AutoSendTimer_Tick);

            //设置定时时间，开启定时器
            autoSendTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(autoSendCycleTextBox.Text));
            autoSendTimer.Start();
        }

        //关闭自动发送定时器
        private void AutoSendCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoSendTimer.Stop();
        }


        int readCound = 0;

        //自动发送时间到
        void AutoSendTimer_Tick(object sender, EventArgs e)
        {
            //发送数据
            //SendMsg_AllProtocal();

#if true

            int startPosition = -1;
            int endPosition = -1;


            saveFileHandle = new FileStream(saveToFile_FileName, FileMode.Open);

            long len = saveFileHandle.Length;

            byte[] recData = new byte[4096];
            saveFileHandle.Seek(4096 * readCound, SeekOrigin.Begin);
            saveFileHandle.Read(recData, 0, 4096);
            readCound++;

            saveFileHandle.Close();


            if (lastStartPosition != -1 && lastEndPosition != -1)
            {
                recDynBuffer.Clear(lastEndPosition);
                lastStartPosition = lastEndPosition = -1;

            }


            recDynBuffer.WriteBuffer(recData, 0, 4096);

            if (startPosition == -1)
                startPosition = IndexOf(recDynBuffer.Buffer, startCode, 0, recDynBuffer.DataCount);
            else
                startPosition = lastStartPosition;

            if (startPosition != -1)
            {
                lastStartPosition = startPosition;
                endPosition = IndexOf(recDynBuffer.Buffer, endCode, startPosition, recDynBuffer.DataCount);
                if (endPosition != -1)
                {

                    lastStartPosition = startPosition;
                    lastEndPosition = endPosition;
                    recStream = new MemoryStream(recDynBuffer.Buffer, startPosition, endPosition - startPosition);
                    image = new BitmapImage();

                        
                    
                    //image.BeginInit();
                    //image.StreamSource = recStream;
                    //image.EndInit();
                    //picImage.Source = image;


                    ImageSourceConverter converterS = new ImageSourceConverter();

                    picImage.Source = converterS.ConvertFrom(recStream) as BitmapFrame;

                    //picImage.Height = 240;
                    //picImage.Width = 320;
                    //recArrary.Clear();
                }
                else
                {
                    //Array.Copy(recData,startPosition,tempData,0,dataLength);
                    return;
                }
            }
            else
            {
                return;
            }
#elif false
            int startPosition = -1; 
            int endPosition = -1;


            saveFileHandle = new FileStream(saveToFile_FileName, FileMode.Open);

            long len = saveFileHandle.Length;

            byte[] recData = new byte[4096];
            saveFileHandle.Seek(4096*readCound,SeekOrigin.Begin);
            saveFileHandle.Read(recData,0,4096);
            readCound++;

            saveFileHandle.Close();


              if (lastStartPosition != -1 && lastEndPosition != -1)
                {
                    recDynBuffer.Clear(lastEndPosition);
                    lastStartPosition = lastEndPosition = -1;

                }


            recDynBuffer.WriteBuffer(recData,0,4096);

            if (startPosition == -1)
                startPosition = IndexOf(recDynBuffer.Buffer, startCode, 0, recDynBuffer.DataCount);
            else
                startPosition = lastStartPosition;

            if (startPosition != -1)
            {
                lastStartPosition = startPosition;
                endPosition = IndexOf(recDynBuffer.Buffer, endCode, startPosition, recDynBuffer.DataCount);
                if (endPosition != -1)
                {

                    lastStartPosition = startPosition;
                    lastEndPosition = endPosition;
                    recStream = new MemoryStream(recDynBuffer.Buffer, startPosition, endPosition - startPosition);
                    image = new BitmapImage();

                    image.BeginInit();
                    image.StreamSource = recStream;
                    image.EndInit();
                    picImage.Source = image;

                    picImage.Height = image.PixelHeight;
                    picImage.Width = image.PixelWidth;


                    //recArrary.Clear();
                }
                else
                {
                    //Array.Copy(recData,startPosition,tempData,0,dataLength);
                    return;
                }
            }
            else
            {
                return;
            }

#else

            saveFileHandle = new FileStream(saveToFile_FileName, FileMode.Open);

            int len = (int)saveFileHandle.Length;
            saveFileHandle.Close();
            byte[] recData = new byte[len];
            recData = File.ReadAllBytes(saveToFile_FileName);

            //Array.Copy(recData, tempData, len);

            startPosition = IndexOf(recData, startCode,endPosition,len); 
            endPosition = IndexOf(recData, endCode,startPosition,len);

            ArraySegment<byte> recSegment = new ArraySegment<byte>(recData, (int)startPosition, (int)(endPosition - startPosition));

            recStream = new MemoryStream(recData, (int)startPosition, (int)(endPosition - startPosition)); //recData.Length

            image = new BitmapImage();


            image.BeginInit();
            image.StreamSource = recStream;
            image.EndInit();
            picImage.Source = image;

            int picHeight = this.image.PixelHeight;
            int picWidth = this.image.PixelWidth;

            picImage.Height = picHeight;
            picImage.Width = picWidth;


#endif

           


            //设置新的定时时间           
            autoSendTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(autoSendCycleTextBox.Text));

        }

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


        private void ProtocalCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             selectedMode = ((ComboBoxItem)protocalCombobox.SelectedItem).Content.ToString();
             try
             {
                 if (selectedMode == "TCP Client")
                 {
                     IPAddressInfoTextBox.Content = "服务器IP地址";
                     portNumInfoTextBox.Content = "服务器端口号";

                     connectObjectLabel.Visibility = Visibility.Hidden;
                     connectObjectComboBox.Visibility = Visibility.Hidden;
                     //显示提示文字
                     connectButton.Content = "连接";

                 }
                 else if (selectedMode == "TCP Server")
                 {
                     IPAddressInfoTextBox.Content = "本地IP地址";
                     portNumInfoTextBox.Content = "本地端口号";

                     connectObjectLabel.Visibility = Visibility.Visible;
                     connectObjectComboBox.Visibility = Visibility.Visible;

                     dictConnectInfo[0] = "没有监听到连接。";
                   
                     connectObjectComboBox.Items.Refresh(); 
                     connectObjectComboBox.SelectedIndex = 0;
                     connectObjectComboBox.IsEditable = false;

                     //显示提示文字
                     connectButton.Content = "监听";
                 }
                 else if (selectedMode == "UDP")
                 {
                     IPAddressInfoTextBox.Content = "服务器IP地址";
                     portNumInfoTextBox.Content = "服务器端口号";

                     connectObjectLabel.Visibility = Visibility.Visible;
                     connectObjectComboBox.Visibility = Visibility.Visible;

                     dictConnectInfo[0] = "输入远端地址，格式 IP:Port";
                   
                     connectObjectComboBox.Items.Refresh(); 
                     connectObjectComboBox.SelectedIndex = 0;
                     connectObjectComboBox.IsEditable = true;
                     //显示提示文字
                     connectButton.Content = "连接";
                 }

             }
             catch { return; }

            
         
        }


        /// <summary>  
        /// 报告指定的 System.Byte[] 在此实例中的第一个匹配项的索引。  
        /// </summary>  
        /// <param name="srcBytes">被执行查找的 System.Byte[]。</param>  
        /// <param name="searchBytes">要查找的 System.Byte[]。</param>  
        /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>  
        internal int IndexOf(byte[] srcBytes, byte[] searchBytes)
        {
            if (srcBytes == null) { return -1; }
            if (searchBytes == null) { return -1; }
            if (srcBytes.Length == 0) { return -1; }
            if (searchBytes.Length == 0) { return -1; }
            if (srcBytes.Length < searchBytes.Length) { return -1; }
            for (int i = 0; i < srcBytes.Length - searchBytes.Length; i++)
            {
                if (srcBytes[i] == searchBytes[0])
                {
                    if (searchBytes.Length == 1) { return i; }
                    bool flag = true;
                    for (int j = 1; j < searchBytes.Length; j++)
                    {
                        if (srcBytes[i + j] != searchBytes[j])
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) { return i; }
                }
            }
            return -1;
        }
        /// <summary>  
        /// 报告指定的 System.Byte[] 在此实例中的第一个匹配项的索引。  
        /// </summary>  
        /// <param name="srcBytes">被执行查找的 System.Byte[]。</param>  
        /// <param name="searchBytes">要查找的 System.Byte[]。</param>  
        /// <param name="startIndex">源字节数组起始位置</param>  
        /// <param name="srcLength">源字节数组长度</param>  
        /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>  

       internal int IndexOf(byte[] srcBytes, byte[] searchBytes,int startIndex, int srcLength)
        {
            if (srcBytes == null) { return -1; }
            if (searchBytes == null) { return -1; }
            if (srcLength == 0 ||srcLength>srcBytes.Length ) { return -1; }
            if(startIndex>=srcBytes.Length ||startIndex<0){return -1;}
            if (searchBytes.Length == 0) { return -1; }
            if (srcLength < searchBytes.Length) { return -1; }
            for (int i = startIndex; i < srcLength - searchBytes.Length; i++)
            {
                if (srcBytes[i] == searchBytes[0])
                {
                    if (searchBytes.Length == 1) { return i; }
                    bool flag = true;
                    for (int j = 1; j < searchBytes.Length; j++)
                    {
                        if (srcBytes[i + j] != searchBytes[j])
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) { return i; }
                }
            }
            return -1;
        }

       /// <summary>  
       /// 报告指定的 System.Byte[] 在此实例中的第一个匹配项的索引。  
       /// </summary>  
       /// <param name="srcBytes">被执行查找的 System.Byte[]。</param>  
       /// <param name="searchBytes">要查找的 System.Byte[]。</param>  
       /// <param name="startIndex">源字节数组起始位置</param>  
       /// <param name="srcLength">源字节数组长度</param>  
       /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>  

       internal int IndexOf(ArrayList srcBytes, byte[] searchBytes, int startIndex, int srcLength)
       {
           if (srcBytes == null) { return -1; }
           if (searchBytes == null) { return -1; }
           if (srcLength == 0 || srcLength > srcBytes.Count) { return -1; }
           if (startIndex >= srcBytes.Count || startIndex < 0) { return -1; }
           if (searchBytes.Length == 0) { return -1; }
           if (srcLength < searchBytes.Length) { return -1; }
           for (int i = startIndex; i < srcLength  - searchBytes.Length; i++)
           {
               if ((byte)srcBytes[i] == searchBytes[0])
               {
                   if (searchBytes.Length == 1) { return i; }
                   bool flag = true;
                   for (int j = 1; j < searchBytes.Length; j++)
                   {
                       if ((byte)srcBytes[i + j] != searchBytes[j])
                       {
                           flag = false;
                           break;
                       }
                   }
                   if (flag) { return i; }
               }
           }
           return -1;
       }


       int startPosition = 0;
       int endPosition = 0;
       byte[] startCode = { 0xff, 0xd8, 0xff };
       byte[] endCode = { 0xff, 0xd9 };
       byte[] tempData = new byte[1024 * 1024 * 10];

        private void FileOpen(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "选择要打开的图片";

            openFile.DefaultExt = ".jpg";
            openFile.Filter = "jpg|*.jpg;*.jpeg|数据文件|*.data|所有文件|*.*";
            if (openFile.ShowDialog() == true)
            {
#if false
                image = new BitmapImage(new System.Uri(openFile.FileName));
                picImage.Source = image;

#else
                saveToFile_FileName = openFile.FileName;
                //saveFileHandle = new FileStream(openFile.FileName, FileMode.Open);

                //long len = saveFileHandle.Length;
                //saveFileHandle.Close();
                //byte[] recData = new byte[len];
                //recData = File.ReadAllBytes(openFile.FileName);

                //Array.Copy(recData,tempData,len);

                //startPosition = IndexOf(recData, startCode);
                //endPosition = IndexOf(recData, endCode);

                //ArraySegment<byte> recSegment = new ArraySegment<byte>(recData, (int)startPosition, (int)(endPosition - startPosition));

                //recStream = new MemoryStream(recData, (int)startPosition, (int)(endPosition - startPosition)); //recData.Length
               
                //image = new BitmapImage();

                
                //image.BeginInit();
                //image.StreamSource = recStream;
                //image.EndInit();
                //picImage.Source = image;

                //int picHeight = this.image.PixelHeight;
                //int picWidth = this.image.PixelWidth;

                //picImage.Height = picHeight;
                //picImage.Width = picWidth;


                

#endif

                fileNameTextBox.Text = openFile.FileName;
            }
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
                saveFile.Filter = "TXT文本|*.txt|数据文件|*.data|所有文件|*.*";
                if (saveFile.ShowDialog() == true)
                {
                    File.AppendAllText(saveFile.FileName, "\r\n******" + DateTime.Now.ToString() + "******\r\n");
                    File.AppendAllText(saveFile.FileName, receiveTextBox.Text);

                }
            }

        }

        private void saveToFileButton_Checked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "TXT文本|*.txt|数据文件|*.data|所有文件|*.*";
            if (saveFile.ShowDialog() == true)
            {
                saveToFile_IsEnable = true;
                try
                {
                    saveFileHandle = File.Open(saveFile.FileName, FileMode.OpenOrCreate);
                }
                catch 
                {
                    saveToFileButton.IsChecked = false;
                    return;
                }

                statusTextBlock.Text = "接收数据已转向文件！";
                receiveTextBox.Foreground = Brushes.Red;
                receiveTextBox.Text = "接收数据已转向文件:" + saveFile.FileName.ToString();
                receiveTextBox.IsEnabled = false;
                
            }

        }

        private void saveToFileButton_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileHandle.Close();
            }
            finally
            {            
                saveToFile_IsEnable = false;
                receiveTextBox.Foreground = Brushes.White;
                receiveTextBox.IsEnabled = true;
                receiveTextBox.Text = "";
                connectButton.IsChecked = false;
            }

        }











    }


}



        

