using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace CodeEndeavors.Distributed.Cache.SampleSignalRServer.Controllers
{
    public class DemoController : ApiController
    {
        // GET api/demo 
        public IEnumerable<string> Get()
        {
            return new string[] { "Hello", "World" };
        }
    }
}
