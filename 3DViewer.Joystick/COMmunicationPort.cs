using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace _3DViewer.Joystick
{
    public class COMmunicationPort
    {
        private string portName = "COM15";
        public SerialPort Port { get; private set; }

        public COMmunicationPort()
        {
            Port = new SerialPort(portName);
            Port.BaudRate = 115200;
            Port.Parity = Parity.None;
            Port.StopBits = StopBits.One;
            Port.DataBits = 8;
            Port.Handshake = Handshake.None;
            Port.RtsEnable = true;
        }
    }
}
