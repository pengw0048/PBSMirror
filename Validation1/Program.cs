using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using System.Linq;
using PBSUtil;

namespace Validation1
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();
            var apdict = new Dictionary<string, WifiAP>();

            var list1a = new List<int>();
            var list1b = new List<int>();
            var list2 = new List<int>();
            var list3a = new List<int>();
            var list3b = new List<int>();
            var list4 = new List<int>();
            var lists = new List<int>[] { list1a, list1b, list2, list3a, list3b, list4 };

            int mrc1=0, mrc2=0, mrc12=0,mwc1=0,mwc2=0,mwc12=0;

            int FraudFromAuthCount = 0, DistanceInvalidCount = 0, Invalid1 = 0, FastSwitchCount = 0, ToIllegalBSCount = 0;
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery2.dat"))
            using (var out1 = new StreamWriter("invalid1.log"))
            using (var out2 = new StreamWriter("invalidany.log"))
            using (var mustright1 = new StreamWriter("mustright1.log"))
            using (var mustright2 = new StreamWriter("mustright2.log"))
            using (var mustwrong1 = new StreamWriter("mustwrong1.log"))
            using (var mustwrong2 = new StreamWriter("mustwrong2.log"))
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
                    //must right 1
                    bool mr1 = false;
                    if (query.bs.Length>0&& (query.bs.Last().tag == true || query.gbase.Last().tag == true))
                    {
                        double md = double.MaxValue;
                        foreach (var wifi in query.wf)
                        {
                            if (wifi.tag == false) continue;
                            if (query.bs.Last().tag == true) md = MMath.Min(md, MMath.Distance(wifi, query.bs.Last()));
                            if (query.gbase.Last().tag == true) md = MMath.Min(md, MMath.Distance(wifi, query.gbase.Last()));
                        }
                        if (md < 5000) { mustright1.WriteLine(line); mr1 = true; mrc1++; }
                    }
                    //must right 2
                    if ( query.gbase.Length == 3 && query.gbase[1].tag && query.gbase[2].tag)
                    {
                        if (MMath.Distance(query.gbase[1], query.gbase[2]) < 10000)
                        {
                            if (mr1) mrc12++;
                            mrc2++;
                            mr1 = true;
                            mustright2.WriteLine(line);
                        }
                    }
                    //检查聚类结果和基站位置是否相符
                    bool DistanceInvalid = false;bool di1 = false;
                    if (query.wifi.tag == true)
                    {
                        foreach (var bs in query.gbase)
                        {
                            if (bs.tag == true && MMath.Distance(bs.lat, bs.lon, query.wifi.lat, query.wifi.lon) > 5000)
                            {
                                DistanceInvalidCount++;
                                DistanceInvalid = true;
                                break;
                            }
                        }
                    }
                    if (DistanceInvalid) { list1a.Add(query.line); di1 = true; }
                    DistanceInvalid = false;
                    if (query.wifi.tag == true)
                    {
                        foreach (var bs in query.gbase)
                        {
                            if (bs.tag == true && MMath.Distance(bs.lat, bs.lon, query.wifi.lat, query.wifi.lon) > 25000)
                            {
                                DistanceInvalid = true;
                                break;
                            }
                        }
                    }
                    if (DistanceInvalid) { list1b.Add(query.line); di1 = true; }
                    //是否内容不正常但来自权威号
                    bool FraudFromAuth = false;
                    if ((query.function == "cheat" || query.function == "spam") && query.isAuthority == true)
                    {
                        FraudFromAuthCount++;
                        mwc1++;
                        FraudFromAuth = true;
                        mustwrong1.WriteLine(line);
                    }
                    if (FraudFromAuth) list2.Add(query.line);
                    //是否基站切换速度过快
                    bool FastSwitch = false, fs1 = false;
                    if (query.gbase.Length >= 2 && query.gbase[0].tag && query.gbase[1].tag)
                    {
                        double speed1 = MMath.Speed(query.gbase[0], query.gbase[1]);
                        double speed2 = MMath.Speed(query.bs[0], query.bs[1]);
                        if (!double.IsInfinity(speed1) && speed1 >= 50) { FastSwitch = true; FastSwitchCount++; }
                    }
                    if (FastSwitch == false && query.gbase.Length >= 3 && query.gbase[2].tag && query.gbase[1].tag)
                    {
                        double speed1 = MMath.Speed(query.gbase[1], query.gbase[2]);
                        double speed2 = MMath.Speed(query.bs[1], query.bs[2]);
                        if (!double.IsInfinity(speed1) && speed1 >= 50) { FastSwitch = true; FastSwitchCount++; }
                    }
                    if (FastSwitch) { list3a.Add(query.line); fs1 = true; }
                    FastSwitch = false;
                    if (query.gbase.Length >= 2 && query.gbase[0].tag && query.gbase[1].tag)
                    {
                        double speed1 = MMath.Speed(query.gbase[0], query.gbase[1]);
                        double speed2 = MMath.Speed(query.bs[0], query.bs[1]);
                        if (!double.IsInfinity(speed1) && speed1 >= 500) { FastSwitch = true;  }
                    }
                    if (FastSwitch == false && query.gbase.Length >= 3 && query.gbase[2].tag && query.gbase[1].tag)
                    {
                        double speed1 = MMath.Speed(query.gbase[1], query.gbase[2]);
                        double speed2 = MMath.Speed(query.bs[1], query.bs[2]);
                        if (!double.IsInfinity(speed1) && speed1 >= 500) { FastSwitch = true;  }
                    }
                    if (FastSwitch) { list3b.Add(query.line); fs1 = true; }
                    //是否切换到了语法错误、查询不到的基站
                    bool ToIllegalBS = false;
                    int bl = query.bs.Length - 1;
                    if (bl >= 1 && (query.bs[bl - 1].tag || query.gbase[bl - 1].tag) && (!query.bs[bl].tag && !query.gbase[bl].tag)/* && !query.bs[bl].legal*/)
                    {
                        ToIllegalBS = true;
                        ToIllegalBSCount++;
                    }
                    if (ToIllegalBS) { list4.Add(query.line); }
                    if (DistanceInvalid) { out1.WriteLine(line); Invalid1++; }
                    if ((di1 || fs1 || FraudFromAuth || ToIllegalBS) && query.wifi.tag) out2.WriteLine(line);
                    //must wrong 2
                    if (query.bs.Length > 0)
                    {
                        var sig = query.bs.Last().cellStrength;
                        if (sig > -99 && sig < 99)
                        {
                            if (sig >= 0) sig = -113 + 2 * sig;
                            if (sig > -30)
                            {
                                mwc2++;
                                if (FraudFromAuth) mwc1++;
                                mustwrong2.WriteLine(line);
                            }
                        }
                    }
                }
            }
            sw.Stop();
            Console.WriteLine("Elapsed time: " + sw.ElapsedMilliseconds + " ms");
            Console.WriteLine("Fraud from authority: " + FraudFromAuthCount);
            Console.WriteLine("Distance invalid: " + DistanceInvalidCount);
            Console.WriteLine("BS handover too fast: " + FastSwitchCount);
            Console.WriteLine("Switch to illegal BS: " + ToIllegalBSCount);
            Console.WriteLine("Invalid1: " + Invalid1);
            Console.WriteLine("Must right: " + mrc1 + "," + mrc2 + "," + mrc12);
            Console.WriteLine("Must wrong: " + mwc1 + "," + mwc2 + "," + mwc12);
            Console.WriteLine("Invalid matrix:");
            for(int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6; j++) Console.Write(lists[i].Intersect(lists[j]).Count() + " ");
                Console.WriteLine();
            }
            Console.WriteLine("---Done---");
            Console.ReadLine();
        }
    }
}
