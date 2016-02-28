using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net;

namespace GoogleBaseLoc
{
    [DataContract]
    class WifiQuery
    {
        [DataMember]
        public BaiduWifiClustering wifi;
        [DataMember]
        public int line;
        [DataMember]
        public string function;
        [DataMember]
        public bool isAuthority;
        [DataMember]
        public WifiRecord[] wf;
        [DataMember(Name = "base")]
        public BaseStationRecord[] bs;
    };
    [DataContract]
    class BaiduWifiClustering
    {
        [DataMember]
        public bool exist;
        [DataMember]
        public double lon;
        [DataMember]
        public double lat;
    };
    [DataContract]
    class WifiRecord
    {
        [DataMember]
        public string wifi;
        [DataMember]
        public double distance;
        [DataMember]
        public double lon;
        [DataMember]
        public double lat;
        [DataMember]
        public bool tag;
    };
    [DataContract]
    class BaseStationRecord
    {
        [DataMember]
        public string id;
        [DataMember]
        public double cellStrength;
        [DataMember]
        public bool legal;
        [DataMember]
        public double lon;
        [DataMember]
        public double lat;
        [DataMember]
        public double radius;
        [DataMember]
        public bool tag;
        [DataMember]
        public long time;
    };
    [DataContract]
    class GoogleLocResponse
    {
        [DataMember]
        public GoogleLocLocation location;
        [DataMember]
        public double accuracy;
    };
    [DataContract]
    class GoogleLocLocation
    {
        [DataMember]
        public double lat;
        [DataMember]
        public double lng;
    };

    class Program
    {
        //private static string key = "AIzaSyCTPP_vSZaC-HDygwlij12UbVLVwVqYzsI";
        //private static string key = "AIzaSyB32yWrWa5C11kt0XpHHS3V13NekRENzS4";
        //private static string key = "AIzaSyC8vvERhzfR-HwI6hR_wqatVy5NSfwAxAk";
        //private static string key = "AIzaSyCgWuezqquuwt1TzEO2IjCZOL9TGtEOOp8";
        //private static string key = "AIzaSyDVPpVM4nNYWhGfYQtc574Qfrk6ixGYD0k";
        //private static string key = "AIzaSyCGfsA1u-9GiKfYWZCjTAkp1XZzFrtTp0Y";
        //private static string key = "AIzaSyDe705w27eoNvJp6gOVNx6MRav24hb8hhc";
        //private static string key = "AIzaSyD7qRLfpYPsY_cTGyuUmgeD8alT2M0457w";
        private static string[] keys = { "AIzaSyCTPP_vSZaC-HDygwlij12UbVLVwVqYzsI", "AIzaSyB32yWrWa5C11kt0XpHHS3V13NekRENzS4", "AIzaSyC8vvERhzfR-HwI6hR_wqatVy5NSfwAxAk", "AIzaSyCgWuezqquuwt1TzEO2IjCZOL9TGtEOOp8" , "AIzaSyDVPpVM4nNYWhGfYQtc574Qfrk6ixGYD0k" , "AIzaSyCGfsA1u-9GiKfYWZCjTAkp1XZzFrtTp0Y" , "AIzaSyDe705w27eoNvJp6gOVNx6MRav24hb8hhc", "AIzaSyD7qRLfpYPsY_cTGyuUmgeD8alT2M0457w",
        "AIzaSyDEyq1Kw77pNb5uvZIpoyLmxmcAROO1NW8", "AIzaSyD0o9VxqGInY1g8bhifouw5quFm9q3J4do", "AIzaSyB3qC6aF5Rl7H32GT_lCHH6vbYNaY6kjio", "AIzaSyAESwcwYREqeCLbTUju0gY1Y_WSQAQlTTg", "AIzaSyACawcyKsUOI_SktQ8rVdk4SWZTt-YCO5w", " AIzaSyB1Je0Zu_328gxrKrSjfSRKDO6t2ZfDzXM", "AIzaSyA6zGS5zPVOG2CfIcz4Bu_NPeKRrLnFtrQ", "AIzaSyBYcUNDut2f_b1oaXhj_0-0EstEFJbIM2o",
        "AIzaSyCIcGu27bZuZY1Qs7Qu1YmUnlFqxFA_9EM", "AIzaSyD8Wq7DnwK3r7AqWM5LFOgJhwc5LUkBJHw", "AIzaSyCsXPrIG5hU2OtBD4nGJ9xEAT74k-pNHBs", "AIzaSyBMSF3azviu0qBpmDKqUkowQcsZe2NKk24", "AIzaSyBZ0H-gDJmwOc2x3XBWz2JVBz_okryzsx4", "AIzaSyBmSMzonZoyDC-QogAu_8W0OSPTJJ32W60", "AIzaSyBo4-KQJUSHpoMeyMgUDtNKtcMe70apcQQ", "AIzaSyDw1YMqyytHiB851szytw5RcrRhywMlIYQ"};
        private static int keypos = 0;

