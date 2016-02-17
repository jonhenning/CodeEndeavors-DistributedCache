using CodeEndeavors.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client.Extensions
{
    public static class ConversionExtensions
    {
        public static Dictionary<string, object> ToDictionary(this object o)
        {
            return o.ToJson().ToObject<Dictionary<string, object>>();
        }
    }
}
