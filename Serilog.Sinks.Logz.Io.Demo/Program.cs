using System;
using Serilog.Context;
using Serilog.Events;

namespace Serilog.Sinks.Logz.Io.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var token = Environment.GetEnvironmentVariable("LOGZIO_TOKEN");

            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.LogzIo(token, "serilog", options: new LogzioOptions
                {
                    BoostProperties = false,
                    UseHttps = true,
                    BatchPostingLimit = 5,
                    Period = TimeSpan.FromSeconds(5),
                    RestrictedToMinimumLevel = LogEventLevel.Information
                })
                .CreateLogger();
            
            using (LogContext.PushProperty("foo", "bar"))
            {
                logger.Information("Test {message}", "sample message");
                var exception = new Exception("BANG");
                exception.Data.Add("prop", "propvalue");
                logger.Error(exception, "Unhandled exception: {message}", exception.Message);
            }

            logger.Dispose();
        }
    }
}
