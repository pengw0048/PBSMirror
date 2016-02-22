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
        [DataMember] public BaiduWifiClustering wifi;
        [DataMember] public int line;
        [DataMember] public string function;
        [DataMember] public bool isAuthority;
        [DataMember] public WifiRecord[] wf;
        [DataMember(Name = "base")] public BaseStationRecord[] bs;
    };
    [DataContract] class BaiduWifiClustering
    {
        [DataMember] public bool exist;
        [DataMember] public double lon;
        [DataMember] public double lat;
    };
    [DataContract] class WifiRecord
    {
        [DataMember] public string wifi;
        [DataMember] public double distance;
        [DataMember] public double lon;
        [DataMember] public double lat;
        [DataMember] public bool tag;
    };
    [DataContract] class BaseStationRecord
    {
        [DataMember] public string id;
        [DataMember] public double cellStrength;
        [DataMember] public bool legal;
        [DataMember] public double lon;
        [DataMember] public double lat;
        [DataMember] public double radius;
        [DataMember] public bool tag;
        [DataMember] public long time;
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
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery.dat")) {
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
