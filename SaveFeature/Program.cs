using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using System.Linq;
using PBSUtil;

namespace SaveFeature
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery2.dat"))
            using (var fout = new StreamWriter("D:\\wifi\\feature.dat"))
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

                    if (query.bs.Length < 3) continue;

                    // x1: 基站定位与最近的Wi-Fi AP之间的距离
                    double eps1 = 10000;
                    double dist1 = double.MaxValue;
                    foreach (var wifi in query.wf)
                    {
                        if (wifi.tag)
                        {
                            if (query.bs[2].tag) dist1 = MMath.Min(dist1, MMath.Distance(query.bs[2], wifi));
                            if (query.gbase[2].tag) dist1 = MMath.Min(dist1, MMath.Distance(query.gbase[2], wifi));
                        }
                    }
                    double x1 = dist1 == double.MaxValue ? 0 : (1 - Math.Log(dist1) / Math.Log(eps1)) / (1 + Math.Log(dist1) / Math.Log(eps1));

                    // x2: Wi-Fi聚类中心与最近AP的距离
                    double dist2 = double.MaxValue;
                    if (query.wifi.tag)
                    {
                        foreach (var wifi in query.wf)
                        {
                            if (wifi.tag) dist2 = MMath.Min(dist2, MMath.Distance(wifi, query.wifi));
                        }
                    }
                    double x2 = (dist2 == double.MaxValue || dist2==0 ? 0 : MMath.Max(0, 1 - Math.Log10(dist2) / 6));

                    // x3: 来自权威号的异常消息
                    double x3 = 0;
                    if (query.isAuthority && query.function != "good") x3 = 1;

                    // x4: 最新两个基站之间的切换速度
                    double dist4 = double.MaxValue;
                    if (query.bs[1].tag && query.bs[2].tag) dist4 = MMath.Distance(query.bs[1], query.bs[2]);
                    if (query.gbase[1].tag && query.gbase[2].tag) dist4 = MMath.Min(dist4, MMath.Distance(query.gbase[1], query.gbase[2]));
                    double x4 = (dist4 == double.MaxValue || query.bs[1].time == query.bs[2].time) ? 0 : MMath.Min(dist4 / Math.Abs(query.bs[2].time - query.bs[1].time)/100, 1);

                    // x5: 切换到id不符合语法的基站
                    double x5 = 0;
                    if ((query.bs[1].tag || query.gbase[1].tag) && !(query.bs[2].tag || query.gbase[2].tag)) x5 = 1;

                    // x6: 信号强度
                    double sig = query.bs[2].cellStrength;
                    if (sig > 0) sig = sig * 2 - 113;
                    double x6 = sig >= -99 && sig < 0 ? MMath.Max(0, 1 + sig / 40) : 0;

                    // x7: 从正常定位的基站切换到无法定位的基站
                    double x7 = 0;
                    if (query.gbase[1].tag && !query.gbase[2].tag)
                    {
                        double dist7 = double.MaxValue;
                        foreach (var wifi in query.wf)
                        {
                            if (wifi.tag)
                            {
                                if (query.bs[1].tag) dist7 = MMath.Min(dist7, MMath.Distance(query.bs[1], wifi));
                                if (query.gbase[1].tag) dist7 = MMath.Min(dist7, MMath.Distance(query.gbase[1], wifi));
                            }
                        }
                        x7 = dist7 == double.MaxValue ? 0 : (1 - Math.Log(dist7) / Math.Log(eps1)) / (1 + Math.Log(dist7) / Math.Log(eps1));
                    }

                    fout.WriteLine(query.line + " " + x1 + " " + x2 + " " + x3 + " " + x4 + " " + x5 + " " + x6 + " " + x7);
                }

            sw.Stop();
            Console.WriteLine("Time: " + sw.ElapsedMilliseconds + "ms");
            Console.ReadLine();
        }
    }
}
