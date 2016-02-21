using CodeEndeavors.Extensions;
using Owin;
using System.Collections.Generic;
using System.Web.Http;

namespace CodeEndeavors.Distributed.Cache.Server
{
    public class Service
    {
        private static List<INotifierService> _notifiers = new List<INotifierService>();

        public static void Register(IAppBuilder app, HttpConfiguration config)
        {
            _notifiers = ReflectionExtensions.GetAllInstances<INotifierService>();
            _notifiers.ForEach(n =>
            {
                n.Register(app, config);
            });
        }

    }
}
