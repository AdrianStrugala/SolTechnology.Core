using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolTechnology.Core.Logging
{
    public class AsyncStopwatch
    {
        private static readonly AsyncLocal<Stopwatch> InternalStopwatch = new();

        public AsyncStopwatch()
        {
            InternalStopwatch.Value = Stopwatch.StartNew();
        }
        public TimeSpan Elapsed => InternalStopwatch.Value.Elapsed;
    }
}
