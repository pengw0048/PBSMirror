using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Device.Location;
using System;

namespace PBSUtil
{ 
    /// <summary> 对应wifiQuery中一条记录 </summary>
    [DataContract] public class WifiQuery
    {
        /// <summary> 百度聚类的结果 </summary>
        [DataMember] public Position wifi;
        /// <summary> 原始记录当中的行号 </summary>
        [DataMember] public int line;
        /// <summary> 根据短信内容的分类 good cheat spam </summary>
        [DataMember] public string function;
        /// <summary> 是否来自权威号 </summary>
        [DataMember] public bool isAuthority;
        /// <summary> wifi记录 </summary>
        [DataMember] public WifiRecord[] wf;
        /// <summary> 最近连接的基站记录 </summary>
        [DataMember(Name = "base")] public BaseStationRecord[] bs;
        /// <summary> google查询的的基站记录 </summary>
        [DataMember] public BaseStationRecord[] gbase;
    };
    /// <summary> 表示一个位置的经纬度、精度和时间 </summary>
    [DataContract] public class Position
    {
        /// <summary> 是否确定了位置 </summary>
        [DataMember] public bool tag;
        /// <summary> 经度 </summary>
        [DataMember] public double lon;
        /// <summary> 纬度 </summary>
        [DataMember] public double lat;
        /// <summary> 精度 </summary>
        [DataMember] public double accuracy;
        /// <summary> 相关的时间 </summary>
        [DataMember] public long time;
        /// <summary> 从BaseStationRecord转换 </summary>
        /// <param name='bs'> 传入的BaseStationRecord </param>
        /// <returns> 转换得到的Position对象 </returns>
        static public implicit operator Position(BaseStationRecord bs)
        {
            return new Position() { tag = bs.tag, lon = bs.lon, lat = bs.lat, accuracy = bs.radius, time = bs.time };
        }
    };
    /// <summary> 表示一个扫描到的WiFi记录 </summary>
    [DataContract] public class WifiRecord
    {
        /// <summary> mac地址，小写16进制，以:分割 </summary>
        [DataMember] public string wifi;
        /// <summary> 与百度聚类结果之间的距离（米） </summary>
        [DataMember] public double distance;
        /// <summary> 经度 </summary>
        [DataMember] public double lon;
        /// <summary> 纬度 </summary>
        [DataMember] public double lat;
        /// <summary> 是否查到了位置 </summary>
        [DataMember] public bool tag;
    };
    /// <summary> 表示一个扫描到的基站记录 </summary>
    [DataContract] public class BaseStationRecord
    {
        /// <summary> 基站id，MCC|MNC|LAC|CID </summary>
        [DataMember] public string id;
        /// <summary> 信号强度 </summary>
        [DataMember] public double cellStrength;
        /// <summary> id语法是否正确 </summary>
        [DataMember] public bool legal;
        /// <summary> 经度 </summary>
        [DataMember] public double lon;
        /// <summary> 纬度 </summary>
        [DataMember] public double lat;
        /// <summary> 基站覆盖半径或误差范围？ </summary>
        [DataMember] public double radius;
        /// <summary> 是否查到了位置 </summary>
        [DataMember] public bool tag;
        /// <summary> 连接到此基站的时间，最大的为当前 </summary>
        [DataMember] public long time;
    };
    /// <summary> 对TKey类型对象的计数器 </summary>
    public class OccurenceCounter<TKey>
    {
        /// <summary> 只读。用来记录次数的Dictionary对象 </summary>
        public Dictionary<TKey, int> dict { get { return _dict; } }
        /// <summary> 用来记录次数的私有Dictionary对象 </summary>
        private Dictionary<TKey, int> _dict;
        /// <summary> 构造方法 </summary>
        public OccurenceCounter()
        {
            _dict = new Dictionary<TKey, int>();
        }
        /// <summary> 从BaseStationRecord转换 </summary>
        /// <param name='key'> 给此对象增加一次计数 </param>
        public void add(TKey key)
        {
            if (!dict.ContainsKey(key)) _dict.Add(key, 0);
            _dict[key]++;
        }
        /// <summary> 以字符串形式表示结果 </summary>
        /// <returns> [key, count]形式的计数结果 </returns>
        public override string ToString()
        {
            string ret = "";
            bool first = true;
            foreach (var item in _dict)
            {
                if (!first) ret+=" ";
                ret += item;
                first = false;
            }
            return ret;
        }
    }
    /// <summary> 提供对地图相关操作的类，不能实例化 </summary>
    public static class Map
    {
        /// <summary> 计算地球上两点间的球面距离 </summary>
        /// <param name='sLatitude'> 起点的纬度 </param>
        /// <param name='sLongitude'> 起点的经度 </param>
        /// <param name='eLatitude'> 终点的纬度 </param>
        /// <param name='eLongitude'> 终点的经度 </param>
        /// <returns> 所给两点之间的距离，单位为米 </returns>
        public static double Distance(double sLatitude, double sLongitude, double eLatitude, double eLongitude)
        {
            var sCoord = new GeoCoordinate(sLatitude, sLongitude);
            var eCoord = new GeoCoordinate(eLatitude, eLongitude);
            return sCoord.GetDistanceTo(eCoord);
        }
        /// <summary> 计算地球上两点间的球面距离 </summary>
        /// <param name='s'> 起点 </param>
        /// <param name='e'> 终点 </param>
        /// <returns> 所给两点之间的距离，单位为米 </returns>
        public static double Distance(Position s, Position e)
        {
            return Distance(s.lat, s.lon, e.lat, e.lon);
        }
        /// <summary> 计算指定了时间的两点间的移动速度 </summary>
        /// <param name='s'> 起点 </param>
        /// <param name='e'> 终点 </param>
        /// <returns> 移动速度，单位为米每秒 </returns>
        public static double Speed(Position s, Position e)
        {
            double dist = Distance(e, s);
            dist -= s.accuracy + e.accuracy;
            if (dist < 0) dist = 0;
            return Math.Abs(dist / ((e.time - s.time) / 1000.0));
        }
    }
}