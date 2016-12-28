using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeyMove.Tools;
using System.IO.Ports;
using System.IO;

namespace KControl
{
    public partial class MainForm : System.Windows.Forms.Form
    {

        bool Debug = false;
        bool JxsMode = false;
        double X, Y, Z;       //位置
        double N, O, A;       //姿态

        double[] Angle = new double[6];       //角度

        Axis6.AxisStruct FirstValue;
        Axis6.AxisStruct LastValue = new Axis6.AxisStruct();
        Axis6.AxisStruct TempValue = new Axis6.AxisStruct();

        double PosIncValue = 0.01;   //位置增量
        double AngIncValue = 1;   //角度增量

        ProgressBar[] anglebar = new ProgressBar[6];
        TextBox[] angletext = new TextBox[6];
        Panel[] View3Ds = new Panel[2];
        TextBox[] postext = new TextBox[6];

        UartProtocol UP;
        UartProtocol COM_UP,NET_UP;
        UartProtocol HP;
        UartProtocol COM_HP, NET_HP;
        SerialPort UCOM = new SerialPort();
        SerialPort HCOM = new SerialPort();
        bool isLinkUP = false, isLinkHP = false;
        ESPUDP esp = new ESPUDP();

        int saveWidth, saveHeight;

        System.Windows.Forms.Timer ButtonTick = new System.Windows.Forms.Timer();
        Button DestButtonTick;

        List<Axis6.AxisStruct> ListPoint = new List<Axis6.AxisStruct>();
        List<LogicRun.Module> Modules = new List<LogicRun.Module>();
        int ModulesCount = 0;
        ErrorItem EItem = new ErrorItem();
        KeyMove.UI.OutputButton OutputBit;
        KeyMove.UI.InputStats InputBit;
        NumKey touchkey = new NumKey();

        TcpPort ListenerPort = new TcpPort();

        int HPSelectIndex = -1;
        bool UpdatePos = false;

        ButtonMode buttonmode;

        bool isExit = false;

        bool PosResetMode = false;

        GCodeLoader GLoader = new GCodeLoader();

        ConfigIO Config = new ConfigIO();

        byte[] LastIOStats;

        TreeView MainLogic;
        TreeNode SelectNode;
        int SelectNodeIndex;

        Axis6.AxisStruct CachePos;

        enum ButtonMode : int
        {
            Pos = 0,
            Angle = 1,
            IO = 2,
        }
        enum DevID : int
        {
            UP = 0xff50,
            HP = 0xff51,
        }

        enum KeyButton : int
        {
            NUM1 = 0x1,
            NUM2 = 0x2,
            NUM3 = 0x4,
            A = 0x8,
            NUM4 = 0x10,
            NUM5 = 0x20,
            NUM6 = 0x40,
            B = 0x80,
            NUM7 = 0x100,
            NUM8 = 0x200,
            NUM9 = 0x400,
            C = 0x800,
            Star = 0x1000,
            NUM0 = 0x2000,
            Sort = 0x4000,
            D = 0x8000,
            AP1 = 0x10000,
            AP2 = 0x20000,
            AP3 = 0x40000,
            AP4 = 0x80000,
        }

        static class PortInfo {
            public static IOInfo[] sourpoint = new IOInfo[5];
            public static IOInfo[] leftmaxpoint = new IOInfo[3];
            public static IOInfo[] rightmaxpoint = new IOInfo[3];
            public static IOInfo[] errorpoint = new IOInfo[6];
            public static IOInfo[] controlbutton = new IOInfo[4];
        }

        public MainForm()
        {
            this.FormClosed += (object sender, FormClosedEventArgs e) => {
                isExit = true;
                Motor.Enable = false;
                HP.Close();
                UP.Close();
                MessageBox.Show("退出");
                System.Environment.Exit(0);
            };
            //Bitmap b=KControl.Properties.Resources.left;
            InitializeComponent();
            if (!Debug && JxsMode)
            {
                saveWidth = this.Width;
                saveHeight = this.Height;
                this.Width = 600;
                this.Height = 400;
            }
            else
            {
                this.MainTab.Visible = true;
                this.Loading.Visible = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MainTab.Size = Size.Add(Size, new Size(1, 1));
            if (Debug)
                GLoader.Show();
            if (!Debug && JxsMode)
            {
                IntPtr viewid = this.View3DControl.Handle;
                Axis6.Init((bool stats) =>
                {
                    if (stats)
                    {
                        double[] pos = Axis6.DefStruct.getXYZ();
                        X = pos[0];
                        Y = pos[1];
                        Z = pos[2];
                        N = 0;
                        O = 90;
                        A = 0;
                        updateLastStruct();
                        FirstValue = new Axis6.AxisStruct(LastValue);
                        Thread Merge = new Thread(() =>
                        {
                            while (!KeyMove.Tools.MergeWindow.MergeWindowName(viewid, "Axis6 Robot", true))
                                Thread.Sleep(1);
                            KeyMove.Tools.MergeWindow.SetWindowPos((IntPtr)null, -10, -50);
                            KeyMove.Tools.MergeWindow.SetWindowSize((IntPtr)null, 630, 630);
                            Invoke(new MethodInvoker(delegate ()
                            {
                                this.Loading.Visible = false;
                                this.Width = saveWidth;
                                this.Height = saveHeight;
                                this.MainTab.Visible = true;
                                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                                LogicRun.Clear();
                                UpdateDisplay();
                            }));
                        });
                        Merge.Start();
                    }
                    else
                    {
                        Invoke(new MethodInvoker(delegate ()
                        {

                        }));
                    }
                });
            }
            if (!JxsMode)
            {
                MainTab.SelectedIndex = 2;
                MainTab.TabPages.RemoveAt(0);
                MainTab.TabPages.RemoveAt(0);
                LinkHP.Visible = false;
                IPPanle.Visible = false;
                MotorInfoSetting.Visible = false;
                LogicSrcTree.Nodes.Clear();
                TreeNode node = new TreeNode("延时");
                node.SelectedImageIndex = node.ImageIndex = 8;
                LogicSrcTree.Nodes.Add(node);
            }
            anglebar[0] = Bar1;
            anglebar[1] = Bar2;
            anglebar[2] = Bar3;
            anglebar[3] = Bar4;
            anglebar[4] = Bar5;
            anglebar[5] = Bar6;

            angletext[0] = B1;
            angletext[1] = B2;
            angletext[2] = B3;
            angletext[3] = B4;
            angletext[4] = B5;
            angletext[5] = B6;

            postext[0] = XValue;
            postext[1] = YValue;
            postext[2] = ZValue;
            postext[3] = NValue;
            postext[4] = OValue;
            postext[5] = AValue;

            View3Ds[0] = View3D;
            View3Ds[1] = View3D2;

            ButtonTick.Tick += ButtonTick_Tick;

            InitUART();

            Motor.Init(UP);

            OutputBit = new KeyMove.UI.OutputButton(OutputButtonPanel.Controls, new Point(12, 32), new Image[] { KControl.Properties.Resources.outoff, KControl.Properties.Resources.outon }, 24, 3, 8);
            InputBit = new KeyMove.UI.InputStats(InputButtonPanel.Controls, new Point(12, 32), new Image[] { KControl.Properties.Resources.LightOff, KControl.Properties.Resources.LightOn }, 40, 15, 3);
            OutputBit.ButtonSwitch = ClickOutputButton;

            PortInfo.controlbutton[0] = Config.addItem("停止按钮端口 {0}");
            PortInfo.controlbutton[1] = Config.addItem("启动按钮端口 {0}");
            PortInfo.controlbutton[2] = Config.addItem("点动按钮端口 {0}");
            PortInfo.controlbutton[3] = Config.addItem("复位按钮端口 {0}");

            PortInfo.sourpoint[0] = Config.addItem("第一轴对位端口 {0}");
            PortInfo.sourpoint[1] = Config.addItem("第二轴对位端口 {0}");
            PortInfo.sourpoint[2] = Config.addItem("第三轴对位端口 {0}");
            PortInfo.sourpoint[3] = Config.addItem("第四轴对位端口 {0}");
            PortInfo.sourpoint[4] = Config.addItem("第五轴对位端口 {0}");
            PortInfo.leftmaxpoint[0] = Config.addItem("第一轴左极限端口 {0}");
            PortInfo.rightmaxpoint[0] = Config.addItem("第一轴右极限端口 {0}");
            PortInfo.leftmaxpoint[1] = Config.addItem("第二轴左极限端口 {0}");
            PortInfo.rightmaxpoint[1] = Config.addItem("第二轴右极限端口 {0}");
            PortInfo.leftmaxpoint[2] = Config.addItem("第三轴左极限端口 {0}");
            PortInfo.rightmaxpoint[2] = Config.addItem("第三轴右极限端口 {0}");
            for (int i = 0; i < 6; i++)
            {
                PortInfo.errorpoint[i] = Config.addItem("" + i + "号伺服检测端口 {0}");
            }
            Config.load(new Image[] { KControl.Properties.Resources.LightOff, KControl.Properties.Resources.LightOn }, 40);

            System.Windows.Forms.Timer UartFinder = new System.Windows.Forms.Timer();
            UartFinder.Interval = 150;
            UartFinder.Tick += (object s, EventArgs ex) => {
                if (UartFinderSelect != null)
                {
                    if (UartFinderSelect.isLink || FinderCOMIndex >= FinderComName.Length)
                    {


                        if (UartFinderSelect.isLink)
                        {
                            UartFinderSelect = null;
                            FinderCallBack = null;
                            return;
                        }
                        else
                            if (FinderCOMIndex >= FinderComName.Length)
                            if (FinderCallBack != null)
                                FinderCallBack(null, null);
                        UartFinderSelect = null;
                        return;
                    }
                    try
                    {
                        UartFinderSelect.SetCOMPort(FinderComName[FinderCOMIndex++]);
                        UartFinderSelect.SendCmdPacket(UartProtocol.PacketCmd.Alive);
                    }
                    catch { }
                }
            };
            UartFinder.Start();

            System.Windows.Forms.Timer NetUpadate = new System.Windows.Forms.Timer();
            NetUpadate.Interval = 1000;
            NetUpadate.Tick += (object s, EventArgs ex) => {
                if (NetBox.Items.Count > esp.UserGroup.Count)
                    NetBox.Items.Clear();
                foreach(UserInfo info in esp.UserGroup)
                    if(!NetBox.Items.Contains(info))
                        NetBox.Items.Add(info);
            };
            NetUpadate.Start();

            IOPort.Init(UP, (UartProtocol.PacketStats stats, byte[] buff) =>
            {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:

                        Invoke(new MethodInvoker(delegate ()
                        {
                            ByteStream b = new ByteStream(buff);
                            int len = b.ReadByte();
                            len = (len + 7) / 8;
                            InputBit.setStats(b.ReadBuff(len));
                            if (PosResetMode)
                            {
                                byte[] list = InputBit.getStats();

                            }

                        }));
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });
            SelectModule = new LogicRun.Module();
            SelectModule.name = "主模块";
            LogicRun.LogicList = SelectModule.ModelList;
            ModulesAdd(SelectModule);
            ModulesSelect(SelectModule);

        }

        void FindDev(UartProtocol u, Action<object, EventArgs> callback)
        {
            FinderComName = SerialPort.GetPortNames();
            FinderCOMIndex = 0;
            UartFinderSelect = u;
            FinderCallBack = callback;
        }

        void FindClear()
        {
            UartFinderSelect = null;
            FinderCallBack = null;
        }



        void LogicShow()
        {
            LogicTree.Nodes.Clear();
            LogicTreeView.Nodes.Clear();
            foreach (LogicRun.ModeObject obj in SelectModule.ModelList)
            {
                TreeNode Node = new TreeNode(obj.message);
                Node.Tag = obj;
                Node.ToolTipText = obj.ToString();
                LogicTreeView.Nodes.Add(Node);
                Node = new TreeNode(obj.message);
                Node.Tag = obj;
                Node.ToolTipText = obj.ToString();
                LogicTree.Nodes.Add(Node);
            }
        }

        void ModulesSelect(LogicRun.Module m)
        {
            int index = Modules.IndexOf(m);
            int selectindex = Modules.IndexOf(SelectModule);
            if (index != -1) ModuleListView.Nodes[index].BackColor = ModuleList.Nodes[index].BackColor = Color.DodgerBlue; else return;
            if (selectindex != index)
            {
                if (selectindex != -1) ModuleListView.Nodes[selectindex].BackColor = ModuleList.Nodes[selectindex].BackColor = Color.Transparent;
                LogicRun.LogicList = ((LogicRun.Module)m).ModelList;
                LogicShow();
                SelectModule = m;
            }
        }

        void ModuleDel(LogicRun.Module m)
        {
            int index = Modules.IndexOf(m);
            if (index != -1) { ModuleListView.Nodes[index].Remove(); ModuleList.Nodes[index].Remove(); } else return;
            Modules.RemoveAt(index);
            if (Modules.Count == 0)
            {
                ModulesAdd("主模块");
                ModulesSelect(Modules[0]);
            }
        }

        void ModulesAdd(LogicRun.Module m)
        {
            Modules.Add(m);
            ModuleList.Nodes.Add(m.name).Tag = m;
            ModuleListView.Nodes.Add(m.name).Tag = m;
        }
        void ModulesAdd(string name = null)
        {
            var m = new LogicRun.Module();
            if (name != null)
            {
                m.name = name;
            }
            else
            {
                m.name = "新建模块" + ModulesCount++;
            }
            ModulesAdd(m);
        }

        void ClickOutputButton(int id, bool stats)
        {
            if (UP.isLink)
            {
                ByteStream b = new ByteStream(32);
                b.WriteByte(OutputBit.Ecount);
                //b.WriteByte(new Random().Next());
                b.WriteBuff(OutputBit.getStats());
                UP.SendDataPacket(UartProtocol.PacketCmd.SetOutputPort, b.toBytes());
            }
        }

        private void ButtonTick_Tick(object sender, EventArgs e)
        {
            if (DestButtonTick == null)
            {
                ((System.Windows.Forms.Timer)sender).Stop();
                return;
            }
            if (!DestButtonTick.Focused)
            {
                DestButtonTick = null;
                ((System.Windows.Forms.Timer)sender).Stop();
                return;
            }
            ((System.Windows.Forms.Timer)sender).Interval = 100;
            DestButtonTick.PerformClick();
        }

        void SavePosAngle()
        {
            TempValue.X = X;
            TempValue.Y = Y;
            TempValue.Z = Z;
            TempValue.N = N;
            TempValue.O = O;
            TempValue.A = A;
            for (int i = 0; i < Axis6.MaxAxis; i++)
            {
                TempValue.angle[i] = Angle[i];
            }
        }

        void LoadPosAngle()
        {
            X = TempValue.X;
            Y = TempValue.Y;
            Z = TempValue.Z;
            N = TempValue.N;
            O = TempValue.O;
            A = TempValue.A;
            for (int i = 0; i < Axis6.MaxAxis; i++)
            {
                Angle[i] = TempValue.angle[i];
            }
        }

        void updateLastStruct()
        {
            LastValue.X = X;
            LastValue.Y = Y;
            LastValue.Z = Z;
            LastValue.N = N;
            LastValue.O = O;
            LastValue.A = A;
        }

        void SetPosAngle(Axis6.AxisStruct st)
        {
            X = st.X;
            Y = st.Y;
            Z = st.Z;
            N = st.N;
            O = st.O;
            A = st.A;
            for (int i = 0; i < Axis6.MaxAxis; i++)
                Angle[i] = st.angle[i];
        }

        double valueof(double value, double min, double max)
        {
            double v = max - min;
            if (value > max) return 100;
            if (value < min) return 0;
            v /= 100;
            return (value + Math.Abs(min)) / v;
        }

        private void ResetPosAngle(object sender, EventArgs e)
        {
            try
            {
                SetPosAngle(FirstValue);
                UpdateDisplay();
                Axis6.MoveToAngle(FirstValue.angle);
            }
            catch { }
        }

        private void KeyInput(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!(e.KeyChar >= '0' && e.KeyChar <= '9'))
                {
                    if (e.KeyChar == (char)Keys.Enter)
                    {
                        SavePosAngle();
                        double[] v = UpdatePosIn();
                        double[] ag = Axis6.MoveToPos(v[0], v[1], v[2], v[3], v[4], v[5]);
                        if (ag == null) { ErrorItem.ErrorEffect((TextBox)sender); return; }
                        if (!Axis6.AngleSoftMax(ag)) { ErrorItem.ErrorEffect((TextBox)sender); return; }
                        X = v[0];
                        Y = v[1];
                        Z = v[2];
                        N = v[3];
                        O = v[4];
                        A = v[5];
                        Angle = ag;
                        UpdateDisplay();
                    }
                    if (e.KeyChar == '.')
                    {
                        if (((TextBox)sender).Text.IndexOf('.') == -1 || ((TextBox)sender).SelectedText.IndexOf('.') != -1)
                            return;
                        e.Handled = true;
                    }
                    else if (e.KeyChar == '-')
                    {
                        if (((TextBox)sender).Text.IndexOf('-') == -1 && ((TextBox)sender).SelectedText.IndexOf('-') == -1)
                            return;
                        e.Handled = true;
                    }
                    else
                       if (e.KeyChar != 8)
                        e.Handled = true;
                }
            }
            catch { }
        }

