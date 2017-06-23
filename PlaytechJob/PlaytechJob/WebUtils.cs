using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace ALOG.Utilities
{
    public class WebResult
    {
        public string AbsoluteURI { get; set; }
        public string Result { get; set; }
        public HttpStatusCode Code { get; set; }
        public Exception Exc { get; set; }

        public HttpResponseHeaders OutHeaders { get; set; }

        public int CalcSize()
        {
            int cnt = 0;
            if (Result != null)
                cnt += Result.Length;
            if(OutHeaders != null)
                foreach(var kvp in OutHeaders)
                {
                    cnt += kvp.Key.Length;
                    foreach (string str in kvp.Value)
                        cnt += str.Length;
                }
            return cnt;
        }

        public override string ToString()
        {
            if (Exc != null)
                return Exc.ToString();
            string res = string.Format("Code:{0} Result:{1}", Code, Result);
            return res;
        }
    }

    public static class WebUtils
    {
        public async static Task<WebResult> Get(string url, string userAgent = null, int timeoutSeconds = 0, string proxy = null, int port = 0, CookieContainer cookies = null, string referer = null, Encoding enc = null, Dictionary<string, string> headers = null)
        {
            try
            {
                var httpClientHandler = new HttpClientHandler();
                if (proxy != null)
                {
                    httpClientHandler.Proxy = new WebProxy(proxy, port);
                    httpClientHandler.UseProxy = true;
                }
                if (cookies != null)
                {
                    httpClientHandler.CookieContainer = cookies;
                    httpClientHandler.UseCookies = true;
                }
                httpClientHandler.AllowAutoRedirect = true;
                httpClientHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (var client = new HttpClient(httpClientHandler))
                {
                    if (timeoutSeconds > 0)
                        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                    if (referer != null || userAgent != null)
                    {
                        if (headers == null)
                            headers = new Dictionary<string, string>();
                        if (referer != null && !headers.ContainsKey("Referer"))
                            headers.Add("Referer", referer);
                        if (userAgent != null&& !headers.ContainsKey("User-Agent"))
                            headers.Add("User-Agent", userAgent);
                    }
                    if (headers != null)
                        foreach (KeyValuePair<string, string> kvp in headers)
                            client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
                    
                    var result = client.GetAsync(url).Result;
                    string absoluteURI = String.Empty;
                    if (result != null && result.RequestMessage != null)
                        absoluteURI = result.RequestMessage.RequestUri.AbsoluteUri;
                    string resultContent = await result.Content.ReadAsStringAsync();
                    return new WebResult { Result = resultContent, Code = result.StatusCode, OutHeaders = result.Headers, AbsoluteURI = absoluteURI};
                }
            }
            catch (Exception exc)
            {
                return new WebResult { Exc = exc };
            }
        }

        public async static Task<WebResult> Post(string url, string content, string userAgent = null, int timeoutSeconds = 0, string proxy = null, int port = 0, CookieContainer cookies = null,
            string referer = null, Dictionary<string, string> headers = null, string contentType = null, string accept = null, bool? allowAutoRedirect = null)
        {
            try
            {
                var sc = contentType != null ? new StringContent(content, Encoding.UTF8, contentType) : new StringContent(content);
                var httpClientHandler = new HttpClientHandler();
                if (proxy != null)
                {
                    httpClientHandler.Proxy = new WebProxy(proxy, port);
                    httpClientHandler.UseProxy = true;
                }
                if (cookies != null)
                {
                    httpClientHandler.CookieContainer = cookies;
                    httpClientHandler.UseCookies = true;
                }
                if(allowAutoRedirect.HasValue)
                    httpClientHandler.AllowAutoRedirect = allowAutoRedirect.Value;
                httpClientHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (var client = new HttpClient(httpClientHandler))
                {
                    if(accept != null)
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
                    if (timeoutSeconds > 0)
                        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                    if (contentType != null || referer != null || userAgent != null)
                    {
                        if (headers == null)
                            headers = new Dictionary<string, string>();
                        if (referer != null)
                            headers.Add("Referer", referer);
                        if (userAgent != null)
                            headers.Add("User-Agent", userAgent);
                    }
                    if (headers != null)
                        foreach (KeyValuePair<string, string> kvp in headers)
                            client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
                    
                    var result = client.PostAsync(url, sc).Result;

                    string resultContent, absoluteURI = String.Empty;
                    if (result != null && result.RequestMessage != null)
                        absoluteURI = result.RequestMessage.RequestUri.AbsoluteUri;
                    
                    resultContent = await result.Content.ReadAsStringAsync();

                    if (cookies != null && result.StatusCode == HttpStatusCode.Redirect)
                        AddAllCookies(cookies, result.Headers, url);

                    return new WebResult { Result = resultContent, Code = result.StatusCode, AbsoluteURI = absoluteURI, OutHeaders = result.Headers };
                }
            }
            catch (Exception exc)
            {
                return new WebResult { Exc = exc };
            }
        }

        public async static Task<WebResult> PostMultiform(string url, MultipartFormDataContent multipartFormDataContent, string userAgent = null, int timeoutSeconds = 0, string proxy = null, int port = 0, CookieContainer cookies = null,
            string referer = null, Dictionary<string, string> headers = null, string accept = null, bool? allowAutoRedirect = null)
        {
            try
            {
                var httpClientHandler = new HttpClientHandler();
                if (proxy != null)
                {
                    httpClientHandler.Proxy = new WebProxy(proxy, port);
                    httpClientHandler.UseProxy = true;
                }
                if (cookies != null)
                {
                    httpClientHandler.CookieContainer = cookies;
                    httpClientHandler.UseCookies = true;
                }
                if (allowAutoRedirect.HasValue)
                    httpClientHandler.AllowAutoRedirect = allowAutoRedirect.Value;
                httpClientHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (var client = new HttpClient(httpClientHandler))
                {
                    if (accept != null)
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
                    if (timeoutSeconds > 0)
                        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                    if (referer != null || userAgent != null)
                    {
                        if (headers == null)
                            headers = new Dictionary<string, string>();
                        if (referer != null)
                            headers.Add("Referer", referer);
                        if (userAgent != null)
                            headers.Add("User-Agent", userAgent);
                    }
                    if (headers != null)
                        foreach (KeyValuePair<string, string> kvp in headers)
                            client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);

                    var result = client.PostAsync(url, multipartFormDataContent).Result;

                    string resultContent = await result.Content.ReadAsStringAsync();
                    
                    return new WebResult { Result = resultContent, Code = result.StatusCode };
                }
            }
            catch (Exception exc)
            {
                return new WebResult { Exc = exc };
            }
        }

        public static void AddAllCookies(CookieContainer cookies, HttpResponseHeaders httpResponseHeaders, string url)
        {
            var all = cookies.AllCookies();
            string domurl = url;
            int pos = domurl.IndexOf('/', 10);
            if (pos >= 0)
                domurl = domurl.Substring(0, pos);
            Uri u = new Uri(url);
            Uri dom = new Uri(domurl);
            foreach (var kvp in httpResponseHeaders)
                if (kvp.Key == "Set-Cookie")
                {
                    foreach (var v in kvp.Value)
                    {
                        var cc = GetAllCookiesFromHeader(v, u.Host);
                        for (int i = 0; i < cc.Count; i++)
                        {
                            Cookie c = cc[i];
                            if (!all.Any(x => x.Name == c.Name))
                                cookies.Add(dom, c);
                        }
                    }
                }
        }

        public static CookieCollection GetAllCookiesFromHeader(string strHeader, string strHost)
        {
            List<string> al;
            CookieCollection cc = new CookieCollection();
            if (strHeader != string.Empty)
            {
                al = ConvertCookieHeaderToArrayList(strHeader);
                cc = ConvertCookieArraysToCookieCollection(al, strHost);
            }
            return cc;
        }

        private static List<string> ConvertCookieHeaderToArrayList(string strCookHeader)
        {
            strCookHeader = strCookHeader.Replace("\r", "");
            strCookHeader = strCookHeader.Replace("\n", "");
            string[] strCookTemp = strCookHeader.Split(',');
            List<string> al = new List<string>();
            int i = 0;
            int n = strCookTemp.Length;
            while (i < n)
            {
                if (strCookTemp[i].IndexOf("expires=", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    al.Add(strCookTemp[i] + "," + strCookTemp[i + 1]);
                    i = i + 1;
                }
                else
                    al.Add(strCookTemp[i]);
                i = i + 1;
            }
            return al;
        }

        private static CookieCollection ConvertCookieArraysToCookieCollection(List<string> al, string strHost)
        {
            CookieCollection cc = new CookieCollection();

            int alcount = al.Count;
            string strEachCook;
            string[] strEachCookParts;
            for (int i = 0; i < alcount; i++)
            {
                strEachCook = al[i].ToString();
                strEachCookParts = strEachCook.Split(';');
                int intEachCookPartsCount = strEachCookParts.Length;
                string strCNameAndCValue = string.Empty;
                string strPNameAndPValue = string.Empty;
                string strDNameAndDValue = string.Empty;
                string[] NameValuePairTemp;
                Cookie cookTemp = new Cookie();

                for (int j = 0; j < intEachCookPartsCount; j++)
                {
                    if (j == 0)
                    {
                        strCNameAndCValue = strEachCookParts[j];
                        if (strCNameAndCValue != string.Empty)
                        {
                            int firstEqual = strCNameAndCValue.IndexOf("=");
                            string firstName = strCNameAndCValue.Substring(0, firstEqual);
                            string allValue = strCNameAndCValue.Substring(firstEqual + 1, strCNameAndCValue.Length - (firstEqual + 1));
                            cookTemp.Name = firstName;
                            cookTemp.Value = allValue;
                        }
                        continue;
                    }
                    if (strEachCookParts[j].IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        strPNameAndPValue = strEachCookParts[j];
                        if (strPNameAndPValue != string.Empty)
                        {
                            NameValuePairTemp = strPNameAndPValue.Split('=');
                            if (NameValuePairTemp[1] != string.Empty)
                            {
                                cookTemp.Path = NameValuePairTemp[1];
                            }
                            else
                            {
                                cookTemp.Path = "/";
                            }
                        }
                        continue;
                    }

                    if (strEachCookParts[j].IndexOf("domain", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        strPNameAndPValue = strEachCookParts[j];
                        if (strPNameAndPValue != string.Empty)
                        {
                            NameValuePairTemp = strPNameAndPValue.Split('=');

                            if (NameValuePairTemp[1] != string.Empty)
                            {
                                cookTemp.Domain = NameValuePairTemp[1];
                            }
                            else
                            {
                                cookTemp.Domain = strHost;
                            }
                        }
                        continue;
                    }
                }

                if (cookTemp.Path == string.Empty)
                {
                    cookTemp.Path = "/";
                }
                if (cookTemp.Domain == string.Empty)
                {
                    cookTemp.Domain = strHost;
                }
                cc.Add(cookTemp);
            }
            return cc;
        }
    }
}
