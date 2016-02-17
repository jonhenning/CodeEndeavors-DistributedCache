﻿using System.Web.Http;
using Owin;

namespace CodeEndeavors.Distributed.Cache.SampleSignalRServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            app.UseWebApi(config);

            CodeEndeavors.Distributed.Cache.Server.Service.Register(app, config);

        }
    }
}