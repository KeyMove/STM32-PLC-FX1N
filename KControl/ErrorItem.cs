using KeyMove.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KControl
{
    class ErrorItem : Form
    {
        Color def;
        Control c;
        private Button RenameOK;
        private Button RenameCan;
        public TextBox RenameText;
        public Label TitleText;
        private Button button1;
        static List<Control> list = new List<Control>();

        public ErrorItem()
        {
            InitializeComponent();
        }


        bool renamemove = false;
        Point renamepos;
        private void RenameMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                renamepos = e.Location;
                renamemove = true;
            }
        }

        private void RenameMove2(object sender, MouseEventArgs e)
        {
            if (renamemove)
            {
                Point p = this.Location;
                p.Offset(e.X - renamepos.X, e.Y - renamepos.Y);
                //p.Offset(renamepos);
                this.Location = p;
            }
        }

        private void RenameMove3(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                renamemove = false;
            }
        }

        public void show()
        {
            new Task(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    c.Invoke(new MethodInvoker(delegate ()
                    {
                        c.ForeColor = Color.Red;
                    }));
                    Thread.Sleep(200);
                    c.Invoke(new MethodInvoker(delegate ()
                    {
                        c.ForeColor = def;
                    }));
                    Thread.Sleep(200);
                }
            }).Start();
            
        }

        public static void ErrorEffect(Control cont)
        {
            if (list.Contains(cont))
                return;
            list.Add(cont);
            Color defc = cont.ForeColor;
            new Task(() => {
                for(int i = 0; i < 3; i++)
                {
                    cont.Invoke(new MethodInvoker(delegate ()
                    {
                        cont.ForeColor = Color.Red;
                    }));
                    Thread.Sleep(200);
                    cont.Invoke(new MethodInvoker(delegate ()
                    {
                        cont.ForeColor = defc;
                    }));
                    Thread.Sleep(200);
                }
                cont.Invoke(new MethodInvoker(delegate ()
                {
                    list.Remove(cont);
                })); 
            }).Start();
        }

        private void InitializeComponent()
        {
            this.RenameOK = new System.Windows.Forms.Button();
            this.RenameCan = new System.Windows.Forms.Button();
            this.RenameText = new System.Windows.Forms.TextBox();
            this.TitleText = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // RenameOK
            // 
            this.RenameOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.RenameOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RenameOK.Location = new System.Drawing.Point(170, 66);
            this.RenameOK.Name = "RenameOK";
            this.RenameOK.Size = new System.Drawing.Size(55, 32);
            this.RenameOK.TabIndex = 1;
            this.RenameOK.Text = "确定";
            this.RenameOK.UseVisualStyleBackColor = true;
            // 
            // RenameCan
            // 
            this.RenameCan.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.RenameCan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RenameCan.Location = new System.Drawing.Point(231, 66);
            this.RenameCan.Name = "RenameCan";
            this.RenameCan.Size = new System.Drawing.Size(55, 32);
            this.RenameCan.TabIndex = 2;
            this.RenameCan.Text = "取消";
            this.RenameCan.UseVisualStyleBackColor = true;
            // 
            // RenameText
            // 
            this.RenameText.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.RenameText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.RenameText.Font = new System.Drawing.Font("宋体", 14F);
            this.RenameText.Location = new System.Drawing.Point(16, 31);
            this.RenameText.Name = "RenameText";
            this.RenameText.Size = new System.Drawing.Size(270, 29);
            this.RenameText.TabIndex = 0;
            this.RenameText.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RenameText_KeyDown);
            this.RenameText.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RenameText_MouseDown);
            // 
            // TitleText
            // 
            this.TitleText.AutoSize = true;
            this.TitleText.Font = new System.Drawing.Font("宋体", 14F);
            this.TitleText.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.TitleText.Location = new System.Drawing.Point(12, 9);
            this.TitleText.Name = "TitleText";
            this.TitleText.Size = new System.Drawing.Size(66, 19);
            this.TitleText.TabIndex = 0;
            this.TitleText.Text = "重命名";
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.DarkGray;
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Location = new System.Drawing.Point(274, -1);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(23, 26);
            this.button1.TabIndex = 2;
            this.button1.Text = "×";
            this.button1.UseVisualStyleBackColor = false;
            // 
            // ErrorItem
            // 
            this.BackColor = System.Drawing.Color.DimGray;
            this.ClientSize = new System.Drawing.Size(297, 108);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.RenameCan);
            this.Controls.Add(this.RenameOK);
            this.Controls.Add(this.RenameText);
            this.Controls.Add(this.TitleText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ErrorItem";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RenameMove);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.RenameMove2);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.RenameMove3);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void RenameText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                RenameOK.PerformClick();
            }
            if (e.KeyCode == Keys.Escape)
            {
                RenameCan.PerformClick();
            }
        }
        public NumKey key;
        private void RenameText_MouseDown(object sender, MouseEventArgs e)
        {
            if (key != null)
            {
                    try { key.Value = double.Parse(((TextBox)sender).Text); } catch { key.Value = 0; }
                ((TextBox)sender).SelectAll();
                    if (key.ShowDialog() == DialogResult.OK)
                    {
                        ((TextBox)sender).Text = "" + key.Value;
                        ((TextBox)sender).SelectAll();
                    }
                
            }
        }
    }
}
