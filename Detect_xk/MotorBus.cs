using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Modbus.Device;
using System.Net.Sockets;
using System.Threading;
using System.IO.Ports;

namespace Detect_xk
{
    class ModBus
    {
        public static IModbusMaster master;
        //写线圈或写寄存器数组
        public static bool[] coilsBuffer;
        public static ushort[] registerBuffer;
        //功能码
        private static string functionCode;
        //参数(分别为站号,起始地址,长度)
        private static byte slaveAddress;
        private static ushort startAddress;
        private static ushort numberOfPoints;
        public static RichTextBox rb;

        public static void init(SerialPort port,RichTextBox _rb,string _slave = "1")
        {
            slaveAddress = byte.Parse(_slave);
            rb = _rb;
            master = ModbusSerialMaster.CreateRtu(port);
            master.Transport.ReadTimeout = 1000;
            
        }
        /// <summary>
        /// 读取单个线圈
        /// </summary>
        /// <param name="_slave"></param>
        /// <param name="_startAddr"></param>
        /// <param name="_length"></param>
        public static bool ReadCoils(string _startAddr, string _length)
        {
            SetReadParameters(_startAddr, _length);
            coilsBuffer = master.ReadCoils(slaveAddress, startAddress, numberOfPoints);

            for (int i = 0; i < coilsBuffer.Length; i++)
            {
                SetMsg(coilsBuffer[i] + " ");
            }
            SetMsg("\r\n");
            try
            {
                return coilsBuffer[0];
            }
            catch
            {
                MessageBox.Show("没读取到PLC数据");
            }
            return false;
        }
        /// <summary>
        /// 读取输入线圈/离散量线圈
        /// </summary>
        /// <param name="_startAddr"></param>
        /// <param name="_length"></param>
        public static void ReadDisCrete(string _startAddr, string _length)
        {
            SetReadParameters(_startAddr, _length);
            
            coilsBuffer = master.ReadCoils(slaveAddress, startAddress, numberOfPoints);
            for (int i = 0; i < coilsBuffer.Length; i++)
            {
                SetMsg(coilsBuffer[i] + " ");
            }
            SetMsg("\r\n");
        }

        /// <summary>
        /// 读取保持寄存器
        /// </summary>
        /// <param name="_startAddr"></param>
        /// <param name="_length"></param>
        public static void ReadHoldingRegisters(string _startAddr, string _length)
        {
            SetReadParameters(_startAddr, _length);
            registerBuffer = master.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints);
            for (int i = 0; i < registerBuffer.Length; i++)
            {
                SetMsg(registerBuffer[i] + " ");
            }
            SetMsg("\r\n");
        }
        /// <summary>
        /// 读取输入寄存器
        /// </summary>
        /// <param name="_startAddr"></param>
        /// <param name="_length"></param>
        public static void ReadInputRegisters(string _startAddr, string _length)
        {
            SetReadParameters(_startAddr, _length);
            registerBuffer = master.ReadInputRegisters(slaveAddress, startAddress, numberOfPoints);
            for (int i = 0; i < registerBuffer.Length; i++)
            {
                SetMsg(registerBuffer[i] + " ");
            }
            SetMsg("\r\n");
        }
        /// <summary>
        /// 写线圈
        /// </summary>
        /// <param name="_startAddr"></param>
        /// <param name="_data"></param>
        public static void WriteSingleCoilAsync(string _startAddr, string _data)
        {
            SetWriteParametesCoil(_startAddr,_data);
            master.WriteSingleCoilAsync(slaveAddress, startAddress, coilsBuffer[0]);
        }
        public static void WriteSingleRegisterAsync(string _startAddr, string _data)
        {
            SetWriteParametesRegister(_startAddr, _data);
            master.WriteSingleRegisterAsync(slaveAddress, startAddress, registerBuffer[0]);
        }
        public static void WriteMultipleCoilsAsync(string _startAddr, string _data)
        {
            SetWriteParametesCoil(_startAddr, _data);
            master.WriteMultipleCoilsAsync(slaveAddress, startAddress, coilsBuffer);
        }


        /// <summary>
        /// 初始化读参数
        /// </summary>
        private static void SetReadParameters(string _startAddr,string _length)
        {
            if ( _startAddr == "" || _length == "")
            {
                MessageBox.Show("请填写读参数!");
            }
            else
            {
                startAddress = ushort.Parse(_startAddr);
                numberOfPoints = ushort.Parse(_length);
            }
        }
        /// <summary>
        /// 初始化写线圈参数
        /// </summary>
        private static void SetWriteParametesCoil(string _startAddr, string _data)
        {
            if (_startAddr == "" ||  _data == "")
            {
                MessageBox.Show("请填写写参数!");
            }
            else
            {
                startAddress = ushort.Parse(_startAddr);
                //判断是否写线圈
                
                string[] strarr = _data.Split(' ');
                coilsBuffer = new bool[strarr.Length];
                //转化为bool数组
                for (int i = 0; i < strarr.Length; i++)
                {
                    if (strarr[i] == "0")
                    {
                        coilsBuffer[i] = false;
                    }
                    else
                    {
                        coilsBuffer[i] = true;
                    }
                }
            }
        }
        /// <summary>
        /// 初始化写寄存器参数
        /// </summary>
        /// <param name="_startAddr"></param>
        /// <param name="_data"></param>
        private static void SetWriteParametesRegister(string _startAddr, string _data)
        {
            //转化ushort数组
            string[] strarr = _data.Split(' ');
            registerBuffer = new ushort[strarr.Length];
            for (int i = 0; i < strarr.Length; i++)
            {
                registerBuffer[i] = ushort.Parse(strarr[i]);
            }
        }
            public static void SetMsg(string msg)
        {
            rb.Invoke(new Action(() => { rb.AppendText(msg); }));
        }

        
    }
}
