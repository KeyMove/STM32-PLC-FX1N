using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KControl
{
    public class IOInfo
    {
        string msg;
        public int id { get; set; }
        public bool flag { get; set; }
        public IOInfo(string message)
        {
            id = -1;
            msg = message;
        }
        public override string ToString()
        {
            return string.Format(msg, (id<0)?"未指定":""+id);
        }

        public string ToString(string val)
        {
            return string.Format(msg, (id < 0) ? "未指定" : val);
        }
    }
}
