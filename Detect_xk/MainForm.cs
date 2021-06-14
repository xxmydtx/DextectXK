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

namespace Detect_xk
{
    public partial class MainForm : Form
    {
        Thread threadWatch = null; // 负责监听客户端连接请求的 线程；
        Thread MainThread = null;
        Socket socketWatch = null;
        Socket sokConnection = null;
        public static Status status = Status.WAIT_FOR_USER;
        public MainForm()
        {
            InitializeComponent();
            TcpInit();
            //开启主线程
            MainThread = new Thread(MainTd);
            
        }
        void MainTd()
        {
            while (true)
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
        }
        private void SendMsg()
        {
            //在此处发送按在哪里的命令
        }
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

        private void btn_Lisening_Click(object sender, EventArgs e)
        {
            btn_Lisening.Enabled = false;
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
            catch(SocketException es)
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
        private void ShowMsg(string s)
        {
            if (MsgBox.InvokeRequired)
            {
                MsgBox.Invoke(new Action<string>((str) =>
                {
                    ShowMsg(str);
                }),s);

            }
            else
            {
                s += "\r\n";
                MsgBox.Text += s;
            }
            
        }

        void WatchConnecting()
        {
            ShowMsg("新客户端连接成功");
            while (true)  // 持续不断的监听客户端的连接请求；
            {
                // 开始监听客户端连接请求，Accept方法会阻断当前的线程；
                sokConnection = socketWatch.Accept(); // 一旦监听到一个客户端的请求，就返回一个与该客户端通信的 套接字；
                var ssss = sokConnection.RemoteEndPoint.ToString().Split(':');
                // 将与客户端连接的 套接字 对象添加到集合中；
                Thread thr = new Thread(RecMsg);
                thr.IsBackground = true;
                thr.Start(sokConnection);
            }
        }
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
                catch (Exception e)
                {
                    
                    // 从列表中移除被中断的连接IP
                    lbOnline.Items.Remove(sokClient.RemoteEndPoint.ToString());
                    ShowMsg("异常消息：" + e.Message + "\r\n");
                    // log.log("遇见异常" + e.Message);
                    break;
                }
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            string s = SendBox.Text;
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(s);
            sokConnection.Send(byteArray);
        }
    }
    
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