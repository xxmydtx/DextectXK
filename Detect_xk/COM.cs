using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms;

namespace Detect_xk
{
    class COM
    {
        public SerialPort com1 = null;
        public SerialPort com2 = null;
        public SerialPort com3 = null;
        public SerialPort com4 = null;
        public void Init()
        {
            SerialPort mySerialPort = new SerialPort("COM1");

            mySerialPort.BaudRate = 9600;
            mySerialPort.Parity = Parity.None;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.DataBits = 8;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;

            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            mySerialPort.Open();

            Console.WriteLine("Press any key to continue...");
            Console.WriteLine();
            while (true)
            {
                ;
            }
            mySerialPort.Close();
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received:");
            Console.Write(indata);
        }

        public void InitCom(string name,string baud,string data,string hand,string stop, ref SerialPort com)
        {
            
            try
            {
                com = new SerialPort(name);
            }
            catch (Exception ex)
            {
                MessageBox.Show("串口名字为空" + ex);
            }
            com.BaudRate = Convert.ToInt32(baud);
            switch (hand)
            {
                case "None": com.Parity = Parity.None;
                    break;
                case "Even": com.Parity = Parity.Even;
                    break;
                case "Odd":  com.Parity = Parity.Odd;
                    break;
            }
            switch(stop)
            {
                case "1":
                    com.StopBits = StopBits.One;
                    break;
                case "2":
                    com.StopBits = StopBits.Two;
                    break;
            }
            com.DataBits = Convert.ToInt32(data);
            com.RtsEnable = true;
            //com.Open();
        }
    }
}
