using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Distributed.Cache.Client
{
    public class OperationTimer : IDisposable
    {
        private Stopwatch _watch = new Stopwatch();
        private string _message;

        public OperationTimer(string message)
            : this(message, null)
        {
        }
        public OperationTimer(string message, params object[] args)
        {
            if (args != null)
                _message = string.Format(message, args);
            else
                _message = message;
            _watch.Start();
        }

        public void Dispose()
        {
            _watch.Stop();
            Logging.Log(Logging.LoggingLevel.Detailed, string.Format("TIMER:  {0} : {1}ms", _message, _watch.ElapsedMilliseconds));
            GC.SuppressFinalize(this);
        }
    }
}
