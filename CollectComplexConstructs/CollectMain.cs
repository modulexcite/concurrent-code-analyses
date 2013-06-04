
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;



namespace CollectComplexConstructs
{
    class CollectMain
    {
        private static string dir;

        private static int inc;
        private static string logFile = @"C:\Users\Semih\Desktop\log.txt";
        private static string appsFile = @"C:\Users\Semih\Desktop\apps.txt";
       
        private static List<string> analyzedProjects;

        static void Main(string[] args)
        {
            Start(args);
            //Test(); ;
        }

        static void Initialize()
        {
            inc = 5;
            dir = @"Z:\C#PROJECTS\GithubMostWatched990Projects";
            if (File.Exists(logFile))
                analyzedProjects = System.IO.File.ReadAllLines(logFile).Select(a => a.Split(',')[0]).ToList();
            else
                analyzedProjects = new List<string>(); 
            
           
        }


        static void Start(string [] args)
        {
            Initialize();
            if (args.Length > 0)
            {
                inc = int.Parse(args[0]);
                dir = args[1];
            }


            foreach (var subdir in Directory.GetDirectories(dir).Where(subdir => !analyzedProjects.Any(s => subdir.Split('\\')[3].Equals(s))).OrderBy(s => s).Take(inc))
            {
  
                ThreadTaskAnalysis app = new ThreadTaskAnalysis(subdir.Split('\\').Last(), subdir);

                Console.WriteLine(app.appName);
               

                try
                {
                    app.load();
                    app.analyze();
                    Helper.WriteLogger(logFile,app.appName + ","+ app.numTotalProjects + "," + app.numUnloadedProjects + "," + app.numUnanalyzedProjects + "\r\n");


                    Helper.WriteLogger(appsFile, app.appName + "," + app.isTPL + "," + app.isThreading + "," + app.isDataflow + "," + app.numberOfTaskInstances + "," + app.numberOfThreadInstances + "," + (app.numberOfThreadInstances+app.numberOfTaskInstances) + "\n");
                    
                }
                catch (Exception e)
                {
                    Helper.WriteLogger(logFile, "EXCEPTION\r\n");
                }
            }
            
        }

        static void Test()
        {
            ThreadTaskAnalysis app = new ThreadTaskAnalysis("ravendb-ravendb", @"Z:\GithubMostWatched990Projects\acken-AutoTest.Net");

            app.load();
            app.analyze();
            Console.WriteLine(app.isThreading);
            Console.ReadLine();
        }
    }
}
