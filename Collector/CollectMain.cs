using System;

namespace Collector
{
    class CollectMain
    {
        static void Main(string[] args)
        {
            Start(args);
        }

        static void Start(string[] args)
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
                var collector = new Collector(@"C:\Users\david\Downloads\C# Projects\Candidates", 1000);
                collector.Run();
            }

            Console.WriteLine(@"Program finished. Press any key to quit ...");
            Console.ReadKey();
        }
    }
}
