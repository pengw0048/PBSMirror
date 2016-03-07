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
    };
    [DataContract] class Position
    {
        [DataMember] public bool tag;       //是否确定了位置
        [DataMember] public double lon;     //经度
        [DataMember] public double lat;     //纬度
        [DataMember] public double accuracy;
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
        static double distance(double sLatitude, double sLongitude, double eLatitude, double eLongitude)
        {
            var sCoord = new GeoCoordinate(sLatitude, sLongitude);
            var eCoord = new GeoCoordinate(eLatitude, eLongitude);
            return sCoord.GetDistanceTo(eCoord);
        }

        static void Main(string[] args)
        {
            var google = new GoogleBaseLoc();
            var sw = new Stopwatch();
            sw.Start();
            int FraudFromAuthCount = 0, DistanceInvalidCount = 0, Invalid1 = 0;
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery1.dat"))
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
                    bool DistanceInvalid = false;
                    if (query.wifi.tag == true)
                    {
                        foreach (var bs in query.bs)
                        {
                            var pos = google.query(bs.id);
                            if (pos.tag == true && distance(pos.lat, pos.lon, query.wifi.lat, query.wifi.lon) > 10000)
                            {
                                DistanceInvalidCount++;
                                DistanceInvalid = true;
                            }
                        }
                    }
                    bool FraudFromAuth = false;
                    if ((query.function == "cheat" || query.function == "spam") && query.isAuthority == true)
                    {
                        FraudFromAuthCount++;
                        FraudFromAuth = true;
                    }
                    var tline = line.Substring(0, line.Length - 1)+",\"gbase\":[";
                    bool first = true;
                    foreach (var bs in query.bs)
                    {
                        if (!first) tline += ",";
                        first = false;
                        var pos = google.query(bs.id);
                        tline += "{\"id\":\"" + bs.id + "\",\"tag\":" + pos.tag.ToString().ToLower() + ",\"lat\":" + pos.lat + ",\"lon\":" + pos.lon + ",\"radius\":" + pos.accuracy + "}";
                    }
                    tline += "]}";
                    if (FraudFromAuth && DistanceInvalid) { out1.WriteLine(tline); Invalid1++; }
                }
            }
            sw.Stop();
            Console.WriteLine("Elapsed time: " + sw.ElapsedMilliseconds + " ms");
            Console.WriteLine("Fraud from authority: " + FraudFromAuthCount);
            Console.WriteLine("Distance invalid: " + DistanceInvalidCount);
            Console.WriteLine("Invalid1: " + Invalid1);
            Console.WriteLine("---Done---");
            Console.ReadLine();
        }
    }
}
