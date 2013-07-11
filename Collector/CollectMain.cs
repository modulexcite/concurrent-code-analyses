using System;
using System.Configuration;

namespace Collector
{
    class CollectMain
    {
        private static string AppsPath = ConfigurationManager.AppSettings["AppsPath"];

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var batchSize = int.Parse(args[0]);
                var topDir = args[1];

                var collector = new Collector(topDir, batchSize);
                collector.Run();
            }
            else
            {
                var collector = new Collector(AppsPath, 1000);
                collector.Run();
            }

            Console.WriteLine(@"Program finished. Press any key to quit ...");
            Console.ReadKey();
        }
    }
}
