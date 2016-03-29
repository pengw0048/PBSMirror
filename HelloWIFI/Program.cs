using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using PBSUtil;

namespace HelloWIFI
{

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
            var wifioccur = new OccurenceCounter<string>();
            var wifioccurcount = new OccurenceCounter<int>();

            var sw = new Stopwatch();
            sw.Start();
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery2.dat")) {
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
                    foreach (var wf in query.wf)
                    {
                        wifioccur.add(wf.wifi);
                    }
                }
            }

            foreach (var item in wifioccur.dict)
            {
                wifioccurcount.add(item.Value);
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
            Console.WriteLine("WiFi occurence: " + wifioccurcount);
            Console.WriteLine("---Done---");
            Console.ReadLine();
        }
    }
}
