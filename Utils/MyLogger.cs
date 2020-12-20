using Serilog;
using Serilog.Core;

namespace auto_creamapi.Utils
{
    public class MyLogger
    {
        public static readonly Logger Log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("autocreamapi.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}