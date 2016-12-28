using KeyMove.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KControl
{
    class BuildCode
    {

        enum CodeBinData:int
        {
            RESTOUT = 0,
            OUTSIG = 1,
            INPUTSIG = 2,
            DELAY = 3,
            PWMCONFIG = 4,
            PWMOUT = 5,
            PWMSTOP = 6,
            PLCCODE = 7,
        }
        static public List<LogicRun.ModeObject> BytesToCode(byte[] code)
        {
            List<LogicRun.ModeObject> objs=new List<LogicRun.ModeObject>();
            ByteStream bdata = new ByteStream(code);
            ByteStream bpos = new ByteStream(code);
            int count = bdata.ReadWord();
            int offset = bdata.ReadByte();
            bdata.offset(count * 2+offset);
            bpos.offset(3+offset);
            offset = 3 + offset + count * 2;
            int len;
            for(int i = 0; i < count; i++)
            {
                bdata.offset(bpos.ReadWord(), offset);
                switch ((CodeBinData)bdata.ReadByte())
                {
                    case CodeBinData.OUTSIG:
                        len = bdata.ReadByte();
                        objs.Add(new LogicRun.SetOutput(bdata.ReadBuff((len + 7) / 8), len));
                        break;
                    case CodeBinData.INPUTSIG:
                        len = bdata.ReadByte();
                        objs.Add(new LogicRun.CheckInput(bdata.ReadBuff((len + 7) / 8), bdata.ReadBuff((len + 7) / 8), len));
                        break;
                    case CodeBinData.DELAY:
                        objs.Add(new LogicRun.Delay(bdata.ReadWord()));
                        break;
                    case CodeBinData.PWMCONFIG:
                        objs.Add(new LogicRun.SetSpeed(bdata.ReadWord(), bdata.ReadWord(), bdata.ReadDWorde()));
                        break;
                    case CodeBinData.PWMOUT:
                        len = bdata.ReadByte();
                        int[] pls = new int[len];
                        for (int j = 0; j < len; j++)
                            pls[j] = bdata.ReadDWorde();
                        objs.Add(new LogicRun.PWMRun(len,pls));
                        break;
                    case CodeBinData.PWMSTOP:break;
                    case CodeBinData.PLCCODE:
                        len = bdata.ReadWord();
                        objs.Add(new LogicRun.PLCCode(bdata.ReadBuff(len)));
                        break;
                }
            }
            return objs;
        }
        static public byte[] CodeToBytes(List<LogicRun.ModeObject> logic,byte[] headdata=null) 
        {
            int size=0;
            foreach (LogicRun.ModeObject obj in logic)
                size += obj.size;
            size += logic.Count;
            ByteStream head = new ByteStream(logic.Count * 2+18);
            ByteStream data = new ByteStream(size);
            head.WriteWord(logic.Count);
            if (headdata != null)
            {
                head.WriteByte(headdata.Length);
                head.WriteBuff(headdata);
            }
            else
            {
                head.WriteByte(3);
                head.WriteByte(4);
                head.WriteWord(0);
            }
            foreach(LogicRun.ModeObject obj in logic)
            {
                head.WriteWord(data.Pos);
                if(obj is LogicRun.SetOutput)
                {
                    data.WriteByte((int)CodeBinData.OUTSIG);
                    data.WriteByte(((LogicRun.SetOutput)obj).len);
                    data.WriteBuff(((LogicRun.SetOutput)obj).outputbit);
                }
                else if(obj is LogicRun.CheckInput)
                {
                    data.WriteByte((int)CodeBinData.INPUTSIG);
                    data.WriteByte(((LogicRun.CheckInput)obj).len);
                    data.WriteBuff(((LogicRun.CheckInput)obj).inputdata);
                    data.WriteBuff(((LogicRun.CheckInput)obj).enableinput);
                }
                else if(obj is LogicRun.Delay)
                {
                    data.WriteByte((int)CodeBinData.DELAY);
                    data.WriteWord(((LogicRun.Delay)obj).time);
                }
                else if(obj is LogicRun.SetSpeed)
                {
                    data.WriteByte((int)CodeBinData.PWMCONFIG);
                    data.WriteWord(((LogicRun.SetSpeed)obj).startspeed);
                    data.WriteWord(((LogicRun.SetSpeed)obj).addtime);
                    data.WriteDWord(((LogicRun.SetSpeed)obj).frequency);
                }
                else if(obj is LogicRun.G0)
                {
                    data.WriteByte((int)CodeBinData.PWMOUT);
                    data.WriteByte(Axis6.MaxAxis);
                    int[] pls= Motor.AngleToPluse(((LogicRun.G0)obj).dest.angle);
                    for (int i = 0; i < Axis6.MaxAxis; i++)
                    {
                        data.WriteByte(i);
                        data.WriteDWord(pls[i]);
                    }
                }
                else if(obj is LogicRun.PLCCode)
                {
                    data.WriteByte((int)CodeBinData.PLCCODE);
                    data.WriteWord(((LogicRun.PLCCode)obj).code.Length);
                    data.WriteBuff(((LogicRun.PLCCode)obj).code);
                }
            }
            int pos = head.Pos;
            size = head.Pos + data.Pos;
            head.offset(4, 0);
            head.WriteWord(size);
            head.offset(pos, 0);
            byte[] a = head.toBytes();
            byte[] b = data.toBytes();
            ByteStream c = new ByteStream(a.Length + b.Length);
            c.WriteBuff(a);
            c.WriteBuff(b);
            return c.toBytes();
        }
    }
}