        private void AngleInput(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (((TextBox)sender).ReadOnly) return;
                if (!(e.KeyChar >= '0' && e.KeyChar <= '9'))
                {
                    if (e.KeyChar == (char)Keys.Enter)
                    {
                        e.Handled = true;
                        SavePosAngle();
                        double[] v = UpdateAagleIn();
                        if (!Axis6.AngleSoftMax(v)) { ErrorItem.ErrorEffect((TextBox)sender); UpdateDisplay(); return; }
                        Angle = v;
                        v = Axis6.MoveToAngle(v);
                        X = v[0];
                        Y = v[1];
                        Z = v[2];
                        UpdateDisplay();
                    }
                    if (e.KeyChar == '.')
                    {
                        if (((TextBox)sender).Text.IndexOf('.') == -1 || ((TextBox)sender).SelectedText.IndexOf('.') != -1)
                            return;
                        e.Handled = true;
                    }
                    else if (e.KeyChar == '-')
                    {
                        if (((TextBox)sender).Text.IndexOf('-') == -1 && ((TextBox)sender).SelectedText.IndexOf('-') == -1)
                            return;
                        e.Handled = true;
                    }
                    else
                       if (e.KeyChar != 8)
                        e.Handled = true;
                }
            }
            catch { }
        }

        private void IntInput(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!(e.KeyChar >= '0' && e.KeyChar <= '9'))
                {
                    if (e.KeyChar == '-')
                    {
                        if (((TextBox)sender).Text.IndexOf('-') == -1 && ((TextBox)sender).SelectedText.IndexOf('-') == -1)
                            return;
                        e.Handled = true;
                    }
                    else
                       if (e.KeyChar != 8)
                        e.Handled = true;
                }
            }
            catch { }
        }

        private void FloatInput(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!(e.KeyChar >= '0' && e.KeyChar <= '9'))
                {
                    if (e.KeyChar == '.')
                    {
                        if (((TextBox)sender).Text.IndexOf('.') == -1 || ((TextBox)sender).SelectedText.IndexOf('.') != -1)
                            return;
                        e.Handled = true;
                    }
                    else if (e.KeyChar == '-')
                    {
                        if (((TextBox)sender).Text.IndexOf('-') == -1 && ((TextBox)sender).SelectedText.IndexOf('-') == -1)
                            return;
                        e.Handled = true;
                    }
                    else
                       if (e.KeyChar != 8)
                        e.Handled = true;
                }
            }
            catch { }
        }

        private void ShiftMove(object sender, MouseEventArgs e)
        {
            Button b = ((Button)sender);
            DestButtonTick = b;
            ButtonTick.Interval = 1500;
            ButtonTick.Start();
        }

        private void ShiftUp(object sender, MouseEventArgs e)
        {
            DestButtonTick = null;
            ButtonTick.Stop();
        }

        private void AngleClick(object sender, EventArgs e)
        {
            try
            {
                string tag = (string)((Button)sender).Tag;
                int index = tag[1] - '1';
                if (tag[0] == '+')
                    Angle[index] += AngIncValue;
                else
                    Angle[index] -= AngIncValue;

                if (PosResetMode)
                {
                    HPSelectIndex = index;
                    SelectAngleText(HPSelectIndex);
                    return;
                }

                if (!Axis6.AngleSoftMax(Angle))
                {
                    if (e == EventArgs.Empty)
                    {
                        DestButtonTick = null;
                        ButtonTick.Stop();
                    }
                    ErrorItem.ErrorEffect(angletext[index]);
                }
                Angle = Axis6.AngleSoftSet(Angle);
                double[] pos = Axis6.MoveToAngle(Angle);
                X = pos[0];
                Y = pos[1];
                Z = pos[2];
                UpdateDisplay();
                if (MoveTogg.Checked)
                {
                    if (Motor.Enable && Motor.IsReady)
                    {
                        Motor.MotorRun(Angle);
                    }
                }
            }
            catch { }
        }

        private void ChangeTab(object sender, EventArgs e)
        {
            int index = ((TabControl)sender).SelectedIndex;
            if (index < View3Ds.Length)
            {
                if (!View3Ds[index].Controls.Contains(View3DControl))
                    View3Ds[index].Controls.Add(View3DControl);
            }
            SetInfo.Text = "";
            if (index == 1)
            {

            }
            List<TreeNode> NodeList = new List<TreeNode>();
            TreeNode Node;
            switch (index)
            {
                case 0:

                    break;
                case 1:
                    MainLogic = LogicTree;
                    SavePoint.Nodes.Clear();
                    foreach (TreeNode obj in LocalPoint.Nodes)
                    {
                        Node = (TreeNode)obj.Clone();
                        SavePoint.Nodes.Add(Node);
                    }
                    break;
                case 2:
                    MainLogic = LogicTreeView;
                    break;
            }
        }

        private void PosAngleSetInput(object sender, KeyPressEventArgs e)
        {
            if (!(e.KeyChar >= '0' && e.KeyChar <= '9'))
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    try
                    {
                        double v = double.Parse(((TextBox)sender).Text);
                        if (((string)((TextBox)sender).Tag)[0] == 'P') {
                            PosIncValue = v / 1000;
                        }
                        else
                        {
                            AngIncValue = v;
                        }
                    }
                    catch
                    {
                        ErrorItem.ErrorEffect((TextBox)sender);
                        return;
                    }
                }
                if (e.KeyChar == '.')
                {
                    if (((TextBox)sender).Text.IndexOf('.') == -1 || ((TextBox)sender).SelectedText.IndexOf('.') != -1)
                        return;
                    e.Handled = true;
                }
                else if (e.KeyChar == '-')
                {
                    if (((TextBox)sender).Text.IndexOf('-') == -1 && ((TextBox)sender).SelectedText.IndexOf('-') == -1)
                        return;
                    e.Handled = true;
                }
                else
                   if (e.KeyChar != 8)
                    e.Handled = true;
            }
        }

        string doublevalue(double value, int len = 8)
        {
            string v = value.ToString();
            return v.Length > len ? double.Parse(value.ToString("f" + len)).ToString() : v;
        }

        private void UpdateDisplay()
        {
            XValue.Text = doublevalue(X * 1000);
            YValue.Text = doublevalue(Y * 1000);
            ZValue.Text = doublevalue(Z * 1000);
            NValue.Text = doublevalue(N);
            OValue.Text = doublevalue(O);
            AValue.Text = doublevalue(A);

            for (int i = 0; i < 6; i++)
            {
                angletext[i].Text = doublevalue(Angle[i]) + '°';
                anglebar[i].Value = (int)valueof(Angle[i], Axis6.SoftMax[i][1], Axis6.SoftMax[i][0]);
            }

        }

        double[] UpdatePosIn()
        {
            double[] v = new double[6];
            v[0] = double.Parse(XValue.Text) / 1000;
            v[1] = double.Parse(YValue.Text) / 1000;
            v[2] = double.Parse(ZValue.Text) / 1000;
            v[3] = double.Parse(NValue.Text);
            v[4] = double.Parse(OValue.Text);
            v[5] = double.Parse(AValue.Text);
            return v;
        }

        double[] UpdateAagleIn()
        {
            double[] ag = new double[6];
            for (int i = 0; i < 6; i++)
            {
                string text = angletext[i].Text;
                int index = text.IndexOf('°');
                string num;
                if (index != -1)
                    num = text.Substring(0, index);
                else
                    num = text;

                ag[i] = double.Parse(num);
            }
            return ag;
        }

        private void PosClick(object sender, EventArgs e)
        {
            try
            {
                string button = ((Button)sender).Text;
                bool tag = ((string)((Button)sender).Tag)[0] == '+';
                if (PosResetMode)
                {
                    if (button == string.Empty)
                    {
                        ByteStream b = new ByteStream(10);
                        b.WriteByte(HPSelectIndex);
                        b.WriteByte(tag ? 1 : 0);
                        b.WriteByte(Motor.offsetdir[HPSelectIndex]);
                        int v = (int)(360F / Motor.scaleAngle[HPSelectIndex]);
                        b.WriteDWord(v / 2);
                        b.WriteWord((v / 100) > 10000 ? 10000 : (v / 100));
                        //b.WriteWord()
                        UP.SendDataPacket(UartProtocol.PacketCmd.SetPosSour, b.toBytes());
                    }
                    return;
                }
                SavePosAngle();
                if (button == string.Empty)
                {
                    if (tag)
                    {
                        O += AngIncValue;
                    }
                    else
                    {
                        O -= AngIncValue;
                    }
                    double[] st = Axis6.PosToAngle(X, Y, Z, N, O, A);
                    if (st != null && Axis6.AngleSoftMax(st))
                    {
                        Angle = st;
                        Axis6.MoveToAngle(st);
                    }
                    else
                    {
                        if (e == EventArgs.Empty)
                        {
                            ShiftUp(null, null);
                        }
                        ErrorItem.ErrorEffect((TextBox)OValue);
                        LoadPosAngle();
                    }

                }
                else
                {
                    switch (button[0])
                    {
                        case 'X':
                            if (tag)
                            {
                                X += PosIncValue;
                            }
                            else
                            {
                                X -= PosIncValue;
                            }
                            break;
                        case 'Y':
                            if (tag)
                            {
                                Y += PosIncValue;
                            }
                            else
                            {
                                Y -= PosIncValue;
                            }
                            break;
                        case 'Z':
                            if (tag)
                            {
                                Z += PosIncValue;
                            }
                            else
                            {
                                Z -= PosIncValue;
                            }
                            break;
                    }
                    double[] st = Axis6.PosToAngle(X, Y, Z, N, O, A);
                    if (st != null && Axis6.AngleSoftMax(st))
                    {
                        Angle = st;
                        Axis6.MoveToAngle(st);
                    }
                    else
                    {
                        switch (button[0])
                        {
                            case 'X':
                                ErrorItem.ErrorEffect((TextBox)XValue);
                                break;
                            case 'Y':
                                ErrorItem.ErrorEffect((TextBox)YValue);
                                break;
                            case 'Z':
                                ErrorItem.ErrorEffect((TextBox)ZValue);
                                break;
                        }
                        if (e == EventArgs.Empty)
                        {
                            ShiftUp(null, null);
                        }
                        LoadPosAngle();
                    }
                }
                if (MoveTogg.Checked)
                {
                    if (Motor.Enable && Motor.IsReady)
                    {
                        Motor.MotorRun(Angle);
                    }
                }
                //KeyMove.Tools.MergeWindow.SetWindowPos((IntPtr)null, (int)X, (int)Y);
                UpdateDisplay();
            }
            catch { }
        }

        private void Stop(object sender, EventArgs e)
        {
            Motor.MotorStop();
        }


        void sendXYZNOAHPData()
        {
            ByteStream b = new ByteStream(32);
            if (HP.isLink)
            {
                b.WriteByte(1);
                b.WriteWord((int)(X * 1000) % 1000);
                b.WriteWord((int)(Math.Abs(X) * 1000000 % 1000));
                b.WriteWord((int)(Y * 1000) % 1000);
                b.WriteWord((int)(Math.Abs(Y) * 1000000 % 1000));
                b.WriteWord((int)(Z * 1000) % 1000);
                b.WriteWord((int)(Math.Abs(Z) * 1000000 % 1000));
                b.WriteWord((int)N);
                b.WriteWord((int)N * 1000 % 1000);
                b.WriteWord((int)O);
                b.WriteWord((int)O * 1000 % 1000);
                b.WriteWord((int)A);
                b.WriteWord((int)A * 1000 % 1000);
                b.WriteWord((int)(PosIncValue * 1000));
                b.WriteWord((int)(PosIncValue * 1000 % 1 * 100));
                b.WriteByte(Motor.Frequency / 1000);
                b.WriteByte(HPModeControl);
                HP.SendDataPacket(UartProtocol.PacketCmd.HPData, b.toBytes());
            }
        }

        void sendAxisHPData()
        {
            ByteStream b = new ByteStream(32);
            if (HP.isLink)
            {
                b.WriteByte(0);
                for (int i = 0; i < 6; i++)
                {
                    b.WriteWord((int)(Angle[i]));
                    b.WriteWord((int)(Angle[i] * 1000) % 1000);
                }
                b.WriteWord((int)(AngIncValue));
                b.WriteWord((int)(AngIncValue * 100) % 100);
                b.WriteByte(Motor.Frequency / 1000);
                b.WriteByte(HPSelectIndex);
                HP.SendDataPacket(UartProtocol.PacketCmd.HPData, b.toBytes());
            }
        }

        void sendPortHPData()
        {
            ByteStream b = new ByteStream(32);
            if (HP.isLink)
            {
                b.WriteByte(2);
                byte[] data;
                int val = 0;
                data = InputBit.getStats();
                if (InputBit.Ecount < 23)
                {
                    b.WriteDWord(0);
                }
                else {
                    val = data[2];
                    val <<= 8;
                    val |= data[1];
                    val <<= 8;
                    val |= data[0];
                    b.WriteDWord(val);
                }
                data = OutputBit.getStats();
                b.WriteByte(data[0]);
            }
            HP.SendDataPacket(UartProtocol.PacketCmd.HPData, b.toBytes());
        }

        void InitUART()
        {
            HCOM.BaudRate = UCOM.BaudRate = 115200;
            HCOM.DataBits = UCOM.DataBits = 8;
            HCOM.StopBits = UCOM.StopBits = StopBits.One;
            HCOM.Parity = UCOM.Parity = Parity.None;

            COM_UP = UP = new UartProtocol(UCOM);
            COM_HP = HP = new UartProtocol(HCOM);

            UP.RegisterCmdEvent(UartProtocol.PacketCmd.LoadCode, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        ByteStream bs = new ByteStream(buff);
                        ByteStream b;
                        if (UploadFlag)
                            switch (bs.ReadByte())
                            {
                                case 2:
                                    UploadSize = bs.ReadWord();
                                    if (UploadSize == 0)
                                    {
                                        UploadFlag = false;
                                        Invoke(new MethodInvoker(delegate ()
                                        {
                                            MessageBox.Show("程序为空!");
                                        }));
                                    }
                                    UploadBuff = new byte[UploadSize];
                                    b = new ByteStream(3);
                                    b.WriteByte(3);
                                    b.WriteWord(UploadPos);
                                    UP.SendDataPacket(UartProtocol.PacketCmd.LoadCode, b.toBytes());
                                    break;
                                case 3:
                                    UploadPos = bs.ReadWord();
                                    int size = bs.ReadByte();
                                    byte[] data = bs.ReadBuff(size);
                                    for (int i = 0; i < size; i++)
                                    {
                                        UploadBuff[UploadPos + i] = data[i];
                                    }
                                    UploadPos += size;
                                    if (UploadPos < UploadSize)
                                    {
                                        b = new ByteStream(3);
                                        b.WriteByte(3);
                                        b.WriteWord(UploadPos);
                                        UP.SendDataPacket(UartProtocol.PacketCmd.LoadCode, b.toBytes());
                                    }
                                    else
                                    {
                                        UploadFlag = false;
                                        Invoke(new MethodInvoker(delegate ()
                                        {
                                            LogicTreeView.Nodes.Clear();
                                            LogicTree.Nodes.Clear();
                                            LogicRun.Clear();
                                            SelectClear();
                                            foreach (LogicRun.ModeObject obj in BuildCode.BytesToCode(UploadBuff))
                                            {
                                                AddLogic(obj);
                                            }
                                        }));
                                    }
                                    break;

                            }
                        break;
                }
            });

            UP.RegisterCmdEvent(UartProtocol.PacketCmd.Alive, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        Invoke(new MethodInvoker(delegate ()
                        {
                            ByteStream b = new ByteStream(buff);
                            if (buff.Length >= 2)
                                if (b.ReadWord() == (int)DevID.UP)
                                {

                                    StringBuilder sb = new StringBuilder();
                                    this.LinkUP.Text = "断开设备";
                                    for (int i = 0; i < Motor.offsetAngle.Length; i++)
                                        Motor.offsetAngle[i] = 0;
                                    this.isLinkUP = true;
                                    //Motor.IsReady = true;
                                    Motor.Enable = true;
                                    Motor.SetInfo(Motor.StartSpeed, Motor.Frequency, Motor.SpeedTime);
                                    IOPort.OutputCount = b.ReadByte();
                                    IOPort.InputCount = b.ReadByte();
                                    int pwm = b.ReadByte();
                                    sb.AppendLine("设备已连接!");
                                    sb.AppendLine("输出端口数量:" + IOPort.OutputCount);
                                    sb.AppendLine("输入端口数量:" + IOPort.InputCount);
                                    sb.AppendLine("脉冲输出端口数量:" + pwm);
                                    sb.AppendLine("脉冲数量偏移");
                                    if (UseMotorOffset.Checked)
                                    {
                                        for (int i = 0; i < pwm; i++)
                                        {
                                            Motor.plsoffset[i] = b.ReadDWorde();
                                            sb.AppendLine("第" + (i + 1) + "轴:" + Motor.plsoffset[i]);
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < pwm; i++)
                                        {
                                            Motor.plsoffset[i] = 0;
                                            sb.AppendLine("第" + (i + 1) + "轴:" + Motor.plsoffset[i]);
                                        }
                                    }
                                    OutputBit.EnableCount = IOPort.OutputCount;
                                    InputBit.EnableCount = IOPort.InputCount;
                                    Motor.SetInfo();
                                    DevInfo.Text = sb.ToString();
                                }
                                else
                                {
                                    UP.Timeout();
                                }
                        }));
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        UCOM.Close();
                        break;
                }
            });

            HP.RegisterCmdEvent(UartProtocol.PacketCmd.Alive, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        ByteStream b = new ByteStream(buff);
                        if (b.ReadWord() == (int)DevID.HP)
                        {
                            Invoke(new MethodInvoker(delegate ()
                            {
                                this.LinkHP.Text = "断开手轮";
                                this.isLinkHP = true;
                            }));
                        }
                        else
                        {
                            HP.Timeout();
                        }
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        HCOM.Close();
                        break;
                }
            });

            UP.RegisterCmdEvent(UartProtocol.PacketCmd.SetPosSour, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        Invoke(new MethodInvoker(delegate ()
                        {
                            if (buff[0] == 0)
                            {
                                //MessageBox.Show("第"+buff[0]+"对位失败");
                            }
                            //DevInfo.Text += "Stop\r\n";
                        }));
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });

            HP.RegisterCmdEvent(UartProtocol.PacketCmd.KeyControl, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        ByteStream b = new ByteStream(buff);
                        int v = b.ReadDWorde();
                        Invoke(new MethodInvoker(delegate ()
                        {
                            //DevInfo.Text += ((KeyButton)(1 << i)).ToString()+"\r\n";
                            if (v == 0) {
                                if (buttonmode != ButtonMode.Angle)
                                    HPSelectIndex = -1;
                                return;
                            }
                            ByteStream bs = new ByteStream(32);
                            switch (v)
                            {
                                case (int)KeyButton.AP1: buttonmode = ButtonMode.Angle; SelectAngleText(HPSelectIndex);
                                    sendAxisHPData();
                                    break;
                                case (int)KeyButton.AP2: buttonmode = ButtonMode.Pos; SelectAngleText(-1);
                                    sendXYZNOAHPData();
                                    break;
                                case (int)KeyButton.AP3: //AddPointSave.PerformClick();
                                    SelectAngleText(-1);
                                    AddPosClick(null, null);
                                    break;
                                case (int)KeyButton.AP4: buttonmode = ButtonMode.IO; SelectAngleText(-1);
                                    sendPortHPData();
                                    break;
                            }
                            int posmode = 0;
                            switch (buttonmode)
                            {
                                case ButtonMode.Pos:
                                    SavePosAngle();
                                    if (!Motor.Enable) posmode = 1;
                                    else if (Motor.IsReady) posmode = 2;
                                    else if (CachePos == null) posmode = 3;
                                    if (posmode != 0)
                                    {
                                        SavePosAngle();
                                        switch (v)
                                        {
                                            case (int)KeyButton.NUM1: HPSelectIndex = 0; Y += PosIncValue; X += PosIncValue; break;
                                            case (int)KeyButton.NUM2: HPSelectIndex = 1; X += PosIncValue; break;
                                            case (int)KeyButton.NUM3: HPSelectIndex = 2; X += PosIncValue; Y -= PosIncValue; break;
                                            case (int)KeyButton.NUM4: HPSelectIndex = 3; Y += PosIncValue; break;
                                            case (int)KeyButton.NUM5:
                                                if (HPModeControl == -1)
                                                    HPModeControl = 3;
                                                else if (HPModeControl < 5)
                                                    HPModeControl++;
                                                else HPModeControl = -1;
                                                break;
                                            case (int)KeyButton.NUM6: HPSelectIndex = 5; Y -= PosIncValue; break;
                                            case (int)KeyButton.NUM7: HPSelectIndex = 6; X -= PosIncValue; Y += PosIncValue; break;
                                            case (int)KeyButton.NUM8: HPSelectIndex = 7; X -= PosIncValue; break;
                                            case (int)KeyButton.NUM9: HPSelectIndex = 8; X -= PosIncValue; Y -= PosIncValue; break;
                                            case (int)KeyButton.Star: Z += PosIncValue; break;
                                            case (int)KeyButton.NUM0: break;
                                            case (int)KeyButton.Sort: Z -= PosIncValue; break;
                                            case (int)KeyButton.A: if (PosIncValue < 0.1) PosIncValue *= 10; break;
                                            case (int)KeyButton.B: if (PosIncValue > 0.0001) PosIncValue /= 10; break;
                                            case (int)KeyButton.C:

                                                if (HPModeControl != -1)
                                                {
                                                    switch (HPModeControl)
                                                    {
                                                        case 3:
                                                            N += AngIncValue;
                                                            break;
                                                        case 4:
                                                            O += AngIncValue;
                                                            break;
                                                        case 5:
                                                            A += AngIncValue;
                                                            break;
                                                    }
                                                    UpdateDisplay();
                                                    UpdatePos = true;
                                                    SelectPosText(HPModeControl);
                                                }
                                                else if
                                                (Motor.Frequency < 15000)
                                                    Motor.Frequency += 1000; Motor.SetInfo();
                                                break;
                                            case (int)KeyButton.D:
                                                if (HPModeControl != -1)
                                                {
                                                    switch (HPModeControl)
                                                    {
                                                        case 3:
                                                            N -= AngIncValue;
                                                            break;
                                                        case 4:
                                                            O -= AngIncValue;
                                                            break;
                                                        case 5:
                                                            A -= AngIncValue;
                                                            break;
                                                    }
                                                    UpdateDisplay();
                                                    UpdatePos = true;
                                                    SelectPosText(HPModeControl);
                                                }
                                                else if
                                                    (Motor.Frequency > 2000) Motor.Frequency -= 1000; Motor.SetInfo();
                                                break;
                                        }

                                        UpdateDisplay();
                                        UpdatePos = true;
                                        //bs.WriteByte(4);
                                        //bs.WriteByte(1);
                                        //bs.WriteDWord((int)(X * 1000000));
                                        //bs.WriteDWord((int)(Y * 1000000));
                                        //bs.WriteDWord((int)(Z * 1000000));
                                        //bs.WriteDWord((int)(N * 1000));
                                        //bs.WriteDWord((int)(O * 1000));
                                        //bs.WriteDWord((int)(A * 1000));
                                        sendXYZNOAHPData();
                                        if (posmode == 3)
                                            CachePos = new Axis6.AxisStruct(X, Y, Z, N, O, A, Angle);
                                    }
                                    break;
                                case ButtonMode.IO:
                                    switch (v)
                                    {
                                        case (int)KeyButton.NUM1: OutputBit.ClickButtion(0); break;
                                        case (int)KeyButton.NUM2: OutputBit.ClickButtion(1); break;
                                        case (int)KeyButton.NUM3: OutputBit.ClickButtion(2); break;
                                        case (int)KeyButton.NUM4: OutputBit.ClickButtion(3); break;
                                        case (int)KeyButton.NUM5: OutputBit.ClickButtion(4); break;
                                        case (int)KeyButton.NUM6: OutputBit.ClickButtion(5); break;
                                        case (int)KeyButton.NUM7: OutputBit.ClickButtion(6); break;
                                        case (int)KeyButton.NUM8: OutputBit.ClickButtion(7); break;
                                    }
                                    sendPortHPData();
                                    break;
                                case ButtonMode.Angle:
                                    if (!Motor.Enable || (Motor.IsReady && CachePos == null))
                                    {
                                        switch (v)
                                        {
                                            case (int)KeyButton.NUM1: Angle[HPSelectIndex = 0] += AngIncValue; break;
                                            case (int)KeyButton.NUM2: Angle[HPSelectIndex = 1] += AngIncValue; break;
                                            case (int)KeyButton.NUM3: Angle[HPSelectIndex = 2] += AngIncValue; break;
                                            case (int)KeyButton.NUM4: Angle[HPSelectIndex = 0] -= AngIncValue; break;
                                            case (int)KeyButton.NUM5: Angle[HPSelectIndex = 1] -= AngIncValue; break;
                                            case (int)KeyButton.NUM6: Angle[HPSelectIndex = 2] -= AngIncValue; break;
                                            case (int)KeyButton.NUM7: Angle[HPSelectIndex = 3] += AngIncValue; break;
                                            case (int)KeyButton.NUM8: Angle[HPSelectIndex = 4] += AngIncValue; break;
                                            case (int)KeyButton.NUM9: Angle[HPSelectIndex = 5] += AngIncValue; break;
                                            case (int)KeyButton.Star: Angle[HPSelectIndex = 3] -= AngIncValue; break;
                                            case (int)KeyButton.NUM0: Angle[HPSelectIndex = 4] -= AngIncValue; break;
                                            case (int)KeyButton.Sort: Angle[HPSelectIndex = 5] -= AngIncValue; break;
                                            case (int)KeyButton.A: if (AngIncValue < 10) AngIncValue++; break;
                                            case (int)KeyButton.B:
                                                if (AngIncValue > 1) AngIncValue--; break;
                                            case (int)KeyButton.C: if (Motor.Frequency < 15000) Motor.Frequency += 1000; Motor.SetInfo(); break;
                                            case (int)KeyButton.D:
                                                if (Motor.Frequency > 1000) Motor.Frequency -= 1000; Motor.SetInfo(); break;

                                        }
                                        UpdateDisplay();
                                        UpdatePos = true;
                                        sendAxisHPData();
                                    }
                                    break;
                            }
                            //switch (v)
                            //{
                            //    case (int)KeyButton.NUM1: HPSelectIndex = 0; if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(0); break;
                            //    case (int)KeyButton.NUM2: HPSelectIndex = 1; if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(1); break;
                            //    case (int)KeyButton.NUM3: HPSelectIndex = 2; if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(2); break;
                            //    case (int)KeyButton.NUM4: HPSelectIndex = 3; if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(3); break;
                            //    case (int)KeyButton.NUM5: HPSelectIndex = 4; if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(4); break;
                            //    case (int)KeyButton.NUM6: HPSelectIndex = 5; if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(5); break;
                            //    case (int)KeyButton.NUM7: if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(6); break;
                            //    case (int)KeyButton.NUM8: if (buttonmode == ButtonMode.IO) OutputBit.ClickButtion(7); break;
                            //    case (int)KeyButton.A: buttonmode = ButtonMode.Angle; break;
                            //    case (int)KeyButton.B: buttonmode = ButtonMode.Pos; break;
                            //    case (int)KeyButton.C: AddPointSave.PerformClick(); return;
                            //    case (int)KeyButton.D: buttonmode = ButtonMode.IO; return;
                            //}
                            switch (buttonmode)
                            {
                                case ButtonMode.Angle: SelectAngleText(HPSelectIndex); break;
                                    //case ButtonMode.Pos: SelectPosText(HPSelectIndex); break;
                            }
                        }));
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });

            HP.RegisterCmdEvent(UartProtocol.PacketCmd.ButtonControl, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        Invoke(new MethodInvoker(delegate ()
                        {
                            int v = buff[0];
                            if ((v & 0x80) != 0)
                            {
                                v -= 256;
                            }
                            if (HPSelectIndex == -1) return;

                            if (PosResetMode)
                            {
                                ByteStream b = new ByteStream(10);
                                b.WriteByte(HPSelectIndex);
                                b.WriteByte(v > 0 ? 1 : -1);
                                UP.SendDataPacket(UartProtocol.PacketCmd.SetPosSour, b.toBytes());
                                return;
                            }

                            switch (buttonmode)
                            {
                                case ButtonMode.Angle:
                                    SavePosAngle();
                                    Angle[HPSelectIndex] += AngIncValue * v;
                                    if (!Axis6.AngleSoftMax(Angle))
                                    {
                                        ErrorItem.ErrorEffect(angletext[HPSelectIndex]);
                                        LoadPosAngle();
                                        return;
                                    }
                                    //Angle = Axis6.AngleSoftSet(Angle);
                                    UpdateDisplay();
                                    break;
                                case ButtonMode.Pos:
                                    switch (HPSelectIndex)
                                    {
                                        case 0: X += PosIncValue * v; break;
                                        case 1: Y += PosIncValue * v; break;
                                        case 2: Z += PosIncValue * v; break;
                                        case 3: N += AngIncValue * v; break;
                                        case 4: O += AngIncValue * v; break;
                                        case 5: A += AngIncValue * v; break;
                                    }
                                    //RotValueList.Add(v);
                                    //DevInfo.Text += "Roll " + v + "\r\n";
                                    UpdateDisplay();
                                    break;
                            }
                            UpdatePos = true;
                        }));
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });
            HP.RegisterCmdEvent(UartProtocol.PacketCmd.StopControl, (UartProtocol.PacketStats stats, byte[] buff) => {
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        Invoke(new MethodInvoker(delegate ()
                        {
                            if (LogicRun.IsRun)
                                StartStopLogic(RunStop, null);
                            Motor.MotorFastStop();
                            Motor.isReady = 0;
                            //DevInfo.Text += "Stop\r\n";
                        }));
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        break;
                }
            });

            UP.RegisterCmdEvent(UartProtocol.PacketCmd.WriteData, new UartProtocol.UartFunction((UartProtocol.PacketStats stats, byte[] buff) => {
                ByteStream b = new ByteStream(100);
                switch (stats)
                {
                    case UartProtocol.PacketStats.RecvOK:
                        if (buff[0] == 0)
                        {
                            if ((CodeDataPos % 1024) == 0)
                            {
                                b.WriteByte(1);
                                b.WriteWord(CodeDataPos);
                                UP.SendDataPacket(UartProtocol.PacketCmd.WriteData, b.toBytes());
                                break;
                            }
                        }
                        int len = CodeLen;
                        if (len > 32) len = 32;
                        if (CodeLen == 0)
                        {
                            UP.SendCmdPacket(UartProtocol.PacketCmd.LoadCode);
                            //isRun = false;
                            MessageBox.Show("下载成功");
                            return;
                        }
                        CodeLen -= len;
                        b.WriteByte(0);
                        b.WriteWord(CodeDataPos);
                        b.WriteByte(len);
                        b.WriteBuff(CodeData, CodeDataPos, len);
                        CodeDataPos += len;
                        UP.SendDataPacket(UartProtocol.PacketCmd.WriteData, b.toBytes());
                        break;
                    case UartProtocol.PacketStats.RecvError:
                        //isRun = false;
                        break;
                    case UartProtocol.PacketStats.RecvTimeOut:
                        //isRun = false;
                        MessageBox.Show("下载失败");
                        break;
                }
            }));

            new Task(() =>
            {
                while (!isExit)
                    if (UpdatePos)
                    {
                        Invoke(new MethodInvoker(delegate ()
                        {
                            try
                            {
                                if (CachePos != null && !Motor.IsReady) return;
                                UpdatePos = false;
                                switch (buttonmode)
                                {
                                    case ButtonMode.Angle:
                                        if (HPSelectIndex != -1)
                                            if (Axis6.AngleSoftMax(Angle))
                                            {
                                                double[] pos = Axis6.MoveToAngle(Angle);
                                                X = pos[0];
                                                Y = pos[1];
                                                Z = pos[2];
                                                Invoke(new MethodInvoker(delegate ()
                                                {
                                                    UpdateDisplay();
                                                }));
                                                if (MoveTogg.Checked)
                                                {
                                                    Motor.MotorRun(Angle);
                                                }
                                            }
                                        break;
                                    case ButtonMode.Pos:
                                        //SavePosAngle();
                                        //double[] v = UpdatePosIn();
                                        double x, y, z, n, o, a;
                                        if (CachePos != null)
                                        {
                                            x = CachePos.X;
                                            y = CachePos.Y;
                                            z = CachePos.Z;
                                            n = CachePos.N;
                                            o = CachePos.O;
                                            a = CachePos.A;
                                            CachePos = null;
                                        }
                                        else
                                        {
                                            x = X;
                                            y = Y;
                                            z = Z;
                                            n = N;
                                            o = O;
                                            a = A;
                                        }
                                        double[] ag = Axis6.MoveToPos(x, y, z, n, o, a);
                                        if (ag == null) { return; }
                                        if (!Axis6.AngleSoftMax(ag)) { return; }
                                        Angle = ag;
                                        UpdateDisplay();
                                        //KeyInput(postext[HPSelectIndex], new KeyPressEventArgs((char)Keys.Enter));
                                        if (MoveTogg.Checked)
                                        {
                                            //double[] ax = Axis6.PosToAngle(Angle, X, Y, Z, N, O, A);
                                            //if (ax != null)
                                            Motor.MotorRun(ag);
                                        }
                                        break;
                                    case ButtonMode.IO:
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }
                        }));
                        Thread.Sleep(1);
                    }
                    else
                        Thread.Sleep(20);
            }).Start();
            new Task(() =>
            {
                while (!isExit)
                {
                    Invoke(new MethodInvoker(delegate ()
                    {
                        if (UP.isLink && UP.isIdle && !LogicRun.IsRun)
                        {
                            //UP.SendCmdPacket(UartProtocol.PacketCmd.GetInputPort);
                            UP.SendDataPacket(UartProtocol.PacketCmd.GetInputPort, new byte[] { 0 });
                            //if (isExit)
                            //{
                            //    ByteStream b = new ByteStream(10);
                            //    b.WriteByte(0);
                            //    b.WriteByte(0xff);
                            //    b.WriteByte(0x02);
                            //    UP.SendDataPacket(UartProtocol.PacketCmd.GetInputPort,b.toBytes());
                            //}
                        }
                    }));

                    Thread.Sleep(100);
                }
            }).Start();
        }

        void SelectPosText(int index)
        {
            for (int i = 0; i < postext.Length; i++) postext[i].BackColor = Color.White;
            for (int i = 0; i < angletext.Length; i++) angletext[i].BackColor = Color.White;
            if (index == -1) return;
            postext[index].BackColor = Color.Yellow;
        }

        void SelectAngleText(int index)
        {
            for (int i = 0; i < postext.Length; i++) postext[i].BackColor = Color.White;
            for (int i = 0; i < angletext.Length; i++) angletext[i].BackColor = Color.White;
            if (index == -1) return;
            angletext[index].BackColor = Color.Yellow;
        }

        private void LinkUPDev(object sender, EventArgs e)
        {
            if (UP.isLink || LinkStatus)
            {
                esp.FindNewDev = true;
                LinkStatus = false;
                if(UP.isLink)
                    UP.SendCmdPacket(UartProtocol.PacketCmd.LoadCode, (byte)1);
                else
                    FindClear();
                UP.Disconnect();
                Motor.Enable = false;
                this.LinkUP.Text = "连接设备";
                isLinkUP = false;
                PosResetMode = false;
                SourPos.BackColor = Color.Transparent;
                return;
            }
            else
            {
                LinkStatus = true;
                this.LinkUP.Text = "连接中...";
                UP.Init();
                if (!NetCheck.Checked)
                    FindDev(UP, LinkUPDev);
                else
                {
                    esp.FindNewDev = false;
                    UserInfo info = (UserInfo)NetBox.SelectedItem;
                    if (info == null)
                    {
                        MessageBox.Show("请选择一个设备!");
                        return;
                    }
                    UP.RecvDataMaxTime = 200;
                    UP.SetNet(new System.Net.IPEndPoint(info.ip,esp.port));
                    UP.SendCmdPacket(UartProtocol.PacketCmd.Alive);
                }

            }
        }

        private void MoveMode(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            if (b.BackColor != Color.Lime)
            {
                if (G0Mode.BackColor == Color.Lime)
                {
                    G0Mode.BackColor = Color.Transparent;
                    G1Mode.BackColor = Color.Lime;
                }
                else
                {
                    G1Mode.BackColor = Color.Transparent;
                    G0Mode.BackColor = Color.Lime;
                }
            }

            if (SavePoint.SelectedNode == null) return;
            try
            {
                if (b == G0Mode)
                {
                    AddLogic(new LogicRun.G0(new Axis6.AxisStruct((Axis6.AxisStruct)SavePoint.SelectedNode.Tag)));
                    //TreeNode Node = new TreeNode("快速运动到点");
                    //Node.Tag = new LogicRun.G0((Axis6.AxisStruct)SavePoint.SelectedItem);
                    //Node.ToolTipText = ((LogicRun.G0)Node.Tag).ToString();
                    //LogicTree.Nodes.Add(Node);
                }
                else if (b == G1Mode)
                {
                    AddLogic(new LogicRun.G1(new Axis6.AxisStruct((Axis6.AxisStruct)SavePoint.SelectedNode.Tag)));
                    //TreeNode Node = new TreeNode("直线运动到点");
                    //Node.Tag = new LogicRun.G0((Axis6.AxisStruct)SavePoint.SelectedItem);
                    //Node.ToolTipText = ((LogicRun.G0)Node.Tag).ToString();
                    //LogicTree.Nodes.Add(Node);
                }
            }
            catch { }
        }

        private void DragInF(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Scroll | DragDropEffects.Move;
            TreeNode DropNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(new Point(e.X, e.Y)));
            if (DropNode != null)
            {
                if (SelectNode != null)
                    SelectNode.BackColor = Color.Transparent;
                SelectNode = DropNode;
                SelectNode.BackColor = Color.DodgerBlue;
            }
        }

        private void DragIn(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Scroll | DragDropEffects.Move;
        }

        private void DragStart(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move | DragDropEffects.Scroll);
        }

        void CheckAdd(TreeNode TargetNode)
        {
            object obj = TargetNode.Tag;
            foreach (TreeNode Node in LocalPoint.Nodes)
            {
                if (Node.Tag == obj)
                {
                    SavePoint.SelectedNode = Node;
                    return;
                }
            }
            LocalPoint.Nodes.Add((TreeNode)TargetNode.Clone());
            SavePoint.Nodes.Add((TreeNode)TargetNode.Clone());
        }

        private void SavePoint_DragDrop(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetType() != typeof(TreeNode))) return;
            TreeNode Node = (TreeNode)e.Data.GetData(typeof(TreeNode));
            //TreeNode obj=e.Data.GetData()
            TreeNode DropNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(new Point(e.X, e.Y)));
            if (Node == DropNode) return;
            //if (DropNode != null) return;
            if (Node.Tag is LogicRun.Delay || Node.Tag is LogicRun.SetOutput || Node.Tag is LogicRun.CheckInput)
            {
                CheckAdd(Node);
            }
        }

        void objMove(int objindex, int index)
        {
            TreeNode Node;
            if (objindex != -1)
            {
                object obj = SelectModule.ModelList[objindex];
                SelectModule.ModelList.RemoveAt(objindex);
                if (index == -1)
                    SelectModule.ModelList.Add((LogicRun.ModeObject)obj);
                else
                    SelectModule.ModelList.Insert(index, (LogicRun.ModeObject)obj);

                Node = LogicTree.Nodes[objindex];
                Node.Remove();
                if (index == -1)
                    LogicTree.Nodes.Add(Node);
                else
                    LogicTree.Nodes.Insert(index, Node);

                Node = LogicTreeView.Nodes[objindex];
                Node.Remove();
                if (index == -1)
                    LogicTreeView.Nodes.Add(Node);
                else
                    LogicTreeView.Nodes.Insert(index, Node);
            }
        }
        private void DropTo(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetType() != typeof(TreeNode))) return;
            TreeNode Node = (TreeNode)e.Data.GetData(typeof(TreeNode));
            //TreeNode obj=e.Data.GetData()
            TreeNode DropNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(new Point(e.X, e.Y)));
            int index = (DropNode != null) ? DropNode.Index : -1;
            if (Node.TreeView == sender)
            {
                if (e.KeyState == 8)
                {
                    CloneLogic((LogicRun.ModeObject)Node.Tag, DropNode != null, index);
                }
                else {
                    if (Node == DropNode) return;
                    objMove(Node.Index, (DropNode != null) ? DropNode.Index : -1);
                }
                if (((LogicRun.ModeObject)Node.Tag).redraw && DropNode == null)
                    LogicRun.UpdateLine();
                if (((LogicRun.ModeObject)Node.Tag).redraw && ((LogicRun.ModeObject)DropNode.Tag).redraw)
                    LogicRun.UpdateLine();
            }
            else
            {
                if (Node.Tag is Axis6.AxisStruct)
                {
                    if (G0Mode.BackColor == Color.Lime)
                        AddLogic(new LogicRun.G0((Axis6.AxisStruct)Node.Tag), DropNode != null, index);
                    else if (G1Mode.BackColor == Color.Lime)
                        AddLogic(new LogicRun.G1((Axis6.AxisStruct)Node.Tag), DropNode != null, index);
                }
                else if (Node.Tag is LogicRun.SetOutput)
                    AddLogic(((LogicRun.SetOutput)Node.Tag), DropNode != null, index);
                else if (Node.Tag is LogicRun.CheckInput)
                    AddLogic(((LogicRun.CheckInput)Node.Tag), DropNode != null, index);
                else if (Node.Tag is LogicRun.Delay)
                    AddLogic(((LogicRun.Delay)Node.Tag), DropNode != null, index);
                else if (Node.TreeView == LogicSrcTree)
                {
                    if (Node.Text == "延时")
                    {
                        EItem.RenameText.Text = "500";
                        string last = EItem.TitleText.Text;
                        EItem.TitleText.Text = "延时时间(毫秒)";
                        if (touchkeyenable.Checked)
                            EItem.key = touchkey;
                        if (EItem.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                int v = int.Parse(EItem.RenameText.Text);
                                LogicRun.Delay d = new LogicRun.Delay(v);
                                AddLogic(d, true);
                            }
                            catch { }
                        }
                        EItem.TitleText.Text = last;
                    }

                }
            }
        }

        private void LinkHPDev(object sender, EventArgs e)
        {
            if (HCOM.IsOpen)
            {
                try
                {
                    HCOM.Close();
                }
                catch { }
                this.LinkHP.Text = "连接手轮";
                isLinkHP = false;
                return;
            }
            else
            {
                this.LinkHP.Text = "连接中...";
                new Task(() => {
                    string[] COMNames = SerialPort.GetPortNames();
                    for (int i = 0; i < COMNames.Length; i++)
                    {
                        try
                        {
                            this.HCOM.PortName = COMNames[i];
                            this.HCOM.Open();
                            HP.SendCmdPacket(UartProtocol.PacketCmd.Alive);
                        }
                        catch
                        {
                            if (i == (COMNames.Length - 1))
                            {
                                MessageBox.Show("连接失败!");
                                Invoke(new MethodInvoker(delegate ()
                                {
                                    this.LinkHP.Text = "连接手轮";
                                }));
                                return;
                            }
                        }
                        while (HCOM.IsOpen)
                        {
                            Thread.Sleep(100);
                            if (isLinkHP)
                                return;
                        }
                        try { this.HCOM.Close(); } catch { }
                    }
                    MessageBox.Show("连接失败!");
                    Invoke(new MethodInvoker(delegate ()
                    {
                        this.LinkHP.Text = "连接手轮";
                    }));
                    try { HCOM.Close(); }
                    catch { }
                }).Start();
            }
        }

        private void AddPosClick(object sender, EventArgs e)
        {
            TreeNode Node;
            //object a = LocalPoint.Items[0];
            switch (buttonmode)
            {
                case ButtonMode.Angle:
                case ButtonMode.Pos:
                    Axis6.AxisStruct p = new Axis6.AxisStruct();
                    p.X = X;
                    p.Y = Y;
                    p.Z = Z;
                    p.N = N;
                    p.O = O;
                    p.A = A;
                    for (int i = 0; i < 6; i++) p.angle[i] = Angle[i];
                    Node = new TreeNode(p.ToString());
                    Node.Tag = p;
                    LocalPoint.Nodes.Add(Node);
                    break;
                case ButtonMode.IO:
                    if (sender != null) goto case ButtonMode.Pos;
                    byte[] stats = OutputBit.getStats();
                    if (LastIOStats != null)
                    {
                        for (int i = 0; i < LastIOStats.Length; i++)
                        {
                            if (stats[i] != LastIOStats[i])
                            {
                                LastIOStats = stats;
                                Node = new TreeNode();
                                Node.Tag = new LogicRun.SetOutput(LastIOStats, IOPort.OutputCount);
                                Node.Text = Node.Tag.ToString();
                                LocalPoint.Nodes.Add(Node);
                            }
                        }
                    }
                    else
                    {
                        LastIOStats = stats;
                        Node = new TreeNode();
                        Node.Tag = new LogicRun.SetOutput(LastIOStats, IOPort.OutputCount);
                        Node.Text = Node.Tag.ToString();
                        LocalPoint.Nodes.Add(Node);
                    }
                    break;
            }
        }

        private void DelPosItem(object sender, EventArgs e)
        {
            if (null != LocalPoint.SelectedNode)
            {
                int index = LocalPoint.SelectedNode.Index;
                LocalPoint.SelectedNode.Remove();
                if (LocalPoint.Nodes.Count != 0)
                    LocalPoint.SelectedNode = LocalPoint.Nodes[(index != 0) ? index - 1 : 0];
            }
        }

        private void ClickMoveTo(object sender, EventArgs e)
        {
            try
            {
                Axis6.AxisStruct point = (Axis6.AxisStruct)((ListBox)sender).SelectedItem;
                Axis6.MoveToAngle(point.angle);
            }
            catch { }
        }
        private void StartStopLogic(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            if (((string)b.Tag)[0] == '0')
            {
                //b.Tag = "1";
                //b.Image = KControl.Properties.Resources.Stop;
                //b.Text = "停止";
                // if (LogicRun.LogicList.Count == 0) return;
                RunStop.BringToFront();
                FastStop.BringToFront();
                MainTab.Enabled = false;
                if (UCOM.IsOpen)
                {
                    Motor.SetInfo();
                    //UP.SendCmdPacket(UartProtocol.PacketCmd.Alive);
                }
                LogicRun.LogicList = SelectModule.ModelList;
                LogicRun.Start();
            }
            else
            {
                //b.Tag = "0";
                //b.Image = KControl.Properties.Resources.Start;
                //b.Text = "启动";
                RunStop.SendToBack();
                FastStop.SendToBack();
                MainTab.Enabled = true;
                LogicRun.Stop();
                if (JxsMode)
                {
                    SetPosAngle(LogicRun.LastStruct);
                    UpdateDisplay();
                    Axis6.MoveToAngle(LogicRun.LastStruct.angle);
                }
            }
        }

        object[] SelectLine = new object[2];

        void DelLogic(LogicRun.ModeObject obj)
        {
            try
            {
                int index = SelectModule.ModelList.IndexOf(obj);
                LogicTreeView.Nodes.RemoveAt(index);
                LogicTree.Nodes.RemoveAt(index);
                LogicRun.Remove(obj);
                SelectModule.ModelList.Remove(obj);
                //LogicTree.SelectedNode = null;
                SelectClear();
            }
            catch { }
        }
        void DelLogic(int index)
        {
            try
            {
                LogicTreeView.Nodes.RemoveAt(index);
                LogicTree.Nodes.RemoveAt(index);
                LogicRun.Remove(index);
                //SelectModule.ModelList.Remove();
                //LogicTree.SelectedNode = null;
                SelectClear();
            }
            catch { }
        }

        void CloneLogic(LogicRun.ModeObject obj, bool ins = false, int index = -1)
        {
            if (obj is LogicRun.G0)
            {
                AddLogic(new LogicRun.G0(((LogicRun.G0)obj).dest), ins, index);
            }
            else if (obj is LogicRun.G1)
            {
                AddLogic(new LogicRun.G1(((LogicRun.G1)obj).dest), ins, index);
            }
            else
                AddLogic(obj, ins, index);
        }

        void AddLogic(LogicRun.ModeObject obj, bool ins = false, int index = -1)
        {
            TreeNode Node = new TreeNode(obj.ToString());
            Node.Tag = obj;
            Node.ToolTipText = obj.ToString();
            if (ins && MainLogic.SelectedNode != null)
            {
                if (index == -1)
                    index = MainLogic.SelectedNode.Index;
                LogicTree.Nodes.Insert(index, Node);
                LogicTreeView.Nodes.Insert(index, (TreeNode)Node.Clone());
                SelectModule.Insert(index, obj);
            }
            else
            {
                LogicTree.Nodes.Add(Node);
                LogicTreeView.Nodes.Add((TreeNode)Node.Clone());
                SelectModule.Add(obj);
            }
        }

        private void DelayAdd_Click(object sender, EventArgs e)
        {
            int time = 0;
            try { time = (int)double.Parse(DelayText.Text); } catch { ErrorItem.ErrorEffect((TextBox)sender); return; }
            if (time < 0)
            {
                ErrorItem.ErrorEffect((TextBox)sender);
                return;
            }
            AddLogic(new LogicRun.Delay(time), true);
        }

        private void RangeSet_Click(object sender, EventArgs e)
        {
            double range = 0;
            try { range = double.Parse(RangeText.Text); } catch { ErrorItem.ErrorEffect((TextBox)sender); return; }
            if (range <= 0)
            {
                ErrorItem.ErrorEffect((TextBox)sender);
                return;
            }
            AddLogic(new LogicRun.SetRange(range), true);
        }

        private void MotorSpeedSet_Click(object sender, EventArgs e)
        {
            int speed = 0;
            try { speed = (int)double.Parse(MotorSpeedText.Text); } catch { ErrorItem.ErrorEffect((TextBox)sender); return; }
            if (speed <= 0)
            {
                ErrorItem.ErrorEffect((TextBox)sender);
                return;
            }
            AddLogic(new LogicRun.SetSpeed(speed), true);
        }

        //private void SavePoint_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        Axis6.AxisStruct g = (Axis6.AxisStruct)((ListBox)sender).SelectedItem;
        //        SX.Text = (g.X * 1000).ToString("f5");
        //        SY.Text = (g.Y * 1000).ToString("f5");
        //        SZ.Text = (g.Z * 1000).ToString("f5");
        //        SN.Text = g.N.ToString("f5");
        //        SO.Text = g.O.ToString("f5");
        //        SA.Text = g.A.ToString("f5");
        //        Axis6.MoveToAngle(g.angle);
        //    }
        //    catch
        //    {

        //    }
        //}

        private void LogicDel_Click(object sender, EventArgs e)
        {
            TreeNode Node = LogicTree.SelectedNode;
            if (Node == null) return;
            DelLogic((LogicRun.ModeObject)Node.Tag);
        }

        private void EnableAngleInput_CheckedChanged(object sender, EventArgs e)
        {
            bool check = ((CheckBox)sender).Checked;
            foreach (TextBox b in angletext)
            {
                if (check)
                {
                    b.ReadOnly = false;
                    b.BorderStyle = BorderStyle.FixedSingle;
                }
                else
                {
                    b.ReadOnly = true;
                    b.BorderStyle = BorderStyle.None;
                }
            }
        }

        private void SetInfoButton_Click(object sender, EventArgs e)
        {
            int plus;
            int startfq;
            int time;
            try { plus = int.Parse(PluseTo360Text.Text); } catch { ErrorItem.ErrorEffect((TextBox)sender); return; };
            try { startfq = int.Parse(StartFqText.Text); } catch { ErrorItem.ErrorEffect((TextBox)sender); return; };
            try { time = int.Parse(ADSpeedText.Text); } catch { ErrorItem.ErrorEffect((TextBox)sender); return; };
            Motor.PlusToDeg = plus;
            Motor.StartSpeed = startfq;
            Motor.SpeedTime = time;
            SetInfo.Text = "更新成功";
        }

        private void FastStop_Click(object sender, EventArgs e)
        {
            Motor.MotorFastStop();
            StartStopLogic(RunStop, null);
        }

        private void ClickMove(object sender, EventArgs e)
        {
            if (LogicTree.SelectedNode == null) return;
            LogicRun.ModeObject obj = (LogicRun.ModeObject)LogicTree.SelectedNode.Tag;
            if (obj is LogicRun.G0)
            {
                Axis6.MoveArc(((LogicRun.G0)obj).sour.angle, ((LogicRun.G0)obj).dest.angle, 'r', 1, 15, false);
            }
            if (obj is LogicRun.G1)
            {
                //((LogicRun.G1)obj).run();
                double[][] value = ((LogicRun.G1)obj).getData();
                if (value != null)
                {
                    int[][] plist = new int[value.Length][];
                    int count = 0;
                    foreach (double[] p in value)
                    {
                        plist[count++] = Motor.AngleToPluse(p);
                    }
                    StringBuilder sb = new StringBuilder();
                    sb.Append("G1 Data  Count:");
                    sb.AppendLine(plist.Length + "");
                    foreach (int[] v in plist)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            sb.Append(v[i]);
                            sb.Append(',');

                        }
                        sb.AppendLine();
                    }
                    textBox1.Text = sb.ToString();
                }
            }
        }

        string RenameWindow(string name)
        {
            EItem.RenameText.Text = name;
            if (EItem.ShowDialog() == DialogResult.OK)
            {
                return EItem.RenameText.Text;
                //object obj = LocalPoint.SelectedNode.Tag;
                //int index = LocalPoint.SelectedNode.Index;
                //LocalPoint.Nodes.RemoveAt(index);
                //((Axis6.AxisStruct)obj).name = 
                //LocalPoint.Items.Insert(index, obj);
            }
            return null;
        }

        private void 重命名ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (LocalPoint.SelectedNode == null) return;
            //EItem.RenameText.Text = ((Axis6.AxisStruct)LocalPoint.SelectedItem).name;
            string name;
            if (LocalPoint.SelectedNode.Tag is Axis6.AxisStruct)
            {
                name = ((Axis6.AxisStruct)LocalPoint.SelectedNode.Tag).name;
                ((Axis6.AxisStruct)LocalPoint.SelectedNode.Tag).name = (((name = RenameWindow(name)) != null) ? name : ((Axis6.AxisStruct)LocalPoint.SelectedNode.Tag).name);
                LocalPoint.SelectedNode.Text = LocalPoint.SelectedNode.Tag.ToString();
            }
        }

        private void InputR_Click(object sender, EventArgs e)
        {
            //StringBuilder sb = new StringBuilder();
            //sb.Append("Input Select Stats:");
            //foreach(byte v in InputBit.getSelectStats())
            //{
            //    sb.AppendFormat("0x{0:X2}", v);
            //    sb.Append(' ');
            //}
            //sb.Append("\r\nInput Stats:");
            //foreach (byte v in InputBit.getStats())
            //{
            //    sb.AppendFormat("0x{0:X2}", v);
            //    sb.Append(' ');
            //}
            //sb.Append("\r\nOutput Stats:");
            //foreach (byte v in OutputBit.getStats())
            //{
            //    sb.AppendFormat("0x{0:X2}", v);
            //    sb.Append(' ');
            //}
            //TreeNode Node = new TreeNode("端口输出");
            AddLogic(new LogicRun.CheckInput(InputBit.getStats(), InputBit.getSelectStats(), InputBit.Ecount));
            //LogicTreeView.Nodes.Add(Node);
            //InOutDebug.Text = sb.ToString();
        }

        private void OpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Filter = "控制步骤文件kcn|*.kcn|G代码文本文件|*.txt|pmw梯形图文件|*.pmw";
            if (of.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(of.FileName, FileMode.Open);
                if (of.FileName.ToLower().Contains(".txt"))
                {
                    GLoader.LoadFile(of.FileName, X, Y, Z, N, O, A);
                    //double[] xl=GLoader.getXList();
                    Axis6.AxisStruct laststr = new Axis6.AxisStruct(X, Y, Z, N, O, A, Angle);
                    AddLogic(new LogicRun.GList(GLoader.getXList(), GLoader.getYList(), GLoader.getZList(), laststr));
                    //if (GLoader.ShowDialog() == DialogResult.OK)
                    //{
                    //    List<double[]> list = GLoader.getData();
                    //    Axis6.AxisStruct laststr = new Axis6.AxisStruct();
                    //    Axis6.AxisStruct astr;
                    //    foreach(double[] ag in list)
                    //    {
                    //        astr = new Axis6.AxisStruct(0, 0, 0, 0, 0, 0, ag);
                    //        AddLogic("快速运动到点", new LogicRun.G0(laststr,astr));
                    //        laststr = astr;
                    //    }
                    //}
                    return;
                }
                else if (of.FileName.ToLower().Contains(".pmw"))
                {
                    AddLogic(new LogicRun.PLCCode(fs));
                    return;
                }
                int len;
                if ((len = fs.ReadByte()) != '[')
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    LogicTreeView.Nodes.Clear();
                    LogicTree.Nodes.Clear();
                    LogicRun.Clear();
                    SelectClear();
                    foreach (LogicRun.ModeObject obj in BuildCode.BytesToCode(data))
                    {
                        AddLogic(obj);
                    }
                    return;
                }
                StreamReader sr = new StreamReader(of.FileName);
                List<object> staticlist = new List<object>();
                List<object> iostaticlist = new List<object>();
                List<LogicRun.ModeObject> logiclist = new List<LogicRun.ModeObject>();

                int readmode = -1;
                int linecount = 1;

                LogicRun.LastStruct = Axis6.DefStruct;

                do
                {
                    try
                    {
                        string line = sr.ReadLine();
                        linecount++;
                        switch (line)
                        {
                            case "[POINT]":
                                readmode = 0;
                                continue;
                            case "[IO]":
                                readmode = 1;
                                continue;
                            case "[CONFIG]":
                                readmode = 2;
                                continue;
                            case "[LOGIC]":
                                readmode = 3;
                                continue;
                            case "[MODEL]":
                                continue;

                        }
                        switch (readmode)
                        {
                            case 0:
                                string[] vx = line.Split(' ');
                                string[] pdata = vx[1].Split(',');

                                Axis6.AxisStruct a = new Axis6.AxisStruct();
                                a.name = vx[0];
                                a.X = double.Parse(pdata[0]);
                                a.Y = double.Parse(pdata[1]);
                                a.Z = double.Parse(pdata[2]);
                                a.N = double.Parse(pdata[3]);
                                a.O = double.Parse(pdata[4]);
                                a.A = double.Parse(pdata[5]);
                                double[] ag = new double[6];
                                for (int i = 0; i < 6; i++)
                                {
                                    ag[i] = double.Parse(pdata[6 + i]);
                                }
                                a.angle = ag;
                                staticlist.Add(a);
                                break;
                            case 1:
                                vx = line.Split(' ');
                                pdata = vx[1].Split(',');
                                len = int.Parse(pdata[0]);
                                byte[] databit = new byte[(len + 7) / 8];
                                for (int i = 0; i < databit.Length; i++)
                                {
                                    databit[i] = (byte)int.Parse(pdata[1 + i]);
                                }
                                iostaticlist.Add(new LogicRun.SetOutput(databit, len));
                                break;
                            case 2:

                                break;
                            case 3:
                                switch (line[0])
                                {
                                    case 'G':
                                        //bool ispoint = false;
                                        //Axis6.AxisStruct start;
                                        //Axis6.AxisStruct stop;
                                        //if (line[2] != 'P') throw new Exception("");
                                        int index = int.Parse(line.Substring(line.IndexOf("P") + 1));
                                        if (index == -1)
                                        {
                                            MessageBox.Show("在第" + linecount + "行的位置发生错误!\r\n无法找到点" + line);
                                            return;
                                        }
                                        if (line[1] == '0')
                                        {

                                            LogicRun.G0 g = new LogicRun.G0((Axis6.AxisStruct)staticlist[index]);
                                            logiclist.Add(g);
                                        }
                                        else if (line[1] == '1')
                                        {
                                            LogicRun.G1 g = new LogicRun.G1((Axis6.AxisStruct)staticlist[index]);
                                            logiclist.Add(g);
                                        }
                                        break;
                                    case 'O':
                                        index = int.Parse(line.Substring(line.IndexOf("IO") + 2));
                                        logiclist.Add((LogicRun.SetOutput)iostaticlist[index]);
                                        break;
                                    case 'D':
                                        logiclist.Add(new LogicRun.Delay(int.Parse(line.Substring(line.IndexOf(' ')))));
                                        break;
                                    case 'S':
                                        logiclist.Add(new LogicRun.SetSpeed(int.Parse(line.Substring(line.IndexOf(' ')))));
                                        break;
                                    case 'R':
                                        logiclist.Add(new LogicRun.SetRange(double.Parse(line.Substring(line.IndexOf(' ')))));
                                        break;
                                }
                                break;
                            case 4: break;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("在第" + linecount + "行的位置发生错误!");
                        sr.Close();
                        return;
                    }
                } while (!sr.EndOfStream);
                LocalPoint.Nodes.Clear();
                TreeNode Node;
                foreach (object obj in staticlist)
                {
                    Node = new TreeNode();
                    Node.Tag = obj;
                    Node.Text = Node.Tag.ToString();
                    LocalPoint.Nodes.Add(Node);
                }
                foreach (object obj in iostaticlist)
                {
                    Node = new TreeNode();
                    Node.Tag = obj;
                    Node.Text = Node.Tag.ToString();
                    LocalPoint.Nodes.Add(Node);
                }
                foreach (object obj in logiclist)
                {
                    AddLogic((LogicRun.ModeObject)obj);
                }
                sr.Close();
            }
        }

        private void SaveFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Filter = "控制步骤文件kcn|*.kcn";
            if (sf.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(sf.FileName, FileMode.OpenOrCreate);
                if (CodeSave.Checked)
                {
                    StreamWriter ws = new StreamWriter(fs);
                    StringBuilder sb = new StringBuilder();
                    StringBuilder logic = new StringBuilder();
                    List<object> PointList = new List<object>();
                    List<object> IOStatsList = new List<object>();
                    logic.AppendLine("[LOGIC]");
                    sb.AppendLine("[IO]");
                    ws.WriteLine("[POINT]");
                    foreach (TreeNode Node in LocalPoint.Nodes)
                    {
                        object obj = Node.Tag;
                        if (obj is Axis6.AxisStruct)
                        {
                            Axis6.AxisStruct s = (Axis6.AxisStruct)obj;
                            string val = s.name + " " + s.X + "," + s.Y + "," + s.Z + "," + s.N + "," + s.O + "," + s.A;
                            for (int i = 0; i < s.angle.Length; i++)
                            {
                                val += "," + s.angle[i];
                            }
                            ws.WriteLine(val);
                            PointList.Add(obj);
                        }
                    }
                    for (int i = 0; i < Modules.Count; i++)
                        foreach (LogicRun.ModeObject obj in Modules[i].ModelList)
                        {
                            if (obj is LogicRun.G0)
                            {
                                int index = PointList.IndexOf(((LogicRun.G0)obj).dest);
                                if (index != -1)
                                {
                                    logic.AppendLine("G0 P" + index);
                                }
                            }
                            else if (obj is LogicRun.G1)
                            {
                                int index = PointList.IndexOf(((LogicRun.G1)obj).dest);
                                if (index != -1)
                                {
                                    logic.AppendLine("G1 P" + index);
                                }
                            }
                            else if (obj is LogicRun.Delay)
                            {
                                logic.AppendLine("Delay " + ((LogicRun.Delay)obj).time);
                            }
                            else if (obj is LogicRun.SetRange)
                            {
                                logic.AppendLine("Range " + ((LogicRun.SetRange)obj).r);
                            }
                            else if (obj is LogicRun.SetSpeed)
                            {
                                logic.AppendLine("Speed " + ((LogicRun.SetSpeed)obj).frequency / 1000);
                            }
                            else if (obj is LogicRun.SetOutput)
                            {

                                sb.Append("OUT " + ((LogicRun.SetOutput)obj).len);
                                byte[] outputbit = ((LogicRun.SetOutput)obj).outputbit;
                                int len = (((LogicRun.SetOutput)obj).len + 7) / 8;
                                for (int j = 0; j < len; j++)
                                {
                                    sb.Append(',');
                                    sb.Append(outputbit[j]);
                                }
                                sb.AppendLine();
                                IOStatsList.Add(obj);

                                int index = IOStatsList.IndexOf(obj);
                                if (index != -1)
                                    logic.AppendLine("OUT IO" + index);
                            }
                            else if (obj is LogicRun.CheckInput)
                            {
                                sb.Append("IN " + ((LogicRun.CheckInput)obj).len);

                            }
                        }

                    ws.Write(sb.ToString());
                    ws.Write(logic.ToString());
                    ws.Flush();
                    ws.Close();
                }
                else
                {
                    byte[] data = BuildCode.CodeToBytes(SelectModule.ModelList);
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
                fs.Close();
            }
        }

        private void SetOffsetAngle_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Axis6.MaxAxis; i++)
            {
                Motor.offsetAngle[i] += Angle[i];
            }
            ResetPosAngleButton.PerformClick();
        }

        private void 清空ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("该操作会删除所有步骤。是否继续？", "警告", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                return;
            }
            //LogicRun.LogicList=SelectModule.ModelList;
            LogicRun.Clear();
            SelectModule.Clear();
            LogicTree.Nodes.Clear();
            LogicTreeView.Nodes.Clear();
            SelectClear();
        }

        private void LocalPoint_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (LocalPoint.SelectedNode != null)
                {
                    if (LocalPoint.SelectedNode.Tag is Axis6.AxisStruct)
                    {
                        SetPosAngle((Axis6.AxisStruct)LocalPoint.SelectedNode.Tag);
                        UpdateDisplay();
                        Axis6.MoveToAngle(Angle);
                    }
                }
            }
            if (e.KeyCode == Keys.Delete)
            {
                DelPosItem(null, null);
            }
        }

        private void MotorUpdatePos_Click(object sender, EventArgs e)
        {
            if (MoveTogg.Checked)
            {
                if (Motor.Enable && Motor.IsReady)
                {
                    Motor.MotorRun(Angle);
                }
            }
        }

        private void SourPos_Click(object sender, EventArgs e)
        {
            if (UP.isLink)
                if (SourPos.BackColor == Color.Transparent)
                {
                    PosResetMode = true;
                    SourPos.BackColor = Color.Gray;

                }
                else
                {
                    PosResetMode = false;
                    SourPos.BackColor = Color.Transparent;
                }
        }

        void SendIOInfo()
        {
            ByteStream b = new ByteStream(100);
            b.WriteByte(17);
            for (int i = 0; i < InputBit.Count; i++) {
                InputBit.setEnableStats(i, true);
            }
            for (int i = 0; i < 4; i++)
            {
                //b.WriteByte(PortInfo.controlbutton[i].id);
                if (PortInfo.controlbutton[i].id != -1)
                    InputBit.setEnableStats(PortInfo.controlbutton[i].id, false);
            }
            for (int i = 0; i < 5; i++)
            {
                b.WriteByte(PortInfo.sourpoint[i].id);
                if (PortInfo.sourpoint[i].id != -1)
                    InputBit.setEnableStats(PortInfo.sourpoint[i].id, false);
            }
            for (int i = 0; i < 3; i++)
            {
                b.WriteByte(PortInfo.leftmaxpoint[i].id);
                b.WriteByte(PortInfo.rightmaxpoint[i].id);
                if (PortInfo.leftmaxpoint[i].id != -1)
                    InputBit.setEnableStats(PortInfo.leftmaxpoint[i].id, false);
                if (PortInfo.rightmaxpoint[i].id != -1)
                    InputBit.setEnableStats(PortInfo.rightmaxpoint[i].id, false);
            }
            for (int i = 0; i < 6; i++)
            {
                b.WriteByte(PortInfo.errorpoint[i].id);
                if (PortInfo.errorpoint[i].id != -1)
                    InputBit.setEnableStats(PortInfo.errorpoint[i].id, false);
            }
            UP.SendDataPacket(UartProtocol.PacketCmd.Alive, b.toBytes());
        }

        private void ConfigPort_Click(object sender, EventArgs e)
        {
            if (Config.ShowDialog() == DialogResult.OK)
            {
                SendIOInfo();

            }
        }

        int SelectIndex;

        private void DownloadCode_Click(object sender, EventArgs e)
        {
            if (SelectModule.ModelList.Count != 0)
            {
                ByteStream bs = new ByteStream(64);
                for (int i = 0; i < PortInfo.controlbutton.Length; i++)
                {
                    if (PortInfo.controlbutton[i].id != -1)
                    {
                        bs.WriteByte(i);
                        bs.WriteByte(PortInfo.controlbutton[i].id);
                    }
                }
                //byte[] code=
                CodeData = BuildCode.CodeToBytes(SelectModule.ModelList, bs.Pos != 0 ? bs.toBytes() : null);
                CodeDataPos = 0;
                CodeLen = CodeData.Length;
                UP.SendDataPacket(UartProtocol.PacketCmd.WriteData, new byte[] { 1, 0 });
            }
        }

        private void IOOutput_Click(object sender, EventArgs e)
        {
            if (SavePoint.SelectedNode != null)
                if (SavePoint.SelectedNode.Tag is LogicRun.SetOutput)
                    AddLogic((LogicRun.SetOutput)SavePoint.SelectedNode.Tag);
        }

        private void ModuleList_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void OutputButton_Click(object sender, EventArgs e)
        {
            AddLogic(new LogicRun.SetOutput(OutputBit.getStats(), OutputBit.Ecount), true);
        }

        private void AddModule_Click(object sender, EventArgs e)
        {
            ModulesAdd();
        }

        private void DelModule_Click(object sender, EventArgs e)
        {
            //Modules
        }

        bool isSelectNode;
        private LogicRun.Module SelectModule;
        private int CodeDataPos;

        private void TableControlKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                TreeNode Node = ((TreeView)sender).SelectedNode;
                if (Node != null)
                {

                    DelLogic(Node.Index);
                }
            }
        }

        private void TableControlKey(object sender, KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Delete)
            {
                TreeNode Node = ((TreeView)sender).SelectedNode;
                if (Node != null)
                {
                    Node.Remove();
                    SelectModule.ModelList.Remove((LogicRun.ModeObject)Node.Tag);
                }
            }
        }

        private void LocalPoint_DoubleClick(object sender, EventArgs e)
        {
            if (LocalPoint.SelectedNode != null)
            {
                if (LocalPoint.SelectedNode.Tag is Axis6.AxisStruct)
                {
                    Axis6.MoveToAngle(((Axis6.AxisStruct)LocalPoint.SelectedNode.Tag).angle);
                }
            }
        }

        private void SavePoint_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!(((TreeView)sender).SelectedNode.Tag is Axis6.AxisStruct)) return;
            try
            {
                Axis6.AxisStruct g = (Axis6.AxisStruct)((TreeView)sender).SelectedNode.Tag;
                SX.Text = (g.X * 1000).ToString("f5");
                SY.Text = (g.Y * 1000).ToString("f5");
                SZ.Text = (g.Z * 1000).ToString("f5");
                SN.Text = g.N.ToString("f5");
                SO.Text = g.O.ToString("f5");
                SA.Text = g.A.ToString("f5");
                Axis6.MoveToAngle(g.angle);
            }
            catch
            {

            }
        }

        private void SelectUpdate(object sender, MouseEventArgs e)
        {
            ((TreeView)sender).SelectedNode = ((TreeView)sender).GetNodeAt(new Point(e.X, e.Y));
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        bool WindowMoveFlag = false;
        Point WindowMovePos;

        private void WindowMove1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowMovePos = e.Location;
                WindowMoveFlag = true;
            }
        }

        private void WindowMove3(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowMoveFlag = false;
            }
        }

        private void WindowMove2(object sender, MouseEventArgs e)
        {
            if (WindowMoveFlag)
            {
                if (e.Button != MouseButtons.Left)
                {
                    WindowMoveFlag = false;
                    return;
                }
                Point p = this.Location;
                p.Offset(e.X - WindowMovePos.X, e.Y - WindowMovePos.Y);
                //p.Offset(renamepos);
                this.Location = p;
            }
        }

        private void TouchKeyShow(object sender, EventArgs e)
        {
            if (touchkeyenable.Checked)
            {
                try { touchkey.Value = double.Parse(((TextBox)sender).Text); } catch { touchkey.Value = 0; }
                ((TextBox)sender).SelectAll();
                if (touchkey.ShowDialog() == DialogResult.OK)
                {
                    ((TextBox)sender).Text = "" + touchkey.Value;
                    ((TextBox)sender).SelectAll();
                }
            }
        }

        private int CodeLen;

        private void ClickSelect(object sender, EventArgs e)
        {
            if (!((TextBox)sender).ReadOnly && ((TextBox)sender).SelectedText == string.Empty)
                ((TextBox)sender).SelectAll();
            TouchKeyShow(sender, e);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {

        }

        private void UploadCode_Click(object sender, EventArgs e)
        {
            if (UploadFlag)
            {
                UploadFlag = false;
                return;
            }
            UploadFlag = true;
            UploadSize = 0;
            UploadPos = 0;
            UP.SendCmdPacket(UartProtocol.PacketCmd.LoadCode, (byte)2);
        }

        private byte[] CodeData;
        private int HPModeControl;
        private bool UploadFlag;
        private int UploadSize;

        private void LogicTreeView_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private int UploadPos;
        private byte[] UploadBuff;
        private volatile bool LinkStatus;
        private UartProtocol UartFinderSelect;
        private int FinderCOMIndex;

        private void LogicSrcTree_DoubleClick(object sender, EventArgs e)
        {

            if (LogicSrcTree.SelectedNode.Text == "延时")
            {
                EItem.RenameText.Text = "500";
                string last = EItem.TitleText.Text;
                EItem.TitleText.Text = "延时时间(毫秒)";
                if (touchkeyenable.Checked)
                    EItem.key = touchkey;
                if (EItem.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int v = int.Parse(EItem.RenameText.Text);
                        LogicRun.Delay d = new LogicRun.Delay(v);
                        AddLogic(d, true);
                    }
                    catch { }
                }
                EItem.TitleText.Text = last;
            }
        }

        void SelectTreeNode(TreeNode node)
        {
            if (SelectNode != null)
            {
                SelectNode.BackColor = Color.Transparent;
                SelectNode.ForeColor = Color.Black;
            }
            SelectNode = node;
            SelectNode.BackColor = Color.DodgerBlue;//LogicTree.SelectedNode
            SelectNode.ForeColor = Color.White;
            MainLogic.SelectedNode=MainLogic.Nodes[node.Index];
        }
    

        private void LogicTreeView_DoubleClick(object sender, EventArgs e)
        {
            TreeNode DropNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(Control.MousePosition));
            SelectTreeNode(DropNode);
        }

        private void NetCheck_CheckedChanged(object sender, EventArgs e)
        {
            if ((IPPanle.Visible = NetCheck.Checked))
            {

            }
        }

        private string[] FinderComName;
        private Action<object, EventArgs> FinderCallBack;

        //private bool buttonmode;

        void SelectClear()
        {
            if (SelectLine[0] != null)
            {
                Axis6.removeLine(SelectLine[0]);
                Axis6.removeLine(SelectLine[1]);
                SelectLine[0] = null;
                SelectLine[1] = null;
            }
            SelectIndex = -1;
            isSelectNode = false;
            //LogicTree.SelectedNode = null;
        }

        void ShowPosLine(Point e, bool update)
        {
            TreeNode Node = LogicTree.GetNodeAt(e.X, e.Y);
            if (Node != null)
            {
                int index = LogicTree.Nodes.IndexOf(Node);
                if (SelectIndex == index)
                {
                    if (update)
                        isSelectNode = true;
                    return;
                }
                if (index != -1)
                {
                    LogicRun.ModeObject g = (LogicRun.ModeObject)LogicTree.Nodes[index].Tag;
                    if (!(g is LogicRun.G0)&&!(g is LogicRun.G1)) return;
                    if (SelectLine[0] != null)
                    {
                        Axis6.removeLine(SelectLine[0]);
                        Axis6.removeLine(SelectLine[1]);
                        SelectLine[0] = null;
                        SelectLine[1] = null;
                    }

                    SelectIndex = index;
                    if (g is LogicRun.G0)
                    {
                        SelectLine[0] = Axis6.DrawArcLine(((LogicRun.G0)g).sour.angle, ((LogicRun.G0)g).dest.angle, 'g', 2);
                        SelectLine[1] = Axis6.DrawArcLine(((LogicRun.G0)g).sour.angle, ((LogicRun.G0)g).dest.angle);
                    }
                    else
                    {

                        SelectLine[0] = Axis6.DrawArcLine(((LogicRun.G1)g).sour.angle, ((LogicRun.G1)g).dest.angle, 'g', 2,1);
                        SelectLine[1] = Axis6.DrawArcLine(((LogicRun.G1)g).sour.angle, ((LogicRun.G1)g).dest.angle,'r',1,1);
                    }
                }
                else
                {
                    SelectClear();
                }
            }
            else
            {
                SelectClear();
            }
        }

        private void MoveShow(object sender, MouseEventArgs e)
        {
            if (!isSelectNode)
                ShowPosLine(new Point(e.X, e.Y), false);
        }

        private void SelectNodeClink(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                LogicTree.SelectedNode = LogicTree.GetNodeAt(e.X, e.Y);
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if(LogicTree.GetNodeAt(e.X, e.Y) == null)
                {
                    LogicTree.SelectedNode = null;
                }
                ShowPosLine(new Point(e.X, e.Y), true);
            }
        }

    }
}
