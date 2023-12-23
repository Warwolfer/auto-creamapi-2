using auto_creamapi.Core;
using auto_creamapi.Utils;
using Microsoft.Extensions.Logging;
using MvvmCross.Platforms.Wpf.Core;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace auto_creamapi
{
    public class Setup : MvxWpfSetup<MainApplication>
    {
        protected override ILoggerFactory CreateLogFactory()
        {
            Log.Logger = MyLogger.Log;

            return new SerilogLoggerFactory();
        }

        protected override ILoggerProvider CreateLogProvider()
        {
            return new SerilogLoggerProvider();
        }
    }
}
