using KeyMove.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KControl
{
    public partial class GCodeLoader : Form
    {

        Bitmap map = new Bitmap(600, 600);
        Graphics g;
        Graphics draw;
        Dictionary<int, int> charmap = new Dictionary<int, int>();
        List<int> charlist = new List<int>();
        List<byte[]> datalist = new List<byte[]>();
        List<byte[]> numlist = new List<byte[]>();

        List<Panel> UIPanelList = new List<Panel>();

        int RGB565(Color color)
        {
            int c = (color.R&0xf8)>>3;
            c <<= 6;
            c |= (color.G & 0xfc)>>2;
            c <<= 5;
            c |= (color.B & 0xf8) >> 3;
            return c;
        }

        int GetFontPixle(float f)
        {
            return (int)Math.Round((f * 1.33333f));
        }

        byte[] chardata(char c,Font f)
        {
            byte[] data=null;
            string s = (char)c + "";
            int vx;// = GetFontPixle(c,f.Size);
            int vy;// = vx;
            int xoffset=0;
            Size sf= TextRenderer.MeasureText( s, f);
            vx = (int)(sf.Width/(c<128?1.66666:1.3333));
            vy = (int)sf.Height;
            xoffset = -((sf.Width - vx) / 2);
            draw.FillRectangle(Brushes.Black, 0, 0, vx, vy);
            //vx /= (c > 128 ? 1 : 2);
            draw.DrawString(s, f, Brushes.White, xoffset, 0);
            
            data = new byte[((vx + 7) / 8) * vy+2];
            data[0] = (byte)vx;
            data[1] = (byte)vy;
            byte[] bit = { 1, 2, 4, 8, 0x10, 0x20, 0x40, 0x80 };
            for (int y = 0; y < vy; y++)
            {
                int offset = y * ((vx + 7) / 8)+2;
                for (int x = 0; x < vx; x++)
                {
                    Color d = map.GetPixel(x, y);
                    if  (((int)d.R+ (int)d.G+ (int)d.B)!=0)
                        data[offset+(x /8 )] |= bit[x %8];
                }
            }
            return data;
            //f.SizeInPoints
        }

        string getcharMap(string name,Font f)
        {
            string val = "";
            char vx;
            int key;
            for (int i = 0; i < name.Length; i++)
            {
                key = name[i]+(((int)f.Size)<<16);
                if (charmap.ContainsKey(key))
                {
                    vx = (char)(' ' + charmap[key]);
                    val += (vx == '\"') ? "\\\"" : vx + "";
                }
                else
                {
                    datalist.Add(chardata(name[i], f));
                    //val += (char)(' ' + charmap.Keys.Count);
                    vx = (char)(' ' + charmap.Keys.Count);
                    val += (vx == '\"') ? "\\\"" : vx + "";
                    charmap.Add(key, charmap.Keys.Count);
                }
            }
            return val;
        }

        string tabletostring(List<byte[]> data,string name="c",string usename="chartable")
        {
            StringBuilder sb = new StringBuilder();
            for(int i=0;i< data.Count;i++)
            {
                sb.Append("static const u8 " + name + i + "[]={");
                foreach (byte c in data[i])
                {
                    sb.AppendFormat("0x{0:X2},",c);
                }
                sb.AppendLine("};");
            }
            sb.Append("static const u8* "+ usename + "[]={");
            for (int i = 0; i < data.Count; i++) sb.Append(name + i + ",");
            sb.AppendLine("};");
            return sb.ToString();
        }

        void toCode()
        {
            Color POINT_COLOR=Color.Empty;
            Color BACK_COLOR = Color.Empty;
            charmap.Clear();
            int textcount = 0;
            int imgcount = 0;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            int count = 0;
            foreach (Control clist in UIPanelList)
            {
                sb.AppendLine("void UI_View"+count+"(void){");
                sb2.AppendLine("void UI_Text"+count+"(void){");
                sb3.AppendLine("void UI_Block"+count+"(void){");
                count++;
                for (int i = 0; i < clist.Controls.Count; i++)
                {
                    Control c = clist.Controls[clist.Controls.Count - i - 1];
                    if (c is PictureBox)
                    {
                        if (c.Tag != null)
                        {
                            sb3.AppendFormat("LCD_DrawRect({0},{1},{2},{3},(({5})?Color1:Color2),0x{4:X4},1);\r\n", c.Location.X, c.Location.Y, c.Width, c.Height, RGB565(Color.Gray), c.Tag);
                            //sb3.AppendFormat("LCD_Fill({0},{1},{2},{3},value[{4}]);\r\n", c.Location.X, c.Location.Y, c.Width + c.Location.X, c.Height + c.Location.Y, imgcount++);
                            //sb3.AppendFormat("POINT_COLOR=0x{0:X4};\r\n", RGB565(Color.Gray));
                            //sb3.AppendFormat("LCD_DrawRectangle({0},{1},{2},{3});\r\n", c.Location.X, c.Location.Y, c.Width + c.Location.X, c.Height + c.Location.Y);
                        }
                        else
                        {
                            sb.AppendFormat("LCD_DrawRect({0},{1},{2},{3},0x{4:X4},0x{5:X4},1);\r\n", c.Location.X, c.Location.Y, c.Width, c.Height, RGB565(c.BackColor), RGB565(Color.Gray));
                            //sb.AppendFormat("POINT_COLOR=0x{0:X4};\r\n", RGB565(Color.Gray));
                            //sb.AppendFormat("LCD_DrawRectangle({0},{1},{2},{3});\r\n", c.Location.X, c.Location.Y, c.Width + c.Location.X, c.Height + c.Location.Y);
                        }
                    }
                    else if (c is Label)
                    {
                        if (c.Tag != null)
                        {
                            if (numlist.Count == 0)
                            {
                                for (int j = 0; j < 10; j++)
                                    numlist.Add(chardata((char)('0' + j), c.Font));
                                numlist.Add(chardata('-', c.Font));
                                numlist.Add(chardata('.', c.Font));
                            }
                            sb2.AppendFormat("POINT_COLOR=0x{0:X4};\r\n", RGB565(c.ForeColor));
                            POINT_COLOR = Color.Empty;
                            sb2.AppendFormat("BACK_COLOR=0x{0:X4};\r\n", RGB565(c.BackColor));
                            BACK_COLOR = Color.Empty;
                            string tag = c.Tag.ToString();
                            tag = tag.Replace("[x]", "[" + c.TabIndex + "]");
                            sb2.AppendFormat("LCD_DrawNumber({0},{1},(u8**)numtable,{2});\r\n", c.Location.X, c.Location.Y, tag);
                            //sb.AppendFormat("LCD_DrawNum({0},{1},(u8**)chartable,(u8*)\"{2}\");\r\n", c.Location.X, c.Location.Y, getcharMap(c.Text, c.Font));
                        }
                        else
                        {
                            if (POINT_COLOR != c.ForeColor)
                            {
                                sb.AppendFormat("POINT_COLOR=0x{0:X4};\r\n", RGB565(c.ForeColor));
                                POINT_COLOR = c.ForeColor;
                            }
                            if (BACK_COLOR != c.BackColor)
                            {
                                sb.AppendFormat("BACK_COLOR=0x{0:X4};\r\n", RGB565(c.BackColor));
                                BACK_COLOR = c.BackColor;
                            }
                            sb.AppendFormat("LCD_ShowBuffList({0},{1},(u8**)chartable,(u8*)\"{2}\");\r\n", c.Location.X, c.Location.Y, getcharMap(c.Text, c.Font));
                        }
                    }
                }
                sb3.AppendLine("}");
                sb2.AppendLine("}");
                sb.AppendLine("}");
            }
            CodeBox.Text =datalist.Count!=0? tabletostring(datalist):"";
            CodeBox.Text += numlist.Count != 0 ? tabletostring(numlist,"n","numtable") : "";
            CodeBox.Text += sb.ToString() + sb2.ToString() + sb3.ToString() ;
        }

        public GCodeLoader()
        {
            InitializeComponent();
            //UIPanelList.Add(UIPanel);
            UIPanelList.Add(UIPanel3);
            UIPanelList.Add(UIPanel2);
            UIPanelList.Add(UIPanel4);
            g = this.CreateGraphics();
            draw = Graphics.FromImage(map);
            tabControl1.Visible = false;
            toCode();
        }
        OpenFileDialog of = new OpenFileDialog();
        private void OpenGCode_Click(object sender, EventArgs e)
        {
            of.Filter = "";
        }

        public bool LoadFile(string file, double X, double Y, double Z, double N, double O, double A)
        {
            oX = this.X = X;
            oY = this.Y = Y;
            oZ = this.Z = Z;
            this.N = N;
            this.O = O;
            this.A = A;
            DecodeGCode(file);
            return true;
        }
        List<double[]> AngleList = new List<double[]>();
        double[] LastAngle;
        double XnPos, YnPos, ZnPos;
        double X, Y, Z,N,O,A;
        List<double> PosXList = new List<double>();
        List<double> PosYList = new List<double>();
        List<double> PosZList = new List<double>();
        private void GCodeLoader_Load(object sender, EventArgs e)
        {
            //g = Graphics.FromImage(map);
            //draw = pictureBox1.CreateGraphics();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tabControl1.Visible = true;
            g.DrawImage(map, 0, 0);
        }

        double oX, oY, oZ;
        int GetNum(string p, int offset)
        {
            int x = 0;
            bool fv = false;
            int index = offset;
            while ((p[index] < ('9' + 1) && p[index] > ('0' - 1)) || p[index] == '-')
            {
                if (p[index] == '-')
                {
                    index++;
                    fv = true;
                    continue;
                }
                x *= 10;
                x += p[index] - '0';
                index++;
                if (p.Length <= index)
                    return (fv) ? -x : x;
            }
            return (fv) ? -x : x;
        }

        public List<double[]> getData()
        {
            return AngleList;
        }

        public double[] getXList()
        {
            return PosXList.ToArray();
        }

        public double[] getYList()
        {
            return PosYList.ToArray();
        }

        public double[] getZList()
        {
            return PosZList.ToArray();
        }

        void DecodeGCode(string filename, double sacl = 0.25,double xscal=1,double yscal=1,double zscal=1)
        {
            double x, y, z;
            string GCodeString = string.Empty;
            try
            {
                GCodeString = new System.IO.StreamReader(filename).ReadToEnd();
            }
            catch { return; }
            bool Grid = false;
            int index = 0;
            int GCodeID = 0;
            if (GCodeString == string.Empty)
                return;
            XnPos = 0;
            YnPos = 0;
            ZnPos = 0;
            PosXList.Clear();
            PosYList.Clear();
            PosZList.Clear();
            LastAngle = new double[6] {0,0,0,0,0,0 };
            while (index != -1)
            {
                if (Grid)
                {
                    x = 0;
                    y = 0;
                    z = 0;
                }
                else
                {
                    x = XnPos;
                    y = YnPos;
                    z = ZnPos;
                }
                while (GCodeString[index] != '\n')
                {
                    switch (GCodeString[index++])
                    {
                        case 'D':

                            break;
                        case 'F':

                            break;
                        case 'G':
                            GCodeID = GetNum(GCodeString, index);
                            break;
                        case 'X':
                            x = GetNum(GCodeString, index);
                            break;
                        case 'Y':
                            y = GetNum(GCodeString, index);
                            break;
                        case 'Z':
                            z = GetNum(GCodeString, index);
                            break;
                    }
                    if (index >= GCodeString.Length)
                    {
                        index = -1;
                        break;
                    }
                }
                if (Grid)
                {
                    x += XnPos;
                    y += YnPos;
                    z += ZnPos;
                }
                if (index != -1)
                    if (GCodeString[index] == '\n')
                        index++;
                if (index >= GCodeString.Length)
                {
                    index = -1;
                }

                X = x / 1000 * sacl;
                Y = y / 1000 * sacl;
                Z = z / 1000 * sacl;
                X *= xscal;
                Y *= yscal;
                Z *= zscal;

                switch (GCodeID)
                {
                    case 0:
                    case 1:
                        PosXList.Add(X+oX);
                        PosYList.Add(Y+oY);
                        PosZList.Add(Z+oZ);
                        //Axis6.AxisStruct posdata = new Axis6.AxisStruct(X+oX, Y+oY, Z+oZ, N, O, A, new double[] { 0, 0, 0, 0, 0, 0 });
                        //double[] angle = Axis6.PosToAngle(LastAngle, X + oX, Y + oY, Z + oZ, N, O, A);
                        //if (angle != null)
                        //{
                        //    LastAngle = angle;
                        //    AngleList.Add(angle);
                        //}
                        break;
                    case 2: break;
                    case 3: break;
                    case 90:
                        Grid = false;
                        break;
                    case 91:
                        Grid = true;
                        break;
                    default:
                        break;
                }
                XnPos = x;
                YnPos = y;
                ZnPos = z;
            }
            //BuildGcode();
        }
    }
}
