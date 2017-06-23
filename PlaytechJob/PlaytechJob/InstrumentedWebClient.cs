using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ALOG.Utilities
{
    public class InstrumentedWebClient
    {
        public InstrumentedWebClient(Action<string> trace, Action<string> error, string rateLimits, int retries, int maxCalls, 
            int retrySleepSeconds, int throttleSleepSeconds, int[] brokenRateCodes = null, int sleepRateAfterRateBrokenSeconds = 0)
        {
            if (rateLimits != null && rateLimits.Length > 0)
                throttler = new Throttler(rateLimits);
            this.trace = trace;
            this.error = error;
            if (retries == 0)
                retries = 1;
            this.retries = retries;
            this.maxCalls = maxCalls;
            this.retrySleepSeconds = retrySleepSeconds;
            this.throttleSleepSeconds = throttleSleepSeconds;
            this.brokenRateCodes = brokenRateCodes;
            this.sleepRateAfterRateBrokenSeconds = sleepRateAfterRateBrokenSeconds;
        }

        public WebResult Get(string url, Dictionary<string, string> headers, string userAgent = null, CookieContainer cc = null)
        {
            int attempt = 0;
            while (true)
            {
                UpdateCallStats();
                attempt++;
                trace("Call " + numCalls + " Attempt: " + attempt + " GET: " + url);
                WebResult wr = WebUtils.Get(url, headers : headers, cookies : cc, userAgent : userAgent).Result;
                LogResult(wr);
                if (wr.Code == System.Net.HttpStatusCode.OK)
                    return wr;
                if (attempt >= retries)
                    return wr;
                if (!IsRateBroken(wr))
                {
                    if (!IsRetryable(wr))
                        return wr;
                    SleepForRetry();
                }
            }
        }

        public WebResult Post(string url, string content, string accept, string contentType, string userAgent = null, CookieContainer cc = null, Dictionary<string, string> headers = null, bool allowRedirect = true)
        {
            int attempt = 0;
            while (true)
            {
                UpdateCallStats();
                attempt++;
                trace("Call " + numCalls + " Attempt: " + attempt + " POST: " + url);
                var wr = WebUtils.Post(url, content, accept: accept, contentType: contentType, cookies: cc, userAgent: userAgent, headers : headers, allowAutoRedirect : allowRedirect).Result;
                LogResult(wr);
                if (wr.Code == System.Net.HttpStatusCode.OK)
                    return wr;
                if (attempt >= retries)
                    return wr;
                if(!IsRateBroken(wr))
                { 
                    if (!IsRetryable(wr))
                        return wr;
                    SleepForRetry();
                }
            }
        }

        public WebResult PostRaw(string url, string contentType, string content, Dictionary<string, string> headers, CookieContainer cc)
        {
            int attempt = 0;
            while (true)
            {
                UpdateCallStats();
                attempt++;
                trace("Call " + numCalls + " Attempt: " + attempt + " POST: " + url);
                var wr = InnerPostRaw(url, contentType, cc, content, headers);
                LogResult(wr);
                if (wr.Code == System.Net.HttpStatusCode.OK)
                    return wr;
                if (attempt >= retries)
                    return wr;
                if (!IsRateBroken(wr))
                {
                    if (!IsRetryable(wr))
                        return wr;
                    SleepForRetry();
                }
            }
        }

        private WebResult InnerPostRaw(string url, string contentType, CookieContainer cc, string content, Dictionary<string, string> headers)
        {
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.Method = "POST";
            wr.KeepAlive = false;
            foreach (var kvp in headers)
            {
                if (kvp.Key == "Host")
                    wr.Host = kvp.Value;
                else if (kvp.Key == "Accept")
                    wr.Accept = kvp.Key;
                else if (kvp.Key == "Referer")
                    wr.Referer = kvp.Value;
                else
                    wr.Headers.Add(kvp.Key, kvp.Value);
            }
            wr.ContentType = contentType;
            wr.CookieContainer = cc;
            wr.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            WebResponse wresp = null;
            try
            {
                Stream rs = wr.GetRequestStream();
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(content);
                rs.Write(formitembytes, 0, formitembytes.Length);

                rs.Close();
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                string str = reader2.ReadToEnd();
                HttpWebResponse hr = wresp as HttpWebResponse;
                return new WebResult() { Result = str, Code = hr.StatusCode };
            }
            catch (Exception exc)
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
                return new WebResult() { Exc = exc };
            }
            finally
            {
                wr = null;
            }
        }

        private bool IsRateBroken(WebResult wr)
        {
            if (brokenRateCodes == null)
                return false;
            int c = (int)wr.Code;
            if(Array.IndexOf<int>(brokenRateCodes, c) < 0)
                return false;

            int sec = sleepRateAfterRateBrokenSeconds;
            if (sec == 0)
                sec = throttleSleepSeconds;
            error("Broken limit response detected going to sleep for seconds:" + sec);
            System.Threading.Thread.Sleep(sec * 1000);
            return true;
        }

        private void UpdateCallStats()
        {
            CheckCallLimit();
            numCalls++;
            RateLimits();
        }

        private void SleepForRetry()
        {
            trace("Going to sleep for seconds: " + retrySleepSeconds);
            System.Threading.Thread.Sleep(retrySleepSeconds * 1000);
        }

        private void CheckCallLimit()
        {
            if (maxCalls <= 0)
                return;
            if (numCalls >= maxCalls)
                throw new InvalidOperationException("Number of calls has reached specified limit:" + maxCalls);
        }

        private void RateLimits()
        {
            if (throttler == null)
                return;
            while (throttler.IsThrottled(DateTime.UtcNow))
            {
                trace("Throttling calls because rate limit encountered, sleeping for: " + throttleSleepSeconds); 
                System.Threading.Thread.Sleep(throttleSleepSeconds * 1000);
            }
            throttler.AddToHistory(DateTime.UtcNow, true);
        }

        private void LogResult(WebResult wr)
        {
            if (wr.Code == System.Net.HttpStatusCode.OK)
                trace("OK");
            else if(wr.Code == System.Net.HttpStatusCode.Redirect)
                trace("Redirect");
            else
                LogError(wr);
        }

        private void LogError(WebResult wr)
        {
            if (wr.Exc != null)
                error(wr.Exc.ToString());
            else
                error(wr.Code.ToString());
        }

        private static bool IsRetryable(WebResult wr)
        {
            if (wr.Exc != null)
                return true;
            return IsRetryable(wr.Code);
        }

        private static bool IsRetryable(System.Net.HttpStatusCode code)
        {
            return code == System.Net.HttpStatusCode.ServiceUnavailable ||
                   code == System.Net.HttpStatusCode.RequestTimeout ||
                   code == System.Net.HttpStatusCode.InternalServerError ||
                   code == System.Net.HttpStatusCode.BadGateway ||
                   code == System.Net.HttpStatusCode.GatewayTimeout;
        }

        private readonly Action<string> trace;
        private readonly Action<string> error;
        private readonly Throttler throttler;
        private readonly int retries;
        private readonly int maxCalls;
        private readonly int retrySleepSeconds;
        private readonly int throttleSleepSeconds;
        private int numCalls;
        private readonly int[] brokenRateCodes;
        private readonly int sleepRateAfterRateBrokenSeconds;
    }
}