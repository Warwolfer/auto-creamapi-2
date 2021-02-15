using Serilog;
using Serilog.Core;
using Serilog.Exceptions;

namespace auto_creamapi.Utils
{
    public class MyLogger
    {
        public static readonly Logger Log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console()
            .WriteTo.File("autocreamapi.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}