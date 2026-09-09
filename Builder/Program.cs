using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;

namespace Builder
{
    public class Program
    {
        internal static BuilderManager _buildManager = new BuilderManager();
        public static void Main(string[] args)
        {
            var mapping = new Dictionary<string, string>
            {
                {"-dir", "ProjectRoot" },
                {"-port", "ServerPort" },
            };

            var argConfig = new ConfigurationBuilder()
                .AddCommandLine(args, mapping)
                .Build();
            _buildManager.RunLoadScript("./");
        }
    }
}