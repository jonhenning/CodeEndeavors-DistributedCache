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
        private Action<Service.LoggingLevel, string> _onLoggingMessage;

        public OperationTimer(Action<Service.LoggingLevel, string> onLoggingMessage, string message)
            : this(onLoggingMessage, message, null)
        {
        }
        public OperationTimer(Action<Service.LoggingLevel, string> onLoggingMessage, string message, params object[] args)
        {
            _onLoggingMessage = onLoggingMessage;
            if (args != null)
                _message = string.Format(message, args);
            else
                _message = message;
            _watch.Start();
        }

        public void Dispose()
        {
            _watch.Stop();
            if (_onLoggingMessage != null)
                _onLoggingMessage(Service.LoggingLevel.Detailed, string.Format("TIMER:  {0} : {1}ms", _message, _watch.ElapsedMilliseconds));
            GC.SuppressFinalize(this);
        }
    }
}
