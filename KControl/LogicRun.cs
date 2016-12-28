using KeyMove.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KControl
{
    class LogicRun
    {

        enum Mode:int{
            G0=0,
            G1=1,
            Delay=2,
            SetSpeed=3,
            SetRange=4,
            OUT=5,
            IN=6,
        }

        public static bool sumlot=true;
        public static bool looprun = true;
        static double speed=10;
        static double rg=0.1;
        static volatile bool First;
        //static volatile bool TaskThread = false;

        static volatile bool isRun = false;
        public static bool IsRun {
            get { return isRun; }
            private set { }
        }
        static volatile int Index;
        static int DefDelay = 20;
        public static List<ModeObject> LogicList=new List<ModeObject>();
        public static Axis6.AxisStruct LastStruct;
        static Axis6.AxisStruct RunLastStruct;
        public static UartProtocol UP;


        public class ModeObject{
            public string message;
            public bool redraw;
            public int size;
            public virtual void run() { }
            public virtual void del() { }
            public virtual void line() { }
            public virtual void remove() { }
        }

        public class GList : ModeObject
        {
            //public Axis6.AxisStruct sour;
            //public Axis6.AxisStruct dest;
            public object linehandl;
            public object drawhandle;
            int count = 0;
            int bcount = 0;
            int docount = 0;
            List<double[]> aglist = new List<double[]>();
            //Axis6.AxisStruct[] list;
            double[][] poslist;
            double[][] destangle;
            double N, O, A;
            public GList()
            {
                message = "执行G代码";
            }

            public GList(double[] Xl,double[] Yl,double[] Zl,Axis6.AxisStruct last)
            {
                poslist = new double[3][];
                poslist[0] = Xl;
                poslist[1] = Yl;
                poslist[2] = Zl;
                count = Xl.Length;
                bcount = 1;
                docount= 0;
                N = last.N;
                O = last.O;
                A = last.A;
                destangle = new double[count][];
                destangle[0] = Axis6.PosToAngle(LastStruct.angle, last.X, last.Y, last.Z, N, O, A);
                if(destangle[0]!=null)
                    LastStruct.angle = destangle[0];
                linehandl=Axis6.DrawLineList(Xl, Yl, Zl);
            }

            public override void run()
            {

                new Task(() => {
                    while (isRun)
                    {
                        if (bcount < count)
                        {
                            double[] value;
                            destangle[bcount] = value = Axis6.PosToAngle(LastStruct.angle, poslist[0][bcount], poslist[1][bcount], poslist[2][bcount], N, O, A);
                            if (value == null)
                                destangle[bcount] = destangle[bcount - 1];
                            bcount++;
                        }
                        else
                            break;
                    }
                }).Start();
                while (isRun)
                {
                    if (Motor.IsReady || !Motor.Enable)
                    {
                        if (docount < bcount)
                        {
                            Motor.MotorRun(destangle[docount]);
                            Axis6.MoveToAngle(destangle[docount]);
                            docount++;
                        }
                    }
                    Thread.Sleep(1);
                }
                LastStruct.angle=destangle[count-1];
                //Axis6.AxisStruct last = new Axis6.AxisStruct();

            }

            public override string ToString()
            {
                return "G代码\r\n长度" + count;
            }

            public override void del()
            {
                if (linehandl != null)
                    Axis6.removeLine(linehandl);
            }

            public override void remove()
            {
                if (linehandl != null)
                    Axis6.removeLine(linehandl);
                if (drawhandle != null)
                    Axis6.removeLine(drawhandle);
                linehandl = drawhandle = null;
            }

        }

        public class G0 : ModeObject
        {
            public Axis6.AxisStruct sour;
            public Axis6.AxisStruct dest;
            public object linehandl;
            public object drawhandle;

            public string Name
            {
                get { return "快速定位到 " + dest.name; }
                private set {  }
            }

            public G0(Axis6.AxisStruct start,Axis6.AxisStruct stop)
            {
                message = "快速定位到点";
                sour = start;
                dest = stop;
                drawhandle = Axis6.DrawArcLine(sour.angle, dest.angle);
                LastStruct = dest;
                redraw = true;
            }

            public G0(Axis6.AxisStruct stop)
            {
                message = "快速定位到点";
                sour = LastStruct;
                dest = stop;
                drawhandle = Axis6.DrawArcLine(sour.angle, dest.angle);
                LastStruct = dest;
                redraw = true;
            }

            public override void run()
            {
                if (!isRun) return;
                //while (isRun && !Motor.IsReady) Thread.Sleep(1);
                if (Motor.Enable)
                    Motor.MotorRun(dest.angle);
                if (sumlot)
                {
                    if (First)
                        linehandl=Axis6.MoveArc(sour.angle, dest.angle, 'b', 2, 15, true);
                    else
                        Axis6.MoveArc(LastStruct.angle, dest.angle, 'b', 2, 15, false);
                }
                if(Motor.Enable)
                    while (isRun && !Motor.IsReady) Thread.Sleep(1);
                LastStruct = dest;
                //while (isRun && !Motor.IsReady)
                //{
                //    Thread.Sleep(1);
                //}
                //Thread.Sleep(DefDelay);
            }

            public override void remove()
            {
                if (linehandl != null)
                    Axis6.removeLine(linehandl);
                if (drawhandle != null)
                    Axis6.removeLine(drawhandle);
                linehandl = drawhandle = null;
            }

            public override void del()
            {
                if (linehandl != null)
                    Axis6.removeLine(linehandl);
            }

            public override void line()
            {
                if (drawhandle != null)
                    Axis6.removeLine(drawhandle);
                drawhandle = Axis6.DrawArcLine(sour.angle, dest.angle);
            }

            public override string ToString()
            {
                
                return "弧线运动到点 :\r\n起点:" + sour + "\r\n终点:" + dest + "\r\n运动速度:" + speed.ToString("f4") + "KHz  " + message;
            }
        }

        public class G1 : ModeObject
        {
            public Axis6.AxisStruct sour;
            public Axis6.AxisStruct dest;
            double[][] AngleList;
            object lineObj;
            public object linehandl;
            public object drawhandle;
            int count;
            volatile int bcount;
            int docount;
            double[] startpos;
            double[] stoppos;
            int inoutcount = 0;
            double r;

            public double[][] getData()
            {
                if (AngleList == null)
                    sumdata(true);
                return AngleList;
            }

            public G1(Axis6.AxisStruct start, Axis6.AxisStruct stop)
            {
                message = "直线插补到点";
                sour = start;
                dest = stop;
                drawhandle = Axis6.DrawLine(sour, dest);
                LastStruct = dest;
                count = docount = 0;
                bcount = -1;
                redraw = true;
                this.r = rg;
            }

            public G1(Axis6.AxisStruct stop)
            {
                message = "直线插补到点";
                sour = LastStruct;
                dest = stop;
                drawhandle=Axis6.DrawLine(sour, dest);
                LastStruct = dest;
                count = docount = 0;
                bcount = -1;
                redraw = true;
                this.r = rg;
            }

            void getdata()
            {
                Axis6.AxisStruct G = dest;
                Axis6.AxisStruct LastG = sour;
                double[] v = new double[6];
                double[] d = new double[6];
                double[] lastag = G.angle;
                List<double[]> anglelist = new List<double[]>();
                v[0] = LastG.X;
                v[1] = LastG.Y;
                v[2] = LastG.Z;
                v[3] = v[4] = v[5] = 0;
                d[0] = G.X;
                d[1] = G.Y;
                d[2] = G.Z;
                d[3] = d[4] = d[5] = 0;
                Line.Setup(v, d, rg);
                if (Line.Count == 1)
                {
                    v[0] = v[1] = v[2] = 0;
                    d[0] = d[1] = d[2] = 0;
                    v[3] = LastG.N;
                    v[4] = LastG.O;
                    v[5] = LastG.A;
                    d[3] = G.N;
                    d[4] = G.O;
                    d[5] = G.A;
                    Line.Setup(v, d, 1.0);
                    v[0] = LastG.X;
                    v[1] = LastG.Y;
                    v[2] = LastG.Z;
                    d[0] = G.X;
                    d[1] = G.Y;
                    d[2] = G.Z;
                }
                v[3] = LastG.N;
                v[4] = LastG.O;
                v[5] = LastG.A;
                d[3] = G.N;
                d[4] = G.O;
                d[5] = G.A;
                count = Line.Count+1;
                startpos = v;
                stoppos = d;
            }

            void StartInOut()
            {
                int v = inoutcount * (inoutcount + 1) / 2;
                double[] addval = new double[6];
                double[] decval = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    addval[i] = AngleList[1][i] - sour.angle[i];
                    addval[i] /= v;
                }
                for (int i = 0; i < 6; i++)
                {
                    decval[i] = dest.angle[i] - AngleList[count-2][i];
                    decval[i] /= v;
                }
                AngleList[0] = AngleList[1];
                AngleList[inoutcount + count] = AngleList[count - 1] = AngleList[count - 2];
                for (int i = 0; i < count; i++)
                {
                    AngleList[(inoutcount + count - 1) - i] = AngleList[count - i - 1];
                }
                AngleList[0] = sour.angle;
                //AngleList[inoutcount] = AngleList[inoutcount + 1];
                for (int i = 1; i < inoutcount; i++)
                {
                    AngleList[i] = (double[])AngleList[i - 1].Clone();
                    for (int j = 0; j < 6; j++)
                    {
                        AngleList[i][j] += addval[j] * i;
                    }
                }
                int pos = count + inoutcount;
                for (int i = 0; i < inoutcount; i++)
                {
                    AngleList[i + pos] = (double[])AngleList[i + pos - 1].Clone();
                    for (int j = 0; j < 6; j++)
                    {
                        AngleList[i+pos][j] += decval[j] * (inoutcount - i);
                    }
                }
                AngleList[AngleList.Length - 1] = dest.angle;
            }

            void sumdata(bool ext)
            {
                docount = 0;
                if (count==0) getdata();
                if (bcount < count)
                {
                    getdata();
                    Line.Setup(startpos, stoppos, count - 1);
                    bcount = 0;
                    AngleList = new double[count + inoutcount * 2][];
                    Axis6.AxisStruct LastG = LastStruct;
                    double[] lastag = LastStruct.angle;

                    new Task(() =>
                    {
                        Parallel.For(0, count - 1, (int index) =>
                        {
                            if(!ext)
                                if (!isRun) return;
                            Axis6.AxisStruct G = new Axis6.AxisStruct();
                            G.X = Line.pls[0] + Line.inc[0] * index;
                            G.Y = Line.pls[1] + Line.inc[1] * index;
                            G.Z = Line.pls[2] + Line.inc[2] * index;
                            G.N = Line.pls[3] + Line.inc[3] * index;
                            G.O = Line.pls[4] + Line.inc[4] * index;
                            G.A = Line.pls[5] + Line.inc[5] * index;
                            AngleList[index] = Axis6.PosToAngle(LastG, G);
                        });
                        AngleList[count - 1] = dest.angle;
                        bcount = count;
                    }).Start();

                }
                if (ext)
                {
                    while (bcount < count)
                        Thread.Sleep(1);
                }
            }

            public override void run()
            {
                int f, s, d;
                if (rg != r)
                {
                    r = rg;
                    bcount = 0;
                }
                f = Motor.Frequency;
                s = Motor.StartSpeed;
                d = Motor.SpeedTime;
                Motor.Frequency = (Motor.Frequency > 5000) ? 5000 : Motor.Frequency;
                Motor.SetInfo(Motor.Frequency, Motor.Frequency, 1);
                sumdata(false);
                if (sumlot)
                {
                    if (First)
                        linehandl = Axis6.DrawLine(sour, dest, 'b', 2);
                }
                LastStruct = dest;
                if (bcount < count)
                {
                    while (isRun)
                    {
                        if (Motor.IsReady || !Motor.Enable)
                        {
                            if (docount < count)
                            {
                                if (AngleList[docount] == null) continue;
                                if (AngleList[docount] == LastStruct.angle)
                                {
                                    docount++;
                                    if (docount >= count)
                                    {
                                        if(inoutcount!=0)
                                            StartInOut();
                                    }
                                    continue;
                                }
                                Axis6.MoveToAngle(AngleList[docount]);
                                //Motor.MotorRun(AngleList[docount]);
                                docount++;
                                if (docount >= count)
                                {
                                    if (inoutcount != 0)
                                        StartInOut();
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        Thread.Sleep(1);
                    }
                }
                else
                {
                    Axis6.PointToPointMove(AngleList);
                }
                if (!isRun) return;
                Motor.MotorRunList(AngleList);
                while (isRun && !Motor.IsReady) Thread.Sleep(1);
                if (!isRun) return;
                Motor.SetInfo(s, f, d);
                while (isRun && !Motor.IsReady) Thread.Sleep(1);
                //Axis6.AxisStruct last = new Axis6.AxisStruct();

                //if(AngleList==null)
                //{
                //    AngleList = Axis6.PointToPointLineAngle(sour, dest, rg);
                //    lineObj = Axis6.AngleListToObject(AngleList);
                //}
                //Motor.MotorRunList(AngleList);

                //Thread.Sleep(DefDelay);
            }

            public override void remove()
            {
                if (linehandl != null)
                    Axis6.removeLine(linehandl);
                if (drawhandle != null)
                    Axis6.removeLine(drawhandle);
                linehandl = drawhandle = null;
            }

            public override void del()
            {
                if (linehandl != null)
                    Axis6.removeLine(linehandl);
            }

            public override void line()
            {
                if (drawhandle != null)
                    Axis6.removeLine(drawhandle);
                drawhandle = Axis6.DrawLine(sour, dest);
            }

            public override string ToString()
            {
                return "直线运动到点 :\r\n起点:" + sour + "\r\n终点:" + dest + "\r\n运动速度:" + speed + "KHz\r\n精度:" + rg + "mm  "+message;
            }
        }

        public class Delay : ModeObject
        {
            public int time;
            public Delay(int t)
            {
                size = 2;
                time = t;
                message = "延时" + time + "毫秒";               
            }
            public override void run()
            {
                DefDelay = time;
                Thread.Sleep(time);
            }

            public override string ToString()
            {
                return message;
            }
        }

        public class SetSpeed : ModeObject
        {
            int speed;

            public int startspeed;
            public int addtime;
            public int frequency;
            public SetSpeed(int s)
            {
                size = 8;
                message = "设置电机运行速度" + s + "KHz";
                speed = s;
                startspeed = Motor.StartSpeed;
                addtime = Motor.SpeedTime;
                frequency = speed * 1000;
            }
            public SetSpeed(int start,int add,int frq)
            {
                size = 8;
                message = "设置电机运行速度" + frq/1000 + "KHz";
                speed = frq / 1000;
                startspeed = start;
                addtime = add;
                frequency = frq;
            }
            public override void run()
            {
                Motor.Frequency = speed * 1000;
                Motor.SetInfo();
            }
            public override string ToString()
            {
                return message;
            }
        }

        public class SetRange : ModeObject
        {
            public double r;
            public SetRange(double r)
            {
                message = "设置运行精度" + r + "毫米";
                this.r = r;
            }
            public override void run()
            {
                rg = r/1000;
            }
            public override string ToString()
            {
                return message;
            }
        }

        public class PWMRun : ModeObject
        {
            public int Count;
            public int[] Plus;
            public PWMRun(int count,int[] pls)
            {
                message = "PWM输出";

            }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("PWM输出:");
                for(int i = 0; i < Count; i++)
                {
                    sb.Append("[第" + i + "轴输出");
                    sb.Append(Plus[i]);
                    sb.Append("]");
                    sb.AppendLine();
                }
                return sb.ToString();
            }
        }
        public class CheckInput : ModeObject
        {
            public byte[] inputdata;
            public byte[] enableinput;
            public int len;

            public CheckInput(byte[] data,byte[] enabledata,int len)
            {
                
                message = "端口输入";
                inputdata = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                    inputdata[i] = data[i];
                //this.inputdata = data;
                enableinput = new byte[enabledata.Length];
                for (int i = 0; i < enabledata.Length; i++)
                    enableinput[i] = enabledata[i];
                //this.enableinput = enabledata;
                this.len = len;
                size = data.Length + enabledata.Length + 1;
            }

            bool checkInput(byte[] InputValue,byte[] EnableValue,int Len)
            {
                int len = InputValue.Length;
                byte[] bd = InputValue;
                int count = 0;
                for (int i = 0; i < len; i++)
                {
                    int t = 1;
                    for (int j = 0; j < 8; j++)
                    {
                        if (count++ == Len)
                            return true;
                        if ((EnableValue[i] & t) != 0)
                        {
                            if ((bd[i] & t) != (InputValue[i] & t))
                            {
                                return false;
                            }
                        }
                        t <<= 1;
                    }
                }
                return true;
            }

            string getinputstats(byte[] blist, byte[] dlist, int len)
            {
                string vcl = "";
                string vop = "";
                byte t;
                int count = 0;
                for (int i = 0; i < blist.Length; i++)
                {
                    if (blist[i] == 0)
                    {
                        count++;
                    }
                }
                if (count == blist.Length)
                    for (int i = 0; i < blist.Length; i++) blist[i] = 0xff;

                count = 0;

                for (int i = 0; i < blist.Length; i++)
                {
                    t = 1;
                    for (int j = 0; j < 8; j++)
                    {
                        if (count++ == len)
                        {
                            i = blist.Length;
                            j = 8;
                            break;
                        }
                        if ((t & blist[i]) != 0)
                        {
                            if ((t & dlist[i]) != 0)
                                vop += " I" + (j + i * 8 + 1);
                            else
                                vcl += " I" + (j + i * 8 + 1);

                        }
                        t <<= 1;
                    }

                }
                if (vop == "")
                {
                    return "检测端口" + vcl + "为低";
                }
                if (vcl == "")
                {
                    return "检测端口" + vop + "为高";
                }
                return "检测端口" + vop + "为高\r\n端口" + vcl + "为低";
            }

            bool check(byte[] bit)
            {
                if (bit.Length != inputdata.Length) return false;
                int len = bit.Length;
                for(int i=0;i< len; i++)
                {
                    if ((bit[i] & enableinput[i]) != (inputdata[i] & enableinput[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public override void run()
            {
                if (!IOPort.isLink) return;
                IOPort.GetInputPort();
                while (isRun)
                {
                    if (IOPort.IsRecv)
                    {
                        if (check(IOPort.InputBit))
                            break;
                        else
                            IOPort.GetInputPort();
                    }
                    Thread.Sleep(1);
                }
            }

            public override string ToString()
            {
                return getinputstats(enableinput, inputdata, len);
            }

        }
        public class SetOutput : ModeObject
        {
            public byte[] outputbit;
            public int len;
            public SetOutput(byte[] data,int len)
            {
                message = "端口输出";
                outputbit = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                    outputbit[i] = data[i];
                this.len = len;
                size = outputbit.Length + 1;
            }

            public override void run()
            {
                //if (UP==null) return;
                if (!IOPort.isLink) return;
                IOPort.SetIO(outputbit);
                while (isRun && !IOPort.IsSend) Thread.Sleep(1);
            }

            string getDataStats(byte[] blist, int len)
            {
                string vcl = "";
                string vop = "";
                byte t;
                int count = 0;
                for (int i = 0; i < blist.Length; i++)
                {
                    t = blist[i];
                    for (int j = 0; j < 8; j++)
                    {
                        if (count++ == len)
                        {
                            i = blist.Length;
                            j = 8;
                            break;
                        }
                        if ((t & 1) != 0)
                        {
                            vop += " O" + (j + i * 8 + 1);
                        }
                        else
                        {
                            vcl += " O" + (j + i * 8 + 1);
                        }
                        t >>= 1;
                    }

                }
                if (vop == "")
                {
                    return "端口" + vcl + "输出低";
                }
                if (vcl == "")
                {
                    return "端口" + vop + "输出高";
                }
                return "端口" + vop + "输出高 \r\n端口" + vcl + "输出低";
            }

            public override string ToString()
            {
                return "端口输出:\r\n"+getDataStats(outputbit,len);
            }
        }

        public class PLCCode : ModeObject
        {
            public byte[] code;
            //public string name;
            public PLCCode(byte[] code)
            {
                this.code = code;
                size = code.Length+2;
            }

            public PLCCode(Stream s)
            {
                byte[] rom = new byte[16*1024];
                s.Seek(0x15c, SeekOrigin.Begin);
                s.Read(rom, 0, (int)(s.Length > rom.Length ? rom.Length : s.Length));
                int count = 0;
                for(int i = 0; i < rom.Length; i++)
                {
                    if (rom[i] == 0xff)
                    {
                        if (++count == 6)
                        {
                            if (rom[i - 6] == 0x00 && rom[i - 7] == 0x0f)
                            {
                                count = i - 6;
                            }
                            else
                            {
                                count = i - 4;
                                rom[i - 4] = 0x00;
                                rom[i - 5] = 0x0F;
                            }
                            code = new byte[count+1];
                            Array.Copy(rom, code, count+1);
                            break;
                        }
                    }
                    else
                        count = 0;
                }
                if (code == null)
                    code = rom;
                size = code.Length+2;
            }

            public override string ToString()
            {
                return "PLC代码[" + code.Length + "字节]";
            }
        }
        public class Module : ModeObject
        {
            public List<ModeObject> ModelList;
            public string name;
            public Module()
            {
                message = "模块";
                ModelList = new List<ModeObject>();
            }

            public void Add(ModeObject obj)
            {
                ModelList.Add(obj);
            }

            public void Insert(int index,ModeObject obj)
            {
                ModelList.Insert(index,obj);
                UpdateLine(ModelList);
            }

            public void Remove(int index)
            {
                //ModelList.RemoveAt(index);
                LogicRun.Remove(index,ModelList);
            }

            public void Clear()
            {
                ModelList.Clear();
            }

            public override void run()
            {
                int Index = 0;
                while (isRun&& Index < ModelList.Count)
                {
                    if (Motor.IsReady || !Motor.Enable)
                    {
                        LogicList[Index++].run();
                    }
                    else
                        Thread.Sleep(10);
                }
            }

            public override string ToString()
            {
                return name;
            }

        }


        class Loop : ModeObject
        {
            List<ModeObject> LoopList;
            int count;
            public Loop(int count)
            {
                message = "循环";
                LoopList = new List<ModeObject>();
            }

            public void Add(ModeObject obj)
            {
                LoopList.Add(obj);
            }

            public void Remove(int index)
            {
                LoopList.RemoveAt(index);
            }

            public void Clear()
            {
                LoopList.Clear();
            }

            public override void run()
            {
                int Index = 0;
                while (isRun)
                {                     
                    if (Motor.IsReady || !Motor.Enable)
                    {
                        LogicList[Index++].run();
                        if(Index >= LoopList.Count)
                        {
                            if (--this.count <= 0)
                            {
                                break;
                            }
                            else
                            {
                                Index = 0;
                            }
                        }
                    }
                    else
                        Thread.Sleep(10);
                }
            }

        }



        static public void Add(ModeObject obj)
        {
            LastStruct = (obj is G0) ? ((G0)obj).dest : (obj is G1) ? ((G1)obj).dest : LastStruct;
            LogicList.Add(obj);
        }

        static public void Insert(int index,ModeObject obj)
        {
            LogicList.Insert(index,obj);
            if(obj.redraw)
            {
                UpdateLine();
            }
        }

        static public void Clear()
        {
            LastStruct = Axis6.DefStruct;
            foreach(ModeObject m in LogicList)
            {
                m.remove();
            }
            LogicList.Clear();
        }

        public static void UpdateLine(List<ModeObject> Modules=null)
        {
            if (Modules == null) Modules = LogicList;
            LastStruct = Axis6.DefStruct;
            foreach (ModeObject o in Modules)
            {
                if (o is G0)
                {
                    ((G0)o).sour = LastStruct;
                    LastStruct = ((G0)o).dest;
                    o.line();
                }
                else if (o is G1)
                {
                    ((G1)o).sour = LastStruct;
                    LastStruct = ((G1)o).dest;
                    o.line();
                }
            }
        }

        static public void Remove(int index, List<ModeObject> Modules = null)
        {
            Modules = (Modules != null) ? Modules : LogicList;
            if (index == -1) return;
            Modules[index].remove();
            if (Modules[index].redraw)
            {
                Modules.RemoveAt(index);
                UpdateLine(Modules);
            }
            else
                Modules.RemoveAt(index);
        }

        static public void Remove( ModeObject obj, List<ModeObject> Modules = null)
        {
            Modules = (Modules != null) ? Modules : LogicList;
            int index = Modules.IndexOf(obj);
            if (index == -1) return;
            obj.remove();
            if (obj.redraw)
            {
                Modules.RemoveAt(index);
                UpdateLine(Modules);
            }
            else
                Modules.RemoveAt(index);
        }


        static public void Start()
        {
            if (isRun) return;
            Index = 0;
            First = true;
            //ModeObject start=null;            
            
            RunLastStruct = Axis6.DefStruct;
            if (!isRun)
            {
                isRun = true;
                new Task(() =>
                {
                    while (isRun)
                    {
                        //Thread.Sleep(100);
                        if (Index >= LogicList.Count)
                        {
                            if (looprun)
                            {
                                Index = 0;
                                Thread.Sleep(200);
                            }
                            else Stop();
                            First = false;
                            //TaskThread = true;
                            continue;
                        }
                        try
                        {
                            LogicList[Index++].run();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Forms.MessageBox.Show("运行错误:\r\n" + ex.ToString());
                            StopLogic();
                            return;
                        }
                    }
                }).Start();
            }
            
    }

        static public void Stop()
        {
            isRun = false;
            foreach (ModeObject obj in LogicList)
                obj.del();
            Motor.MotorStop();
        }

        static public void StopLogic()
        {
            //TaskThread = false;
        }

    }
}