        static void Main(string[] args)
        {
            var conn = new MySqlConnection("server=127.0.0.1;user id=root;password=;database=pbsmirror;pooling=false");
            conn.Open();
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            var ser2 = new DataContractJsonSerializer(typeof(GoogleLocResponse));
            using (var fs = new StreamReader("wifiQuery.dat"))
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
                    foreach (var bs in query.bs)
                    {
                        if (bs.id.Contains("null") || bs.id.Contains("-") || bs.id.Contains("/") || bs.id.Contains("@")) continue;
                        var cmd = new MySqlCommand("SELECT COUNT(*) FROM googlebs WHERE id='" + bs.id + "'", conn);
                        var reader = cmd.ExecuteReader();
                        reader.Read();
                        int ti = reader.GetInt32(0);
                        reader.Close();
                        if (ti == 0)
                        {
                            Console.Write(bs.id + " ... ");
                            int mcc, mnc, lac, cid;
                            try
                            {
                                var ts = bs.id.Split('|');
                                mcc = int.Parse(ts[0]);
                                mnc = int.Parse(ts[1]);
                                lac = int.Parse(ts[2]);
                                cid = int.Parse(ts[3]);
                            }
                            catch (Exception e) { Console.WriteLine(e.Message); continue; }
                            HttpWebRequest req = null;
                            HttpWebResponse res = null;
                            try
                            {
                                req = (HttpWebRequest)WebRequest.Create(new Uri("https://www.googleapis.com/geolocation/v1/geolocate?key=" + keys[keypos]));
                                req.Method = "POST";
                                req.ContentType = "application/json";
                                var post = "{\"considerIp\": \"false\",\"cellTowers\":[{\"cellId\":" + cid + ",\"locationAreaCode\":" + lac + ",\"mobileCountryCode\":" + mcc + ",\"mobileNetworkCode\":" + mnc + "}]}";
                                var data = Encoding.UTF8.GetBytes(post);
                                req.ContentLength = data.Length;
                                var stream = req.GetRequestStream();
                                stream.Write(data, 0, data.Length);
                                stream.Close();
                            }
                            catch (Exception e) { Console.WriteLine(e.Message); continue; }
                            bool tag = true, nolog = false;
                            double lat = 0, lon = 0, accuracy = 0;
                            try
                            {
                                res = (HttpWebResponse)req.GetResponse();
                                var glr = (GoogleLocResponse)ser2.ReadObject(res.GetResponseStream());
                                try { res.Close(); } catch (Exception) { }
                                lon = glr.location.lng;
                                lat = glr.location.lat;
                                accuracy = glr.accuracy;
                            }
                            catch (WebException e)
                            {
                                res = (HttpWebResponse)e.Response;
                                Console.Write(e.Message);
                                if (res.StatusCode == HttpStatusCode.Forbidden)
                                {
                                    Console.WriteLine("No quota. Move to next key.");
                                    keypos++;
                                    nolog = true;
                                    if (keypos == keys.Length)
                                    {
                                        Console.WriteLine("All keys invalid.");
                                        return;
                                    }
                                    continue;
                                }
                                else if (res.StatusCode == HttpStatusCode.NotFound)
                                {
                                    Console.Write(" ");
                                    tag = false;
                                }
                                else Console.WriteLine();
                                try { res.Close(); } catch (Exception) { }
                            }
                            catch (Exception e) { Console.WriteLine(e.Message); try { res.Close(); } catch (Exception) { } continue; }
                            if (!nolog)
                            {
                                cmd = new MySqlCommand("INSERT INTO googlebs(id,tag,lon,lat,accuracy) VALUES('" + bs.id + "'," + tag + "," + lon + "," + lat + "," + accuracy + "0)", conn);
                                cmd.ExecuteScalar();
                            }
                            Console.WriteLine(lon + " " + lat + " " + accuracy);
                        }
                    }
                }
            }
        }
    }
}

