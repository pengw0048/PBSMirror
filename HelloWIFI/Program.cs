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
            var gtag = new OccurenceCounter<int>();
            var wifioccur = new OccurenceCounter<string>();
            var wifioccurcount = new OccurenceCounter<int>();
            int b1g0 = 0, b0g1 = 0;

            var sw = new Stopwatch();
            sw.Start();
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            using (var fout = new StreamWriter("D:\\wifi\\dist.dat"))
            using (var foutg = new StreamWriter("D:\\wifi\\dist_g.dat"))
            using (var foutm = new StreamWriter("D:\\wifi\\distm.dat"))
            using (var foutgm = new StreamWriter("D:\\wifi\\dist_gm.dat"))
            using (var foutbg = new StreamWriter("D:\\wifi\\dist_bg.dat"))
            using (var foutbgm = new StreamWriter("D:\\wifi\\dist_bgm.dat"))
            using (var foutcenter = new StreamWriter("D:\\wifi\\dist_center.dat"))
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery2.dat"))
            using (var ffastswitch = new StreamWriter("D:\\wifi\\fastswitch.dat"))
            using (var signal = new StreamWriter("D:\\wifi\\signal.dat"))
            {
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
                    for (int i = 0; i < query.bs.Length; i++) query.gbase[i].time = query.bs[i].time;

                    functions.add(query.function);
                    if (query.isAuthority) isAuths++;
                    wfs.add(query.wf.Length);
                    bss.add(query.bs.Length);
                    int legal = 0;
                    for (int i = 0; i < query.bs.Length; i++)
                        if (query.bs[i].legal) legal++;
                    bslegal.add(legal);
                    int tag = 0;
                    int gtagc = 0;
                    for (int i = 0; i < query.bs.Length; i++)
                        if (query.bs[i].tag) tag++;
                    for (int i = 0; i < query.gbase.Length; i++)
                        if (query.gbase[i].tag) gtagc++;
                    bstag.add(tag);
                    gtag.add(gtagc);
                    foreach (var bs in query.bs)
                    {
                        if (bs.legal == false && bs.tag == true)
                            bs.legal = false;
                    }

                    double maxdist = double.MaxValue;
                    double maxdistg = double.MaxValue;
                    foreach (var wf in query.wf)
                    {
                        wifioccur.add(wf.wifi);
                    }
                    foreach (var bs in query.bs)
                    {
                        if (!bs.tag) continue;
                        foreach (var wf in query.wf)
                        {
                            if (!wf.tag) continue;
                            maxdist = MMath.Min(maxdist, MMath.Distance(bs, wf));
                            fout.WriteLine(MMath.Distance(bs, wf));
                        }
                    }
                    foreach (var bs in query.gbase)
                    {
                        if (!bs.tag) continue;
                        foreach (var wf in query.wf)
                        {
                            if (!wf.tag) continue;
                            maxdistg = MMath.Min(maxdistg, MMath.Distance(bs, wf));
                            foutg.WriteLine(MMath.Distance(bs, wf));
                        }
                    }
                    for (int i = 0; i < query.bs.Length; i++)
                    {
                        if (!query.bs[i].tag && query.gbase[i].tag) b0g1++;
                        if (query.bs[i].tag && !query.gbase[i].tag) b1g0++;
                        if (!query.bs[i].tag || !query.gbase[i].tag) continue;
                        foutbg.WriteLine(MMath.Distance(query.bs[i], query.gbase[i]));
                        if (query.wifi.tag)
                        {
                            double md = double.MaxValue;
                            if (query.bs[i].tag) md = MMath.Min(md, MMath.Distance(query.wifi, query.bs[i]));
                            if (query.gbase[i].tag) md = MMath.Min(md, MMath.Distance(query.wifi, query.gbase[i]));
                            if (md < double.MaxValue) foutcenter.WriteLine(md);
                        }
                    }
                    for (int i = query.bs.Length - 1; i < query.bs.Length; i++)
                    {
                        if (i < 0) continue;
                        double md = double.MaxValue;
                        foreach (var wf in query.wf)
                        {
                            if (query.bs[i].tag) md = MMath.Min(md, MMath.Distance(query.bs[i], wf));
                            if (query.gbase[i].tag) md = MMath.Min(md, MMath.Distance(query.gbase[i], wf));
                        }
                        if (md < double.MaxValue) foutbgm.WriteLine(md);
                    }
                    if (maxdist < double.MaxValue) foutm.WriteLine(maxdist);
                    if (maxdistg < double.MaxValue) foutgm.WriteLine(maxdistg);

                    bool FastSwitch = false;
                    if (query.gbase.Length >= 2 && query.gbase[0].tag && query.gbase[1].tag)
                    {
                        double speed1 = MMath.Speed(query.gbase[0], query.gbase[1]);
                        double speed2 = MMath.Speed(query.bs[0], query.bs[1]);
                        if (!double.IsInfinity(speed1) && speed1 > 0) { FastSwitch = true; ffastswitch.WriteLine(speed1); }
                    }
                    if (query.gbase.Length >= 3 && query.gbase[2].tag && query.gbase[1].tag)
                    {
                        double speed1 = MMath.Speed(query.gbase[1], query.gbase[2]);
                        double speed2 = MMath.Speed(query.bs[1], query.bs[2]);
                        if (!double.IsInfinity(speed1) && speed1 > 0) { FastSwitch = true; ffastswitch.WriteLine(speed1); }
                    }
                    foreach (var bs in query.bs)
                    {
                        signal.WriteLine(bs.cellStrength);
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
            Console.WriteLine("#gbase tag: " + gtag);
            Console.WriteLine("WiFi occurence: " + wifioccurcount);
            Console.WriteLine("B1G0: " + b1g0);
            Console.WriteLine("B0G1: " + b0g1);
            Console.WriteLine("---Done---");
            Console.ReadLine();
        }
    }
}
