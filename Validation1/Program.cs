using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using System.Device.Location;

namespace Validation1
{
    [DataContract] class WifiQuery
    {
        [DataMember] public Position wifi;               //百度聚类的结果
        [DataMember] public int line;                               //原始记录当中的行号
        [DataMember] public string function;                        //根据短信内容的分类 good cheat spam
        [DataMember] public bool isAuthority;                       //是否来自权威号
        [DataMember] public WifiRecord[] wf;                        //wifi记录
        [DataMember(Name = "base")] public BaseStationRecord[] bs;  //最近连接的基站记录
        [DataMember] public BaseStationRecord[] gbase;
    };
    [DataContract] class Position
    {
        [DataMember] public bool tag;       //是否确定了位置
        [DataMember] public double lon;     //经度
        [DataMember] public double lat;     //纬度
        [DataMember] public double accuracy;
        [DataMember] public long time;
        static public implicit operator Position(BaseStationRecord bs)
        {
            return new Position() { tag = bs.tag, lon = bs.lon, lat = bs.lat, accuracy = bs.radius, time = bs.time };
        }
    };
    [DataContract] class WifiRecord
    {
        [DataMember] public string wifi;        //mac地址
        [DataMember] public double distance;    //与百度聚类结果之间的距离（米）
        [DataMember] public double lon;         //经度
        [DataMember] public double lat;         //纬度
        [DataMember] public bool tag;           //是否查到了位置
    };
    [DataContract] class BaseStationRecord
    {
        [DataMember] public string id;          //基站id，MCC|MNC|LAC|CID
        [DataMember] public double cellStrength;//信号强度
        [DataMember] public bool legal;         //id语法是否正确
        [DataMember] public double lon;         //经度
        [DataMember] public double lat;         //纬度
        [DataMember] public double radius;      //基站覆盖半径或误差范围？
        [DataMember] public bool tag;           //是否查到了位置
        [DataMember] public long time;          //连接到此基站的时间，最大的为当前
    };

    class OccurenceCounter<TKey>
    {
        private Dictionary<TKey, int> dict;
        public OccurenceCounter()
        {
            dict = new Dictionary<TKey, int>();
        }
        public void add(TKey key)
        {
            if (!dict.ContainsKey(key)) dict.Add(key, 0);
            dict[key]++;
        }
        public override string ToString()
        {
            string ret = "";
            bool first = true;
            foreach (var item in dict)
            {
                if (!first) ret+=" ";
                ret += item;
                first = false;
            }
            return ret;
        }
    }

    class GoogleBaseLoc
    {
        private Dictionary<string, Position> dict;
        public GoogleBaseLoc()
        {
            dict = new Dictionary<string, Position>();
            using(var sr=new StreamReader("D:\\wifi\\google.dat"))
                while (!sr.EndOfStream)
                {
                    string[] ts = sr.ReadLine().Split('\t');
                    if (ts.Length != 6) continue;
                    if (ts[1] != "1") continue;
                    var pos = new Position() { tag = true, lon = double.Parse(ts[2]), lat = double.Parse(ts[3]), accuracy = double.Parse(ts[4]) };
                    dict.Add(ts[0].Trim(), pos);
                }
            Console.WriteLine("Google base station record loaded, " + dict.Count + " records.");
        }
        public Position query(string id)
        {
            if (!dict.ContainsKey(id))
                return new Position() { tag = false };
            return dict[id];
        }
    }

    class Program
    {
        static double Distance(double sLatitude, double sLongitude, double eLatitude, double eLongitude)
        {
            var sCoord = new GeoCoordinate(sLatitude, sLongitude);
            var eCoord = new GeoCoordinate(eLatitude, eLongitude);
            return sCoord.GetDistanceTo(eCoord);
        }

        static double Distance(Position s, Position e)
        {
            return Distance(s.lat, s.lon, e.lat, e.lon);
        }

        static double Speed(Position s, Position e)
        {
            double dist = Distance(e, s);
            dist -= s.accuracy + e.accuracy;
            if (dist < 0) dist = 0;
            return Math.Abs(dist / ((e.time - s.time) / 1000.0));
        }

        static void Main(string[] args)
        {
            //var google = new GoogleBaseLoc();
            var sw = new Stopwatch();
            sw.Start();
            int FraudFromAuthCount = 0, DistanceInvalidCount = 0, Invalid1 = 0, FastSwitchCount = 0, ToIllegalBSCount = 0;
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery2.dat"))
            using (var out1 = new StreamWriter("invalid1.log"))
            {
                while (!fs.EndOfStream)
                {
                    var line = fs.ReadLine();
                    WifiQuery query = null;
                    using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(line)))
                        try
                        {
                            query = (WifiQuery)ser.ReadObject(ms);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            continue;
                        }
                    for (int i = 0; i < query.bs.Length; i++) query.gbase[i].time = query.bs[i].time;
                    //检查聚类结果和基站位置是否相符
                    bool DistanceInvalid = false;
                    if (query.wifi.tag == true)
                    {
                        foreach (var bs in query.gbase)
                        {
                            if (bs.tag == true && Distance(bs.lat, bs.lon, query.wifi.lat, query.wifi.lon) > 20000)
                            {
                                DistanceInvalidCount++;
                                DistanceInvalid = true;
                                break;
                            }
                        }
                    }
                    //是否内容不正常但来自权威号
                    bool FraudFromAuth = false;
                    if ((query.function == "cheat" || query.function == "spam") && query.isAuthority == true)
                    {
                        FraudFromAuthCount++;
                        FraudFromAuth = true;
                    }
                    //是否基站切换速度过快
                    bool FastSwitch = false;
                    if (query.gbase.Length >= 2 && query.gbase[0].tag && query.gbase[1].tag)
                    {
                        double speed1 = Speed(query.gbase[0], query.gbase[1]);
                        double speed2 = Speed(query.bs[0], query.bs[1]);
                        if (!double.IsInfinity(speed1) && speed1 >= 100) { FastSwitch = true; FastSwitchCount++; }
                    }
                    if (FastSwitch == false && query.gbase.Length >= 3 && query.gbase[2].tag && query.gbase[1].tag)
                    {
                        double speed1 = Speed(query.gbase[1], query.gbase[2]);
                        double speed2 = Speed(query.bs[1], query.bs[2]);
                        if (!double.IsInfinity(speed1) && speed1 >= 100) { FastSwitch = true; FastSwitchCount++; }
                    }
                    //是否切换到了语法错误、查询不到的基站
                    bool ToIllegalBS = false;
                    int bl = query.bs.Length - 1;
                    if (bl >= 1 && (query.bs[bl - 1].tag || query.gbase[bl - 1].tag) && (!query.bs[bl].tag && !query.gbase[bl].tag)/* && !query.bs[bl].legal*/)
                    {
                        ToIllegalBS = true;
                        ToIllegalBSCount++;
                    }
                    if (ToIllegalBS) { out1.WriteLine(line); Invalid1++; }
                }
            }
            sw.Stop();
            Console.WriteLine("Elapsed time: " + sw.ElapsedMilliseconds + " ms");
            Console.WriteLine("Fraud from authority: " + FraudFromAuthCount);
            Console.WriteLine("Distance invalid: " + DistanceInvalidCount);
            Console.WriteLine("BS handover too fast: " + FastSwitchCount);
            Console.WriteLine("Switch to illegal BS: " + ToIllegalBSCount);
            Console.WriteLine("Invalid1: " + Invalid1);
            Console.WriteLine("---Done---");
            Console.ReadLine();
        }
    }
}
