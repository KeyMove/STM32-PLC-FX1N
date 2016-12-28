using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axis6v006Native;
//using MathWorks.MATLAB.NET.Arrays;
using System.Threading;
using KeyMove.Tools;

namespace KControl
{
    class Axis6
    {
        public const int MaxAxis = 6;
        static Class1 Axis;
        public delegate void InitCallBack(bool stats);

        public static double[] StartAngle = { 0, 0, 0, 0, 0, 0 };

        public static double[][] SoftMax = new double[][] {
            new double[]{170,-170},
            new double[]{110,-110},
            new double[]{70,-70},
            new double[]{170,-170},//
            new double[]{105,-105},//135
            new double[]{360,-360},//360
        };

        public static AxisStruct DefStruct = new AxisStruct();
        static bool zoffsetflag = false;
        public class AxisStruct
        {
            public string name = "点";
            public double[] angle;
            public double X, Y, Z, N, O, A;
            public AxisStruct()
            {
                angle = new double[6];
                X = Y = Z = N = O = A = 0;
                for (int i = 0; i < angle.Length; i++) angle[i] = 0;
            }

            public AxisStruct(AxisStruct old)
            {
                angle = new double[6];
                X = Y = Z = N = O = A;
                for (int i = 0; i < 6; i++)
                    angle[i] = old.angle[i];
                X = old.X;
                Y = old.Y;
                Z = old.Z;
                N = old.N;
                O = old.O;
                A = old.A;
            }

            public AxisStruct(double x, double y, double z, double n, double o, double a, double[] ang)
            {
                X = x;
                Y = y;
                Z = z;
                N = n;
                O = o;
                A = a;
                angle = ang;
            }

            public double[] getXYZ()
            {
                return new double[] { X, Y, Z };
            }

            public double[] getNOA()
            {
                return new double[] { N, O, A };
            }

            public override string ToString()
            {
                string v;
                StringBuilder sb=new StringBuilder();
                sb.Append(name);
                sb.Append(": X=");
                v = X.ToString();
                sb.Append(v.Length > 6 ? v.Substring(0, 6) : v);
                sb.Append(" Y=");
                v = Y.ToString();
                sb.Append(v.Length > 6 ? v.Substring(0, 6) : v);
                sb.Append(" Z=");
                v = Z.ToString();
                sb.Append(v.Length > 6 ? v.Substring(0, 6) : v);
                sb.Append(" N=");
                v = N.ToString();
                sb.Append(v.Length > 6 ? v.Substring(0, 6) : v);
                sb.Append(" O=");
                v = O.ToString();
                sb.Append(v.Length > 6 ? v.Substring(0, 6) : v);
                sb.Append(" A=");
                v = A.ToString();
                sb.Append(v.Length > 6 ? v.Substring(0, 6) : v);
                //return name + ": X=" + ((X.ToString().Length > 6)? X.ToString("f4") : X.ToString()) + " Y=" + Y.ToString("f3") + " Z=" + Z.ToString("f3") + " N=" + N.ToString("f3") + " O=" + O.ToString("f3") + " A=" + A.ToString("f3");
                return sb.ToString();
            }

            public override bool Equals(object obj)
            {
                if(obj is AxisStruct)
                {
                    AxisStruct a = (AxisStruct)obj;
                    if (this.X != a.X) return false;
                    if (this.Y != a.Y) return false;
                    if (this.Z != a.Z) return false;
                    if (this.N != a.N) return false;
                    if (this.O != a.O) return false;
                    if (this.A != a.A) return false;
                    for(int i = 0; i < 6; i++)
                    {
                        if (this.angle[i] != a.angle[i])
                            return false;
                    }
                    return true;
                }
                return false;
                //return base.Equals(obj);
            }

        }

        public static bool Init()
        {
            try
            {
                Axis = new Class1();
                Axis.show_init(0.01);
                double[] pos = Axis6.AngleToPosArray(Axis6.StartAngle);
                DefStruct.X = pos[0];
                DefStruct.Y = pos[1];
                DefStruct.Z = pos[2];
                DefStruct.N = 0;
                DefStruct.O = 90;
                DefStruct.A = 0;
                return true;
            }
            catch { }
            return false;
        }

        public static void Init(InitCallBack callback)
        {
            Thread thread = new Thread(() => {
                callback(Init());
            });
            thread.Start();
        }

