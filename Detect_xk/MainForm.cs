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
using System.Runtime.InteropServices;

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
        string XmlCntAdr = System.Environment.CurrentDirectory + "\\current_cnt.xml"; // 存放 《今日检测的所有数据》 的地址
        string ErrorAdr = System.Environment.CurrentDirectory + "\\ErrorPic\\";
        string PicAdr = System.Environment.CurrentDirectory + "\\pic\\";

        //配置信息
        XmlDocument XmlConfig = new XmlDocument();// 读取的是 《记录上一次所选择xml配置文件》 的这个xml，由这个来决定初始化哪个具体的配置文件
        XmlDocument cur_XmlConfig = new XmlDocument();// 具体加载进来的xml文件，需要从中读取当前要处理屏幕型号的信息
        XmlDocument Today_CntConfig = new XmlDocument();
        int Cnt = 0;
        int curCnt = 0;
        //获取到的配置信息
        public DetectSet[] detectSet;


        //计算的结果信息
        Result[] Res = new Result[19];
        bool Res_End = true;

        //记录一些静态信息
        public static string[] CheckMsg = {
            "软件版本","屏幕亮度+","工作电流","暗电流","按键亮度+","按键亮度-","屏幕亮度-","HOME键","上一曲","音量-","电源键","音量+","下一曲","BACK键","退出键","触摸屏","按键丝印形状","按键丝印位置","按键丝印亮度"
        };
        //当日的统计数据
        int cntOK, cntNG;

        bool Is_Connected = false;

        // 从登录界面获取的信息
        public string userName = "10086";
        public string passWord;
        public bool isIn = false;

        public static Status status = Status.WAIT_FOR_USER;
        public MainForm()
        {
            InitializeComponent();
            this.AllInit();
        }
        public void AllInit()
        {
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
                //配置是否为ROOT模式
                if (isIn == true)
                    RootConfig();

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
                            cntOK++;
                            Result_End.Location = new Point(66, 134);
                            Result_End.Text = "合格";
                            Panel_Result.BackColor = Color.Lime;
                            //更新xml文件,和界面
                            var root1 = XmlTools.getXmlNode(Today_CntConfig, "/last_used/OK");
                            this.cnt_TodayOK.Text = cntOK.ToString();
                            root1.InnerText = cntOK.ToString();
                            //
                        }));
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            cntNG++;
                            Result_End.Location = new Point(44, 134);
                            Result_End.Text = "不合格";
                            Panel_Result.BackColor = Color.Red;
                            //更新xml文件,和界面
                            var root1 = XmlTools.getXmlNode(Today_CntConfig, "/last_used/NG");
                            this.cnt_TodayNG.Text = cntNG.ToString();
                            root1.InnerText = cntNG.ToString();
                        }));
                    }
                    XmlTools.writeXml(Today_CntConfig, XmlCntAdr);
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
        /// <summary>
        /// 在流程中改变每一步的颜色
        /// </summary>
        private void show()
        {
            switch (curCnt)
            {
                case 0:
                    if(Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                    if (Res[curCnt] == Result.True)
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
                case 18:
                    if (Res[curCnt] == Result.True)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result19.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result19.LanternBackground = Color.Red;
                        }));
                    }
                    break;
            }
        }

        private void init()
        {
            //在这里进行两块板子之间各种数据以及界面的初始化
            for(int i = 0;i<19;i++)
            {
                Res[i] = Result.None;
            }
            //初始化所有的灯泡
            this.Result1.LanternBackground = Color.Silver;
            this.Result2.LanternBackground = Color.Silver;
            this.Result3.LanternBackground = Color.Silver;
            this.Result4.LanternBackground = Color.Silver;
            this.Result5.LanternBackground = Color.Silver;
            this.Result6.LanternBackground = Color.Silver;
            this.Result7.LanternBackground = Color.Silver;
            this.Result8.LanternBackground = Color.Silver;
            this.Result9.LanternBackground = Color.Silver;
            this.Result10.LanternBackground = Color.Silver;
            this.Result11.LanternBackground = Color.Silver;
            this.Result12.LanternBackground = Color.Silver;
            this.Result13.LanternBackground = Color.Silver;
            this.Result14.LanternBackground = Color.Silver;
            this.Result15.LanternBackground = Color.Silver;
            this.Result16.LanternBackground = Color.Silver;
            this.Result17.LanternBackground = Color.Silver;
            this.Result18.LanternBackground = Color.Silver;
            this.Result19.LanternBackground = Color.Silver;
            status = Status.SENDGING;
            // 修改结果展示
            Res_End = true;
            this.Invoke(new Action(() =>
            {
                this.Result_End.Text = "";
                this.Panel_Result.BackColor = Color.Silver;
            }));

            //测试用
            if(Detect_Algorithm.tt == 1)
            {
                Detect_Algorithm.tt = 2;
            }
            else
            {
                Detect_Algorithm.tt = 1;
            }
        }
        #endregion
        //-----------------------Init--------------------------//
        #region 初始化
        /// <summary>
        /// 获取配置文件
        /// </summary>
        private void ConfigInit()
        {
            
            //配置上面那一栏的当日信息
            current_cnt();
            try
            {
                //获取Config路径
                //存放所有型号产品的
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

        private void RootConfig()
        {
            if(userName == "HHUC714" && passWord == "123456")
            {
                ;
            }
            else
            {
                if ( userName != "10086") //进来了 ，并且输入了userName
                {
                    this.Invoke(new Action(() =>
                    {
                        this.tabControl2.TabPages.Remove(root_Debug);
                    }));
                    isIn = false;
                }
            }
        }

        private void current_cnt()
        {
            // 设置当日的统计数据
            string time = DateTime.Now.ToShortDateString().ToString();    // 2008-9-4
            int year = 0, month = 0, day = 0;
            int[] t = new int[3];
            int temp = 0;
            int k = 0;
            for (int i = 0; i < time.Length; i++)
            {
                if (time[i] == '/')
                {
                    t[k++] = temp;
                    temp = 0;
                    continue;
                }
                temp *= 10;
                temp += time[i] - '0';
            }
            t[2] = temp;
            year = t[0]; month = t[1]; day = t[2];//获取到开机的时候的日期，年月日

            int yearInXml, monthInXml, dayInXml;
            Today_CntConfig = XmlTools.readXml(XmlCntAdr);
            
            XmlNode root = XmlTools.getXmlNode(Today_CntConfig, "/last_used/year");
            string syear = root.InnerText;
            root = XmlTools.getXmlNode(Today_CntConfig, "/last_used/month");
            string smonth = root.InnerText;
            root = XmlTools.getXmlNode(Today_CntConfig, "/last_used/day");
            string sday = root.InnerText;
            yearInXml = Convert.ToInt32(syear);
            monthInXml = Convert.ToInt32(smonth);
            dayInXml = Convert.ToInt32(sday);

            
            
            if (yearInXml == year && monthInXml == month && dayInXml == day)//同一天
            {
                //获取OK和NG
                var root1 = XmlTools.getXmlNode(Today_CntConfig, "/last_used/OK");
                cntOK = Convert.ToInt32(root1.InnerText);
                var root2 = XmlTools.getXmlNode(Today_CntConfig, "/last_used/NG");
                cntNG = Convert.ToInt32(root2.InnerText);
                this.cnt_TodayOK.Text = root1.InnerText;
                this.cnt_TodayNG.Text = root2.InnerText;
            }
            else
            {
                cntOK = 0;
                cntNG = 0;
                var root1 = XmlTools.getXmlNode(Today_CntConfig, "/last_used/OK");
                var root2 = XmlTools.getXmlNode(Today_CntConfig, "/last_used/NG");
                var rootYear = XmlTools.getXmlNode(Today_CntConfig, "/last_used/year");
                var rootMonth = XmlTools.getXmlNode(Today_CntConfig, "/last_used/month");
                var rootDay = XmlTools.getXmlNode(Today_CntConfig, "/last_used/day");
                root1.InnerText = "0";
                root2.InnerText = "0";
                rootYear.InnerText = year.ToString();
                rootMonth.InnerText = month.ToString();
                rootDay.InnerText = day.ToString();
                XmlTools.writeXml(Today_CntConfig, XmlCntAdr);
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
                isIn = formLogin.IsIn;
                formLogin.Dispose();
            }
            else
            {
                formLogin.Dispose();
                this.Dispose();
            }

        }
        /// <summary>
        /// 输入产品机型并加载到用户操作界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ModelLoad_Click(object sender, EventArgs e)
        {
            if(btn_ModelLoad.Text == "应用")
            {
                btn_ModelLoad.Text = "取消应用";
                lab_Model.Text = tBox_Model.Text;
                tBox_Model.Enabled = false;
            }
            else
            {
                btn_ModelLoad.Text = "应用";
                lab_Model.Text = null;
                tBox_Model.Enabled = true;        
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
                    if (Detect_Algorithm.brightPulsDetection(PicAdr + "1.bmp", PicAdr + "2.bmp",5))
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 1:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 2:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 3:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 4:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 5:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 6:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 7:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 8:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 9:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 10:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 11:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 12:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 13:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 14:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 15:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 16:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 17:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 18:
                    if (algorithm.Alg1())
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
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
                if(status == Status.WAIT_FOR_DOWN)
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
            DataBase.connectToDataBase(ref this.dataGridView,ref this.ConnectToDataBase,ref this.btn_AllDatas,ref this.btn_DBRangeQuery);
        }
        private void btn_AllDatas_Click(object sender, EventArgs e)
        {
            DataBase.GetAllData(ref this.dataGridView);
        }
        private void btn_DBRangeQuery_Click(object sender, EventArgs e)
        {
            DateTime a, b;
            a = this.TimeBeg.Value;
            b = this.TimeEnd.Value;
            DataBase.GetRangeData(ref this.dataGridView, a, b);
        }
        //--------------------UI点击事件-----------------------//
        #region UI点击事件
        private void Error1_Click(object sender, EventArgs e)
        {
            if (Res[0] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\""+ CheckMsg[0] +"\"有错误";
                picBox_Error.Load(ErrorAdr + "1.jpg");
            }   
        }

        private void Error2_Click(object sender, EventArgs e)
        {
            if (Res[1] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[1] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "2.jpg");
            }
        }

        private void Error3_Click(object sender, EventArgs e)
        {
            if (Res[2] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[2] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "3.jpg");
            }
        }

        private void Error4_Click(object sender, EventArgs e)
        {
            if (Res[3] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[3] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "4.jpg");
            }
        }

        private void Error5_Click(object sender, EventArgs e)
        {
            if (Res[4] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[4] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "5.jpg");
            }
        }

        private void Error6_Click(object sender, EventArgs e)
        {
            if (Res[5] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[5] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "6.jpg");
            }
        }

        private void Error7_Click(object sender, EventArgs e)
        {
            if (Res[6] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[6] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "7.jpg");
            }
        }

        private void Error8_Click(object sender, EventArgs e)
        {
            if (Res[7] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[7] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "8.jpg");
            }
        }

        private void Error9_Click(object sender, EventArgs e)
        {
            if (Res[8] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[8] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "9.jpg");
            }
        }

        private void Error10_Click(object sender, EventArgs e)
        {
            if (Res[9] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[9] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "10.jpg");
            }
        }

        private void Error11_Click(object sender, EventArgs e)
        {
            if (Res[10] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[10] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "11.jpg");
            }
        }
        private void Error12_Click(object sender, EventArgs e)
        {
            if (Res[11] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[11] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "12.jpg");
            }
        }
        private void Error13_Click(object sender, EventArgs e)
        {
            if (Res[12] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[12] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "13.jpg");
            }
        }
        private void Error14_Click(object sender, EventArgs e)
        {
            if (Res[13] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[13] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "14.jpg");
            }
        }
        private void Error15_Click(object sender, EventArgs e)
        {
            if (Res[14] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[14] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "15.jpg");
            }
        }
        private void Error16_Click(object sender, EventArgs e)
        {
            if (Res[15] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[15] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "16.jpg");
            }
        }
        private void Error17_Click(object sender, EventArgs e)
        {
            if (Res[16] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[16] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "17.jpg");
            }
        }
        private void Error18_Click(object sender, EventArgs e)
        {
            if (Res[17] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[17] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "18.jpg");
            }
        }

 

        private void Error19_Click(object sender, EventArgs e)
        {
            if (Res[18] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[18] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "19.jpg");
            }
        }
        #endregion

        
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
    public static int tt = 1;
    public bool Alg1()
    {
        bool res = false;
        Random temp = new Random();
        int x = temp.Next();
        if (x % 2 == 1) res = true;
        if (tt == 1)
            return true;
        else
            return false;
    }
    [DllImport("PanelFunDectDLL.dll", EntryPoint = "brightPulsDetection", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool brightPulsDetection(string prePath,string srcPath,int threshold);
    [DllImport("PanelFunDectDLL.dll", EntryPoint = "fnPanelFunDectDLL", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fnPanelFunDectDLL();
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

public enum Result
{
    None = 2,
    True = 1,
    False = 0,
    
}
    