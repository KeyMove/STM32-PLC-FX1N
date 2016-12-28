using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeyMove.Tools;

namespace KControl
{
    class Motor
    {

        public static double[] scaleAngle = new double[] { 360F / 100000, 360F / 100000, 360F / 100000, 360F / 100000, 360F / 95000, 360F / 100000 };
        public static double[] offsetAngle = new double[] { 0, 0, 0, 0, 0, 0 };
        public static int[] plsoffset = new int[] { 0,0,0,0,0,0};
        public static int[] offsetdir = new int[] { 0,1,1,0,0,0 };
        public static bool Enable { get; set; }

        static UartProtocol UP;
        static int[][] pluslist;
        static int Count;
        static int Index;
        static int BuffLen;
        static int BuffPos;
        public static int StartSpeed { get; set; }
        public static int SpeedTime { get; set; }
        public static int Frequency { get; set; }

        static int _Pluse;
        public static int PlusToDeg { private get { return _Pluse; }
            set
            {
                _Pluse = value;
                //缩放值
                scaleAngle = new double[] { 360F / value, 360F / (value), 360F / (value), 360F / value, 360F / (value*0.95), 360F / value };
            }
        }

        public static bool IsReady
        {
            get { return isReady==0; }
            private set { isReady += value ? (isReady > 0 ? -1 : 0) : 1; }
        }

        public static volatile int isReady=0;

        public static void Init(UartProtocol up)
        {
            SpeedTime = 200;
            StartSpeed = 500;
            //PlusToDeg = 100000;
            Frequency = 10000;

            UP = up;
            Enable = false;
            up.RegisterCmdEvent(UartProtocol.PacketCmd.LoadPluseList, (UartProtocol.PacketStats stats,byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        ByteStream s = new ByteStream(buff);
                        switch (s.ReadByte())
                        {
                            case 0:
                                BuffLen = s.ReadWord();
                                BuffPos = 0;
                                goto case 1;                               
                            case 1:
                                if (Count <= 0)
                                {
                                    UP.SendCmdPacket(UartProtocol.PacketCmd.LoadPluseList,(byte)2);
                                    return;
                                }
                                s = new ByteStream(53);
                                s.WriteByte(1);
                                if (Count >= 2 && BuffLen >= 48)
                                {
                                    s.WriteWord(BuffPos);
                                    s.WriteByte(48);
                                    for (int i = 0; i < 2; i++)
                                    {
                                        int[] ag = pluslist[Index + i];
                                        for (int j = 0; j < 6; j++)
                                            s.WriteDWord(ag[j]);
                                    }
                                    Index += 2;
                                    Count -= 2;
                                    BuffPos += 48;
                                    BuffLen -= 48;
                                    UP.SendDataPacket(UartProtocol.PacketCmd.LoadPluseList, 1, s.toBytes());
                                }
                                else if (BuffLen >= 24)
                                {
                                    s.WriteWord(BuffPos);
                                    s.WriteByte(24);
                                    int[] ag = pluslist[Index];
                                    for (int j = 0; j < 6; j++)
                                        s.WriteDWord(ag[j]);
                                    Index ++;
                                    Count --;
                                    BuffPos += 24;
                                    BuffLen -= 24;
                                    UP.SendDataPacket(UartProtocol.PacketCmd.LoadPluseList, 1, s.toBytes());
                                }
                                else
                                    return;
                                break;
                            case 2:

                                break;
                            case 3:
                                
                                break;
                            case 4:
                                IsReady = true;
                                break;
                        }
                       
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:

                        break;
                }
            });

            UartProtocol.UartFunction act = (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        if (buff[0] == 0xaa)
                        {
                            //System.Threading.Volatile.Write(ref isReady, true);
                            IsReady = true;
                        }
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            };

            up.RegisterCmdEvent(UartProtocol.PacketCmd.LoadPluse, act);

            up.RegisterCmdEvent(UartProtocol.PacketCmd.SetupPWMOption, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        IsReady = true;
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });
        }

        public static int[] AngleToPluse(double[] angle)
        {
            int[] p = new int[6];
            for (int i = 0; i < 6; i++)
            {
                p[i] = (int)((angle[i] + offsetAngle[i]) / scaleAngle[i]) + plsoffset[i];
            }
            return p;
        }

        public static void MotorRun(double[] angle)
        {
            if (!Enable) return;
            ByteStream b = new ByteStream(30);
            int[] p = new int[6];
            for (int i = 0; i < 6; i++)
            {
                int plus= (int)((angle[i] + offsetAngle[i]) / scaleAngle[i])+ plsoffset[i];
                b.WriteDWord(plus);
            }
            byte[] data = b.toBytes();
            UP.SendDataPacket(UartProtocol.PacketCmd.LoadPluse, 1, data, 0, data.Length);
            IsReady = false;
        }

        public static void MotorRunList(double[][] list)
        {
            if (!Enable) return;
            pluslist = new int[list.Length][];
            for (int i = 0; i < list.Length; i++)
            {
                pluslist[i] = AngleToPluse(list[i]);
            }
            Index = 0;
            Count = list.Length;
            //b.WriteWord(Count);
            //int[] vp = AngleToPluse(list[0]);
            //for (int i = 0; i < 6; i++)
            //    b.WriteDWord(vp[i]);
            ByteStream b = new ByteStream(3);
            b.WriteByte(0);
            b.WriteWord(Count);
            UP.SendDataPacket(UartProtocol.PacketCmd.LoadPluseList,b.toBytes());
            IsReady = false;
        }

        public static void MotorStop()
        {
            if (!Enable) return;
            UP.SendCmdPacket(UartProtocol.PacketCmd.RunStop);
            isReady = 0;
        }

        public static void MotorFastStop()
        {
            if (!Enable) return;
            UP.SendCmdPacket(UartProtocol.PacketCmd.PWMStop);
        }

        public static void SetInfo( int sp, int MaxSpeed, int time)
        {
            if (!Enable) return;
            StartSpeed = sp;
            Frequency = MaxSpeed;
            SpeedTime = time;
            ByteStream b = new ByteStream(10);
            b.WriteWord(StartSpeed);
            b.WriteDWord(Frequency);
            b.WriteWord(SpeedTime);
            UP.SendDataPacket(UartProtocol.PacketCmd.SetupPWMOption, b.toBytes());
            IsReady = false;
        }
        public static void SetInfo()
        {
            if (!Enable) return;
            ByteStream b = new ByteStream(10);
            b.WriteWord(StartSpeed);
            b.WriteDWord(Frequency);
            b.WriteWord(SpeedTime);
            UP.SendDataPacket(UartProtocol.PacketCmd.SetupPWMOption, b.toBytes());
            IsReady = false;
        }
    }
}