        public static double[] PosToAngle(double[] start,double x, double y, double z, double n, double o, double a)
        {
            double[] ag = new double[6];
            object[] outdata;
            double zoffset = zoffsetflag ? Math.Sqrt(x * x + y * y) * 0.093 : 0;
            object[] indata = { start, new double[] { x, y, z+zoffset }, new double[] { n, o, a } };
            try
            {
                outdata = Axis.Ioper(3, indata[0], indata[1], indata[2]);
            }
            catch
            {
                return null;
            }
            if (outdata == null) return null;
            if (((double[,])outdata[0])[0, 0] != 0)
            {
                double[,] data = (double[,])outdata[2];
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                    {
                        if (Math.Abs(data[j, k]) > 0.0001)
                        {
                            return null;
                        }
                    }
            }
            double[,] v = (double[,])outdata[1];
            for (int i = 0; i < 6; i++)
            {
                ag[i] = v[0, i];
            }
            return ag;
        }

        public static double[] PosToAngle(double x, double y, double z, double n, double o, double a)
        {
            double[] ag = new double[6];
            object[] outdata;
            double zoffset = zoffsetflag ? Math.Sqrt(x * x + y * y) * 0.093 : 0;
            object[] indata = { StartAngle, new double[] { x, y, z+zoffset }, new double[] { n, o, a } };
            try
            {
                outdata = Axis.Ioper(3, indata[0], indata[1], indata[2]);
            }
            catch
            {
                return null;
            }
            if (outdata == null) return null;
            if (((double[,])outdata[0])[0, 0] != 0)
            {
                double[,] data = (double[,])outdata[2];
                for(int j=0;j<4;j++)
                    for(int k = 0; k < 4; k++)
                    {
                        if (Math.Abs(data[j, k]) > 0.0003)
                        {
                            return null;
                        }
                    }
            }
            double[,] v = (double[,])outdata[1];
            for (int i = 0; i < 6; i++)
            {
                ag[i] = v[0, i];
            }
            return ag;
        }

        public static double[] PosToAngle(AxisStruct st)
        {
            return PosToAngle(st.X, st.Y, st.Z, st.N, st.O, st.A);
        }

        public static double[] PosToAngle(AxisStruct start, AxisStruct dest)
        {
            return PosToAngle(start.angle,dest.X, dest.Y, dest.Z, dest.N, dest.O, dest.A);
        }



        public static AxisStruct PosToAngle(AxisStruct start, double x, double y, double z, double n, double o, double a)
        {
            AxisStruct ag = new AxisStruct();
            ag.X = x;
            ag.Y = y;
            ag.Z = z;
            ag.N = n;
            ag.O = o;
            ag.A = a;
            ag.angle=PosToAngle(start.angle, x, y, z, n, o, a);
            return ag;

        }

        public static AxisStruct AngleToPos(double[] angle)
        {
            AxisStruct ax = new AxisStruct();
            double[,] mx = (double[,])Axis.Foper(angle);
            double[] pos = new double[3];
            ax.X = mx[0, 3];
            ax.Y = mx[1, 3];
            ax.Z = mx[2, 3];
            for (int i = 0; i < 6; i++)
                ax.angle[i] = angle[i];
            return ax;
        }

        public static double[] AngleToPosArray(double[] angle)
        {
            //double[,] mx = (double[,])((MWArray)Axis.Foper(new MWNumericArray(angle))).ToArray();
            //object obj = Axis.Foper(new MWNumericArray(angle));
            double[,] mx = (double[,])Axis.Foper(angle);
            double[] pos = new double[3];
            pos[0] = mx[0, 3];
            pos[1] = mx[1, 3];
            pos[2] = mx[2, 3];
            return pos;
        }

        public static object DrawArcLine(double[] Ag1, double[] Ag2, char color = 'r', int w = 1, int count = 15)
        {
            object[] output;
            //object[] input;
            //input = new object[5];
            //input[0] = Ag1;
            //input[1] = Ag2;
            //input[2] = 'r';
            //input[3] = w;
            //input[4] = count;
            //Axis.LineA(output.Length, output, input);
            output = Axis.LineA(2, Ag1,Ag2,color,(double)w,(double)count);
            return output[1];
        }

        public static object DrawLine(AxisStruct g, AxisStruct g2, char color = 'r', int w = 1, int count = 15)
        {
            object[] output;
            //output = new object[4];
            //MWArray[] input;
            //output = new MWArray[4];
            //input = new MWArray[7];
            //input[0] = new MWNumericArray(g.angle);
            //input[1] = new MWNumericArray(g.getXYZ());
            //input[2] = new MWNumericArray(g.getNOA());
            //input[3] = new MWNumericArray(g2.getXYZ());
            //input[4] = new MWNumericArray(g2.getNOA());
            //input[5] = new MWCharArray(color);
            //input[6] = new MWNumericArray(w);
            output =Axis.LineQ(4, g.angle,g.getXYZ(),g.getNOA(),g2.getXYZ(),g2.getNOA(),color,(double)w);
            if (((double[,])output[0])[0,0] == 0)
            {
                return output[3];
            }
            return null;
        }


