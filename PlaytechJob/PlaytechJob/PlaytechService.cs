using ALOG.Utilities;
using AngleSharp.Dom.Html;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PlaytechJob
{
    public class PlaytechService : BasePlaytech
    {
        public PlaytechService(Action<string> error, Action<string> trace, string userName, string password, string url)
        {
            this.userName = userName;
            this.password = password;
            wc = new InstrumentedWebClient(trace, error, null, 5, 0, 10, 0);
            uri1 = new Uri(url);
        }

        // warning: hours and minutes dont work on this report
        public WebResult DownloadFinancialReport(DateTime from, DateTime until)
        {
            LoginIfNeeded();
            if (!logged)
                throw new InvalidOperationException("Not logged");
            return GetFinancialReportRaw(csrf, from, until);
        }

        // warning: hours and minutes dont work on this report
        public WebResult DownloadPlayerLoginsReport(DateTime from, DateTime until)
        {
            LoginIfNeeded();
            if (!logged)
                throw new InvalidOperationException("Not logged");
            return GetPlayerLoginReportRaw(csrf, from, until);
        }

        // i havent tried this report because of credentials, hours and minutes should work
        public WebResult DownloadPlayerGamesReport(DateTime from, DateTime until)
        {
            LoginIfNeeded();
            if (!logged)
                throw new InvalidOperationException("Not logged");
            return GetPlayerGamesReportRaw(csrf, from, until);
        }

        // casino - empty or code (mannycasino - 431)
        // delivery platforms are numbers, can be left along
        // client platform - name, must be present
        // warning: hours and minutes dont work on this report
        public WebResult DownloadDailyStatsReport(DateTime from, DateTime until, string clientPlatform, string deliveryPlatform, string casino)
        {
            if (clientPlatform == null || clientPlatform.Length == 0)
                throw new ArgumentException("clientPlatform must be defined");
            LoginIfNeeded();
            if (!logged)
                throw new InvalidOperationException("Not logged");
            return GetDailyStatsReportRaw(csrf, from, until, clientPlatform, deliveryPlatform, casino);
        }

        private void LoginIfNeeded()
        {
            CheckUri1();
            if (logged)
                return;
            Login1();
        }

        private void CheckUri1()
        {
            if (uri1.PathAndQuery.Length > 1)
                throw new ArgumentException("Url of a service can contain only domain");
        }

        private void Login1()
        {
            string url = uri1.AbsoluteUri;
            var headers = new Dictionary<string, string>();
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Connection", "keep-alive");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            var wr = Get(url, headers: headers);
            if (wr.Code == System.Net.HttpStatusCode.OK)
            {
                uri2 = new Uri(wr.AbsoluteURI);
                uri2 = new Uri(uri2.AbsoluteUri.Replace(uri2.PathAndQuery, ""));

                var tokens = ParseTokensFromLoginPage(wr.Result);
                if (tokens == null)
                    throw new InvalidOperationException("Cannot extract tokens from login page");
                Login2(tokens);
            }
            else
                throw new InvalidOperationException(unexpectedResult);
        }

        private void Login2(Tuple<string, string> tokens)
        {
            string content = CreateLogin1Content(tokens.Item1, tokens.Item2, userName, password);

            string url = uri2.AbsoluteUri + "aas/login.loginform.loginform";
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", uri2.AbsoluteUri.TrimEnd('/'));
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri2.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("X-Requested-With", "XMLHttpRequest");
            var wr = Post(url, content, headers, "*/*", "application/x-www-form-urlencoded");
            if (wr.Code != HttpStatusCode.OK)
                throw new InvalidOperationException(unexpectedResult);

            needsLogoff = true;

            if (wr.Result.Contains("other active session(s)"))
            {
                string token = ParseResponse2(wr.Result);
                content = CreateLogin2Content(token);
                headers = new Dictionary<string, string>();
                headers.Add("Origin", uri2.AbsoluteUri.TrimEnd('/'));
                headers.Add("Accept-Encoding", "gzip, deflate");
                headers["Host"] = uri2.Host;
                headers["Referer"] = uri2.AbsoluteUri + "aas/?method=POST&service=https%3A%2F%2F" + uri1.Host + "%2Fims%2F";
                headers["Accept-Language"] = "en-US,en;q=0.5";
                headers["X-Requested-With"] = "XMLHttpRequest";
                wr = Post(url, content, headers, "*/*", "application/x-www-form-urlencoded");
                if (wr.Code == HttpStatusCode.OK)
                {
                    string ticket = ParseResponse1(wr.Result);
                    Login3(ticket);
                }
                else
                    throw new InvalidOperationException(unexpectedResult);
            }
            else
            {
                string ticket = ParseResponse1(wr.Result);
                Login3(ticket);
            }
        }

        private void Login3(string ticket)
        {
            string content = "ticket=" + ticket;
            string url = uri1.AbsoluteUri + "ims/";      // enter
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", uri2.AbsoluteUri.TrimEnd('/'));
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Referer", uri2.AbsoluteUri + "aas/?method=POST&service=https%3A%2F%2F" + uri1.Host + "%2Fims%2F");
            var wr = Post(url, content, headers, "*/*", "application/x-www-form-urlencoded");
            if (wr.Code != HttpStatusCode.OK)
                throw new InvalidOperationException(unexpectedResult);
            var all = cc.AllCookies();
            Login4();
        }

        private void Login4()
        {
            string url = uri1.AbsoluteUri + "top.php?context=ims&aasSessionPropagation=1";
            var headers = new Dictionary<string, string>();
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "*/*");
            headers.Add("Referer", uri1.AbsoluteUri + "ims/bexcore/welcome");
            var wr = Get(url, headers: headers);
            var all = cc.AllCookies();
            if (wr.Code == HttpStatusCode.OK)
            {
                string ticket = ParseTicket(wr.Result);
                if (ticket == null)
                    throw new InvalidOperationException("Ticket not found");
                Login5(ticket);
            }
            else
                throw new InvalidOperationException(unexpectedResult);
        }

        private void Login5(string ticket)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("redirectAfterLogin", uri1.AbsoluteUri + "ims/bexcore/PHPTabNaviPage/main");
            col.Add("ticket", ticket);
            string content = NameValueCollectionToPost(col);

            string url = uri1.AbsoluteUri + "index.php";
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", uri2.AbsoluteUri.TrimEnd('/'));
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Referer", uri2.AbsoluteUri + "aas/login?service=https%3A%2F%2F" + uri1.Host + "%2Findex.php&redirectAfterLogin=https%3A%2F%2Fadmin.megasportcasino.com%2Fims%2Fbexcore%2FPHPTabNaviPage%2Fmain&method=POST");

            var wr = Post(url, content, headers, "*/*", "application/x-www-form-urlencoded", false);
            if(wr.Code != HttpStatusCode.OK && wr.Code != HttpStatusCode.Redirect)
                throw new InvalidOperationException(unexpectedResult);

            Login6();
        }

        // this one should put csrf_token in cookie
        private void Login6()
        {
            string url = uri1.AbsoluteUri + "main.php?context=ims";
            var headers = new Dictionary<string, string>();
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "*/*");
            var wr = Get(url, headers: headers);
            if (wr.Code != HttpStatusCode.OK)
                throw new InvalidOperationException(unexpectedResult);
            //var all = cc.AllCookies();
            WebUtils.AddAllCookies(cc, wr.OutHeaders, url);
            var all = cc.AllCookies();
            Cookie csrfCookie = all.FirstOrDefault(x => x.Name == "csrf_token");
            if(csrfCookie != null)
            {
                logged = true;
                csrf = HttpUtility.UrlDecode(csrfCookie.Value);
            }
            //ReportMainPage();
        }

        private void ReportMainPage()
        {
            string url = uri1.AbsoluteUri + "report_view.php?context=ims";
            var headers = new Dictionary<string, string>();
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "*/*");
            headers.Add("Referer", uri1.AbsoluteUri + "ims/bexcore/welcome");
            var wr = Get(url, headers: headers);
            if (wr.Result != null)
            {
                int pos = wr.Result.IndexOf("csrf_token");
                if (pos >= 0)
                {
                    csrf = ExtractCSRFToken(wr.Result);
                    if (csrf != null)
                        logged = true;
                }
            }
        }

        public void Logout()
        {
            if (!needsLogoff)
                return;

            string url = uri1.AbsoluteUri + "ims/logout";
            var headers = new Dictionary<string, string>();
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "*/*");
            var wr = Get(url, headers: headers);
        }

        private WebResult GetFinancialReportRaw(string csrf, DateTime from, DateTime until)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", uri1.AbsoluteUri.TrimEnd('/'));
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Referer", uri1.AbsoluteUri + "report_view.php");
            int reportCode = 52601;
            int pageSize = 100;
            int pageNumber = 0;
            string contentType = "multipart/form-data; boundary=----WebKitFormBoundaryjEqMdrARvc5Jej2Z";
            string url = uri1.AbsoluteUri + "report_view.php?reportcode=" + reportCode.ToString();
            string content = string.Format("------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent - Disposition: form - data; name =\"action[exportcsv]\"\r\n\r\nExport all results (CSV)\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"csrf_token\"\r\n\r\n{0}\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"reportgroups\"\r\n\r\n0\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"reportcode\"\r\n\r\n52601\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"casino\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"username\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"usernamecsv\"; filename=\"\"\r\nContent-Type: application/octet-stream\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"usernamecsv_header\"\r\n\r\n1\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"startdate\"\r\n\r\n{1}\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"enddate\"\r\n\r\n{2}\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"method\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"ccmethod\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"status\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"type\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"country\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"currency\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"amount\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"fundreversals\"; filename=\"\"\r\nContent-Type: application/octet-stream\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"fundreversals_header\"\r\n\r\n1\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"externaltranid\"\r\n\r\n\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"filename\"\r\n\r\nreport\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"rowsperpage\"\r\n\r\n{3}\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"action[exportcsv]\"\r\n\r\nExport all results (CSV)\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"page\"\r\n\r\n{4}\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z\r\nContent-Disposition: form-data; name=\"truevalue\"\r\n\r\n1\r\n------WebKitFormBoundaryjEqMdrARvc5Jej2Z--\r\n", 
                csrf, from.ToString("yyyy-MM-dd"), until.ToString("yyyy-MM-dd"), pageSize, pageNumber);
            return PostRaw(url, contentType, content, headers);
        }

        private WebResult GetPlayerLoginReportRaw(string csrf, DateTime from, DateTime until)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", uri1.AbsoluteUri.TrimEnd('/'));
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Referer", uri1.AbsoluteUri + "report_view.php");
            int reportCode = 35601;
            string contentType = "multipart/form-data; boundary=----WebKitFormBoundaryjgejKV6AzUlAMBV4";
            string url = uri1.AbsoluteUri + "report_view.php?reportcode=" + reportCode.ToString();
            string content = string.Format("------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"action[exportcsv]\"\r\n\r\nExport all results (CSV)\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"csrf_token\"\r\n\r\n{0}\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"reportgroups\"\r\n\r\n0\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"reportcode\"\r\n\r\n35601\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"casino\"\r\n\r\n\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"startdate\"\r\n\r\n{1}\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"enddate\"\r\n\r\n{2}\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"ip\"\r\n\r\n\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"serial\"\r\n\r\n\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"username\"\r\n\r\n\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"usernamecsv\"; filename=\"\"\r\nContent-Type: application/octet-stream\r\n\r\n\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"usernamecsv_header\"\r\n\r\n1\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"includefields1\"\r\n\r\n1\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"includefields2\"\r\n\r\n1\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"dplatform\"\r\n\r\n\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"clientplatform\"\r\n\r\n\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"filename\"\r\n\r\nreport\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"rowsperpage\"\r\n\r\n100\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"action[exportcsv]\"\r\n\r\nExport all results (CSV)\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"page\"\r\n\r\n0\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4\r\nContent-Disposition: form-data; name=\"truevalue\"\r\n\r\n1\r\n------WebKitFormBoundaryjgejKV6AzUlAMBV4--\r\n",
                csrf, from.ToString("yyyy-MM-dd"), until.ToString("yyyy-MM-dd"));
            return PostRaw(url, contentType, content, headers);
        }

        private WebResult GetPlayerGamesReportRaw(string csrf, DateTime from, DateTime until)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", uri1.AbsoluteUri.TrimEnd('/'));
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Referer", uri1.AbsoluteUri + "report_view.php");
            int reportCode = 36401;
            string contentType = "multipart/form-data; boundary=----WebKitFormBoundaryQVRswOAf8qeyCFXF";
            string url = uri1.AbsoluteUri + "report_view.php?reportcode=" + reportCode.ToString();
            string content = string.Format("------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"action[exportcsv]\"\r\n\r\nExport all results (CSV)\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"csrf_token\"\r\n\r\n{0}\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"reportgroups\"\r\n\r\n0\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"reportcode\"\r\n\r\n36401\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"casino\"\r\n\r\n\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"startdate\"\r\n\r\n{1}\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"enddate\"\r\n\r\n{2}\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"gametype\"\r\n\r\n\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"gamename\"\r\n\r\n\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"data1\"; filename=\"\"\r\nContent-Type: application/octet-stream\r\n\r\n\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"data1_header\"\r\n\r\n1\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"basecurrency\"\r\n\r\n1\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"filename\"\r\n\r\nreport\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"rowsperpage\"\r\n\r\n100\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"action[exportcsv]\"\r\n\r\nExport all results (CSV)\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"page\"\r\n\r\n0\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF\r\nContent-Disposition: form-data; name=\"truevalue\"\r\n\r\n1\r\n------WebKitFormBoundaryQVRswOAf8qeyCFXF--\r\n",
                csrf, from.ToString("yyyy-MM-dd HH:mm"), until.ToString("yyyy-MM-dd HH:mm"));
            return PostRaw(url, contentType, content, headers);
        }

        private WebResult GetDailyStatsReportRaw(string csrf, DateTime from, DateTime until, string clientPlatform, string deliveryPlatform, string casino)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", uri1.AbsoluteUri.TrimEnd('/'));
            headers.Add("Accept-Encoding", "gzip, deflate");
            headers.Add("Host", uri1.Host);
            headers.Add("Accept-Language", "en-US,en;q=0.8");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Referer", uri1.AbsoluteUri + "daily_stats.php");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            string contentType = "application/x-www-form-urlencoded";
            string url = uri1.AbsoluteUri + "daily_stats.php";
            string content = string.Format("export=Export+all+results&csrf_token={0}&period=&startdate={1}&enddate={2}&signupbefore=&signupafter=&pokervip=&casino={5}&username=&viplevel=&countrycode=&state=&currencycode=BC_&clientplatform%5B%5D={3}&osname=&deliveryplatform={4}&advertiser=&profileid=&language=&kioskadmin=&kioskname=&mintotaldeposit=&casinoclientskincode=&lastloginmore=&lastloginless=&billingsettinggroupcode=&trackingid1=&trackingid2=&trackingid3=&trackingid4=&trackingid5=&trackingid6=&trackingid7=&trackingid8=&trackingid9=&trackingid10=&trackingid11=&trackingid12=&trackingid13=&trackingid14=&trackingid15=&trackingid16=&custom01=&custom02=&custom03=&custom04=&custom05=&custom06=&custom07=&custom08=&custom09=&custom10=&custom11=&custom12=&custom13=&custom14=&custom15=&custom16=&custom17=&custom18=&custom19=&custom20=&pokercustom1=&pokercustom2=&bingocustom1=&bingocustom2=&onlineaccount=1&reportby=Player&rowsperpage=50&export=Export+all+results&orderbyfield=&orderbydirection=&advertisercode=",
                HttpUtility.UrlPathEncode(csrf), from.ToString("yyyy-MM-dd"), until.ToString("yyyy-MM-dd"), clientPlatform, deliveryPlatform, casino);

            return PostRaw(url, contentType, content, headers);
        }

        private readonly string userName;
        private readonly string password;
        private bool logged;
        private bool needsLogoff;
        private readonly string unexpectedResult = "Unexpected result from http call";
        private string csrf;
        private readonly Uri uri1;
        private Uri uri2;
    }
}