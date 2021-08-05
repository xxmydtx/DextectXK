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
using MvCamCtrl.NET;

namespace Detect_xk
{
    public partial class MainForm : Form
    {
        //各种类对象的引入
        Thread threadWatch = null; // 负责监听客户端连接请求的 线程；
        Thread SocMain = null;
        Thread MainThread = null;
        Thread CameraOpenThread = null;
        Socket socketWatch = null;
        Socket sokConnection = null;
        COM com = new COM();
        Detect_Algorithm algorithm = new Detect_Algorithm();

        //各种地址，其实主要是配置文件的地址
        public static DirectoryInfo Dir = new DirectoryInfo(System.Environment.CurrentDirectory + "\\Config");//当前读取或者生成的 《配置文件夹》 的绝对路径
        string CurConfigAdr = null;//当前使用的或者选中的《配置文件》的绝对路径
        string XmlConfigAdr = System.Environment.CurrentDirectory + "\\config_load.xml"; // 存放 《记录上一次所选择xml配置文件》 的地址
        string XmlCntAdr = System.Environment.CurrentDirectory + "\\current_cnt.xml"; // 存放 《今日检测的所有数据》 的地址
        string ErrorAdr = System.Environment.CurrentDirectory + "\\pic\\";
        string PicAdr = System.Environment.CurrentDirectory + "\\pic\\";

        //配置信息
        XmlDocument XmlConfig = new XmlDocument();// 读取的是 《记录上一次所选择xml配置文件》 的这个xml，由这个来决定初始化哪个具体的配置文件
        XmlDocument cur_XmlConfig = new XmlDocument();// 具体加载进来的xml文件，需要从中读取当前要处理屏幕型号的信息
        XmlDocument Today_CntConfig = new XmlDocument();
        int Cnt = 20;
        int curCnt = 0;
        //获取到的配置信息
        public DetectSet[] detectSet;


        //计算的结果信息
        Result[] Res = new Result[20];
        bool Res_End = true;
        DataMes curDataMes;

        //记录一些静态信息
        public static string[] CheckMsg = {
            "软件开启","背光开启","屏幕亮度+","按键亮度+","HOME键","上一曲","Power键","音量-","音量+","下一曲","BACK键","按键亮度-","屏幕亮度-","退出键","暗电流","触摸屏","工作电流","按键丝印形状","按键丝印位置","按键丝印亮度"
        };
        //当日的统计数据
        int cntOK, cntNG;

        bool Is_Connected = false;

        // 从登录界面获取的信息
        public string userName = "10086";
        public string passWord;
        public bool isIn = false;

        public static Status status = Status.WAIT_FOR_USER;


        // 海康相机控制对象
        MyCamera m_MyCamera = new MyCamera();
        MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        bool m_bGrabbing = false;
        Thread m_hReceiveThread = null;
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        // ch:用于从驱动获取图像的缓存 | en:Buffer for getting image from driver
        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver = IntPtr.Zero;
        private static Object BufForDriverLock = new Object();
        // 掉线处理委托
        MyCamera.cbExceptiondelegate pCallBackFunc;

        // 扫码枪
        BardCodeHook BarCode = new BardCodeHook();

        
        public MainForm()
        {
            InitializeComponent();
            this.AllInit();
            Control.CheckForIllegalCrossThreadCalls = false;
        }
     
        public void AllInit()
        {
            // 扫码枪初始化
            // 注册扫码枪委托
            BarCode.BarCodeEvent += new BardCodeHook.BardCodeDeletegate(BarCode_BarCodeEvent);
            BarCode.Start();
            // 获取所有配置文件
            ConfigInit();
            // 初始化COM口
            ComInit();
            // 初始化网络连接
            TcpInit();
            // 初始化海康相机
            Hikinit();
            // 初始化PLC通信
            ModbusInit();
            // 开启主线程
            MainThread = new Thread(MainTd);
            MainThread.Start();
        }
        // 每块板子的预处理
        bool isLastNotBack = false;
        int curMark = 0;
        static int[] LC1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20};
        static int[] LC2 = { 13, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 ,20};
        

        int[] LC = LC1;// 初始化成第一个
        // 退出检测的次数
        int cntBack;
        // 进入检测的次数
        int cntEnter;
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
                    WorkingCur = false;// 工作电流
                    ModBus.WriteSingleCoilAsync("21", "1");// 暂定21为终止指令
                    // 在这里写入数据库
                    status = Status.WAIT_FOR_USER;
                    this.Invoke(new Action(() =>
                    {
                        this.btn_start.Enabled = true;
                    }));
                    
