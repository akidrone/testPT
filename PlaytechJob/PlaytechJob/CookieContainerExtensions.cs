using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ALOG.Utilities
{
    public static class CookieContainerExtensions
    {
        public static List<Cookie> AllCookies(this CookieContainer container)
        {
            var cookies = new List<Cookie>();

            var table = (Hashtable)container.GetType().InvokeMember("m_domainTable",
                                                                    BindingFlags.NonPublic |
                                                                    BindingFlags.GetField |
                                                                    BindingFlags.Instance,
                                                                    null,
                                                                    container,
                                                                    new object[] { });
            List<string> added = new List<string>();
            foreach (var key in table.Keys)
            {
                var domain = key as string;
                if (domain == null)
                    continue;
                if (domain.StartsWith("."))
                    domain = domain.Substring(1);
                foreach (Cookie cookie in container.GetCookies(new Uri(string.Format("https://{0}/", domain))))
                {
                    string name = domain + cookie.Name;
                    if (!added.Contains(name))
                    {
                        cookies.Add(cookie);
                        added.Add(name);
                    }
                }
            }
            return cookies;
        }
    }
}
