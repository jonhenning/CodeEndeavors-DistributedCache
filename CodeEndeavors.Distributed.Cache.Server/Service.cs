using Owin;
using StructureMap;
using StructureMap.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CodeEndeavors.Distributed.Cache.Server
{
    public class Service
    {
        private static List<INotifierService> _notifiers = new List<INotifierService>();

        public static void Register(IAppBuilder app, HttpConfiguration config)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var container = new Container(_ =>
            {
                _.Scan(x =>
                {
                    x.WithDefaultConventions();
                    x.AssembliesFromPath(path);
                    x.AddAllTypesOf<INotifierService>();
                });
            });
            //Debug.WriteLine(container.WhatDidIScan());
            //Debug.WriteLine(container.WhatDoIHave());
            _notifiers = container.GetAllInstances<INotifierService>().ToList();

            _notifiers.ForEach(n =>
            {
                n.Register(app, config);
            });

        }

    }
}
