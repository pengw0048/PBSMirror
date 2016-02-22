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
        static void Main(string[] args)
        {
            var conn = new MySqlConnection("server=localhost;user id=root;password=;database=pbsmirror;pooling=false");
            conn.Open();
            var ser = new DataContractJsonSerializer(typeof(WifiQuery));
            var ser2 = new DataContractJsonSerializer(typeof(GoogleLocResponse));
            using (var fs = new StreamReader("D:\\wifi\\wifiQuery.dat"))
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
                        if (bs.id.Contains("null")) continue;
                        var cmd = new MySqlCommand("SELECT COUNT(*) FROM googlebs WHERE id='" + bs.id + "'", conn);
                        var reader = cmd.ExecuteReader();
                        reader.Read();
                        int ti = reader.GetInt32(0);
                        reader.Close();
                        if (ti == 0)
                        {
                            Console.Write(bs.id + " ... ");
                            string mcc, mnc, lac, cid;
                            try
                            {
                                var ts = bs.id.Split('|');
                                mcc = (ts[0]);
                                mnc = (ts[1]);
                                lac = (ts[2]);
                                cid = (ts[3]);
                            }
                            catch (Exception e) { Console.WriteLine(e.Message); continue; }
                            HttpWebRequest req = null;
                            HttpWebResponse res = null;
                            try
                            {
                                req = (HttpWebRequest)WebRequest.Create(new Uri("https://www.googleapis.com/geolocation/v1/geolocate?key=AIzaSyCgWuezqquuwt1TzEO2IjCZOL9TGtEOOp8"));
                                req.Method = "POST";
                                var post = "{\"considerIp\": \"false\",\"cellTowers\":[{\"cellId\":" + cid + ",\"locationAreaCode\":" + lac + ",\"mobileCountryCode\":" + mcc + ",\"mobileNetworkCode\":" + mnc + "}]}";
                                var data = Encoding.UTF8.GetBytes(post);
                                req.ContentLength = data.Length;
                                var stream = req.GetRequestStream();
                                stream.Write(data, 0, data.Length);
                                stream.Close();
                            }
                            catch (Exception e) { Console.WriteLine(e.Message); continue; }
                            bool tag = true;
                            double lat = 0, lon = 0, accuracy = 0;
                            try
                            {
                                res = (HttpWebResponse)req.GetResponse();
                                try
                                {
                                    var glr = (GoogleLocResponse)ser2.ReadObject(res.GetResponseStream());
                                    try { res.Close(); } catch (Exception) { }
                                    lon = glr.location.lng;
                                    lat = glr.location.lat;
                                    accuracy = glr.accuracy;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    continue;
                                }
                            }
                            catch (WebException e)
                            {
                                Console.WriteLine(e.Message);
                                if (res.StatusCode == HttpStatusCode.Forbidden)
                                {
                                    Console.WriteLine("No quota. Program terminates.");
                                    return;
                                }
                                if (res.StatusCode == HttpStatusCode.NotFound)
                                {
                                    Console.WriteLine("Not found.");
                                    tag = false;
                                }
                                try { res.Close(); } catch (Exception){ }
                            }
                            catch (Exception e) { Console.WriteLine(e.Message); try { res.Close(); } catch (Exception) { } continue; }
                            cmd = new MySqlCommand("INSERT INTO googlebs(id,tag,lon,lat,accuracy) VALUES('"+bs.id+"',"+tag+","+lon+","+lat+","+accuracy+"0)", conn);
                            cmd.ExecuteScalar();
                            Console.WriteLine(bs.lon + " " + bs.lat + " " + accuracy);
                        }
                    }
                }
            }
        }
    }
}
