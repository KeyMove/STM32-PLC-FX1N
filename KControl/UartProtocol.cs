using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Timers;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace KeyMove.Tools
{
    public class UartProtocol
    {
        const int deftimeout = 30;
        public enum PacketCmd
        {
            Alive = 0,
            SetOutputPort = 1,
            GetOutputPort = 2,
            GetInputPort = 3,
            RunStop = 4,
            PWMStop = 5,
            WriteData = 6,
            LoadCode = 7,
            ModeSwitch = 8,
            ButtonControl = 9,
            KeyControl = 10,
            SetupPWMOption = 11,
            LoadPluse = 12,
            LoadPluseList = 13,
            StopControl = 14,
            SetPosSour = 15,
            HPData = 16,
        }

        public enum PacketStats
        {
            RecvOK = 0,
            RecvError = 1,
            RecvTimeOut = 2,
        }

        const int RecvLen = UInt16.MaxValue + 1;

        byte[] RecvBuff = new byte[RecvLen];
        UInt16 ReadPos = 0;
        UInt16 WritePos = 0;
        int Count = 0;
        Action CheckRecvData;
        volatile bool Exit = false;
        int RecvTimeout;

        public UdpClient UDP;
        public System.IO.Ports.SerialPort COM;
        int RecvMode;
        int packetlen;
        int recvpos;
        byte bytecheck;
        byte[] recvbuff;
        byte[] sendbuff = new byte[1024];
        volatile int TimeOut;
        int TimeOutReload;
        int RecvType;
        int RecvFlag;

        int ReSendCount;

        int tcmd;

        PacketCmd sendcmd;
        int sendlen;

        List<byte[]> DataPipe = new List<byte[]>();

        System.Timers.Timer tick = new System.Timers.Timer();

        public delegate void UartFunction(PacketStats stats, byte[] buff);

        List<UartFunction> CallBackList = new List<UartFunction>(20);
        private int RecvDataTime;
        private IPEndPoint OutPoint;

        Action<byte[], int, int> WriteDataAction;
        public void RegisterCmdEvent(PacketCmd cmd, UartFunction fu)
        {
            int index = (int)cmd;
            CallBackList[index] = fu;
        }

        public void Init()
        {
            Count = ReadPos = WritePos = 0;
        }

        void InitUartRecv()
        {
            Exit = false;
            RecvDataMaxTime = 50;
            CheckRecvData = () =>
            {
                try
                {
                    while (!Exit)
                        if (this.Count != 0)
                        {
                            byte b = RecvBuff[ReadPos++];
                            Count--;
                            if(Count==0)
                            {
                                WritePos = 0;
                                ReadPos = 0;
                            }
                            switch (RecvMode)
                            {
                                case 0:
                                    if (b == 0xAA)
                                        RecvMode = 1;
                                    RecvTimeout = 100;
                                    break;
                                case 1:
                                    packetlen = b;
                                    RecvMode = 4;
                                    recvpos = 0;
                                    bytecheck = 0;
                                    break;
                                case 2:
                                    recvbuff[recvpos] = b;
                                    bytecheck += b;
                                    recvpos++;
                                    if (packetlen <= recvpos)
                                        RecvMode = 3;
                                    break;
                                case 3:
                                    RecvMode = 0;
                                    try
                                    {
                                        if (bytecheck == b)
                                            CheckData();
                                        else
                                            RecvError();
                                    }
                                    catch
                                    {

                                    }
                                    break;
                                case 4:
                                    packetlen <<= 8;
                                    packetlen |= b;
                                    if (packetlen > 1024)
                                        RecvMode = 0;
                                    RecvMode = 5;
                                    break;
                                case 5:
                                    recvbuff = new byte[packetlen];
                                    RecvType = b;
                                    RecvMode = 2;
                                    RecvFlag = 1;
                                    break;
                            }
                        }
                        else
                        {
                            Thread.Sleep(10);
                        }
                }
                catch (Exception e)
                {
                    MessageBox.Show("发生错误!\r\n" + e.ToString());
                    new Task(CheckRecvData).Start();
                }
            };
            new Task(CheckRecvData).Start();
            tick.Interval = 15;
            tick.Elapsed += Tick_Elapsed;
            tick.Start();
            //CheckRecvData.Start();
            for (int i = 0; i < 20; i++)
                CallBackList.Add(null);
        }

        public UartProtocol(SerialPort com)
        {
            COM = com;
            COM.DataReceived += (object sender, SerialDataReceivedEventArgs e) =>
            {
                int len = COM.BytesToRead;
                if (Count == 0)
                {
                    WritePos = 0;
                    ReadPos = 0;
                }
                if ((WritePos + len) < RecvLen)
                {
                    if ((Count + len) < RecvLen)
                    {
                        len = COM.Read(RecvBuff, WritePos, len);
                        Count += len;
                        len += WritePos;
                        WritePos = (UInt16)len;
                        //Count += len;
                    }
                }
                else if ((Count + len) < RecvLen)
                {
                    int offset = 65536 - WritePos;
                    int l = len - offset;
                    len = COM.Read(RecvBuff, WritePos, offset);
                    Count += len;
                    len += WritePos;
                    WritePos = (UInt16)len;


                    len = COM.Read(RecvBuff, WritePos, l);
                    Count += len;
                    len += WritePos;
                    WritePos = (UInt16)len;
                }
            };
            InitUartRecv();
        }

        public UartProtocol(IPEndPoint ip)
        {
            UDP = new UdpClient(0);
            RecvDataMaxTime = 100;
            OutPoint = ip;
            new Task(() =>
            {
                while (!Exit)
                {
                    byte[] buff = UDP.Receive(ref OutPoint);
                    int len = buff.Length;
                    Array.Copy(buff, 0, RecvBuff, WritePos, len);
                    Count += len;
                    len += WritePos;
                    WritePos = (UInt16)len;
                }
            }).Start();

        }

        public void SetNet(IPEndPoint ip)
        {
            OutPoint = ip;
            if (UDP == null)
            {
                UDP = new UdpClient(0);
                new Task(() =>
                {
                    while (!Exit)
                    {
                        IPEndPoint end = new IPEndPoint(IPAddress.Any, 0);
                        byte[] buff = UDP.Receive(ref end);
                        int len = buff.Length;
                        Array.Copy(buff, 0, RecvBuff, WritePos, len);
                        Count += len;
                        len += WritePos;
                        WritePos = (UInt16)len;
                    }
                }).Start();
            }
            WriteDataAction = (byte[] data, int offset, int lenght) => {
                UDP.Send(data, lenght, OutPoint);
            };
        }
    

        public void Close()
        {
            Exit = true;
            tick.Stop();
        }

        public bool isIdle
        {
            get { return RecvFlag == 0; }
            set { }
        }

        public void Disconnect()
        {
            Init();
            RecvDataTime = 0;
            COM.Close();
            WriteDataAction = null;
        }
        public bool isLink
        {
            get { return RecvDataTime != 0; }
            set { }
        }

        public int RecvDataMaxTime { get; set; }

        private void Tick_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (RecvDataTime != 0)
                RecvDataTime--;
            if (TimeOut != 0)
                if (--TimeOut == 0)
                {
                    if (ReSendCount != 0)
                    {
                        ReSendCount--;
                        ResendPack();
                        return;
                    }
                    if (RecvFlag != 0)
                    {
                        RecvFlag = 0;
                        if (CallBackList[tcmd] != null && WriteDataAction!=null)
                        {
                            CallBackList[tcmd](PacketStats.RecvTimeOut, recvbuff);
                        }
                    }
                    if (DataPipe.Count != 0 && WriteDataAction!=null)
                    {
                        if(WriteDataAction!=null)WriteDataAction(DataPipe[0], 0, DataPipe[0].Length);
                        DataPipe.RemoveAt(0);
                    }
                }
            if (RecvTimeout != 0)
            {
                if (--RecvTimeout == 0)
                {
                    RecvMode = 0;
                }
            }
        }

        public void ResendPack()
        {
            SendDataPacket(sendcmd, 0, sendbuff, 0, sendlen);
        }

        public void SetCOMPort(string comname)
        {
            try
            {
                COM.Close();
                COM.PortName = comname;
                COM.Open();
                WriteDataAction = COM.Write;
            }
            catch {
                WriteDataAction = null;
            }
        }

        public byte[] BuildDataPack(PacketCmd cmd, byte[] buff, int offset, int len)
        {
            ByteStream b = new ByteStream(10 + buff.Length);
            byte chm = 0;
            int j = 0;
            for (int i = 0; i < len; i++)
            {
                sendbuff[j++] = buff[i + offset];
                chm += buff[i];
            }

            byte[] head = new byte[] { 0xAA, (byte)((len >> 8) & 0xff), (byte)(len & 0xff), (byte)cmd };
            byte[] end = new byte[] { chm, 0x55 };

            b.WriteBuff(head);
            b.WriteBuff(sendbuff, 0, len);
            b.WriteBuff(end);
            return b.toBytes();
        }

        public void SendCmdPacket(PacketCmd cmd)
        {
            SendCmdPacket(cmd, 0, deftimeout, 0);
        }

        public void SendCmdPacket(PacketCmd cmd, byte data)
        {
            SendCmdPacket(cmd, data, deftimeout, 0);
        }

        public void SendCmdPacket(PacketCmd cmd, int timeout)
        {
            SendCmdPacket(cmd, 0, timeout, 0);
        }

        public void SendCmdPacket(PacketCmd cmd, int timeout, int resend)
        {
            SendCmdPacket(cmd, 0, timeout, resend);
        }

        public void SendCmdPacket(PacketCmd cmd, int data, int timeout, int resend)
        {
            if (WriteDataAction!=null)
            {
                byte[] buff = new byte[] { 0xAA, 0x00, 0x01, (byte)cmd, (byte)data, (byte)data, 0x55 };
                if(WriteDataAction!=null)WriteDataAction(buff, 0, buff.Length);
                tcmd = (int)cmd;
                ReSendCount = resend;
                TimeOutReload = TimeOut = timeout;
                RecvFlag = 1;
                if (resend != 0)
                {
                    sendcmd = cmd;
                    sendlen = 1;
                    sendbuff[0] = 0;
                }
            }
        }

        public void SendDataPacket(PacketCmd cmd, int resend, byte[] buff)
        {
            SendDataPacket(cmd, resend, buff, 0, buff.Length);
        }
        public void SendDataPacket(PacketCmd cmd, int resend, byte[] buff, int offset, int len)
        {
            if (WriteDataAction!=null)
            {
                byte[] packet = BuildDataPack(cmd, buff, offset, len);
                try
                {
                    if(WriteDataAction!=null)WriteDataAction(packet, 0, packet.Length);
                }
                catch
                {

                }
                TimeOutReload = TimeOut = deftimeout;
                ReSendCount = resend;
                RecvFlag = 1;
                tcmd = (int)cmd;
                if (resend != 0)
                {
                    sendcmd = cmd;
                    sendlen = packet.Length;
                }
            }
        }

        public void SendDataPacketInPipe(PacketCmd cmd, int resend, byte[] buff, int offset, int len)
        {
            if (WriteDataAction!=null)
            {
                byte[] packet = BuildDataPack(cmd, buff, offset, len);
                ByteStream b = new ByteStream(5);
                b.WriteByte(0);
                b.WriteDWord(resend);
            }
        }

        public void SendDataPacketInPipe(PacketCmd cmd, byte[] buff)
        {
            SendDataPacketInPipe(cmd, 0, buff, 0, buff.Length);
        }

        public void SendDataPacket(PacketCmd cmd, byte[] buff, int offset, int len)
        {
            SendDataPacket(cmd, 0, buff, offset, len);
        }

        public void SendDataPacket(PacketCmd cmd, byte[] buff)
        {
            SendDataPacket(cmd, buff, 0, buff.Length);
        }

        public int GetPacketSize()
        {
            return packetlen;
        }

        public void CheckData()
        {
            if (RecvFlag != 0)
            {
                ReSendCount = TimeOutReload = TimeOut = 0;
                RecvFlag = 0;
                RecvDataTime = RecvDataMaxTime;
                if ((CallBackList[RecvType] != null) && (WriteDataAction!=null))
                    CallBackList[RecvType](PacketStats.RecvOK, recvbuff);
                if (DataPipe.Count != 0)
                {
                    if(WriteDataAction!=null)WriteDataAction(DataPipe[0], 0, DataPipe[0].Length);
                    DataPipe.RemoveAt(0);
                }
            }
        }

        public void RecvError()
        {
            if (RecvFlag != 0)
            {
                ReSendCount = TimeOutReload = TimeOut = 0;
                RecvFlag = 0;
                if (CallBackList[RecvType] != null)
                    CallBackList[RecvType](PacketStats.RecvError, recvbuff);
                if (DataPipe.Count != 0)
                {
                    if(WriteDataAction!=null)WriteDataAction(DataPipe[0], 0, DataPipe[0].Length);
                    DataPipe.RemoveAt(0);
                }

            }
        }

        public void Timeout()
        {
            RecvFlag = 1;
            TimeOut = 1;
        }
    }

    public class TcpPort
    {
        TcpListener Listener;
        int port;
        public volatile bool isRun = false;
        List<UartProtocol.UartFunction> CallBackList = new List<UartProtocol.UartFunction>(20);
        public void Start(int Port)
        {
            if (Listener != null)
            {
                Listener.Server.Dispose();
            }
            Listener = new TcpListener(IPAddress.Any, this.port = Port);
            Listener.Start();
            isRun = true;

            new Task(() => {
                while (isRun)
                {
                    Socket sk = Listener.AcceptSocket();
                    Check(sk);
                }
            }).Start();
        }

        public void Check(Socket sk)
        {
            byte[] buff = new byte[1536];
            byte[] info = new byte[3];
            sk.Receive(info);
            sk.Receive(buff);
            if (CallBackList.Count < info[0]) return;
            if (CallBackList[info[0]] == null) return;
            CallBackList[info[0]](UartProtocol.PacketStats.RecvOK, buff);
        }



        public void Stop()
        {
            isRun = false;
            if (Listener != null)
                Listener.Stop();
        }

    }

    public class ByteStream
    {
        byte[] buff;
        public int Pos = 0;
        public int Size = 0;

        public void offset(int offset, int startpos = -1)
        {
            int pos = Pos;
            if (startpos != -1)
                pos = offset + startpos;
            else
                pos += offset;
            if (pos < 0) return;
            if (pos >= Size) return;
            Pos = pos;
        }

        public ByteStream(int len)
        {
            buff = new byte[len];
            Pos = 0;
            Size = len;
        }

        public ByteStream(byte[] b)
        {
            buff = b;
            Pos = 0;
            Size = b.Length;
        }

        public int ReadByte()
        {
            return buff[Pos++];
        }

        public int ReadWord()
        {
            int v = buff[Pos++];
            v <<= 8;
            v |= buff[Pos++];
            return v;
        }

        public int ReadDWorde()
        {
            int v = buff[Pos++];
            v <<= 8;
            v |= buff[Pos++];
            v <<= 8;
            v |= buff[Pos++];
            v <<= 8;
            v |= buff[Pos++];
            return v;
        }

        public byte[] ReadBuff(int len)
        {
            byte[] b = new byte[len];
            for (int i = 0; i < len; i++)
            {
                b[i] = buff[Pos++];
            }
            return b;
        }



        public void WriteByte(int data)
        {
            buff[Pos++] = (byte)data;
        }
        public void WriteWord(int data)
        {
            buff[Pos++] = (byte)(data >> 8);
            buff[Pos++] = (byte)data;
        }
        public void WriteDWord(int data)
        {
            buff[Pos++] = (byte)(data >> 24);
            buff[Pos++] = (byte)(data >> 16);
            buff[Pos++] = (byte)(data >> 8);
            buff[Pos++] = (byte)(data);
        }
        public void WriteBuff(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buff[Pos++] = data[i];
            }
        }

        public void WriteBuff(byte[] data, int offset, int len)
        {
            for (int i = 0; i < len; i++)
            {
                buff[Pos++] = data[i + offset];
            }
        }

        public void WriteString(string str)
        {
            int len = str.Length;
            char[] strbuff = str.ToCharArray();
            int dat;
            for (int i = 0; i < len; i++)
            {
                dat = strbuff[i];
                if (dat > 127)
                {
                    buff[Pos++] = (byte)(dat >> 8);
                }
                buff[Pos++] = (byte)dat;
            }
        }
        public byte[] toBytes()
        {
            if (Size != Pos)
            {
                byte[] temp = new byte[Pos];
                for (int i = 0; i < Pos; i++)
                {
                    temp[i] = buff[i];
                }
                return temp;
            }
            return buff;
        }
    }
}
