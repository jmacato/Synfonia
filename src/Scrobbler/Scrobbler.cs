using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Xml;
using System.Net;
using System.IO;
using System.Web;
using System.Collections.Specialized;
using System.Xml.Serialization;
using System.Threading;

namespace Symphony.Scrobbler
{
    public class ScrobblerTrack
    {
        [XmlAttribute("artist")]
        public string Artist
        {
            get;
            set;
        }

        [XmlAttribute("album")]
        public string Album
        {
            get;
            set;
        }

        [XmlAttribute("title")]
        public string Title
        {
            get;
            set;
        }

        [XmlAttribute("listened")]
        public DateTime Listened
        {
            get;
            set;
        }
    }

    public class ScrobblerCache
    {
        [XmlElement("track")]
        public List<ScrobblerTrack> Tracks
        {
            get;
            set;
        }

        public static string GetCacheFileName()
        {
            return string.Format("{0}\\wpfmpdclient\\scrobbler.xml", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        public static ScrobblerCache Deserialize(string fileName)
        {
            if (!File.Exists(fileName))
                return null;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ScrobblerCache));

                ScrobblerCache result;
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    result = serializer.Deserialize(fs) as ScrobblerCache;
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        } // Deserialize

        public bool Serialize(string fileName)
        {
            try
            {
                string path = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                XmlSerializer serializer = new XmlSerializer(typeof(ScrobblerCache));
                using (TextWriter textWriter = new StreamWriter(fileName))
                {
                    serializer.Serialize(textWriter, this);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        } // Serialize
    }

    public class XmlCache
    {
        private const int m_MaxCacheElements = 50;
        private List<Uri> m_Keys = new List<Uri>();
        private Dictionary<Uri, XmlDocument> m_XmlCache = new Dictionary<Uri, XmlDocument>();
        private Mutex m_Mutex = new Mutex();

        public void Add(Uri uri, XmlDocument doc)
        {
            m_Mutex.WaitOne();
            if (m_Keys.Count == m_MaxCacheElements)
            {
                m_XmlCache.Remove(m_Keys[0]);
                m_Keys.RemoveAt(0);
            }
            m_XmlCache[uri] = doc;
            m_Keys.Add(uri);
            m_Mutex.ReleaseMutex();
        }

        public XmlDocument Get(Uri uri)
        {
            XmlDocument doc = null;
            m_Mutex.WaitOne();
            m_XmlCache.TryGetValue(uri, out doc);
            m_Mutex.ReleaseMutex();
            return doc;
        }
    }

    public class Scrobbler
    {
        ScrobblerCache m_Cache = null;
        string m_Token = string.Empty;
        string m_SessionKey = string.Empty;
        static Dictionary<string, string> m_ArtistCorrectionsCache = new Dictionary<string, string>();
        static XmlCache m_XmlCache = new XmlCache();

        public enum ImageSize
        {
            small,
            medium,
            large,
            mega
        }

        public Scrobbler(string apiKey, string apiSecret, string baseUrl, string sessionKey)
        {
            m_SessionKey = sessionKey;
            ApiKey = apiKey;
            ApiSecret = apiSecret;
            BaseUrl = baseUrl;

            m_Cache = ScrobblerCache.Deserialize(ScrobblerCache.GetCacheFileName());
            if (m_Cache == null)
                m_Cache = new ScrobblerCache();
            if (m_Cache.Tracks == null)
                m_Cache.Tracks = new List<ScrobblerTrack>();
        }

        #region Properties
        public string ApiKey
        {
            get;
            private set;
        }

        public string ApiSecret
        {
            get;
            private set;
        }

        public string BaseUrl
        {
            get;
            private set;
        }
        #endregion
        #region Public Operations
        public bool SaveCache()
        {
            return m_Cache.Serialize(ScrobblerCache.GetCacheFileName());
        }

        public string GetAuthorizationUrl(string token)
        {
            if (string.IsNullOrEmpty(token))
                token = GetToken();
            m_Token = token;

            if (!string.IsNullOrEmpty(token))
                return string.Format("http://www.last.fm/api/auth/?api_key={0}&token={1}", ApiKey, token);
            return string.Empty;
        } // GetAuthorizationUrl

        public string GetToken()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "auth.getToken";
            parameters["api_key"] = ApiKey;
            parameters["api_sig"] = GetSignature(parameters);

            XmlDocument xml = GetResponse(GetUrl(BaseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList list = xml.SelectNodes("/lfm/token");
                if (list != null && list.Count > 0)
                {
                    return list[0].InnerText;
                }
            }
            return string.Empty;
        } // GetToken

        public string GetSession()
        {
            if (string.IsNullOrEmpty(m_Token))
                return string.Empty;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "auth.getSession";
            parameters["api_key"] = ApiKey;
            parameters["token"] = m_Token;
            parameters["api_sig"] = GetSignature(parameters);

            XmlDocument xml = GetResponse(GetUrl(BaseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList list = xml.SelectNodes("/lfm/session/key");
                if (list != null && list.Count > 0)
                {
                    m_SessionKey = list[0].InnerText;
                    return list[0].InnerText;
                }
            }
            return string.Empty;
        } // GetSession

        public bool UpdateNowPlaying(string artist, string title, string album)
        {
            if (string.IsNullOrEmpty(m_SessionKey))
                return false;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "track.updateNowPlaying";
            parameters["artist"] = artist;
            parameters["track"] = title;
            parameters["album"] = album;
            parameters["api_key"] = ApiKey;
            parameters["sk"] = m_SessionKey;
            parameters["api_sig"] = GetSignature(parameters);
            XmlDocument xml = GetPostResponse(BaseUrl, parameters);
            if (xml != null)
            {
                return true;
            }
            return false;
        } // UpdateNowPlaying

        public bool Scrobble(string artist, string title, string album, DateTime started)
        {
            if (string.IsNullOrEmpty(m_SessionKey))
                return false;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "track.scrobble";

            m_Cache.Tracks.Add(new ScrobblerTrack()
            {
                Artist = artist,
                Album = album,
                Title = title,
                Listened = started
            });

            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            int index = 0;
            foreach (ScrobblerTrack t in m_Cache.Tracks)
            {
                parameters[string.Format("artist[{0}]", index)] = t.Artist;
                parameters[string.Format("track[{0}]", index)] = t.Title;
                parameters[string.Format("album[{0}]", index)] = t.Album;

                Int64 startedEpoch = Convert.ToInt64((t.Listened.ToUniversalTime() - epoch).TotalSeconds);
                parameters[string.Format("timestamp[{0}]", index)] = startedEpoch.ToString("################");

                parameters[string.Format("chosenByUser[{0}]", index)] = "1";
                index++;
            }
            parameters["api_key"] = ApiKey;
            parameters["sk"] = m_SessionKey;
            parameters["api_sig"] = GetSignature(parameters);

            XmlDocument xml = GetPostResponse(BaseUrl, parameters);
            if (xml != null)
            {
                m_Cache.Tracks.Clear();
                XmlNodeList list = xml.SelectNodes("/lfm/scrobbles");
                if (list != null && list.Count > 0)
                    return list[0].Attributes["accepted"].Value != "0";
            }
            return false;
        } // Scrobble
        #endregion

        #region Static operations
        public static ScrobblerTrack GetTrackCorrection(string baseUrl, string apiKey, string artist, string title)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "track.getCorrection";
            parameters["api_key"] = apiKey;
            parameters["artist"] = artist;
            parameters["track"] = title;

            XmlDocument xml = GetResponse(GetUrl(baseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList xnList = xml.SelectNodes("/lfm/corrections/correction/track/name");
                if (xnList != null && xnList.Count > 0)
                {
                    ScrobblerTrack res = new ScrobblerTrack();
                    res.Title = xnList[0].InnerText;
                    xnList = xml.SelectNodes("/lfm/corrections/correction/track/artist/name");
                    if (xnList != null && xnList.Count > 0)
                        res.Artist = xnList[0].InnerText;
                    return res;
                }
            }
            return null;
        } // GetTrackCorrection

        public static string GetArtistArt(string baseUrl, ImageSize size, string apiKey, string artist)
        {
            //artist = GetArtistCorrection(baseUrl, apiKey, artist);
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "artist.getInfo";
            parameters["api_key"] = apiKey;
            parameters["artist"] = artist;

            XmlDocument xml = GetResponse(GetUrl(baseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList xnList = xml.SelectNodes("/lfm/artist/image");
                foreach (XmlNode xn in xnList)
                {
                    if (xn.Attributes["size"].Value == size.ToString())
                        return xn.InnerText;
                }
            }
            return string.Empty;
        } // GetArtistArt

        public static string GetArtistInfo(string baseUrl, string apiKey, string artist)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "artist.getInfo";
            parameters["api_key"] = apiKey;
            parameters["artist"] = artist;

            XmlDocument xml = GetResponse(GetUrl(baseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList xnList = xml.SelectNodes("/lfm/artist/bio/content");
                if (xnList != null && xnList.Count > 0)
                {
                    string res = WebUtility.HtmlDecode(xnList[0].InnerText);
                    // Remove html tags (like <b>...</b>)
                    res = System.Text.RegularExpressions.Regex.Replace(res, @"<[^>]*>", string.Empty);
                    return res;
                }
            }
            return string.Empty;
        } // GetArtistInfo

        public static string GetArtistCorrection(string baseUrl, string apiKey, string artist)
        {
            string correction = string.Empty;
            if (m_ArtistCorrectionsCache.TryGetValue(artist, out correction))
                return correction;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "artist.getCorrection";
            parameters["api_key"] = apiKey;
            parameters["artist"] = artist;

            XmlDocument xml = GetResponse(GetUrl(baseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList xnList = xml.SelectNodes("/lfm/corrections/correction/artist/name");
                if (xnList != null && xnList.Count > 0)
                {
                    correction = xnList[0].InnerText;
                    m_ArtistCorrectionsCache[artist] = correction;
                    return correction;
                }
            }

            m_ArtistCorrectionsCache[artist] = artist;
            return artist;
        } // GetArtistCorrection

        public static string GetAlbumArt(string baseUrl, ImageSize size, string apiKey, string artist, string album)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(album))
                return string.Empty;

            artist = GetArtistCorrection(baseUrl, apiKey, artist);
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "album.getinfo";
            parameters["api_key"] = apiKey;
            parameters["artist"] = artist;
            parameters["album"] = album;

            XmlDocument xml = GetResponse(GetUrl(baseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList xnList = xml.SelectNodes("/lfm/album/image");
                foreach (XmlNode xn in xnList)
                {
                    if (xn.Attributes["size"].Value == size.ToString())
                        return xn.InnerText;
                }
            }
            return string.Empty;
        } // GetAlbumArt

        public static string GetAlbumInfo(string baseUrl, string apiKey, string artist, string album)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["method"] = "album.getInfo";
            parameters["api_key"] = apiKey;
            parameters["artist"] = artist;
            parameters["album"] = album;

            XmlDocument xml = GetResponse(GetUrl(baseUrl, parameters));
            if (xml != null)
            {
                XmlNodeList xnList = xml.SelectNodes("/lfm/album/wiki/content");
                if (xnList != null && xnList.Count > 0)
                {
                    string res = WebUtility.HtmlDecode(xnList[0].InnerText);
                    // Remove html tags (like <b>...</b>)
                    res = System.Text.RegularExpressions.Regex.Replace(res, @"<[^>]*>", string.Empty);
                    return res;
                }
            }
            return string.Empty;
        } // GetAlbumInfo

        private static XmlDocument GetResponse(Uri url)
        {
            XmlDocument tDoc = m_XmlCache.Get(url);
            if (tDoc != null)
                return tDoc;

            using (WebClient client = new WebClient())
            {
                try
                {
                    using (Stream data = client.OpenRead(url))
                    {
                        StreamReader reader = new StreamReader(data);
                        string str = null;
                        StringBuilder sb = new StringBuilder();
                        while ((str = reader.ReadLine()) != null)
                            sb.AppendLine(str);

                        string xml = sb.ToString();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xml);

                        m_XmlCache.Add(url, doc);
                        return doc;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        } // GetResponse

        private static Uri GetUrl(string address, Dictionary<string, string> parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(address);
            if (parameters.Keys.Count > 0)
            {
                sb.Append("?");
                bool first = true;
                foreach (string k in parameters.Keys)
                {
                    if (!first)
                        sb.Append("&");
                    first = false;
                    if (parameters[k] != null)
                        sb.Append(string.Format("{0}={1}", k, Uri.EscapeDataString(parameters[k])));
                }
            }

            return new Uri(sb.ToString());
        } // GetUrl
        #endregion

        #region Private Operations
        private string GetSignature(Dictionary<string, string> parameters)
        {
            StringBuilder sb = new StringBuilder();

            List<string> keys = parameters.Keys.ToList();
            keys.Sort();
            foreach (string k in keys)
            {
                sb.Append(k);
                sb.Append(parameters[k]);
            }
            sb.Append(ApiSecret);

            string res = sb.ToString();
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(res));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                    sBuilder.Append(data[i].ToString("x2"));
                res = sBuilder.ToString();
            }
            return res;
        } // GetSignature

        private XmlDocument GetPostResponse(string url, Dictionary<string, string> parameters)
        {
            using (WebClient client = new WebClient())
            {
                System.Net.ServicePointManager.Expect100Continue = false;
                NameValueCollection data = new NameValueCollection();
                foreach (string k in parameters.Keys)
                    data[k] = parameters[k];

                try
                {
                    byte[] response = client.UploadValues(new Uri(url), "POST", data);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(System.Text.Encoding.UTF8.GetString(response));
                    return doc;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        } // GetPostResponse
        #endregion
    }

    public class LastfmScrobbler : Scrobbler
    {
        private const string api_key = "";
        private const string api_secret = "";
        private const string m_BaseUrl = "http://ws.audioscrobbler.com/2.0/";

        public LastfmScrobbler(string sessionKey)
          : base(api_key, api_secret, m_BaseUrl, sessionKey)
        {

        }

        public static string GetAlbumArt(string artist, string album)
        {
            return Scrobbler.GetAlbumArt(m_BaseUrl, ImageSize.mega, api_key, artist, album);
        }

        public static string GetAlbumArt(string artist, string album, ImageSize size)
        {
            return Scrobbler.GetAlbumArt(m_BaseUrl, size, api_key, artist, album);
        }

        public static string GetAlbumInfo(string artist, string album)
        {
            return Scrobbler.GetAlbumInfo(m_BaseUrl, api_key, artist, album);
        }

        public static string GetArtistArt(string artist)
        {
            return Scrobbler.GetArtistArt(m_BaseUrl, ImageSize.large, api_key, artist);
        }

        public static string GetArtistArt(string artist, ImageSize size)
        {
            return Scrobbler.GetArtistArt(m_BaseUrl, size, api_key, artist);
        }

        public static string GetArtistInfo(string artist)
        {
            return Scrobbler.GetArtistInfo(m_BaseUrl, api_key, artist);
        }
    }

    public class LibrefmScrobbler : Scrobbler
    {
        private const string api_key = "";
        private const string api_secret = "";
        private const string m_BaseUrl = "http://turtle.libre.fm/";

        public LibrefmScrobbler(string sessionKey)
          : base(api_key, api_secret, m_BaseUrl, sessionKey)
        {

        }

        public static string GetAlbumArt(string artist, string album)
        {
            return Scrobbler.GetAlbumArt(m_BaseUrl, ImageSize.mega, api_key, artist, album);
        }

        public static string GetAlbumArt(string artist, string album, ImageSize size)
        {
            return Scrobbler.GetAlbumArt(m_BaseUrl, size, api_key, artist, album);
        }

        public static string GetAlbumInfo(string artist, string album)
        {
            return Scrobbler.GetAlbumInfo(m_BaseUrl, api_key, artist, album);
        }

        public static string GetArtistArt(string artist)
        {
            return Scrobbler.GetArtistArt(m_BaseUrl, ImageSize.large, api_key, artist);
        }

        public static string GetArtistArt(string artist, ImageSize size)
        {
            return Scrobbler.GetArtistArt(m_BaseUrl, size, api_key, artist);
        }

        public static string GetArtistInfo(string artist)
        {
            return Scrobbler.GetArtistInfo(m_BaseUrl, api_key, artist);
        }
    }
}