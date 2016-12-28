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
    public partial class ConfigIO : Form
    {
        KeyMove.UI.InputStats ib;
        Action<int, bool> act;
        TreeNode SelectNode;
        bool isNew = false;
        public ConfigIO()
        {
            InitializeComponent();
            act = (int id, bool flag) =>
             {
                 if (flag)
                 {
                     foreach (TreeNode Node in TextView.Nodes)
                     {
                         if (Node.Tag is IOInfo)
                         {
                             if (((IOInfo)Node.Tag).id == id)
                             {
                                 MessageBox.Show("该端口已被使用!");
                                 return;
                             }
                         }
                     }
                     if (SelectNode != null)
                     {
                         IOInfo info = ((IOInfo)SelectNode.Tag);
                         if (info.id != -1)
                         {
                             ib.setSelectStats(info.id, false);
                             ib.setSelectStats(id, true);
                             info.id = id;
                         }
                         else
                         {
                             info.id = id;
                         }
                         SelectNode.Text = info.ToString(ib[info.id]);
                     }
                 }
                 else
                 {
                     if (SelectNode != null)
                     {
                         ((IOInfo)SelectNode.Tag).id = -1;
                         SelectNode.Text = ((IOInfo)SelectNode.Tag).ToString();
                     }
                 }
             };
        }

        private void ConfigIO_Load(object sender, EventArgs e)
        {

        }

        public void load(Image[] imgs, int count)
        {
            ib = new KeyMove.UI.InputStats(this.panel1.Controls, new Point(10, 10), imgs, count, 8, 6);
            ib.SelectChange = act;
            _count = count;
        }

        int _count;
        public int Count
        {
            get { return _count; }
            set
            {
                _count = value;
                ib.EnableCount = value;
            }
        }

        public IOInfo addItem(string name)
        {
            TreeNode Node = new TreeNode();
            IOInfo info = new IOInfo(name);
            Node.Tag = info;
            Node.Text = info.ToString();
            TextView.Nodes.Add(Node);
            return info;
        }

        private void TextView_Click(object sender, EventArgs e)
        {

        }

        private void TextView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (TextView.SelectedNode == null)
                return;
            if (TextView.SelectedNode != SelectNode)
            {
                IOInfo info;
                if (SelectNode != null)
                {
                    info = ((IOInfo)SelectNode.Tag);
                    if (info.id != -1)
                    {
                        ib.setSelectStats(info.id, false);
                        ib.setEnableStats(info.id, false);
                    }
                }
                SelectNode = TextView.SelectedNode;
                info = ((IOInfo)SelectNode.Tag);
                if (info.id != -1)
                {
                    ib.setSelectStats(info.id, true);
                    ib.setEnableStats(info.id, true);
                }
            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            if (isNew) return;
            foreach (TreeNode Node in TextView.Nodes)
            {
                if(Node.Tag is IOInfo)
                {
                    isNew = true;
                    return;
                }
            }

        }
    }
}
