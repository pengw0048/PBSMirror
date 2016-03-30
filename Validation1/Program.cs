using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using System.Device.Location;
using PBSUtil;

namespace Validation1
{
    class Program
    {
        static void Main(string[] args)
        {
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
                            if (bs.tag == true && Map.Distance(bs.lat, bs.lon, query.wifi.lat, query.wifi.lon) > 20000)
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
                        double speed1 = Map.Speed(query.gbase[0], query.gbase[1]);
                        double speed2 = Map.Speed(query.bs[0], query.bs[1]);
                        if (!double.IsInfinity(speed1) && speed1 >= 100) { FastSwitch = true; FastSwitchCount++; }
                    }
                    if (FastSwitch == false && query.gbase.Length >= 3 && query.gbase[2].tag && query.gbase[1].tag)
                    {
                        double speed1 = Map.Speed(query.gbase[1], query.gbase[2]);
                        double speed2 = Map.Speed(query.bs[1], query.bs[2]);
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
