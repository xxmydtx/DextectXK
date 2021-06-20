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

namespace Detect_xk
{
    public partial class MainForm : Form
    {
        
        Thread threadWatch = null; // 负责监听客户端连接请求的 线程；
        Thread SocMain = null;
        Thread MainThread = null;
        Socket socketWatch = null;
        Socket sokConnection = null;

        //配置
        string CurConfigAdr = null;
        XmlDocument XmlConfig = new XmlDocument();
        string XmlConfigAdr = System.Environment.CurrentDirectory +  "\\config_load.xml" ; // xml配置文件地址
        int Cnt = 0;
        int curCnt = 0;

        bool Is_Connected = false;

        public string userName;
        public string passWord;

        public static Status status = Status.WAIT_FOR_USER;
        public MainForm()
        {
            
            InitializeComponent();
            // 获取所有配置文件
            ConfigInit();
            // 加载Config文件
            
            // 初始化网络连接
            TcpInit();
            // 开启主线程
            MainThread = new Thread(MainTd);
            
        }
        void MainTd()
        {
            while (true)
            {
                //完成一块板子的检测
                if(curCnt == Cnt)
                {
                    status = Status.WAIT_FOR_USER;
                    btn_start.Enabled = true;
                }
                switch (status)
                {
                    case Status.WAIT_FOR_USER:
                        break;
                    case Status.SENDGING:
                        SendMsg();
                        break;
                    case Status.WAIT_FOR_DOWN:
                        break;
                    case Status.DETECTING:
                        break;
                }
                Cnt++;
            }
        }

        //----------------------流程----------------------------//
        #region 流程逻辑
        private void btn_start_Click(object sender, EventArgs e)
        {
            status = Status.SENDGING;
            btn_start.Enabled = false;
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
                DirectoryInfo Dir = new DirectoryInfo(System.Environment.CurrentDirectory + "\\Config");
                var list = Dir.GetFiles();
                for (int i = 0; i < list.Length; i++)
                {
                    ConfigAdr.Items.Add(list[i].Name);
                }
                XmlConfig = XmlTools.readXml(XmlConfigAdr);
                XmlNode root = XmlTools.getXmlNode(XmlConfig,"/last_used/last");
                string name = root.InnerText;
                for(int i = 0;i<list.Length;i++)
                {
                    if(list[i].Name == name)
                    {
                        ConfigAdr.Enabled = false;
                        CurConfigAdr = ConfigAdr.Text;
                        loader.Text = "取消";
                        ConfigAdr.Text = list[i].Name;
                        CurConfigAdr = Dir + "\\" + ConfigAdr.Text;
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
                // 将参数加载进来的处理
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
                XmlTools.writeXml(XmlConfig,XmlConfigAdr);
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
        private void btn_send_Click(object sender, EventArgs e)
        {
            string s = SendBox.Text;
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(s);
            sokConnection.Send(byteArray);
        }
        #endregion
        //----------------------算法DLL------------------------//
        #region 算法
        bool Detecting(int cnt)
        {
            
            return false;
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