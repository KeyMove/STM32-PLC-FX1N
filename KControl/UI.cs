using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace KeyMove
{
    public class UI
    {
        
        //public static PictureBox[] buildInputImage(Control.ControlCollection Con, Point p, Image img)
        //{
        //    PictureBox[] plist = new PictureBox[32];
        //    Font f = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
        //    Size size = new System.Drawing.Size(19, 12);
        //    int count = 0;
        //    for (int y = 0; y < 4; y++)
        //        for (int x = 0; x < 8; x++)
        //        {
        //            Label d = new Label();
        //            d.AutoSize = true;
        //            d.Font = f;
        //            d.Location = new System.Drawing.Point(p.X + 6 + 38 * x, p.Y + 32 + 44 * y);
        //            d.Name = "LI" + count;
        //            d.Size = size;
        //            d.TabIndex = 1;
        //            d.Text = "I" + count;
        //            Con.Add(d);
        //            PictureBox c = new PictureBox();
        //            c.Image = img;
        //            c.Location = new System.Drawing.Point(p.X + 38 * x, p.Y + 44 * y);
        //            c.Name = "I" + count;
        //            c.Size = new System.Drawing.Size(32, 32);
        //            c.TabIndex = 1;
        //            c.TabStop = false;
        //            plist[count++] = c;
        //            Con.Add(c);
        //        }
        //    return plist;
        //}





        public class InputStats
        {
            //const int maxcount = 32;
            PictureBox[] plist;
            Label[] Llist;
            Bitmap[] Imgs=new Bitmap[2];
            Color[] BackColors = new Color[2];
            public int Count;
            public int Ecount;
            public Action<int,bool> SelectChange=null;
            public InputStats(Control.ControlCollection Con, Point p, Image[] imgs, int bcount, int bx, int by)
            {
                int len = (bcount < (bx * by) ? bcount : bx * by);
                plist = new PictureBox[len];
                Llist = new Label[len];
                Count = bcount;
                if (Imgs != null) { 
                    Imgs[0] = new Bitmap(imgs[0]);
                    Imgs[1] = new Bitmap(imgs[1]);
                }
                BackColors[0] = Color.Transparent;
                BackColors[1] = Color.Blue;
                //Image img = imgs.Images[0];
                Font f = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                Size size = new System.Drawing.Size(19, 12);
                int count = 0;
                for (int y = 0; y < by; y++)
                {
                    for (int x = 0; x < bx; x++)
                    {
                        Label d = new Label();
                        d.AutoSize = true;
                        d.Font = f;
                        d.Location = new System.Drawing.Point(p.X + 6 + 38 * x, p.Y + 32 + 44 * y);
                        d.Name = "LI" + count;
                        d.Size = size;
                        d.TabIndex = 1;
                        d.Text = "I" + (count + 1);
                        Con.Add(d);
                        Llist[count] = d;
                        PictureBox c = new PictureBox();
                        c.Image = Imgs[0];
                        c.BackColor = BackColors[0];
                        c.Location = new System.Drawing.Point(p.X + 38 * x, p.Y + 44 * y);
                        c.Name = "I" + count;
                        c.Size = new System.Drawing.Size(32, 32);
                        c.TabIndex = 1;
                        c.TabStop = false;
                        c.Click += new EventHandler((object sender, EventArgs e) =>
                          {
                              PictureBox pb = (PictureBox)sender;
                              if (pb.BackColor == BackColors[0])
                              {
                                  
                                  if(SelectChange!=null)
                                      for (int i = 0; i < plist.Length; i++)
                                      {
                                          if (sender.Equals(plist[i]))
                                          {
                                              SelectChange(i, true);
                                              break;
                                          }
                                      }
                                  pb.BackColor = BackColors[1];
                              }
                              else
                              {
                                  if (SelectChange != null)
                                      for (int i = 0; i < plist.Length; i++)
                                      {
                                          if (sender.Equals(plist[i]))
                                          {
                                              SelectChange(i, false);
                                              break;
                                          }
                                      }
                                  pb.BackColor = BackColors[0];
                              }
                          });
                        plist[count++] = c;
                        Con.Add(c);
                        if (count >= Count)
                        {
                            y = by;
                            x = bx;
                        }
                    }
                }
                for (int i = Count; i < Count; i++)
                {
                    plist[i].Visible = false;
                    Llist[i].Visible = false;
                }
            }

            public bool getSelectStats(int index)
            {
                return (index >= 0 && index < plist.Length) ? (plist[index].BackColor != BackColors[0]):false;
            }
            public void setSelectStats(int index,bool stats)
            {
                if (index>=0&&index < plist.Length) plist[index].BackColor = BackColors[(stats) ? 1 : 0];
            }

            public bool getStats(int index)
            {
                return (index >= 0 && index < plist.Length) ? plist[index].Image != Imgs[0]:false;
            }

            public bool getEnableStats(int index)
            {
                return (index >= 0 && index < plist.Length) ? plist[index].Enabled : false;
            }

            public void setEnableStats(int index,bool stats)
            {
                if (index >= 0 && index < plist.Length)
                {
                    plist[index].Enabled = stats;
                    Llist[index].Enabled = stats;
                }
            }

            public int EnableCount
            {
                get { return Count; }
                set
                {
                    if (value > Count) value = Count;
                    Ecount = value;
                    for(int i = 0; i < Ecount; i++)
                    {
                        plist[i].Visible = true;
                        Llist[i].Visible = true;
                    }
                    for (int i = Ecount; i < Count; i++)
                    {
                        plist[i].Visible = false;
                        Llist[i].Visible = false;
                    }
                }
            }

            public bool BackColor
            {
                get { return true; }
                set
                {
                    for(int i=0;i<this.Count;i++)
                    {
                        if (value)
                            plist[i].BackColor = BackColors[1];
                        else
                            plist[i].BackColor = BackColors[0];
                    }

                }
            }

            public string this[int index]
            {
                get { return Llist[index].Text; }
                set
                {
                    Llist[index].Text = value;
                }
            }

            public bool AllStats
            {
                get { return true; }
                set
                {
                    int index = 0;
                    if (value) index = 1;
                    int l = plist.Length;
                    for (int i = 0; i < l; i++)
                    {
                        plist[i].Image = Imgs[index];
                    }
                    GC.Collect();
                }
            }

            public void setStats(byte[] list)
            {
                int l = list.Length;
                int index = 0;
                for(int i = 0; i < l; i++)
                {
                    byte t = list[i];
                    for(int j = 0; j < 8; j++)
                    {
                        if ((t & 1)!=0)
                        {
                            if(this.plist[index].Image != Imgs[1])
                                this.plist[index].Image = Imgs[1];
                        }
                        else
                        {
                            if(this.plist[index].Image != Imgs[0])
                                this.plist[index].Image = Imgs[0];
                        }
                        t >>= 1;
                        index++;
                        if (index >= this.plist.Length)
                            return;
                    }
                }
            }

            public byte[] getStats()
            {
                int l = Ecount;
                byte[] value = new byte[(((l + 7) / 8) != 0) ? ((l + 7) / 8) : 1];
                int i = 0;
                int index = 0;
                byte t;
                while (l > 8)
                {
                    t = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        t >>= 1;
                        if (plist[i++].Image == Imgs[1])
                            t |= 0x80;
                    }
                    value[index++] = t;
                    l -= 8;
                }
                if (l != 0)
                {
                    t = 0;
                    int k = 0;
                    for (k = 0; k < l; k++)
                    {
                        t >>= 1;
                        if (plist[i++].Image == Imgs[1])
                            t |= 0x80;
                    }
                    k = 8 - k;
                    t >>= k;
                    value[index] = t;
                }
                return value;
            }
            public byte[] getSelectStats()
            {
                int l = Ecount;
                byte[] value = new byte[(((l + 7) / 8) != 0) ? ((l + 7) / 8) : 1];
                int i = 0;
                int index = 0;
                byte t;
                while (l > 8)
                {
                    t = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        t >>= 1;
                        if (plist[i++].BackColor == BackColors[1])
                            t |= 0x80;
                    }
                    value[index++] = t;
                    l -= 8;
                }
                if (l != 0)
                {
                    t = 0;
                    int k = 0;
                    for (k = 0; k < l; k++)
                    {
                        t >>= 1;
                        if (plist[i++].BackColor == BackColors[1])
                            t |= 0x80;
                    }
                    k = 8 - k;
                    t >>= k;
                    value[index] = t;
                }
                return value;
            }
        }

        //public static Button[] buildOutputButton(Control.ControlCollection Con, Point p, ImageList img)
        //{
        //    int x, y;
        //    int count = 0;
        //    Button[] blist = new Button[16];
        //    for (y = 0; y < 4; y++)
        //        for (x = 0; x < 4; x++)
        //        {
        //            Button b = new Button();
        //            b.FlatAppearance.BorderSize = 0;
        //            if (img != null)
        //            {
        //                b.ImageIndex = 0;
        //                b.ImageList = img;
        //            }
        //            b.Location = new System.Drawing.Point(p.X + 46 * x, p.Y + 46 * y);
        //            b.Name = "O" + count;
        //            b.Size = new System.Drawing.Size(40, 32);
        //            b.TabIndex = 1;
        //            b.Text = "O" + count + "\r\nOFF";
        //            b.UseVisualStyleBackColor = true;
        //            b.Click += new EventHandler((object sender, EventArgs e) => {
        //                Button c = (Button)sender;
        //                if (c.ImageList != null)
        //                    if (c.ImageList.Images.Count > 1)
        //                    {
        //                        if (c.ImageIndex == 0)
        //                        {
        //                            c.ImageIndex = 1;
        //                            c.Text = c.Name + "\r\nON";
        //                        }
        //                        else
        //                        {
        //                            c.ImageIndex = 0;
        //                            c.Text = c.Name + "\r\nOFF";
        //                        }
        //                        GC.Collect();
        //                    }
        //            });
        //            blist[count++] = b;
        //            Con.Add(b);
        //        }
        //    return blist;
        //}

        //public static void EnableButton(Button[] blist, bool enable)
        //{
        //    int len = blist.Length;
        //    for(int i = 0; i < len; i++)
        //    {
        //        blist[i].Enabled = enable;
        //    }
        //}



        public class OutputButton
        {
            //const int maxcount = 16;


            Button[] blist;
            public Action<int,bool> ButtonSwitch=null;

            Bitmap[] Imgs = new Bitmap[2];
            public int Count;
            public int Ecount;
            public OutputButton(Control.ControlCollection Con, Point p, Image[] img,int bcount,int bx,int by)
            {
                int x, y;
                int count = 0;
                int len = (bcount < (bx * by) ? bcount : bx * by);
                Count = len;
                blist = new Button[Count];
                if (img != null)
                {
                    Imgs[0] = new Bitmap(img[0]);
                    Imgs[1] = new Bitmap(img[1]);
                }
                for (y = 0; y < bx; y++)
                    for (x = 0; x < by; x++)
                    {
                        Button b = new Button();
                        b.FlatAppearance.BorderSize = 0;
                        b.Image = Imgs[0];
                        b.Location = new System.Drawing.Point(p.X + 46 * x, p.Y + 46 * y);
                        b.Name = "O" + (count + 1);
                        b.Size = new System.Drawing.Size(40, 32);
                        b.TabIndex = 1;
                        b.Text = "O" + (count + 1) + "\r\nOFF";
                        b.UseVisualStyleBackColor = true;
                        b.Click += B_Click;
                        blist[count++] = b;
                        Con.Add(b);
                        if (count >= Count)
                        {
                            y = bx;
                            x = by;
                        }
                    }
                for(int i = count; i < Count; i++)
                {
                    blist[i].Visible = false;
                }
            }

            private void B_Click(object sender, EventArgs e)
            {
               
                    Button c = (Button)sender;
                    if (c.Image == Imgs[0])
                    {
                        c.Image = Imgs[1];
                        c.Text = c.Name + "\r\nON";
                    }
                    else
                    {
                        c.Image = Imgs[0];
                        c.Text = c.Name + "\r\nOFF";
                    }
                    if (ButtonSwitch != null)
                    {
                        int blen = blist.Length;
                        for (int i = 0; i < blen; i++)
                        {
                            if (c == blist[i])
                            {
                                ButtonSwitch(i, (c.Image == Imgs[0]) ? false : true);
                            }
                        }
                    }
            }

            public void ClickButtion(int index)
            {
                if (index < blist.Length)
                    if (blist[index] != null)
                        B_Click(blist[index],null);
            }

            public int EnableCount
            {
                get { return Ecount; }
                set
                {
                    if (value > Count) value = Count;
                    Ecount = value;
                    for (int i = 0; i < Ecount; i++)
                    {
                        blist[i].Visible = true;
                    }
                    for(int i = Ecount; i < Count; i++)
                    {
                        blist[i].Visible = false;
                    }
                }
            }

            public bool Enable
            {
                get { return true; }
                set
                {
                    int l = blist.Length;
                    for (int i = 0; i < l; i++)
                    {
                        blist[i].Enabled = value;
                    }
                }
            }

            public bool AllStats
            {
                get { return true; }
                set
                {
                    int l = blist.Length;
                    for (int i = 0; i < l; i++)
                    {
                        if(value)
                            blist[i].Image=Imgs[1];
                        else
                            blist[i].Image = Imgs[0];
                    }
                    ButtonSwitch(0, value);
                }
            }

            public bool this[int index]
            {
                get { return (blist[index].Image == Imgs[0] ? true : false); }
                set
                {
                    Button c = blist[index];
                    if (c.Image == Imgs[0])
                    {
                        c.Image = Imgs[1];
                        c.Text = c.Name + "\r\nON";
                    }
                    else
                    {
                        c.Image = Imgs[0];
                        c.Text = c.Name + "\r\nOFF";
                    }
                    if (ButtonSwitch != null)
                    {
                        ButtonSwitch(index, (c.Image == Imgs[0]) ? false : true);
                    }
                }
            }

            public byte[] getStats()
            {
                int l = blist.Length;
                byte[] value = new byte[(((l+7) / 8)!=0)?((l+7) / 8):1];
                int i = 0;
                int index = 0;
                byte t;
                while (l > 8)
                {
                    t = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        t >>= 1;
                        if (blist[i++].Image == Imgs[1])
                            t |= 0x80;
                    }
                    value[index++] = t;
                    l -= 8;
                }
                if (l != 0)
                {
                    t = 0;
                    int k = 0;
                    for (k = 0; k < l; k++)
                    {
                        t >>= 1;
                        if (blist[i++].Image == Imgs[1])
                            t |= 0x80;
                    }
                    k = 8 - k;
                    t >>= k;
                    value[index] = t;
                }
                return value;
            }
        }
    }
}
