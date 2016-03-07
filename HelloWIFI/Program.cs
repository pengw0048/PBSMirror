using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;

namespace HelloWIFI
{
    [DataContract] class WifiQuery
    {
        [DataMember] public BaiduWifiClustering wifi;               //百度聚类的结果
        [DataMember] public int line;                               //原始记录当中的行号
        [DataMember] public string function;                        //根据短信内容的分类 good cheat spam
        [DataMember] public bool isAuthority;                       //是否来自权威号
        [DataMember] public WifiRecord[] wf;                        //wifi记录
        [DataMember(Name = "base")] public BaseStationRecord[] bs;  //最近连接的基站记录
    };
    [DataContract] class BaiduWifiClustering
    {
        [DataMember] public bool tag;     //是否确定了位置
        [DataMember] public double lon;     //经度
        [DataMember] public double lat;     //纬度
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

    class Program
    {
        static void Main(string[] args)
        {
            int total = 0;
            int JsonFail = 0;
            var functions = new OccurenceCounter<string>();
            int isAuths = 0;
            var wfs = new OccurenceCounter<int>();
            var bss = new OccurenceCounter<int>();
            var bslegal = new OccurenceCounter<int>();
            var bstag = new OccurenceCounter<int>();

            var sw = new Stopwatch();
            sw.Start();
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery1.dat")) {
                while (!fs.EndOfStream)
                {
                    var line = fs.ReadLine();
                    total++;
                    WifiQuery query = null;
                    using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(line)))
                        try
                        {
                            query = (WifiQuery)ser.ReadObject(ms);
                        }
                        catch (Exception e)
                        {
                            JsonFail++;
                            Console.WriteLine(e.Message);
                            continue;
                        }
                    functions.add(query.function);
                    if (query.isAuthority) isAuths++;
                    wfs.add(query.wf.Length);
                    bss.add(query.bs.Length);
                    int legal = 0;
                    for (int i = 0; i < query.bs.Length; i++)
                        if (query.bs[i].legal) legal++;
                    bslegal.add(legal);
                    int tag = 0;
                    for (int i = 0; i < query.bs.Length; i++)
                        if (query.bs[i].tag) tag++;
                    bstag.add(tag);
                    /*foreach (var bs in query.bs)
                    {
                        if (bs.legal == false && bs.tag == true)
                            bs.legal = false;
                    }*/
                }
            }
            sw.Stop();
            Console.WriteLine("Elapsed time: " + sw.ElapsedMilliseconds + " ms");

            Console.WriteLine("Total records: " + total);
            Console.WriteLine("Json read error: " + JsonFail);
            Console.WriteLine("function:" + functions);
            Console.WriteLine("isAuthority: " + isAuths);
            Console.WriteLine("#wf: " + wfs);
            Console.WriteLine("#bs: " + bss);
            Console.WriteLine("#bs legal: " + bslegal);
            Console.WriteLine("#bs tag: " + bstag);
            Console.WriteLine("---Done---");
            Console.ReadLine();
        }
    }
}
