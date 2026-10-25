using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;

namespace Builder
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //var mapping = new Dictionary<string, string>
            //{
            //    {"-dir", "ProjectRoot" },
            //    {"-port", "ServerPort" },
            //};
            //var argConfig = new ConfigurationBuilder()
            //   .AddCommandLine(args, mapping)
            //   .Build();
            var _cancelationToken = new CancellationTokenSource();
            var _buildManager = new BuilderManager();
            var _cli = new CommandLineController(_buildManager);
            _buildManager.RunLoadScript("./");

            await _cli.Run(_cancelationToken.Token);
        }
    }
}