        static public object MoveArc(double[] Ag1, double[] Ag2, char color = 'r', int w = 1, int count = 15, bool updateag = true)
        {
            object[] output;
            //MWArray[] input;
            //output = new MWArray[2];
            //input = new MWArray[5];
            //input[0] = new MWNumericArray(Ag1);
            //input[1] = new MWNumericArray(Ag2);
            //input[2] = new MWCharArray(color);
            //input[3] = new MWNumericArray(w);
            //input[4] = new MWNumericArray(count);
            output= Axis.FoperS(2, Ag1,Ag2,color,(double)w,(double)count);
            if (updateag)
                return output[1];
            else
                Axis.Dline(output[1]);
            return null;
        }

        static public bool removeLine(object line)
        {
            try
            {
                Axis.Dline(line);
                return true;
            }
            catch { }
            return false;
        }

        public static double[] MoveToAngle(double[] ag)
        {
            double[,] mx = ((double[,])Axis.FoperS(ag));
            double[] pos = new double[3];
            pos[0] = mx[0, 3];
            pos[1] = mx[1, 3];
            pos[2] = mx[2, 3];
            return pos;
        }


        public static double[] MoveToPos(double x, double y, double z, double n, double o, double a)
        {
            double[] ag = new double[6];
            object[] outdata;
            double zoffset = zoffsetflag ? Math.Sqrt(x * x + y * y) * 0.093 : 0;
            object[] indata = { StartAngle, new double[] { x, y, z+zoffset }, new double[] { n, o, a } };
            try
            {
                outdata = Axis.IoperS(3, indata[0], indata[1], indata[2]);
            }
            catch
            {
                return null;
            }
            if (outdata == null) return null;
            if (((double[,])outdata[0])[0, 0] != 0)
            {
                double[,] data = (double[,])outdata[2];
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                    {
                        if (Math.Abs(data[j, k]) > 0.0001)
                        {
                            return null;
                        }
                    }
            }
            double[,] v = (double[,])outdata[1];
            for (int i = 0; i < 6; i++)
            {
                ag[i] = v[0, i];
            }
            return ag;
        }

        public static bool AngleSoftMax(double[] ag)
        {
            for (int i = 0; i < 6; i++)
            {
                if (ag[i] > SoftMax[i][0])
                    return false;
                if (ag[i] < SoftMax[i][1])
                    return false;
            }
            return true;
        }

        public static double[] AngleSoftSet(double[] ag)
        {
            for (int i = 0; i < 6; i++)
            {
                if (ag[i] > SoftMax[i][0])
                    ag[i] = SoftMax[i][0];
                if (ag[i] < SoftMax[i][1])
                    ag[i] = SoftMax[i][1];
            }
            return ag;
        }

        public static void PointToPointMove(object obj)
        {
            Axis.Plot(obj);
            //Axis.Plot()
            //Axis.Plot(new MWNumericArray(2,6, new double[] { 0, 0, 0, 0, 0, 0, 90, 0, 0,0, 0,0 }));
            //new MWNumericArray(MWNumericType)
        }

        public static List<double[]> PointToPointLineAngle(AxisStruct LastG, AxisStruct G, double range = 0.1, double deg = 1, bool MultThread = false)
        {
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
            Line.Setup(v, d, range);
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
                Line.Setup(v, d, deg);
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
            Line.Setup(v, d, Line.Count);
            G = new AxisStruct();
            while (!Line.isEmpty())
            {
                double[] list = Line.GetData();
                G.X = list[0];
                G.Y = list[1];
                G.Z = list[2];
                G.N = list[3];
                G.O = list[4];
                G.A = list[5];
                double[] pag = PosToAngle(LastG, G);
                if (pag != null)
                    lastag = pag;
                anglelist.Add(lastag);
                G.angle = lastag;
                LastG = G;
            }
            return anglelist;
        }

        public static object AngleListToObject(List<double[]> list)
        {
            //double[,] ma = new double[list.Count , 6];
            double[][] ta = new double[list.Count][];
            int Count = 0;
            foreach (double[] db in list)
            {
                ta[Count] = db;
                Count++;
            }
            return ta;
        }

        public static object AngleListToObject(double[][] list)
        {
            return list;
        }


        public static object DrawLineList(double[] x,double[] y,double[] z)
        {
            return Axis.Line(x, y, z);
        }

    }
}
