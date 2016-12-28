using KeyMove.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KControl
{
    class IOPort
    {

        static UartProtocol UP;
        public static int InputCount
        {
            get; set;
        }

        public static int OutputCount
        {
            get; set;
        }
        static byte[] data = { 0 };
        public static volatile byte[] InputBit;
        public static volatile int BitCount;
        static volatile bool isRecv;
        static volatile bool isSend;
        public static bool IsRecv
        {
            get { return isRecv; }
            private set
            {
                isRecv = value;
            }
        }

        public static bool IsSend
        {
            get { return isSend; }
            private set
            {
                isSend = value;
            }
        }

        public static bool isLink
        {
            get { return UP.isLink; }
        }

        public static void Init(UartProtocol up,Action<UartProtocol.PacketStats, byte[]> act)
        {
            UP = up;
            UP.RegisterCmdEvent(UartProtocol.PacketCmd.GetInputPort, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        if(act!=null)
                            act(stats, buff);
                        IsRecv = true;
                        ByteStream b = new ByteStream(buff);
                        BitCount = b.ReadByte();
                        InputBit=b.ReadBuff((BitCount + 7) / 8);
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });
            UP.RegisterCmdEvent(UartProtocol.PacketCmd.SetOutputPort, (UartProtocol.PacketStats stats, byte[] buff) =>
            {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        IsSend = true;
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });
        }

        public static void GetInputPort()
        {
            if (UP.isLink)
            {
                IsRecv = false;
                UP.SendDataPacket(UartProtocol.PacketCmd.GetInputPort, 1, data);
            }
        }

        public static void SetIO(byte[] data)
        {
            if (UP.isLink)
            {
                isSend = false;
                ByteStream b = new ByteStream(32);
                b.WriteByte(OutputCount);
                b.WriteBuff(data);
                UP.SendDataPacket(UartProtocol.PacketCmd.SetOutputPort,1, b.toBytes());
            }
        }

    }
}
