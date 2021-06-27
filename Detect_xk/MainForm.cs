using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Xml;
using System.IO.Ports;

namespace Detect_xk
{
    public partial class MainForm : Form
    {
        //各种类对象的引入
        Thread threadWatch = null; // 负责监听客户端连接请求的 线程；
        Thread SocMain = null;
        Thread MainThread = null;
        Socket socketWatch = null;
        Socket sokConnection = null;
        COM com = new COM();
        Detect_Algorithm algorithm = new Detect_Algorithm();

        //各种地址，其实主要是配置文件的地址
        public static DirectoryInfo Dir = new DirectoryInfo(System.Environment.CurrentDirectory + "\\Config");//当前读取或者生成的 《配置文件夹》 的绝对路径
        string CurConfigAdr = null;//当前使用的或者选中的《配置文件》的绝对路径
        string XmlConfigAdr = System.Environment.CurrentDirectory + "\\config_load.xml"; // 存放 《记录上一次所选择xml配置文件》 的地址

        //配置信息
        XmlDocument XmlConfig = new XmlDocument();// 读取的是 《记录上一次所选择xml配置文件》 的这个xml，由这个来决定初始化哪个具体的配置文件
        XmlDocument cur_XmlConfig = new XmlDocument();// 具体加载进来的xml文件，需要从中读取当前要处理屏幕型号的信息
        int Cnt = 0;
        int curCnt = 0;
        //获取到的配置信息
        public DetectSet[] detectSet;


        //计算的结果信息
        bool[] Res = new bool[18];
        bool Res_End = true;

        bool Is_Connected = false;

        public string userName;
        public string passWord;

