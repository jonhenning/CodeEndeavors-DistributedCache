using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CodeEndeavors.Distributed.Cache.Server
{
    public interface INotifierService
    {
        void Register(IAppBuilder app, HttpConfiguration config);
    }
}
