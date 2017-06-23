using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ALOG.Utilities
{
    public static class JsonHelpers
    {
        public static string Serialize(object obj)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(obj);
        }

        public static T SafeDeserialize<T>(string str)
        {
            if (str == null || str.Length == 0)
                return default(T);
            try
            { 
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = 1024 * 1024 * 100;
                return serializer.Deserialize<T>(str);
            }
            catch
            {
                return default(T);
            }
        }

        public static string ToJson(this object obj)
        {
            return Serialize(obj);
        }

        public static T JsonDeserialize<T>(this string str)
        {
            return SafeDeserialize<T>(str);
        }
    }
}
