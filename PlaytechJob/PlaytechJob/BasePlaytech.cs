using AngleSharp.Dom.Html;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALOG.Utilities
{
    public class BasePlaytech
    {
        public class ClientInfo
        {
            public string platform { get; set; }
            public string language { get; set; }
            public string userAgent { get; set; }
            public int windowWidth { get; set; }
            public int windowHeight { get; set; }
            public int screenWidth { get; set; }
            public int screenHeight { get; set; }
            public bool javaEnabled { get; set; }
            public string browser { get; set; }
        }

        private readonly string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36";
        protected InstrumentedWebClient wc;
        private readonly AngleSharp.Parser.Html.HtmlParser parser = new AngleSharp.Parser.Html.HtmlParser();

        protected Tuple<string, string> ParseTokensFromLoginPage(string content)
        {
            var document = parser.Parse(content);
            var all = document.All.Where(m => m.HasAttribute("value"));
            var node = all.FirstOrDefault(x => x.Attributes["name"].Value == "t:formdata");
            if (node == null)
                return null;
            IHtmlInputElement e = node as IHtmlInputElement;
            if (e == null)
                return null;
            string t1 = e.DefaultValue;
            var node2 = all.FirstOrDefault(x => x.Attributes["name"].Value == "loginTicket");
            IHtmlInputElement e2 = node2 as IHtmlInputElement;
            if (e2 == null)
                return null;
            string t2 = e2.DefaultValue;
            Tuple<string, string> res = new Tuple<string, string>(t1, t2);
            return res;
        }

        protected string ParseResponse1(string resp1)
        {
            Dictionary<string, object> dict = resp1.JsonDeserialize<Dictionary<string, object>>();
            if (dict == null)
                return null;
            object o2 = dict["content"];
            var document = parser.Parse(o2 as string);
            var node = document.All.FirstOrDefault(x => x.LocalName == "textarea");
            if (node == null)
                return null;
            IHtmlTextAreaElement e = node as IHtmlTextAreaElement;
            if (e == null)
                return null;
            return e.DefaultValue;
        }

        protected string ParseResponse2(string resp1)
        {
            Dictionary<string, object> dict = resp1.JsonDeserialize<Dictionary<string, object>>();
            if (dict == null)
                return null;
            object o2 = dict["content"];
            var document = parser.Parse(o2 as string);
            var all = document.All.Where(m => m.HasAttribute("value"));
            var node = all.FirstOrDefault(x => x.Attributes["name"].Value == "t:formdata");
            if (node == null)
                return null;
            IHtmlInputElement e = node as IHtmlInputElement;
            if (e == null)
                return null;
            return e.DefaultValue;
        }

        protected string CreateLogin2Content(string token)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("t:formdata", token);
            col.Add("t:formname", "loginForm");
            col.Add("t:submit", "[\"multiSessionContinue\",\"multiSessionContinue\"]");
            col.Add("t:zoneid", "loginZone");
            return NameValueCollectionToPost(col);
        }

        protected string ParseTicket(string result)
        {
            var doc = parser.Parse(result);
            var node = doc.All.FirstOrDefault(x => x.LocalName == "textarea");
            if (node != null)
            {
                IHtmlTextAreaElement e = node as IHtmlTextAreaElement;
                if (e != null)
                    return e.DefaultValue;
            }
            return null;
        }

        protected string ExtractCSRFToken(string result)
        {
            var doc = parser.Parse(result);
            var node = doc.All.FirstOrDefault(x => x.Attributes != null && x.Attributes["name"] != null && x.Attributes["name"].Value == "csrf_token");
            if (node == null)
                return null;
            IHtmlInputElement e = node as IHtmlInputElement;
            if (e == null)
                return null;
            return e.DefaultValue;
        }

        protected WebResult Get(string url, Dictionary<string, string> headers)
        {
            return wc.Get(url, headers, userAgent, cc);
        }

        protected WebResult Post(string url, string content, Dictionary<string, string> headers, string accept, string contentType, bool allowRedirect = true)
        {
            return wc.Post(url, content, accept, contentType, userAgent, cc, headers, allowRedirect);
        }

        protected WebResult PostMultiform(string url, MultipartFormDataContent mp, Dictionary<string, string> headers, string accept)
        {
            string referer = null;
            if (headers.ContainsKey("Referer"))
            {
                referer = headers["Referer"];
                headers.Remove("Referer");
            }
            return WebUtils.PostMultiform(url, mp, accept: accept, cookies: cc, headers: headers, userAgent: userAgent, referer: referer).Result;
        }

        protected WebResult PostRaw(string url, string contentType, string content, Dictionary<string, string> headers)
        {
            return wc.PostRaw(url, contentType, content, headers, cc);
        }

        protected CookieContainer cc = new CookieContainer();

        protected string CreateLogin1Content(string token, string ticket, string userName, string password)
        {
            NameValueCollection col = new NameValueCollection();
            string j = "{\"platform\":\"Win64\",\"language\":\"en-US\",\"userAgent\":\"Mozilla\",\"windowWidth\":1600,\"windowHeight\":251,\"screenWidth\":1600,\"screenHeight\":900,\"javaEnabled\":false,\"browser\":\"Firefox\"}";
            col.Add("clientInfo", j);
            col.Add("loginTicket", ticket);
            col.Add("password", password);
            col.Add("t:formdata", token);
            col.Add("t:formname", "loginForm");
            col.Add("t:submit", "[\"loginButton\",\"loginButton\"]");
            col.Add("t:zoneid", "loginZone");
            col.Add("username", userName);
            string str = NameValueCollectionToPost(col);
            return str;
        }

        protected static string NameValueCollectionToPost(NameValueCollection col)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in col.AllKeys)
                AppendUrlEncoded(sb, key, col[key]);
            return sb.ToString();
        }
        
        public static void AppendUrlEncoded(StringBuilder sb, string name, string value)
        {
            if (sb.Length != 0)
                sb.Append("&");
            sb.Append(HttpUtility.UrlEncode(name));
            sb.Append("=");
            sb.Append(HttpUtility.UrlEncode(value));
        }
    }
}