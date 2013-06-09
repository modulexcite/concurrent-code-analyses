
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Analysis;


namespace Analysis
{
    class CollectMain
    {
        private static string dir;

        private static int numAnalyzedApps;
        private static string logFile = @"C:\Users\Semih\Desktop\log.txt";

        private static List<string> analyzedProjects;

        static void Main(string[] args)
        {
            Start(args);
            //Test();
        }

        static void Initialize()
        {
            numAnalyzedApps = 1000;
            dir = @"D:\C#PROJECTS\XAMLProjects";
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
                numAnalyzedApps = int.Parse(args[0]);
                dir = args[1];
            }


            foreach (var subdir in Directory.GetDirectories(dir).Where(subdir => !analyzedProjects.Any(s => subdir.Split('\\').Last().Equals(s))).OrderBy(s => s).Take(numAnalyzedApps))
            {

                String appName = subdir.Split('\\').Last();
                IAnalysis app = new AsyncAnalysis(appName, subdir);

                
                Console.WriteLine(appName);


                app.loadSolutions();
                app.analyze();
                Helper.WriteLogger(logFile, app.appName + "," + app.numTotalProjects + "," + app.numUnloadedProjects + "," + app.numUnanalyzedProjects + "\r\n");

                
                //catch (Exception e)
                //catch(EntryPointNotFoundException e)
                //{
                //    Helper.WriteLogger(logFile, app.appName + "EXCEPTION\r\n");
                //}
            }          
        }

        static void Test()
        {
            // C:\Users\Semih\Documents\Visual Studio 2012\Projects\TestApplication
            IAnalysis app = new AsyncAnalysis("testApp", @"C:\Users\Semih\Documents\Visual Studio 2012\Projects\TestApplication");
            app.loadSolutions();
            app.analyze();
            Console.ReadLine();
        }
    }
}
