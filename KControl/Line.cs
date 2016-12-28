using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyMove.Tools
{
    public static class Line
    {
        public static int Count;
        static double Distan;
        const int MaxCount= 6;

        static double[] offset = new double[MaxCount];
        static double[] dis = new double[MaxCount];
        public static double[] pls=new double[MaxCount];

        static double[] bise = new double[MaxCount];
        public static double[] inc = new double[MaxCount];

        public static void Init()
        {
            Distan = 0;
            Count = 0;
            for (int i = 0; i < 6; i++)
            {
                offset[i] = dis[i] = pls[i] = inc[i] = 0;
            }
        }

        public static bool isEmpty()
        {
            return (Count == 0);
        }

        public static void Setup(double[] v, double[] d, double incval)
        {
            Distan = Count = 0;
            for(int i = 0; i < MaxCount; i++)
            {
                dis[i] = d[i] - v[i];
                pls[i] = v[i];
                inc[i] = 0;
                if (dis[i] < 0)
                {
                    dis[i] = -dis[i];
                    inc[i] = -incval;
                }
                else if (dis[i] > 0)
                {
                    inc[i] = incval;
                }
                //dis[i] /= incval;
                if (dis[i] > Distan)
                {
                    Distan = dis[i];
                }
                bise[i] = d[i];
            }
            Count = (int)(Distan / incval)+1;
            for(int i = 0; i < 6; i++)
            {
                if (inc[i] > 0)
                    inc[i] = dis[i] / Count;
                else if (inc[i] < 0)
                    inc[i] = -(dis[i] / Count);
            }
        }

        public static void Setup(double[] v, double[] d, int incval)
        {
            Distan = Count = 0;
            for (int i = 0; i < MaxCount; i++)
            {
                dis[i] = d[i] - v[i];
                pls[i] = v[i];
                inc[i] = 0;
                if (dis[i] < 0)
                {
                    dis[i] = -dis[i];
                    inc[i] = -incval;
                }
                else if (dis[i] > 0)
                {
                    inc[i] = incval;
                }
                //dis[i] /= incval;
                if (dis[i] > Count)
                {
                    Distan = dis[i];
                }
                bise[i] = d[i];
            }
            Count = incval+1;
            for (int i = 0; i < 6; i++)
            {
                if (inc[i] > 0)
                    inc[i] = dis[i] / Count;
                else if (inc[i] < 0)
                    inc[i] = -(dis[i] / Count);
            }
        }

        //public static void Setup(double[] v, double incval)
        //{
        //    Distan = Count = 0;
        //    for(int i = 0; i < MaxCount; i++)
        //    {
        //        pls[i] = v[i];
        //        inc[i] = 0;
        //        bise[i] = 0;
        //        if (pls[i] < 0)
        //        {
        //            pls[i] = -pls[i];
        //            dis[i] = pls[i];
        //            inc[i] = -incval;
        //        }
        //        else if (pls[i] > 0)
        //        {
        //            dis[i] = pls[i];
        //            inc[i] = incval;
        //        }
        //        if (pls[i] > Count)
        //        {
        //            Count = pls[i];
        //        }
        //        offset[i] = pls[i] / 2;
        //        pls[i] += bise[i];
        //    }
        //    Count /= incval;
        //    Distan = Count;
        //    //Count++;
        //}

        //public static void Setup(double[] v, double incval, double[] b)
        //{
        //    Distan = Count = 0;
        //    for (int i = 0; i < MaxCount; i++)
        //    {
        //        pls[i] = v[i];
        //        inc[i] = 0;
        //        bise[i] = b[i];
        //        if (pls[i] < 0)
        //        {
        //            pls[i] = -pls[i];
        //            dis[i] = pls[i];
        //            inc[i] = -incval;
        //        }
        //        else if (pls[i] > 0)
        //        {
        //            dis[i] = pls[i];
        //            inc[i] = incval;
        //        }
        //        if (pls[i] > Count)
        //        {
        //            Count = pls[i];
        //        }
        //        offset[i] = pls[i] / 2;
        //    }
        //    Count /= incval;
        //    Distan = Count;
        //    //Count++;
        //}

        public static double[] GetData()
        {
            if (Count == 0) return null;
            for(int i = 0; i < 6; i++)
            {
                pls[i] += inc[i];
                //offset[i] += dis[i];
                //if (offset[i] > Distan)
                //{
                //    offset[i] -= Distan;
                //    pls[i] += inc[i];
                //}
            }
            Count--;
            if (Count == 0)
            {
                return bise;
            }
            return pls;
        }

    }
}
