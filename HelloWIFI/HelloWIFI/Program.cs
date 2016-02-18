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

    class Program
    {
        static void Main(string[] args)
        {
            int total = 0;
            int JsonFail = 0;
            var functions = new HashSet<string>();
            int isAuths = 0;

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
                    functions.Add(query.function);
                    if (query.isAuthority) isAuths++;
                }
            }
            sw.Stop();
            Console.WriteLine("Elapsed time: " + sw.ElapsedMilliseconds + " ms");

            Console.WriteLine("Total records: " + total);
            Console.WriteLine("Json read error: " + JsonFail);
            Console.Write("function:");
            foreach (var func in functions)
            {
                Console.Write(" "+func);
            }
            Console.WriteLine();
            Console.WriteLine("isAuthority: " + isAuths);
            Console.WriteLine("---Done---");
            Console.ReadLine();
        }
    }
}
