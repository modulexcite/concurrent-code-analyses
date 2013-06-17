using System;

namespace Collector
{
    class CollectMain
    {
        const string Candidates = @"C:\Users\david\Downloads\C# Projects\Candidates";
        //const string Candidates = @"C:\Users\david\Downloads\C# Projects\Codeplex1000MostDownloadedProjects";

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
                var collector = new Collector(Candidates, 1000);
                collector.Run();
            }

            Console.WriteLine(@"Program finished. Press any key to quit ...");
            Console.ReadKey();
        }
    }
}
