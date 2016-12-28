using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeyMove.Tools
{


    public class NetData
    {
        public List<int> cmdTimeout = new List<int>();
        public List<int> cmdUse = new List<int>();
        public List<byte[]> cmdData = new List<byte[]>();
    }
    public class UserInfo
    {
        public long ip;
        public int id;
        public NetData net;
        public UserInfo(int id, long ip)
        {
            this.id = id;
            this.ip = ip;
            net = new NetData();
        }
        public override string ToString()
        {
            return string.Format("ID:{0}[{4}.{3}.{2}.{1}]", id, (ip >> 24) & 0xff, (ip >> 16) & 0xff, (ip >> 8) & 0xff, ip & 0xff);
        }
    }
    class ESPUDP
    {
        UdpClient Search = new UdpClient(0);
        public int port;
        private byte[] GetIPID;
        private byte[] GetData;
        private IPAddress[] ips;
        private IPEndPoint OutPoint;
        private bool isRun;
        private List<UserInfo> UserList = new List<UserInfo>();
        public delegate UserInfo newCallBack(int id, long ip);

        const int TimeOut = 10;

        public List<UserInfo> UserGroup
        {
            get { return UserList; }
        }

        public bool FindNewDev { get; set; }

        newCallBack callback=null;

        Dictionary<int, Action<UserInfo, ByteStream>> ActionMap = new Dictionary<int, Action<UserInfo, ByteStream>>();

        public void WriteInt16(Stream s, int data)
        {
            s.WriteByte((byte)((data >> 8) & 0xff));
            s.WriteByte((byte)((data) & 0xff));
        }

        public void WriteInt32(Stream s, int data)
        {
            s.WriteByte((byte)((data >> 24) & 0xff));
            s.WriteByte((byte)((data >> 16) & 0xff));
            s.WriteByte((byte)((data >> 8) & 0xff));
            s.WriteByte((byte)((data) & 0xff));
        }


        byte[] buildPacket(int id, byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            byte sum = 0;
            ms.WriteByte(0xAA);
            WriteInt16(ms, data.Length);
            ms.WriteByte((byte)id);
            ms.Write(data, 0, data.Length);
            foreach (byte v in data)
                sum += v;
            ms.WriteByte(sum);
            ms.WriteByte(0x55);
            return ms.ToArray();
        }

        byte[] Object2Byte(object[] data)
        {
            MemoryStream ms = new MemoryStream();
            foreach (object obj in data)
            {
                byte[] outbuff;
                if (obj is int)
                    WriteInt16(ms, (int)obj);
                else if (obj is long)
                    WriteInt32(ms, (int)(long)obj);
                else if (obj is byte)
                    ms.WriteByte((byte)obj);
                else if ((outbuff = obj as byte[]) != null)
                    ms.Write(outbuff, 0, outbuff.Length);
            }
            return ms.ToArray();
        }

        //public byte[] buildPacket(int id,byte[] data)
        //{
        //    MemoryStream ms = new MemoryStream();
        //    byte sum = 0;
        //    byte[] outbuff;
        //    ms.WriteByte(0xAA);
        //    WriteInt16(ms, data.Length);
        //    ms.WriteByte((byte)id);
        //    ms.Write(data, 0, data.Length);
        //    ms.WriteByte(0);
        //    ms.WriteByte(0x55);
        //    outbuff = ms.ToArray();
        //    for(int i = 0; i < outbuff.Length-6; i++)
        //    {
        //        sum += outbuff[i + 4];
        //    }
        //    outbuff[1] = (byte)((outbuff.Length - 6) / 256);
        //    outbuff[2] = (byte)((outbuff.Length - 6) % 256);
        //    outbuff[outbuff.Length - 2] = sum;
        //    return outbuff;
        //}

        IPAddress[] getIPAddress()
        {
            List<IPAddress> iplist = new List<IPAddress>();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                //判断是否为以太网卡
                //Wireless80211         无线网卡    Ppp     宽带连接
                //Ethernet              以太网卡   
                //这里篇幅有限贴几个常用的，其他的返回值大家就自己百度吧！
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet || adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    if (adapter.Speed < 0) continue;
                    //获取以太网卡网络接口信息
                    IPInterfaceProperties ip = adapter.GetIPProperties();
                    //获取单播地址集
                    UnicastIPAddressInformationCollection ipCollection = ip.UnicastAddresses;
                    foreach (UnicastIPAddressInformation ipadd in ipCollection)
                    {
                        //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址
                        //Max            MAX 位址
                        if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                            //判断是否为ipv4
                            iplist.Add(ipadd.Address);//获取ip
                    }
                }
            }
            return iplist.ToArray();
        }

        UserInfo getUserFormIP(long ip) {

            foreach(UserInfo info in UserList)
                if (ip == info.ip) return info;
            return null;
        }


        void RecvLoop()
        {
            byte[] buff = Search.Receive(ref OutPoint);
            long ip= OutPoint.Address.Address;
            OutPoint.Address = IPAddress.Any;
            OutPoint.Port = 0;
            string value = Encoding.Default.GetString(buff);
            UserInfo info = getUserFormIP(ip);
            if (buff[0] == 0xAA)
            {
                if (ActionMap.ContainsKey(buff[3])&&info!=null)
                {
                    ByteStream bs = new ByteStream(buff);
                    bs.offset(4);
                    int pos = info.net.cmdUse.IndexOf(buff[3]);
                    if (pos != -1)
                    {
                        info.net.cmdTimeout.RemoveAt(pos);
                        info.net.cmdData.RemoveAt(pos);
                        info.net.cmdUse.RemoveAt(pos);
                    }
                    ActionMap[buff[3]](info,bs);
                }
            }
            else
            {
                if (value.IndexOf("ID:") == 0)
                {
                    int id = int.Parse(value.Substring(3));
                    if (info == null)
                    {
                        if (callback != null)
                            info = callback(id, ip);
                        else
                            info = new UserInfo(id, ip);
                        if (info != null)
                            UserList.Add(info);
                    }
                }
            }
        }

        int SearchCount=0;
        private List<UserInfo> PipeUser = new List<UserInfo>();

        void SendLoop()
        {
            if (++SearchCount == 200 && FindNewDev)
            {
                SearchCount = 0;
                BroadcastData(GetIPID);
                ips = getIPAddress();
            }
            foreach (UserInfo info in UserList)
            {
                if (info.net.cmdUse.Count != 0)
                {
                    lock (info.net)
                    {
                        for (int i = 0; i < info.net.cmdUse.Count; i++)
                            if (info.net.cmdTimeout[i] != 0)
                                if (--info.net.cmdTimeout[i] == 0)
                                {
                                    Search.Send(info.net.cmdData[i], info.net.cmdData[i].Length, new IPEndPoint(info.ip, port));
                                    info.net.cmdTimeout[i] = TimeOut;
                                }
                    }
                }
                if (SearchCount % 10 == 0 && SearchCount != 0 && PipeUser.Contains(info))
                    Search.Send(GetData, GetData.Length, new IPEndPoint(info.ip, port));
            }
        }

        void BroadcastData(byte[] data)
        {
            for (int i = 0; i < ips.Length; i++)
            {
                Search.Send(data, data.Length, new IPEndPoint(ips[i].Address | 0xff000000, 2333));
            }
        }        

        enum EventType
        {
            newUser=0,
            RecvData=1,
        }

        public void NewUserCallBack(newCallBack act)
        {
            callback = act;
        }

        public void OnRecvData(int id, Action<UserInfo, ByteStream> act)
        {
            if (ActionMap.ContainsKey(id))
                ActionMap[id] = act;
            else
                ActionMap.Add(id, act);
        }



        public void SendData(UserInfo info,int id,params object[] buff)
        {
            NetData data = info.net;
            int code = id;
            int pos = data.cmdUse.IndexOf(code);
            byte[] v;           
            if (pos == -1)
            {
                data.cmdUse.Add((int)code);
                data.cmdData.Add(v=buildPacket(id, Object2Byte(buff)));
                data.cmdTimeout.Add(TimeOut);
            }
            else
            {
                data.cmdData[pos] =v= buildPacket(id, Object2Byte(buff));
                data.cmdTimeout[pos] = TimeOut;
            }
            Search.Send(v, v.Length, new IPEndPoint(info.ip, port));
        }
        public ESPUDP(int port=2333)
        {
            this.port = port;
            GetData = buildPacket(0, new byte[] { 0 });
            GetIPID = Encoding.Default.GetBytes("print(\"ID:\"..node.chipid())");
            ips = getIPAddress();
            isRun = true;
            OutPoint = new IPEndPoint(IPAddress.Any, 0);
            new Task(() => {
                while (isRun)
                    try {RecvLoop();}
                    catch (Exception e) { }
            }).Start();
            new Task(() => {
                while (isRun)
                    try { SendLoop(); Thread.Sleep(10); }
                    catch (Exception e) { }
            }).Start();
            FindNewDev = true;
        }




        
    }
}
