﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;


namespace Visualize
{
    public partial class Form1 : Form
    {
        [DataContract]
        class WifiQuery
        {
            [DataMember]
            public BaiduWifiClustering wifi;               //百度聚类的结果
            [DataMember]
            public int line;                               //原始记录当中的行号
            [DataMember]
            public string function;                        //根据短信内容的分类 good cheat spam
            [DataMember]
            public bool isAuthority;                       //是否来自权威号
            [DataMember]
            public WifiRecord[] wf;                        //wifi记录
            [DataMember(Name = "base")]
            public BaseStationRecord[] bs;  //最近连接的基站记录
        };
        [DataContract]
        class BaiduWifiClustering
        {
            [DataMember]
            public bool tag;       //是否确定了位置
            [DataMember]
            public double lon;     //经度
            [DataMember]
            public double lat;     //纬度
        };
        [DataContract]
        class WifiRecord
        {
            [DataMember]
            public string wifi;        //mac地址
            [DataMember]
            public double distance;    //与百度聚类结果之间的距离（米）
            [DataMember]
            public double lon;         //经度
            [DataMember]
            public double lat;         //纬度
            [DataMember]
            public bool tag;           //是否查到了位置
        };
        [DataContract]
        class BaseStationRecord
        {
            [DataMember]
            public string id;          //基站id，MCC|MNC|LAC|CID
            [DataMember]
            public double cellStrength;//信号强度
            [DataMember]
            public bool legal;         //id语法是否正确
            [DataMember]
            public double lon;         //经度
            [DataMember]
            public double lat;         //纬度
            [DataMember]
            public double radius;      //基站覆盖半径或误差范围？
            [DataMember]
            public bool tag;           //是否查到了位置
            [DataMember]
            public long time;          //连接到此基站的时间，最大的为当前
        };

        private StreamReader sr = null;
        private int line = 0;
        private string showpage;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            sr = new StreamReader("D:\\wifi\\wifiQuery1.dat");
            Form1_Resize(null, null);
            showpage = File.ReadAllText("../../1.htm");
        }
        private static string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 1,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            bool HasBs;
            string line;
            WifiQuery query = null;
            do
            {
                this.line++;
                textBox1.Text = "Line " + this.line + "\r\n";
                line = sr.ReadLine();
                using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(line)))
                    try
                    {
                        query = (WifiQuery)ser.ReadObject(ms);
                    }
                    catch (Exception ex)
                    {
                        textBox1.Text += ex.Message + "\r\n";
                    }
                HasBs = false;
                foreach (var bs in query.bs)
                {
                    if (bs.tag) HasBs = true;
                }
            } while (!HasBs||!query.wifi.tag);
            textBox1.Text += ConvertJsonString(line);
            string ts = "";
            List<double> lons = new List<double>(), lats = new List<double>();
            if (query.wifi.tag == true)
            {
                ts += "marker=new BMap.Marker(new BMap.Point({lon},{lat}));map.addOverlay(marker);marker.setLabel(new BMap.Label(\"聚类结果\",{offset:new BMap.Size(20,-10)}));\r\n".Replace("{lon}",query.wifi.lon.ToString()).Replace("{lat}", query.wifi.lat.ToString());
                lons.Add(query.wifi.lon);
                lats.Add(query.wifi.lat);
            }
            foreach (var wifi in query.wf)
            {
                if (wifi.tag == false) continue;
                ts += "marker=new BMap.Marker(new BMap.Point({lon},{lat}),{icon:wifiIcon});map.addOverlay(marker);\r\n".Replace("{lon}", wifi.lon.ToString()).Replace("{lat}", wifi.lat.ToString());
                lons.Add(wifi.lon);
                lats.Add(wifi.lat);
            }
            foreach (var bs in query.bs)
            {
                if (bs.tag == false) continue;
                ts += "marker=new BMap.Marker(new BMap.Point({lon},{lat}),{icon:bsIcon});map.addOverlay(marker);\r\n".Replace("{lon}", bs.lon.ToString()).Replace("{lat}", bs.lat.ToString());
                lons.Add(bs.lon);
                lats.Add(bs.lat);
            }
            if (lons.Count == 0) { lons.Add(116.400244); lats.Add(39.92556); }
            string points = "";
            for(int i = 0; i < lons.Count; i++)
            {
                if (i > 0) points += ",";
                points += "new BMap.Point(" + lons[i] + "," + lats[i] + ")";
            }
            webBrowser1.DocumentText = showpage.Replace("{ts}", ts).Replace("{points}", points);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            webBrowser1.Height = this.Size.Height - 60;
            webBrowser1.Width = this.Size.Width - 280;
            textBox1.Height = this.Size.Height - 90;
            button1.Location = new Point(button1.Location.X, textBox1.Location.Y + textBox1.Height + 5);
        }
    }
    
}