                    if (Res_End == true)
                    {
                        curDataMes.MachineNumber = lab_Card.Text; // 写入扫码枪的值
                        this.Invoke(new Action(() =>
                        {
                            curDataMes.TestRes = "OK";
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
                            curDataMes.TestRes = "NG";
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
                    // 初始化curCnt
                    curCnt = 0;
                    curMark = 0;
                    
                    if (curDataMes.ScreenRightUp == "NG")
                    {
                        isLastNotBack = true;
                        LC = LC2;// 错误退出的流程
                    }
                    else LC = LC1;// 正确退出的流程
                    curCnt = LC[curMark];
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
                        while(WaitForReady())
                        {
                            ;
                        }
                        break;
                    case Status.DETECTING:
                        //
                        //应该在这里拍摄照片,拍照前检测相机是否连接正常

                        //if(!Is_Camera)
                        //{
                        //    MessageBox.Show("相机连接不正常，请检查！");
                        //    break;
                        //}
                        //Get_Picture(curCnt);

                        Detecting();
                        // 退出的持续监测
                        if(curCnt == 13)
                        {
                            int curTime = cntBack;
                            while (curCnt == 13 && Res[13] == Result.False && curTime > 0)//持续检测,与是不是第一次无关
                            {
                                //if (!Is_Camera)// 排除相机干扰
                                //{
                                //    MessageBox.Show("相机连接不正常，请检查！");
                                //    break;
                                //}
                                //Get_Picture(curCnt);
                                Detecting();
                                curTime--;
                            }
                            if(Res[13] == Result.True && isLastNotBack == true) // 如果是因为上一次失败而进入的阶段13
                            {
                                isLastNotBack = false;
                            }
                            if(isLastNotBack == true && curDataMes.ScreenRightUp == "NG")
                            {
                                curMark = 20;// 当前等于20 加一下就成21了 就该退出了
                            }
                            if (Res[13] == Result.False) isLastNotBack = true;
                        }
                        // 进入的持续检测
                        if (curCnt == 0)
                        {
                            int curTime = cntEnter;
                            while (curCnt == 0 && curDataMes.ScreenLeftUp == "NG" && curTime > 0)//持续检测,与是不是第一次无关
                            {
                                //if (!Is_Camera)// 排除相机干扰
                                //{
                                //    MessageBox.Show("相机连接不正常，请检查！");
                                //    break;
                                //}
                                //Get_Picture(curCnt);
                                Detecting();
                                curTime--;
                            }
                        }
                        show();
                        curMark++;
                        curCnt = LC[curMark];
                        ResCheck();
                        status = Status.SENDGING;
                        break;
                }
            }
        }
        //------------------------扫码枪----------------------------//
        #region 扫码枪
        // 扫码枪
        private delegate void ShowInfoDelegate(BardCodeHook.BarCodes barCode);
        private void ShowInfo(BardCodeHook.BarCodes barCode)
        {
            
            
          
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ShowInfoDelegate(ShowInfo), new object[] { barCode });
            }
            else
            {
                this.lab_Card.Text = barCode.BarCode;
            }
        }
        void BarCode_BarCodeEvent(BardCodeHook.BarCodes barCode)
        {
            ShowInfo(barCode);
        }
        #endregion
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
                    if (Res[curCnt] == Result.True)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Result0.LanternBackground = Color.Lime;
                        }));
                    }
                    else
                    {
                        Res_End = false;
                        this.Invoke(new Action(() =>
                        {
                            this.Result0.LanternBackground = Color.Red;
                        }));
                    }
                    break;
                case 1:
                    if (Res[curCnt] == Result.True)
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
                case 2:
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
                case 3:
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
                case 4:
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
                case 5:
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
                case 6:
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
                case 7:
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
                case 8:
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
                case 9:
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
                case 10:
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
                case 11:
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
                case 12:
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
                case 13:
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
                case 14:
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
                case 15:
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
                case 16:
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
                case 17:
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
                case 18:
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
                case 19:
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
        /// <summary>
        /// 在这里进行两块板子之间各种数据以及界面的初始化
        /// </summary>
        private void init()
        {
            // 初始化当前数据库信息
            curDataMes = new DataMes();
            curDataMes.DT = DateTime.Now;
            //在这里进行两块板子之间各种数据以及界面的初始化
            for (int i = 0; i < Cnt; i++)
            {
                Res[i] = Result.None;
            }
            //初始化所有的灯泡
            this.Result0.LanternBackground = Color.Silver;
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
            if (Detect_Algorithm.tt == 1)
            {
                Detect_Algorithm.tt = 2;
            }
            else
            {
                Detect_Algorithm.tt = 1;
            }
        }

        /// <summary>
        /// 等待PLC指令
        /// </summary>
        /// <returns></returns>
        bool WaitForReady()
        {
            Thread.Sleep(1000);//需要延时，时间太短虚拟slave那边会出错
            var temp = ModBus.ReadCoils("500", "1");//M500
            if (temp == false) return true; // 接着循环
            ModBus.WriteSingleCoilAsync("500", "0");
            status = Status.DETECTING;
            return false; //跳出循环
        }
        /// <summary>
        /// 处理结果
        /// </summary>
        void ResCheck()
        {
            switch(curCnt)
            {
                case 0: // APP是否打开
                    if (Res[curCnt] == Result.False) //如果是第一次检测 且失败
                    {
                        curDataMes.BootUp = "NG";
                        curDataMes.ScreenMid = "NG";
                        curCnt = Cnt; // 这里的特殊处理，因为特殊所以特殊，第一个错了后面就都不用检测了
                    }
                    else
                    {
                        curDataMes.ScreenMid = "OK";
                        curDataMes.BootUp = "OK";
                    }
                    break;
                case 1: // 关闭背光
                    if(Res[curCnt] == Result.False) //
                    {
                        curDataMes.ScreenLeftUp = "NG";
                    }
                    else
                    {
                        curDataMes.ScreenLeftUp = "OK";
                    }
                    break;
                case 2: // 屏幕亮度 + 
                    if (Res[curCnt] == Result.False) // 如果屏幕亮度没增加 这里就要置为false
                    {
                        curDataMes.ScreenBrightness = "NG";
                    }
                    
                    break;
                case 3: // 按键亮度+
                    if (Res[curCnt] == Result.False) // 说明按键亮度没有增加
                    {
                        // 那么这里数据库里要怎么搞？
                        curDataMes.ScreenLeftDown = "NG";
                    }
                    else
                    {
                        curDataMes.ScreenLeftDown = "OK";
                    }
                    break;
                case 4:// HOME
                    break;
                case 5:// 上一曲
                    break;
                case 6://POWER
                    break;
                case 7:// 音量-
                    break;
                case 8://音量+
                    if (Res[8] == Result.False || Res[7] == Result.False)
                    {
                        curDataMes.KnobFunc = "OK";
                    }
                    else curDataMes.KnobFunc = "NG";
                    break;
                case 9://下一曲
                    break;
                case 10:// BACK
                    if (Res[4] == Result.False ||
                        Res[5] == Result.False ||
                        Res[6] == Result.False ||
                        Res[9] == Result.False ||
                        Res[10] == Result.False )
                    {
                        curDataMes.KeyFunc = "NG";
                    }
                    else
                    {
                        curDataMes.KeyFunc = "OK";
                    }
                    break;
                case 11:// 按键亮度-
                    if (Res[curCnt] == Result.False) // 说明按键亮度没有减少
                    {
                        // 那么这里数据库里要怎么搞？
                        curDataMes.ScreenRightDown = "NG";
                    }
                    else
                    {
                        curDataMes.ScreenRightDown = "OK";
                    }
                    break;
                case 12:// 屏幕亮度-
                    if(Res[12] == Result.False)
                    {
                        curDataMes.ScreenBrightness = "NG";
                    }
                    break;
                case 13: // 退出
                    if (Res[13] == Result.False)
                    {
                        curDataMes.ScreenRightUp = "NG";
                    }
                    else curDataMes.ScreenRightUp = "OK";
                    break;
                case 14: //暗电流
                    if (Res[14] == Result.False)
                    {
                        curDataMes.DarkCur = "NG";
                    }
                    else curDataMes.DarkCur = "OK";
                    break;
                case 15: // 触摸屏
                    break;
                case 16:// 工作电流
                    if(Res[16] == Result.False)
                    {
                        curDataMes.Current = "NG";
                    }
                    else curDataMes.Current = "OK";
                    break;
                case 17: //丝印形状
                    if (Res[17] == Result.False)
                    {
                        curDataMes.SilkShape = "NG";
                    }
                    else curDataMes.SilkShape = "OK";
                    break;
                case 18:// 位置
                    if (Res[18] == Result.False)
                    {
                        curDataMes.SilkLoc = "NG";
                    }
                    else curDataMes.SilkLoc = "OK";
                    break;
                case 19:// 亮度
                    if (Res[19] == Result.False)
                    {
                        curDataMes.SilkBrightness = "NG";
                    }
                    else curDataMes.SilkBrightness = "OK";
                    if (Res[19] == Result.False || Res[18] == Result.False || Res[17] == Result.False)
                    {
                        curDataMes.Exterior = "NG";
                    }
                    else curDataMes.Exterior = "OK";
                    break;
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
            // 进入时间和退出时间配置
            cntBack = Convert.ToInt32(this.cbx_BackCnt.Text);
            cntEnter = Convert.ToInt32(this.cbx_EnterCnt.Text);
            //配置上面那一栏的当日信息
            current_cnt();
            try
            {
                //获取Config路径
                //存放所有型号产品的,获取Config文件夹下所有文件名称
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
                // 获取配置文件中关于两个时间的配置
                XmlNode XmlCntBack = XmlTools.getXmlNode(XmlConfig, "/last_used/curBack");
                cntBack = Convert.ToInt32(XmlCntBack.InnerText);
                XmlNode XmlCntEnter = XmlTools.getXmlNode(XmlConfig, "/last_used/curEnter");
                cntEnter = Convert.ToInt32(XmlCntEnter.InnerText);

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
                    //Cnt = Convert.ToInt32(root.ChildNodes[0].InnerText); // 这里固定用20个吧
                    detectSet = new DetectSet[Cnt];
                    for (int i = 1; i < root.ChildNodes.Count; i++)
                    {
                        XmlElement temp = (XmlElement)root.ChildNodes[i];
                        if (temp.GetAttribute("type") == "PUSH")
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
                            if (loction[j] == ',')
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
            if (userName == "HHUC714" && passWord == "123456")
            {
                ;
            }
            else
            {
                if (userName != "10086") //进来了 ，并且输入了userName
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


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭扫码枪hook
            BarCode.Stop();

            // 1.关闭摄像头采集
            bnStopGrab_Click(null, null);
            // 2.关闭摄像头设备
            bnClose_Click(null, null);
        }
        /// <summary>
        /// 输入产品机型并加载到用户操作界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ModelLoad_Click(object sender, EventArgs e)
        {
            if (btn_ModelLoad.Text == "应用")
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
            btn_OpenCom1_Click(null, null);
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
            switch(curCnt)
            {
                case 0:
                    ModBus.WriteSingleCoilAsync("1", "1");//开始第一个 按下 桌面APP图标
                    break;
                case 1:
                    ModBus.WriteSingleCoilAsync("2", "1");//开始第二个 按下 “关闭背光”
                    break;
                case 2:
                    ModBus.WriteSingleCoilAsync("3", "1");//开始第三个 按下 “屏幕亮度+”
                    break;
                case 3:
                    ModBus.WriteSingleCoilAsync("4", "1");//开始第四个 按下 “按键亮度+”
                    break;
                case 4:
                    ModBus.WriteSingleCoilAsync("5", "1");//开始第五个 按下 “HOME+”
                    break;
                case 5:
                    ModBus.WriteSingleCoilAsync("6", "1");// 开始第六个 按下 丝印“上一曲”
                    break;
                case 6:
                    ModBus.WriteSingleCoilAsync("7", "1");// 开始第七个 按下 丝印“power”
                    break;
                case 7:
                    ModBus.WriteSingleCoilAsync("8", "1");// 开始第八个 旋转 旋钮“音量-”
                    break;
                case 8:
                    ModBus.WriteSingleCoilAsync("9", "1");// 开始第九个 旋转 旋钮“音量+”
                    break;
                case 9:
                    ModBus.WriteSingleCoilAsync("10", "1");// 开始第十个 按下 按键“下一曲”
                    break;
                case 10:
                    ModBus.WriteSingleCoilAsync("11", "1");// 开始第十一个 按下 丝印 “BACK”
                    break;
                case 11:
                    ModBus.WriteSingleCoilAsync("12", "1");// 开始第十二个 按下 “按键亮度-”
                    break;
                case 12:
                    ModBus.WriteSingleCoilAsync("13", "1");// 开始第十三个 按下 “屏幕亮度-”
                    break;
                case 13:
                    ModBus.WriteSingleCoilAsync("14", "1");// 开始第十四个 按下 “退出”
                    break;
            }

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
        public bool WorkingCur = false;
        void Detecting()
        {
            switch (curCnt)
            {
                // P
                case 0:// 点击桌面APP
                    //if (Detect_Algorithm.brightPulsDetection(PicAdr + "1.bmp", PicAdr + "2.bmp", 5))
                    if(true)// 检测APP是否打开，执行APP是否打开函数 入口是一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 1: // 关闭背光
                    
                    if (algorithm.Alg1())// 执行关闭背光判断程序，（zj 入口是两个参数，一个是 “检测APP是否打开” 拍的图，一个是按下 “关闭背光” 后拍的图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 2://屏幕亮度 +
                    ModBus.ReadHoldingRegisters("180", "1");// 采集工作电流
                    if (ModBus.registerBuffer[0] > 100) WorkingCur = false;// 假设 100 是设置的最大额定电流
                    else WorkingCur = true;
                    if (algorithm.Alg1()) // 判断 屏幕亮度 是否增加（zj 入口参数有两个 一个是 case0 时拍摄的照片 “检测APP是否打开”，一个是这个步骤拍的照片
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 3:// 按键亮度 + 
                    
                    if (algorithm.Alg1()) // 判断 丝印按键亮度 是否增加 （hjp 入口参数是两个，一个是 “关闭背光” 拍的图，一个是 “按键亮度+” 拍的图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 4:// 丝印 HOME
                    if (algorithm.Alg1())// 判断 丝印 HOME 是否有效 (zj 入口参数就一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 5:// 丝印 上一曲
                    if (algorithm.Alg1())// 判断 丝印 上一曲 是否有效(zj 入口参数一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 6:// 丝印 Power
                    if (algorithm.Alg1())// 判断 丝印 power 是否有效(zj 入口参数一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 7:// 旋钮 音量-
                    if (algorithm.Alg1())// 判断 旋钮 音量- 是否有效 （zj 入口参数一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 8:// 旋钮 音量+
                    if (algorithm.Alg1())// 判断 旋钮 音量+ 是否有效(zj 入口参数一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 9://丝印 下一曲
                    if (algorithm.Alg1())// 判断 丝印 下一曲 是否有效 （zj 入口一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 10:// 丝印 back 
                    if (algorithm.Alg1())// 判断 丝印 back 是否有效（zj 入口一张图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                // P
                case 11:// 按键亮度 -

                    if (algorithm.Alg1())// 判断 丝印按键亮度 是否减少 （hjp 入口参数是两个，一个是 “按键亮度+” 拍的图，一个是当前“按键亮度-” 拍的图
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 12:// 屏幕亮度 -
                    if (algorithm.Alg1())// 判断 屏幕亮度 是否减少 （zj 入口两张图  “屏幕亮度+” 时拍的图，以及当前拍的 
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                // P
                case 13:// 退出
                    if(isLastNotBack == true)// 特殊情况，第一次就按下退出
                    {
                        if (algorithm.Alg1()) // 判断 是否退出 （zj 应该只需要一张图
                        {
                            Res[curCnt] = Result.True;
                            isLastNotBack = false;
                        }
                        else
                        {
                            Res[curCnt] = Result.False;
                        }
                    }
                    else
                    {
                        if (algorithm.Alg1()) // 判断 是否退出 （zj 应该只需要一张图
                        {
                            Res[curCnt] = Result.True;
                        }
                        else Res[curCnt] = Result.False;
                    }
                    break;
                case 14:// 检测暗电流
                    ModBus.ReadHoldingRegisters("180", "1");// 假设电流写在22里
                    if (ModBus.registerBuffer[0] > 100) // 这里不需要跑算法，只需要 读一下线圈 然后比对一下即可,假设100是从配置界面读到的参数
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 15:// 触摸屏 也不需要跑算法
                    if (Res[1] == Result.True && // 左上
                        Res[2] == Result.True && // 左下
                        Res[0] == Result.True &&
                        Res[3] == Result.True &&
                        Res[13] == Result.True)// 中间
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 16:// 工作电流
                    if (WorkingCur)
                    {
                        Res[curCnt] = Result.True;
                    }
                    else Res[curCnt] = Result.False;
                    break;
                case 17: // 丝印1
                    
                    if (algorithm.Alg1()) Res[17] = Result.True;
                    else Res[17] = Result.False;
                    
                    break;
                case 18:// 丝印2
                    
                    if (algorithm.Alg1()) Res[18] = Result.True;
                    else Res[18] = Result.False;
                    
                    break;
                case 19:// 丝印3
                    
                    if (algorithm.Alg1()) Res[19] = Result.True;
                    else Res[19] = Result.False;
                    
                    break;
            }
        }

        #endregion

        //--------------------串口处理事件---------------------//
        #region COM1
        private void btn_OpenCom1_Click(object sender, EventArgs e)
        {
            if (btn_OpenCom1.Text == "打开串口")
            {
                com.InitCom(com1_Name.Text, com1_Baud.Text, com1_Data.Text, com1_Hand.Text, com1_Stop.Text, ref com.com1);
                //com.com1.DataReceived += new SerialDataReceivedEventHandler(Com1Receive);
                com.com1.Open();
                com1_Name.Enabled = false;
                com1_Baud.Enabled = false;
                com1_Data.Enabled = false;
                com1_Stop.Enabled = false;
                com1_Hand.Enabled = false;
                btn_OpenCom1.Text = "关闭串口";
                btn_RefreshCom1.Enabled = false;
            }
            else if (btn_OpenCom1.Text == "关闭串口")
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
            for (int i = 0; i < ArryPort.Length; i++)
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
            if (indata == "11")//这里需要更改，和机械臂那边进行一个沟通
            {
                if (status == Status.WAIT_FOR_DOWN)
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
        #region 数据库
        private void ConnectToDataBase_Click(object sender, EventArgs e)
        {
            DataBase.connectToDataBase(ref this.dataGridView, ref this.ConnectToDataBase, ref this.btn_AllDatas, ref this.btn_DBRangeQuery);
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
        #endregion
        //--------------------UI点击事件-----------------------//
        #region UI点击事件
        private void Error0_Click(object sender, EventArgs e)
        {
            if (Res[0] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[0] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "0.jpg");
            }
        }

        private void Error1_Click(object sender, EventArgs e)
        {
            if (Res[1] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[1] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "1.jpg");
            }
        }

        private void Error2_Click(object sender, EventArgs e)
        {
            if (Res[2] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[2] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "2.jpg");
            }
        }

        private void Error3_Click(object sender, EventArgs e)
        {
            if (Res[3] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[3] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "3.jpg");
            }
        }

        private void Error4_Click(object sender, EventArgs e)
        {
            if (Res[4] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[4] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "4.jpg");
            }
        }

        private void Error5_Click(object sender, EventArgs e)
        {
            if (Res[5] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[5] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "5.jpg");
            }
        }

        private void Error6_Click(object sender, EventArgs e)
        {
            if (Res[6] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[6] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "6.jpg");
            }
        }

        private void Error7_Click(object sender, EventArgs e)
        {
            if (Res[7] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[7] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "7.jpg");
            }
        }

        private void Error8_Click(object sender, EventArgs e)
        {
            if (Res[8] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[8] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "8.jpg");
            }
        }

        private void Error9_Click(object sender, EventArgs e)
        {
            if (Res[9] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[9] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "9.jpg");
            }
        }

        private void Error10_Click(object sender, EventArgs e)
        {
            if (Res[10] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[10] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "10.jpg");
            }
        }
        private void Error11_Click(object sender, EventArgs e)
        {
            if (Res[11] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[11] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "11.jpg");
            }
        }
        private void Error12_Click(object sender, EventArgs e)
        {
            if (Res[12] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[12] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "12.jpg");
            }
        }
        private void Error13_Click(object sender, EventArgs e)
        {
            if (Res[13] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[13] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "13.jpg");
            }
        }
        private void Error14_Click(object sender, EventArgs e)
        {
            if (Res[14] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[14] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "14.jpg");
            }
        }
        private void Error15_Click(object sender, EventArgs e)
        {
            if (Res[15] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[15] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "15.jpg");
            }
        }
        private void Error16_Click(object sender, EventArgs e)
        {
            if (Res[16] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[16] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "16.jpg");
            }
        }
        private void Error17_Click(object sender, EventArgs e)
        {
            if (Res[17] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[17] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "17.jpg");
            }
        }
        private void Error18_Click(object sender, EventArgs e)
        {
            if (Res[0] == Result.False)
            {
                lab_ErrorMsg.Text = "当前\"" + CheckMsg[0] + "\"有错误";
                picBox_Error.Load(ErrorAdr + "18.jpg");
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            this.M_x.Text = e.X.ToString();
            this.M_y.Text = e.Y.ToString();
        }

        private void btn_OpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();                //new一个方法
            string adr = Environment.CurrentDirectory;
            ofd.Filter = "JPG Files (*.jpg)|*.jpg|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|GIF Files (*.gif)|*.gif";
            ofd.InitialDirectory = adr + "\\pic";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                //得到打开的文件路径（包括文件名）
                Pic_Config.Image = Image.FromFile(ofd.FileName);
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

        //----------------------海康相机-----------------------//
        #region 海康相机
        private void Hikinit()
        {
            //注册相机异常掉线回调
            
            //这里开个线程搞这个
            CameraOpenThread = new Thread(OpenCamera);
            CameraOpenThread.Start();
            pCallBackFunc = new MyCamera.cbExceptiondelegate(cbExceptiondelegate);
        }
        // ch:回调函数 | en:Callback function
        private void cbExceptiondelegate(uint nMsgType, IntPtr pUser)
        {
            if (nMsgType == MyCamera.MV_EXCEPTION_DEV_DISCONNECT)
            {
                DeInitCamera();
                Console.WriteLine("断了断了");
                // ch:获取选择的设备信息 | en:Get Used Device Info
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                                  typeof(MyCamera.MV_CC_DEVICE_INFO));

                // ch:打开设备 | en:Open Device
                while (true)
                {
                    int nRet = m_MyCamera.MV_CC_CreateDevice_NET(ref device);
                    if (MyCamera.MV_OK != nRet)
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    nRet = m_MyCamera.MV_CC_OpenDevice_NET();
                    if (MyCamera.MV_OK != nRet)
                    {
                        Thread.Sleep(5);
                        m_MyCamera.MV_CC_DestroyDevice_NET();
                        continue;
                    }
                    else
                    {
                        nRet = InitCamera();
                        if (MyCamera.MV_OK != nRet)
                        {
                            Thread.Sleep(5);
                            m_MyCamera.MV_CC_DestroyDevice_NET();
                            continue;
                        }
                        break;
                    }
                }
            }
        }
        private int InitCamera()
        {
            int nRet = m_MyCamera.MV_CC_RegisterExceptionCallBack_NET(pCallBackFunc, IntPtr.Zero);
            GC.KeepAlive(pCallBackFunc);
            if (MyCamera.MV_OK != nRet)
            {
                return nRet;
            }

            // ch:控件操作 | en:Control operation
            SetCtrlWhenOpen();

            // ch:标志位置位true | en:Set position bit true
            m_bGrabbing = true;
            m_hReceiveThread = new Thread(ReceiveThreadProcess);
            m_hReceiveThread.Start();

            // ch:开始采集 | en:Start Grabbing
            nRet = m_MyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                m_bGrabbing = false;
                m_hReceiveThread.Join();
                return nRet;
            }

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStartGrab();

            return MyCamera.MV_OK;
        }


        private void DeInitCamera()
        {
            // ch:标志位设为false | en:Set flag bit false
            m_bGrabbing = false;
            m_hReceiveThread.Join();

            // ch:停止采集 | en:Stop Grabbing
            m_MyCamera.MV_CC_StopGrabbing_NET();

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStopGrab();

            // ch:关闭设备 | en:Close Device
            m_MyCamera.MV_CC_CloseDevice_NET();
            m_MyCamera.MV_CC_DestroyDevice_NET();

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenClose();
        }
        private bool cameraIsOpen = false;
        private void OpenCamera()
        {
            while (cameraIsOpen == false)
            {
                while (is_CameraList == false)
                {
                    DeviceListAcq();
                }
                // 做各种设置
                // 1.打开设备
                bt_Open_Click(null, null);
                // 2.打开触发模式
                bnTriggerMode.Checked = true;
                cbSoftTrigger.Checked = true;
                bnStartGrab_Click(null, null);
            }
            panelNetConnect.BackColor = Color.Green;
            lab_CameraConnected.Text = "通信正常";
            // 到这里说明已经开启了，可以把这个线程关了
            Is_Camera = true;
            CameraOpenThread.Abort();
        }
        /// <summary>
        /// 显示海康相机错误信息
        /// </summary>
        /// <param name="csMessage"></param>
        /// <param name="nErrorNum"></param>
        private void ShowErrorMsg(string csMessage, int nErrorNum)
        {
            string errorMsg;
            if (nErrorNum == 0)
            {
                errorMsg = csMessage;
            }
            else
            {
                errorMsg = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: errorMsg += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: errorMsg += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: errorMsg += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: errorMsg += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: errorMsg += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: errorMsg += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: errorMsg += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: errorMsg += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: errorMsg += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: errorMsg += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: errorMsg += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: errorMsg += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: errorMsg += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: errorMsg += " No permission "; break;
                case MyCamera.MV_E_BUSY: errorMsg += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: errorMsg += " Network error "; break;
            }

            MessageBox.Show(errorMsg, "PROMPT");
        }
        /// <summary>
        /// 获取相机枚举
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Enum_Click(object sender, EventArgs e)
        {
            DeviceListAcq();
        }

        private void bt_Open_Click(object sender, EventArgs e)
        {
            if (m_stDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
            {
                ShowErrorMsg("No device, please select", 0);
                return;
            }

            // ch:获取选择的设备信息 | en:Get selected device information
            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                              typeof(MyCamera.MV_CC_DEVICE_INFO));

            // ch:打开设备 | en:Open device
            if (null == m_MyCamera)
            {
                m_MyCamera = new MyCamera();
                if (null == m_MyCamera)
                {
                    return;
                }
            }

            int nRet = m_MyCamera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                return;
            }

            nRet = m_MyCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                m_MyCamera.MV_CC_DestroyDevice_NET();
                ShowErrorMsg("Device open fail!", nRet);
                return;
            }

            // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = m_MyCamera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = m_MyCamera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    if (nRet != MyCamera.MV_OK)
                    {
                        ShowErrorMsg("Set Packet Size failed!", nRet);
                    }
                }
                else
                {
                    ShowErrorMsg("Get Packet Size failed!", nPacketSize);
                }
            }

            // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
            m_MyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            bnGetParam_Click(null, null);// ch:获取参数 | en:Get parameters

            // ch:控件操作 | en:Control operation
            SetCtrlWhenOpen();
        }
        private void SetCtrlWhenOpen()
        {
            bt_Open.Enabled = false;

            bnClose.Enabled = true;
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            bnContinuesMode.Enabled = true;
            bnContinuesMode.Checked = true;
            bnTriggerMode.Enabled = true;
            cbSoftTrigger.Enabled = false;
            bnTriggerExec.Enabled = false;

            tbExposure.Enabled = true;
            tbGain.Enabled = true;
            tbFrameRate.Enabled = true;
            bnGetParam.Enabled = true;
            bnSetParam.Enabled = true;
        }
        private void bnGetParam_Click(object sender, EventArgs e)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            int nRet = m_MyCamera.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                tbExposure.Text = stParam.fCurValue.ToString("F1");
            }

            nRet = m_MyCamera.MV_CC_GetFloatValue_NET("Gain", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                tbGain.Text = stParam.fCurValue.ToString("F1");
            }

            nRet = m_MyCamera.MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                tbFrameRate.Text = stParam.fCurValue.ToString("F1");
            }
        }
        private void SetCtrlWhenClose()
        {
            bt_Open.Enabled = true;
            bnClose.Enabled = false;
            tbExposure.Enabled = false;
            tbGain.Enabled = false;
            tbFrameRate.Enabled = false;
            bnGetParam.Enabled = false;
            bnSetParam.Enabled = false;
        }
        /// <summary>
        /// 关闭设备按键操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bnClose_Click(object sender, EventArgs e)
        {
            // ch:取流标志位清零 | en:Reset flow flag bit
            

            // ch:关闭设备 | en:Close Device
            m_MyCamera.MV_CC_CloseDevice_NET();
            m_MyCamera.MV_CC_DestroyDevice_NET();

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenClose();
        }

        private void bnSetParam_Click(object sender, EventArgs e)
        {
            try
            {
                float.Parse(tbExposure.Text);
                float.Parse(tbGain.Text);
                float.Parse(tbFrameRate.Text);
            }
            catch
            {
                ShowErrorMsg("Please enter correct type!", 0);
                return;
            }

            m_MyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            int nRet = m_MyCamera.MV_CC_SetFloatValue_NET("ExposureTime", float.Parse(tbExposure.Text));
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Exposure Time Fail!", nRet);
            }

            m_MyCamera.MV_CC_SetEnumValue_NET("GainAuto", 0);
            nRet = m_MyCamera.MV_CC_SetFloatValue_NET("Gain", float.Parse(tbGain.Text));
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Gain Fail!", nRet);
            }

            nRet = m_MyCamera.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", float.Parse(tbFrameRate.Text));
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Frame Rate Fail!", nRet);
            }
        }
        bool Is_Camera = false;
        public void ReceiveThreadProcess()
        {
            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
            int nRet = MyCamera.MV_OK;

            while (m_bGrabbing)
            {
                MyCamera.MV_CC_DEVICE_INFO info = new MyCamera.MV_CC_DEVICE_INFO();
                var res = m_MyCamera.MV_CC_GetDeviceInfo_NET(ref info);
                if (res != MyCamera.MV_OK)
                {
                    Thread.Sleep(5);
                    panelNetConnect.BackColor = Color.Red;
                    lab_CameraConnected.Text = "通信异常";
                    Is_Camera = false;
                    continue;
                }
                panelNetConnect.BackColor = Color.Green;
                lab_CameraConnected.Text = "通信正常";
                Is_Camera = true;
                nRet = m_MyCamera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    lock (BufForDriverLock)
                    {
                        if (m_BufForDriver == IntPtr.Zero || stFrameInfo.stFrameInfo.nFrameLen > m_nBufSizeForDriver)
                        {
                            if (m_BufForDriver != IntPtr.Zero)
                            {
                                Marshal.Release(m_BufForDriver);
                                m_BufForDriver = IntPtr.Zero;
                            }

                            m_BufForDriver = Marshal.AllocHGlobal((Int32)stFrameInfo.stFrameInfo.nFrameLen);
                            if (m_BufForDriver == IntPtr.Zero)
                            {
                                return;
                            }
                            m_nBufSizeForDriver = stFrameInfo.stFrameInfo.nFrameLen;
                        }

                        m_stFrameInfo = stFrameInfo.stFrameInfo;
                        CopyMemory(m_BufForDriver, stFrameInfo.pBufAddr, stFrameInfo.stFrameInfo.nFrameLen);
                    }

                    if (RemoveCustomPixelFormats(stFrameInfo.stFrameInfo.enPixelType))
                    {
                        m_MyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                        continue;
                    }
                 
                    //stDisplayInfo.hWnd = Pic_Config.Handle;
                    stDisplayInfo.pData = stFrameInfo.pBufAddr;
                    stDisplayInfo.nDataLen = stFrameInfo.stFrameInfo.nFrameLen;
                    stDisplayInfo.nWidth = stFrameInfo.stFrameInfo.nWidth;
                    stDisplayInfo.nHeight = stFrameInfo.stFrameInfo.nHeight;
                    stDisplayInfo.enPixelType = stFrameInfo.stFrameInfo.enPixelType;
                    m_MyCamera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
                    m_MyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                    btn_SaveJpg_Click(null, null);
                }
                else
                {
                    if (bnTriggerMode.Checked)
                    {
                        Thread.Sleep(5);
                    }
                }
            }
        }
        // ch:去除自定义的像素格式 | en:Remove custom pixel formats
        private bool RemoveCustomPixelFormats(MyCamera.MvGvspPixelType enPixelFormat)
        {
            Int32 nResult = ((int)enPixelFormat) & (unchecked((Int32)0x80000000));
            if (0x80000000 == nResult)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void bnStartGrab_Click(object sender, EventArgs e)
        {
            // ch:标志位置位true | en:Set position bit true
            m_bGrabbing = true;
            // 在这个线程里开始读取数据
            m_hReceiveThread = new Thread(ReceiveThreadProcess);
            m_hReceiveThread.Start();

            m_stFrameInfo.nFrameLen = 0;//取流之前先清除帧长度
            m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            // ch:开始采集 | en:Start Grabbing
            int nRet = m_MyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                m_bGrabbing = false;
                m_hReceiveThread.Join();
                ShowErrorMsg("Start Grabbing Fail!", nRet);
                return;
            }
           
            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStartGrab();
            //到这里表示相机以及打开了，可以在主线程里把这个线程关了
            cameraIsOpen = true;
        }
        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;

            if (bnTriggerMode.Checked && cbSoftTrigger.Checked)
            {
                bnTriggerExec.Enabled = true;
            }

            //bnSaveBmp.Enabled = true;
            //bnSaveJpg.Enabled = true;
            //bnSaveTiff.Enabled = true;
            //bnSavePng.Enabled = true;
        }

        private void bnStopGrab_Click(object sender, EventArgs e)
        {
            // ch:标志位设为false | en:Set flag bit false
            m_bGrabbing = false;
            m_hReceiveThread.Join();

            // ch:停止采集 | en:Stop Grabbing
            int nRet = m_MyCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Stop Grabbing Fail!", nRet);
            }

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStopGrab();
        }
        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;

            bnTriggerExec.Enabled = false;


            //bnSaveBmp.Enabled = false;
            //bnSaveJpg.Enabled = false;
            //bnSaveTiff.Enabled = false;
            //bnSavePng.Enabled = false;
        }

        private void bnTriggerExec_Click(object sender, EventArgs e)
        {
            // ch:触发命令 | en:Trigger command
            int nRet = m_MyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Trigger Software Fail!", nRet);
            }
        }

        private void cbSoftTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSoftTrigger.Checked)
            {
                // ch:触发源设为软触发 | en:Set trigger source as Software
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                if (m_bGrabbing)
                {
                    bnTriggerExec.Enabled = true;
                }
            }
            else
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                bnTriggerExec.Enabled = false;
            }
        }
        /// <summary>
        /// 打开触发模式
        /// </summary>
        private void OpenTriggerMode()
        {
            m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

            // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
            //           1 - Line1;
            //           2 - Line2;
            //           3 - Line3;
            //           4 - Counter;
            //           7 - Software;
            if (cbSoftTrigger.Checked)
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                if (m_bGrabbing)
                {
                    bnTriggerExec.Enabled = true;
                }
            }
            else
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
            }
            cbSoftTrigger.Enabled = true;
        }
        private void bnTriggerMode_CheckedChanged(object sender, EventArgs e)
        {
            // ch:打开触发模式 | en:Open Trigger Mode
            if (bnTriggerMode.Checked)
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                //           1 - Line1;
                //           2 - Line2;
                //           3 - Line3;
                //           4 - Counter;
                //           7 - Software;
                if (cbSoftTrigger.Checked)
                {
                    m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                    if (m_bGrabbing)
                    {
                        bnTriggerExec.Enabled = true;
                    }
                }
                else
                {
                    m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                }
                cbSoftTrigger.Enabled = true;
            }
        }


        /// <summary>
        /// 按照当前步骤获取图片
        /// </summary>
        /// <param name="cnt">当前进行到哪一步了</param>
        private void Get_Picture(int cnt)
        {
            bnTriggerExec_Click(null, null);//触发一次
            while (!Is_saved)
            {
                ;
            }
            Is_saved = false;
        }
        private bool is_CameraList = false;
        private bool Is_saved = false;
        /// <summary>
        /// 保存海康摄像头的图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SaveJpg_Click(object sender, EventArgs e)
        {
            if (false == m_bGrabbing)
            {
                ShowErrorMsg("Not Start Grabbing", 0);
                return;
            }

            if (RemoveCustomPixelFormats(m_stFrameInfo.enPixelType))
            {
                ShowErrorMsg("Not Support!", 0);
                return;
            }

            MyCamera.MV_SAVE_IMG_TO_FILE_PARAM stSaveFileParam = new MyCamera.MV_SAVE_IMG_TO_FILE_PARAM();

            lock (BufForDriverLock)
            {
                if (m_stFrameInfo.nFrameLen == 0)
                {
                    ShowErrorMsg("Save Jpeg Fail!", 0);
                    return;
                }
                stSaveFileParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Jpeg;
                stSaveFileParam.enPixelType = m_stFrameInfo.enPixelType;
                stSaveFileParam.pData = m_BufForDriver;
                stSaveFileParam.nDataLen = m_stFrameInfo.nFrameLen;
                stSaveFileParam.nHeight = m_stFrameInfo.nHeight;
                stSaveFileParam.nWidth = m_stFrameInfo.nWidth;
                stSaveFileParam.nQuality = 80;
                stSaveFileParam.iMethodValue = 2;
                stSaveFileParam.pImagePath = "pic/"+ curCnt.ToString() + ".jpg";
                int nRet = m_MyCamera.MV_CC_SaveImageToFile_NET(ref stSaveFileParam);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Save Jpeg Fail!", nRet);
                    return;
                }
            }
            Is_saved = true;
        }

        

        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
            System.Environment.Exit(0);
        }

        /// <summary>
        /// 获取设备列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceListAcq()
        {
            // ch:创建设备列表 | en:Create Device List
            System.GC.Collect();
            cbDeviceList.Items.Clear();
            m_stDeviceList.nDeviceNum = 0;
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_stDeviceList);
            if (0 != nRet)
            {
                ShowErrorMsg("Enumerate devices fail!", 0);
                return;
            }
            if(0 == m_stDeviceList.nDeviceNum)
            {
                is_CameraList = false;
                
                return;
            }
            else
            {
                is_CameraList = true;
            }
            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                    if (gigeInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("GEV: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("U3V: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                    }
                }
            }

            // ch:选择第一项 | en:Select the first item
            if (m_stDeviceList.nDeviceNum != 0)
            {
                cbDeviceList.SelectedIndex = 0;
            }
        }

        private void btn_ModBus_Click(object sender, EventArgs e)
        {
            ModBus.WriteSingleCoilAsync("500", "1");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ModBus.ReadDisCrete("500", "1");
        }

        private void Time_Config_Click(object sender, EventArgs e)
        {
            XmlNode root = XmlTools.getXmlNode(XmlConfig, "/last_used/curBack");
            root.InnerText = cbx_BackCnt.Text;
            cntBack = Convert.ToInt32(root.InnerText);
            root = XmlTools.getXmlNode(XmlConfig, "/last_used/curEnter");
            root.InnerText = cbx_EnterCnt.Text;
            cntEnter = Convert.ToInt32(root.InnerText);
            XmlTools.writeXml(XmlConfig, XmlConfigAdr);
            //加载当前选中的文档
            
        }


        #endregion

        //--------------------Modbus---------------------------//
        public void ModbusInit()
        {
            ModBus.init(com.com1,rBox_Com1Receive);
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
    UnKnown = 3,
}

public class DataMes
{
    public string MachineNumber = null;// 整机编号 
    public string Tester = null; // 测试人 (undone)
    public DateTime DT = new DateTime(); // 日期 
    public string DarkCur = null; // 暗电流
    public string BootUp = null; // 开机
    public string Exterior = null; // 外观 由以下三个 丝印形状 丝印位置 丝印亮度决定
    public string SilkShape = null; // 丝印形状
    public string SilkLoc = null; // 丝印位置
    public string SilkBrightness = null; //丝印亮度
    public string ScreenBrightness = null; //屏幕亮度
    public string KeyFunc = null; // 按键功能
    public string KnobFunc = null; // 旋钮功能
    public string ScreenMid = null; // 触摸屏中
    public string ScreenLeftUp = null; // 触摸屏左上 关闭屏幕背光
    public string ScreenLeftDown = null; // 触摸屏左下 按键亮度+ 
    public string ScreenRightUp = null; // 触摸屏右上 退出
    public string ScreenRightDown = null; // 触摸屏右下 按键亮度-
    public string Current = null; // 工作电流
    public string TestRes = null; // 测试结果
}
    