        public static Status status = Status.WAIT_FOR_USER;
        public MainForm()
        {
            InitializeComponent();
            // 获取所有配置文件
            ConfigInit();
            // 初始化COM口
            ComInit();
            // 初始化网络连接
            TcpInit();
            // 开启主线程
            MainThread = new Thread(MainTd);
            MainThread.Start();
        }
        void MainTd()
        {
            while (true)
            {
                //完成一块板子的检测
                if (curCnt == Cnt)
                {
                    status = Status.WAIT_FOR_USER;
                    this.Invoke(new Action(() =>
                    {
                        this.btn_start.Enabled = true;
                    }));
                    curCnt = 0;
                    if (Res_End == true)
                    {

                        this.Invoke(new Action(() =>
                        {
                            Result_End.Location = new Point(92, 105);
                            Result_End.Text = "合格";
                            Panel_Result.BackColor = Color.Lime;
                        }));
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            Result_End.Location = new Point(72, 105);
                            Result_End.Text = "不合格";
                            Panel_Result.BackColor = Color.Red;
                        }));
                    }
                }
                switch (status)
                {
                    case Status.WAIT_FOR_USER:
                        break;
                    case Status.START:
                        init();
                        break;
                    case Status.SENDGING:
                        SendMsg();
                        status = Status.WAIT_FOR_DOWN;
                        break;
                    case Status.WAIT_FOR_DOWN:
                        break;
                    case Status.DETECTING:
                        Detecting();
                        show();
                        curCnt++;
                        status = Status.SENDGING;
                        break;
                }
                
            }
        }

        //----------------------流程----------------------------//
        #region 流程逻辑
        private void btn_start_Click(object sender, EventArgs e)
        {
            status = Status.START;
            btn_start.Enabled = false;
        }
        private void show()
        {
            switch (curCnt)
            {
                case 0:
                    if(Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result1.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result1.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 1:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result2.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result2.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 2:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result3.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result3.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 3:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result4.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result4.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 4:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result5.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result5.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 5:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result6.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result6.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 6:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result7.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result7.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 7:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result8.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result8.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 8:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result9.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result9.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 9:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result10.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result10.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 10:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result11.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result11.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 11:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result12.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result12.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 12:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result13.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result13.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 13:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result14.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result14.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 14:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result15.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result15.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 15:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result16.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result16.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 16:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result17.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result17.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 17:
                    if (Res[curCnt] == true)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result18.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result18.LanternBackground = Color.Red;
                        }));
                    }
                    break;
            }
        }

        private void init()
        {
            //在这里进行两块板子之间各种数据以及界面的初始化

            status = Status.SENDGING;
        }
        #endregion
        //-----------------------Init--------------------------//
        #region 初始化
        /// <summary>
        /// 获取配置文件
        /// </summary>
        private void ConfigInit()
        {
            try
            {
                //获取Config路径
                var list = Dir.GetFiles();
                for (int i = 0; i < list.Length; i++)
                {
                    ConfigAdr.Items.Add(list[i].Name);
                }
                XmlConfig = XmlTools.readXml(XmlConfigAdr);
                XmlNode root = XmlTools.getXmlNode(XmlConfig, "/last_used/last");
                string name = root.InnerText;
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i].Name == name)
                    {
                        ConfigAdr.Enabled = false;
                        CurConfigAdr = ConfigAdr.Text;
                        loader.Text = "取消";
                        ConfigAdr.Text = list[i].Name;
                        CurConfigAdr = Dir.FullName + "\\" + ConfigAdr.Text;
                        load_Config(CurConfigAdr);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("未获取到配置文件信息:" + ex);
            }

        }
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="adr"></param>
        private void load_Config(string adr)
        {

            if (adr == null)
            {
                MessageBox.Show("error ： 配置文件为空");
            }
            else
            {
                cur_XmlConfig = XmlTools.readXml(adr);// 将参数加载进来的处理
                XmlNodeList rootList = cur_XmlConfig.SelectNodes("(./root)");
                XmlNode root = rootList[0];
                if (root.HasChildNodes)
                {
                    Cnt = Convert.ToInt32(root.ChildNodes[0].InnerText);
                    detectSet = new DetectSet[Cnt];
                    for (int i = 1; i < root.ChildNodes.Count; i++)
                    {
                        XmlElement temp = (XmlElement)root.ChildNodes[i];
                        if(temp.GetAttribute("type") == "PUSH")
                        {
                            detectSet[i - 1].type = Det_Type.Push;
                        }
                        else if (temp.GetAttribute("type") == "SPIN")
                        {
                                detectSet[i - 1].type = Det_Type.Spin;
                        }
                        string sx = null, sy = null;
                        string loction = temp.InnerText;
                        for (int j = 0; j < loction.Length; j++)
                        {
                            if(loction[j] == ',')
                            {
                                sx = loction.Substring(0, j);
                                sy = loction.Substring(j + 1);
                            }
                        }
                        detectSet[i - 1].loc_x = Convert.ToInt32(sx);
                        detectSet[i - 1].loc_y = Convert.ToInt32(sy);
                        Console.WriteLine(root.ChildNodes[i].InnerText);
                    }
                }
            }
        }

        /// <summary>
        /// 加载指定文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loader_Click(object sender, EventArgs e)
        {
            if (ConfigAdr.Enabled == true)
            {
                ConfigAdr.Enabled = false;
                CurConfigAdr = ConfigAdr.Text;
                loader.Text = "取消";

                //修改当前config的xml文件路径
                XmlNode root = XmlTools.getXmlNode(XmlConfig, "/last_used/last");
                root.InnerText = ConfigAdr.Text;
                XmlTools.writeXml(XmlConfig, XmlConfigAdr);
                //加载当前选中的文档
                CurConfigAdr = Dir.FullName + "\\" + ConfigAdr.Text;
                load_Config(CurConfigAdr);
            }
            else
            {
                ConfigAdr.Enabled = true;
                CurConfigAdr = null;
                loader.Text = "加载";
            }
        }

        /// <summary>
        /// 主界面加载出登录界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            UserLoader formLogin = new UserLoader();
            formLogin.ShowDialog();
            if (formLogin.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                userName = formLogin.UserName;
                passWord = formLogin.PassWord;
                formLogin.Dispose();
            }
            else
            {
                formLogin.Dispose();
                this.Dispose();
            }

        }
        #endregion

        #region COM初始化
        void ComInit()
        {
            com1_Name.Text = "COM1";
            com1_Baud.Text = "9600";
            com1_Data.Text = "8";
            com1_Hand.Text = "None";
            com1_Stop.Text = "1";
            com.InitCom(com1_Name.Text, com1_Baud.Text, com1_Data.Text, com1_Hand.Text, com1_Stop.Text, ref com.com1);
            com.com1.DataReceived += new SerialDataReceivedEventHandler(Com1Receive);
            //com.InitCom(com2_Name.Text, com2_Baud.Text, com2_Data.Text, com2_Hand.Text, com2_Stop.Text, ref com.com2);
        }
        #endregion

        //-----------------------Socket------------------------//
        #region 通信协议
        /// <summary>
        /// TCP 服务端初始化
        /// </summary>
        private void TcpInit()
        {
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            txtIp.Text = AddressIP;
        }

        /// <summary>
        /// 端口监听线程
        /// </summary>
        void WatchConnecting()
        {
            while (Is_Connected == false)  // 持续不断的监听客户端的连接请求；
            {
                // 开始监听客户端连接请求，Accept方法会阻断当前的线程；
                sokConnection = socketWatch.Accept(); // 一旦监听到一个客户端的请求，就返回一个与该客户端通信的 套接字；
                var ssss = sokConnection.RemoteEndPoint.ToString().Split(':');
                // 将与客户端连接的 套接字 对象添加到集合中；
                SocMain = new Thread(RecMsg);
                SocMain.IsBackground = true;
                SocMain.Start(sokConnection);
                Is_Connected = true;
                ShowMsg("新客户端连接成功");
            }
        }

        /// <summary>
        /// 消息接受线程
        /// </summary>
        /// <param name="sokConnectionparn"></param>
        void RecMsg(object sokConnectionparn)
        {
            Socket sokClient = sokConnectionparn as Socket;
            while (true)
            {
                // 定义一个缓存区；
                byte[] arrMsgRec = new byte[1024];
                // 将接受到的数据存入到输入  arrMsgRec中；
                int length = -1;
                try
                {
                    length = sokClient.Receive(arrMsgRec); // 接收数据，并返回数据的长度；
                    if (length > 0)
                    {
                        //主业务
                        string RecByte = System.Text.Encoding.Default.GetString(arrMsgRec);
                        ShowMsg(RecByte);
                    }
                    else
                    {
                        // 从列表中移除被中断的连接IP
                        lbOnline.Items.Remove(sokClient.RemoteEndPoint.ToString());
                        ShowMsg("" + sokClient.RemoteEndPoint.ToString() + "断开连接\r\n");
                        //log.log("遇见异常"+se.Message);
                        break;
                    }
                }
                catch (SocketException se)
                {

                    // 从列表中移除被中断的连接IP
                    lbOnline.Items.Remove(sokClient.RemoteEndPoint.ToString());
                    ShowMsg("" + sokClient.RemoteEndPoint.ToString() + "断开,异常消息：" + se.Message + "\r\n");
                    //log.log("遇见异常"+se.Message);
                    break;
                }
            }
        }
        /// <summary>
        /// 消息展示线程
        /// </summary>
        /// <param name="s"></param>
        private void ShowMsg(string s)
        {
            if (MsgBox.InvokeRequired)
            {
                MsgBox.Invoke(new Action<string>((str) =>
                {
                    ShowMsg(str);
                }), s);

            }
            else
            {
                s += "\r\n";
                MsgBox.Text += s;
            }

        }

        /// <summary>
        /// 命令发送指令
        /// </summary>
        private void SendMsg()
        {
            //在此处发送按在哪里的命令
            Arm.PushLoc(com.com1,detectSet[curCnt].loc_x, detectSet[curCnt].loc_y,detectSet[curCnt].type);
            
        }
        
        /// <summary>
        /// 开始监听某个IP 和 端口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Lisening_Click(object sender, EventArgs e)
        {
            if (txtIp.Enabled == true)
            {
                btn_Lisening.Text = "关闭监听";
                txtIp.Enabled = false;
                txtport.Enabled = false;
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress address = IPAddress.Parse(txtIp.Text.Trim());
                //一个套接字
                IPEndPoint endPoint = new IPEndPoint(address, int.Parse(txtport.Text.Trim()));
                try
                {
                    socketWatch.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    socketWatch.Bind(endPoint);
                }
                catch (SocketException es)
                {
                    MessageBox.Show("异常：" + es.Message);
                    return;
                }
                socketWatch.Listen(100);
                threadWatch = new Thread(WatchConnecting);
                threadWatch.IsBackground = true;
                threadWatch.Start();
                ShowMsg("服务器启动监听成功！");
            }
            else
            {
                btn_Lisening.Text = "开始监听";
                txtIp.Enabled = true;
                txtport.Enabled = true;

                socketWatch.Close();
                threadWatch.Abort();
                while (threadWatch.ThreadState != ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
                SocMain.Abort();
                sokConnection.Close();
                Is_Connected = false;
            }

        }
        /// <summary>
        /// 发送当前TEXT中的字符串
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SocketSend_Click(object sender, EventArgs e)
        {
            string s = SendBox.Text;
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(s);
            sokConnection.Send(byteArray);
        }
        #endregion
        //----------------------算法DLL------------------------//
        #region 算法
        void Detecting()
        {
            switch(curCnt)
            {
                case 0:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 1:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 2:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 3:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 4:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 5:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 6:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 7:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 8:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 9:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 10:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 11:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 12:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 13:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 14:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 15:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 16:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
                case 17:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = true;
                    }
                    else Res[curCnt] = false;
                    break;
            }
        }

        #endregion

        //--------------------串口处理事件---------------------//
        #region COM1
        private void btn_OpenCom1_Click(object sender, EventArgs e)
        {
            if(btn_OpenCom1.Text == "打开串口")
            {
                com.InitCom(com1_Name.Text, com1_Baud.Text, com1_Data.Text, com1_Hand.Text, com1_Stop.Text, ref com.com1);
                com.com1.DataReceived += new SerialDataReceivedEventHandler(Com1Receive);
                com.com1.Open();
                com1_Name.Enabled = false;
                com1_Baud.Enabled = false;
                com1_Data.Enabled = false;
                com1_Stop.Enabled = false;
                com1_Hand.Enabled = false;
                btn_OpenCom1.Text = "关闭串口";
                btn_RefreshCom1.Enabled = false;
            }
            else if(btn_OpenCom1.Text == "关闭串口")
            {
                com.com1.Close();
                com1_Name.Enabled = true;
                com1_Baud.Enabled = true;
                com1_Data.Enabled = true;
                com1_Stop.Enabled = true;
                com1_Hand.Enabled = true;
                btn_OpenCom1.Text = "打开串口";
                btn_RefreshCom1.Enabled = true;
            }
        }
        private void btn_RefreshCom1_Click(object sender, EventArgs e)
        {
            string[] ArryPort = SerialPort.GetPortNames();
            for(int i = 0;i<ArryPort.Length;i++)
            {
                com1_Name.Items.Add(ArryPort[i]);
            }
        }
        private void btn_Com1Send_Click(object sender, EventArgs e)
        {
            string s = rBox_Com1Send.Text;
            com.com1.Write(s);
        }

        public void Com1Receive(
                       object sender,
                       SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            //处理接收到的数据
            this.Invoke(new Action(() =>
            {
                this.rBox_Com1Receive.Text += indata;
            }));
            if(indata == "11")//这里需要更改，和机械臂那边进行一个沟通
            {
                status = Status.DETECTING;
            }
        }
        #endregion
        #region COM2
        private void btn_OpenCom2_Click(object sender, EventArgs e)
        {
            if (btn_OpenCom2.Text == "打开串口")
            {
                com.InitCom(com2_Name.Text, com2_Baud.Text, com2_Data.Text, com2_Hand.Text, com2_Stop.Text, ref com.com2);
                com.com2.DataReceived += new SerialDataReceivedEventHandler(Com2Receive);
                com.com2.Open();
                com2_Name.Enabled = false;
                com2_Baud.Enabled = false;
                com2_Data.Enabled = false;
                com2_Stop.Enabled = false;
                com2_Hand.Enabled = false;
                btn_OpenCom2.Text = "关闭串口";
                btn_RefreshCom2.Enabled = false;
            }
            else if (btn_OpenCom2.Text == "关闭串口")
            {
                com.com2.Close();
                com2_Name.Enabled = true;
                com2_Baud.Enabled = true;
                com2_Data.Enabled = true;
                com2_Stop.Enabled = true;
                com2_Hand.Enabled = true;
                btn_OpenCom2.Text = "打开串口";
                btn_RefreshCom2.Enabled = true;
            }
        }
        private void Com2Receive(
                       object sender,
                       SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            this.Invoke(new Action(() =>
            {
                rBox_Com2Receive.Text += indata;
            }));
        }

        private void btn_RefreshCom2_Click(object sender, EventArgs e)
        {
            string[] ArryPort = SerialPort.GetPortNames();
            for (int i = 0; i < ArryPort.Length; i++)
            {
                com2_Name.Items.Add(ArryPort[i]);
            }
        }

        private void btn_Com2Send_Click(object sender, EventArgs e)
        {
            string s = rBox_Com2Send.Text;
            com.com2.Write(s);
        }
        #endregion


        //-----------------------数据库------------------------//
        private void ConnectToDataBase_Click(object sender, EventArgs e)
        {
            DataBase.connectToDataBase(ref this.dataGridView);
        }
    }
}

