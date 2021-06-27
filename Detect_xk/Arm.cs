using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detect_xk
{
    public static class Arm
    {
        public static void PushLoc(SerialPort com, int x, int y, Det_Type type)
        {
            string Msg = x.ToString() + "," + y.ToString() + "\r\n";
            com.Write(Msg);
        }
    }
}