public class Message
{
    int x = 0, y = 0;
    
}

public enum Status
{
    WAIT_FOR_USER = 1,
    //等待操作员按下 “开始检测”按键，发送将要按下的按钮位置
    START = 5,
    //按下开始按钮，开始初始化
    SENDGING = 2,

    WAIT_FOR_DOWN = 3,
    //收到机械手 “已按下” 信号

    DETECTING = 4,
    //运行完算法之后，记录并展示结果 进入SENDING状态
}

public enum Fun
{
    Fun_
}

public static class Config {
    static string adr = null;
    static void Init(ComboBox adrlist)
    {
        adr = adrlist.Text;
    }
}
public struct DetectSet 
{
    public Det_Type type;
    public int loc_x;
    public int loc_y;
}
public enum Det_Type
{
    Push = 1,
    Spin = 2,
}

//绑定算法函数
public class Detect_Algorithm
{
    public bool Alg1()
    {
        bool res = false;
        Random temp = new Random();
        int x = temp.Next();
        if (x % 2 == 1) res = true;
        return true;
    }
    public bool Alg2()
    {
        bool res = false;
        Random temp = new Random();
        int x = temp.Next();
        if (x % 2 == 1) res = true;
        return res;
    }
    public bool Alg3()
    {
        bool res = false;
        Random temp = new Random();
        int x = temp.Next();
        if (x % 2 == 1) res = true;
        return res;
    }
    public bool Alg4()
    {
        bool res = false;
        Random temp = new Random();
        int x = temp.Next();
        if (x % 2 == 1) res = true;
        return res;
    }
}