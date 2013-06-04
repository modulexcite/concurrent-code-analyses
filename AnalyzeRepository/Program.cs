
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.IO;
using System.Threading.Tasks;

using System.Threading;
using System.Collections;
using Roslyn.Compilers.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security;

namespace TPLAnalyzer
{

    class Program
    {

        static string processedFile = @"C:\Users\semih\Desktop\processed.txt";

        static string reportFile = @"C:\Users\semih\Desktop\report.csv";

        static string tempFile = @"C:\Users\semih\Desktop\tmp.csv";
        static StreamWriter objWriter;
        private static string detailedUsageReportFile = @"C:\Users\semih\Desktop\detailedUsage.txt";
        private static string overallUsageReportFile = @"C:\Users\semih\Desktop\overallUsage.csv";

        static int amount = 5;
        static void Main2(string[] args)
        {


            //temp();

            fillMembers();
            readAllUsage();
            Console.ReadLine();


            //AnalyzeOneProject("test");
            //Console.ReadLine();
            //AnalyzeOneProject("ravendb,ravendb");
            //Console.WriteLine("finish");
            //Console.ReadLine();

            //Console.WriteLine("finish");
            //Console.ReadLine();

            //Project.blacklist = System.IO.File.ReadAllLines(@"C:\Users\semih\Desktop\blacklist.txt");

            //if (args.Length > 0)
            //{
            //    WriteUsageForCommits(args[0], args[1], args[2]);
            //    return;
            //}

            //var apps=AnalyzeParallelProjects();
            var apps = AnalyzeTplPlinqProjects();
            var processedApps = System.IO.File.ReadAllLines(processedFile);

            //readAllSumDistribution();

#if !TEST


            String folderName;

            for (int i = 0; amount >= 0; i++)
            {

                //if (i >= githubApps.Length)
                //{
                //    isGithub = false;
                //    index = i - githubApps.Length;
                //    apps = codeplexApps;
                //    if (index >= codeplexApps.Length)
                //    {
                //        Console.WriteLine("finished");
                //        break;
                //    }
                //}
                folderName = apps.ElementAt(i).Replace("+", ",");
                if (processedApps.Any(a => a.Equals(folderName)) || !folderName.Contains(","))
                    continue;

                amount--;

                Console.WriteLine(folderName);

                Project project = new Project(folderName);

                try
                {
                    project.Start();
                    MarkProcessed(folderName);
                    //WriteLockReport(project);
                    //WriteReport(project);
                    //WriteDetailedUsageReport(project);
                    //WriteOverallUsageReport(project);
                }
                catch (System.IO.PathTooLongException e)
                {
                    MarkProcessed(folderName + ",PathTooLong");
                }
                catch (System.IO.FileNotFoundException e)
                {
                    MarkProcessed(folderName + ",FileNotFound");
                }


                //makeUsageZeros();

            }

#else
            string dir = @"D:\\a\ravendb,ravendb\";

            Project project = new Project("ravendb,ravendb");
            project.Start(dir);
            Console.WriteLine(Project.tplUsage.Sum(a => a.Value));
            //project.printHistogram();

            //readAllSumDistribution();
            // writeReport(project,projectName);
            //writeResult(project);

            Console.WriteLine("thats it");
            Console.ReadLine();
#endif
        }


        private static void temp()
        {
            fillMembers();
            string dir = @"C:\Users\semih\Desktop\newww";

            string[] files = System.IO.Directory.GetFiles(dir);

            foreach (var file in files)
            {
                if (file.Contains("detailed"))
                {
                    var lines = System.IO.File.ReadLines(file);
                    foreach (var line in lines)
                    {
                        var result = temp2(line);
                        Console.WriteLine(result[0] + "-" + result[1]);
                    }
                }

                Console.WriteLine("---------");

            }


        }


        private static int[] temp2(String line)
        {
            makeUsageZeros();
            var results = line.Split(',');
            int i = 1;
            int sum = 0;
            int sum2 = 0;

            string foldername = results[0];

            if (results.Length == 1654)
            {
                i = 2;
                foldername += "," + results[1];
            }

            HashSet<string> set = new HashSet<string>();



            foreach (var key in Project.threadingUsage.Keys.ToList())
            {
                int val = int.Parse(results[i++]);
                string methodname = key.Replace("System.Threading.", "").Split('.')[0] + "." + key.Replace("System.Threading.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];

                if (methodname.Contains("Thread.") || methodname.Contains("ThreadPool."))
                {
                    sum += val;
                }
                else
                {
                    sum2 += val;
                }




            }


            set = new HashSet<string>();
            foreach (var key in Project.tplUsage.Keys.ToList())
            {
                int val = int.Parse(results[i++]);

                string methodname = key.Replace("System.Threading.Tasks.", "").Split('.')[0] + "." + key.Replace("System.Threading.Tasks.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];



            }

            set = new HashSet<string>();
            foreach (var key in Project.plinqUsage.Keys.ToList())
            {
                int val = int.Parse(results[i++]);

                string methodname = key.Replace("System.Linq.", "").Split('.')[0] + "." + key.Replace("System.Linq.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];

            }

            set = new HashSet<string>();
            foreach (var key in Project.colConUsage.Keys.ToList())
            {
                int val = int.Parse(results[i++]);

                string methodname = key.Replace("System.Collections.Concurrent.", "").Split('.')[0] + "." + key.Replace("System.Collections.Concurrent.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];


            }
            return new int[] { sum, sum2 };
        }

        private static void WriteUsageForCommits(string name, string folder, string logFile)
        {
            detailedUsageReportFile = logFile + "_detailed.txt";
            overallUsageReportFile = logFile + "_overview.txt";
            Project project = new Project(name);
            project.Start(folder);
            WriteOverallUsageReport(project);
            WriteDetailedUsageReport(project);
        }

        private static void AnalyzeOneProject(string name)
        {

            Project project = new Project(name);
            project.Start("C:\\Users\\semih\\Documents\\Visual Studio 2010\\Projects\\ConsoleApplication1"); // 
        }

        private static IEnumerable<string> AnalyzeParallelProjects()
        {
            string[] lines = System.IO.File.ReadAllLines(tempFile);

            string[] dirs = Directory.GetDirectories(@"D:\\a\");

            var newLines = lines.Where(a => a.Split(',')[22] == "TRUE").Select(a => a.Split(',')[0].Replace("+", ","));


            //foreach (var line in newLines)
            //{

            //    string folderName = line.Split(',')[0].Replace("+", ",");
            //    Console.WriteLine(folderName);
            //    if (line.Split(',')[1].Equals("TRUE"))
            //    {
            //        CopyFilesRecursively(@"K:\\a\g\" + folderName, @"D:\\a\" + folderName);
            //    }
            //    else
            //    {
            //        CopyFilesRecursively(@"K:\\a\c\" + folderName, @"D:\\a\" + folderName);
            //    }

            //}
            return newLines;
        }


        private static IEnumerable<string> AnalyzeTplProjects()
        {
            string[] lines = System.IO.File.ReadAllLines(overallUsageReportFile);

            var folderNames = lines.Where(a => (int.Parse(a.Split(',')[8]) > 0)).Select(a => a.Split(',')[0].Replace("/", ","));

            return folderNames;
        }
        private static IEnumerable<string> AnalyzeTplPlinqProjects()
        {
            string[] lines = System.IO.File.ReadAllLines(overallUsageReportFile);

            var folderNames = lines.Where(a => (int.Parse(a.Split(',')[8]) > 0) || (int.Parse(a.Split(',')[7]) > 0)).Select(a => a.Split(',')[0].Replace("/", ","));
            return folderNames;
        }


        private static void CopyFilesRecursively(String SourcePath, String DestinationPath)
        {
            // First create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            // Copy all the files
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath));
        }

        private static void WriteReport(Project project)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(reportFile, true))
            {
                file.WriteLine(project.GetReport());
            }
        }
        private static void WriteOverallUsageReport(Project project)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(overallUsageReportFile, true))
            {
                file.WriteLine(project.GetOverallUsageReport());
            }
        }
        private static void WriteDetailedUsageReport(Project project)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(detailedUsageReportFile, true))
            {
                file.WriteLine(project.GetDetailedUsageReport());
            }
        }

        private static void MarkProcessed(String folderName)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(processedFile, true))
            {
                file.WriteLine(folderName);

            }
        }

        private static void WriteLockReport(Project project)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\lockUsage.txt", true))
            {
                file.WriteLine(project.folderName + "," + project.numLockStatements + "," + project.numVolatileStatements);
            }
        }

        private static void makeUsageZeros()
        {
            foreach (var key in Project.tplUsage.Keys.ToList())
            {
                Project.tplUsage[key] = 0;
            }
            foreach (var key in Project.colConUsage.Keys.ToList())
            {
                Project.colConUsage[key] = 0;
            }
            foreach (var key in Project.threadingUsage.Keys.ToList())
            {
                Project.threadingUsage[key] = 0;
            }
            foreach (var key in Project.plinqUsage.Keys.ToList())
            {
                Project.plinqUsage[key] = 0;
            }
        }



        private static bool isTplPlinq(string name)
        {
            var lines = System.IO.File.ReadAllLines(@"C:\Users\semih\Dropbox\CloudDocuments\Things Folders\ Projects\ParallelLibraries\Results\new results\overallUsage.csv");
            name = name.Replace(",", "+");

            var tmp = lines.Where(a => a.Split(',')[0] == name);

            if (tmp.Count() == 0)
                return false;

            var line = tmp.First();
            return int.Parse(line.Split(',')[7]) + int.Parse(line.Split(',')[8]) > 0;
        }



        static Dictionary<string, int[]> statistics = new Dictionary<string, int[]>();
        private static void readAllUsage()
        {
            var lines = System.IO.File.ReadAllLines(@"C:\Users\semih\Dropbox\CloudDocuments\Things Folders\ Projects\ParallelLibraries\Results\new results\detailedUsage.txt");
            makeUsageZeros();

            Dictionary<string, int> threadingPer = new Dictionary<string, int>();
            Dictionary<string, int> tplPer = new Dictionary<string, int>();
            Dictionary<string, int> plinqPer = new Dictionary<string, int>();
            Dictionary<string, int> colConPer = new Dictionary<string, int>();

            string prev = "";

            int tmpCount = 0;
            int count = 0;
            int count2 = 0;
            int[] sizes = { 0, 0, 0 };
            int index = -1;
            foreach (var line in lines)
            {
                index++;
                var results = line.Split(',');
                bool isUseTplPlinq = false;
                bool isUseThread = false;
                bool isJoined = false;
                bool isThreadStarted = false;
                int i = 1;

                bool isConcurrency = false;
                bool isParallelism = false;


                string foldername = results[0];

                if (results.Length == 1654)
                {
                    i = 2;
                    foldername += "," + results[1];
                }

                HashSet<string> set = new HashSet<string>();


                if (!isTplPlinq(foldername) || foldername == "websitepanel")
                    continue;


                foreach (var key in Project.threadingUsage.Keys.ToList())
                {
                    int val = int.Parse(results[i++]);
                    Project.threadingUsage[key] += val;
                    string methodname = key.Replace("System.Threading.", "").Split('.')[0] + "." + key.Replace("System.Threading.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];

                    if (!threadingPer.ContainsKey(methodname))
                        threadingPer.Add(methodname, 0);
                    if (val > 0 && !set.Contains(methodname))
                    {
                        threadingPer[methodname]++;
                        set.Add(methodname);
                    }

                    //if ((key.Contains("Thread.Thread(") || key.Contains("ThreadPool.QueueUserWorkItem(")) && val > 0)
                    //    isUseThread = true;

                    //if ((key.Contains("Thread.Join")) && val > 0)
                    //{
                    //    isJoined = true;
                    //    isParallelism = true;
                    //}
                    //if ((key.Contains("Thread.Start")) && val > 0)
                    //    isThreadStarted = true;
                    //if ((key.Contains("ThreadPool")) && val > 0)
                    //    isConcurrency = true;

                    if (!statistics.ContainsKey(methodname))
                        statistics[methodname] = new int[lines.Length];
                    statistics[methodname][index] += val;

                }

                if (isThreadStarted && !isJoined)
                    isConcurrency = true;

                set = new HashSet<string>();
                foreach (var key in Project.tplUsage.Keys.ToList())
                {
                    int val = int.Parse(results[i++]);
                    Project.tplUsage[key] += val;
                    string methodname = key.Replace("System.Threading.Tasks.", "").Split('.')[0] + "." + key.Replace("System.Threading.Tasks.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];




                    if (!tplPer.ContainsKey(methodname))
                        tplPer.Add(methodname, 0);
                    if (val > 0 && !set.Contains(methodname))
                    {
                        tplPer[methodname]++;
                        set.Add(methodname);
                    }

                    if (val > 0)
                        isUseTplPlinq = true;

                    //if ((key.Contains("Parallel.") || key.Contains("WaitAll")) && val > 0)
                    //    isParallelism = true;

                    //// || key.Contains("TaskCompletion")
                    //if ((key.Contains("FromAsync") || key.Contains("Wait(") || key.Contains("TaskCompletion")) && val > 0)
                    //    isConcurrency = true;
                    if (!statistics.ContainsKey(methodname))
                        statistics[methodname] = new int[lines.Length];
                    statistics[methodname][index] += val;

                }

                set = new HashSet<string>();
                foreach (var key in Project.plinqUsage.Keys.ToList())
                {
                    int val = int.Parse(results[i++]);
                    Project.plinqUsage[key] += val;
                    string methodname = key.Replace("System.Linq.", "").Split('.')[0] + "." + key.Replace("System.Linq.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];

                    if (!plinqPer.ContainsKey(methodname))
                        plinqPer.Add(methodname, 0);
                    if (val > 0 && !set.Contains(methodname))
                    {
                        plinqPer[methodname]++;
                        set.Add(methodname);
                    }
                    if (val > 0)
                    {
                        isParallelism = true;
                        isUseTplPlinq = true;
                    }

                    if (!statistics.ContainsKey(methodname))
                        statistics[methodname] = new int[lines.Length];
                    statistics[methodname][index] += val;
                }

                set = new HashSet<string>();
                foreach (var key in Project.colConUsage.Keys.ToList())
                {
                    int val = int.Parse(results[i++]);
                    Project.colConUsage[key] += val;
                    string methodname = key.Replace("System.Collections.Concurrent.", "").Split('.')[0] + "." + key.Replace("System.Collections.Concurrent.", "").Split('.')[1].Split('<')[0].Split('(')[0].Split('.')[0];

                    if (!colConPer.ContainsKey(methodname))
                        colConPer.Add(methodname, 0);
                    if (val > 0 && !set.Contains(methodname))
                    {
                        colConPer[methodname]++;
                        set.Add(methodname);
                    }

                    if (!statistics.ContainsKey(methodname))
                        statistics[methodname] = new int[lines.Length];
                    statistics[methodname][index] += val;

                }

                //if (amount == 0)
                //    break;


                //    var processedApps = System.IO.File.ReadAllLines(processedFile);
                //    if (!processedApps.Any(a => a == foldername))
                //    {
                //        Console.WriteLine(foldername);
                //        Project project = new Project(foldername);
                //        project.Start();
                //        MarkProcessed(foldername);
                //        if (isUseTplPlinq && !project.isUseAsParallel)
                //        {
                //            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\errorPlinq.txt", true))
                //            {
                //                file.WriteLine(foldername);
                //            }
                //        }
                //        amount--;
                //    }




                //if (!isUseTplPlinq && isUseThread)
                //{
                //    count++;
                //    string[] apps = System.IO.File.ReadAllLines(@"C:\Users\semih\Desktop\overallUsage.csv");
                //    var tmp = apps.Where(a => a.Split(',')[0] == foldername.Replace(",", "+"));
                //    string size=tmp.First().Split(',')[9];
                //    if (size == "SMALL")
                //        sizes[0]++;
                //    else if (size == "MEDIUM")
                //        sizes[1]++;
                //    else if (size == "BIG")
                //        sizes[2]++;
                //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\onlyThreadingProjects.txt", true))
                //        file.WriteLine(foldername);

                //}
                //if (isUseTplPlinq && isUseThread)
                //    count2++;

                //if (isParallelism)
                //{
                //    count2++;
                //    string[] apps = System.IO.File.ReadAllLines(@"C:\Users\semih\Desktop\overallUsage.csv");
                //    Console.WriteLine(foldername);
                //    var tmp = apps.Where(a => a.Split(',')[0] == foldername.Replace(",", "+")).First();
                //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\parallelismProjects.txt", true))
                //        file.WriteLine(tmp);

                //}
                //if (isConcurrency)
                //    count++;
                //if (isUseAsParallel)
                //    tmpCount++;
            }

            //Console.WriteLine(count + " " + sizes[0] + " " + sizes[1] + " " + sizes[2]);
            Console.WriteLine(count + " - " + count2 + "-" + tmpCount);

            /* HISTOGRAM */
            //        var list = Project.tplUsage;
            //        var tmp = list.Select(a => new KeyValuePair<string, int>(a.Key.Replace("System.Threading.Tasks.", "").Replace("System.Threading.", "").Replace("System.Linq.", "").Replace("System.Collections.Concurrent.", ""), a.Value))
            //.OrderByDescending(a => a.Value).ToDictionary(a => a.Key, a => a.Value);

            //        //var tmpList = tmp.GroupBy(a => a.Key.Split('.')[0] + a.Key.Split('.')[1].Split('<')[0].Split('(')[0] ).Select(group => new { name = group.Key, count = group.Sum(x => x.Value) }).OrderByDescending(a => a.count);

            //        var tmpList = tmp;
            //        Console.WriteLine(tmpList.Count());

            //        int sum = tmp.Sum(a => a.Value);
            //        int index = 0;
            //        int length = tmp.Count;
            //        int tempSum = 0;
            //        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\temp.txt", false))
            //        {
            //            foreach (var entry in tmpList)
            //            {
            //                index++;
            //                tempSum += entry.Value;
            //                file.WriteLine(entry.Key.Replace(",","+") + "," + entry.Value+ "," + getPercent(index, length) + "," + getPercent(tempSum, sum));
            //            }
            //        }

            WriteUsageForLibrary(Project.threadingUsage, threadingPer, @"C:\Users\semih\Desktop\threading.txt");
            WriteUsageForLibrary(Project.tplUsage, tplPer, @"C:\Users\semih\Desktop\tpl.txt");
            WriteUsageForLibrary(Project.plinqUsage, plinqPer, @"C:\Users\semih\Desktop\plinq.txt");
            WriteUsageForLibrary(Project.colConUsage, colConPer, @"C:\Users\semih\Desktop\colcon.txt");


        }

        private static void WriteUsageForLibrary(Dictionary<string, int> list, Dictionary<string, int> list2, String fileName)
        {
            list = list.Select(a => new KeyValuePair<string, int>(a.Key.Replace("System.Threading.Tasks.", "").Replace("System.Threading.", "").Replace("System.Linq.", "").Replace("System.Collections.Concurrent.", ""), a.Value))
                .OrderByDescending(a => a.Value).ToDictionary(a => a.Key, a => a.Value);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName, false))
            {
                int totalNS = list.Sum(a => a.Value);
                //file.WriteLine("Namespace : " + totalNS + "\n");


                var tempList = list.GroupBy(a => a.Key.Split('.')[0]).Select(group => new { name = group.Key, count = group.Sum(x => x.Value) }).OrderByDescending(a => a.count);

                int temp = 0;
                int index = 0;
                int sum = tempList.Sum(a => a.count);
                int total = tempList.Count();

                foreach (var classCons in tempList)
                {
                    //file.WriteLine(String.Format("{0} {1} - {2}%", classCons.name, classCons.count, getPercent(classCons.count, totalNS)));

                    foreach (var method in list.Where(a => a.Key.Split('.')[0] == classCons.name).GroupBy(a => a.Key.Split('.')[1].Split('<')[0].Split('(')[0]).Select(group => new { name = group.Key, count = group.Sum(x => x.Value) }).OrderByDescending(a => a.count))
                    {
                        //file.WriteLine(String.Format("{0,5}{1,-5}{2,-5}{3,-5}{4}", "", method.count, getPercent(method.count, classCons.count) + "%", list2[classCons.name + "." + method.name], method.name));
                        if (method.count > 0)
                        {
                            string key = classCons.name + "." + method.name;
                            Array.Sort<int>(statistics[key]);
                            file.WriteLine(String.Format("<tr>\n    <td>{0}</td>\n    <td>{1}</td>\n    <td></td>\n    <td>{2}</td>\n    <td>{3}</td>\n    <td>{4}</td>\n    <td>{5}</td>\n     <td>{6}</td>\n    <td>{7:0.00}</td>\n    <td>{8:0.00}</td></tr>\n", classCons.name.Replace(">", "&gt").Replace("<", "&lt"), method.name, method.count, getPercent(method.count, classCons.count) + "%", list2[classCons.name + "." + method.name], statistics[key].Max(), statistics[key][statistics[key].Length / 2], statistics[key].Average(), CalculateStdDev(statistics[key])));
                        }

                        foreach (var methodVar in list.Where(a => a.Key.Contains(classCons.name + "." + method.name + "(") || a.Key.Contains(classCons.name + "." + method.name + ".") || a.Key.Contains(classCons.name + "." + method.name + "<")).OrderByDescending(a => a.Value))
                        {
                            if (methodVar.Value > 0)
                            {
                                //file.WriteLine(String.Format("{0,20}{1,-5}{2}", "", methodVar.Value, methodVar.Key.Replace(classCons.name + ".", "")));

                                file.WriteLine(String.Format("<tr>\n    <td></td>\n    <td></td>\n    <td><a href=\"{0}\">{1}</a></td>\n    <td>{2}</td>\n    <td></td>\n    <td></td>\n    <td></td>\n     <td></td>\n    <td></td>\n    <td></td></tr>\n", "usage/" + classCons.name.Replace("<", "(").Replace(">", ")") + "-" + method.name + "-" + getIndex(methodVar.Key) + ".html", methodVar.Key.Replace(classCons.name + ".", "").Replace(">", "&gt").Replace("<", "&lt"), methodVar.Value));

                            }
                        }
                    }

                    index++;
                    temp += classCons.count;
                    //Console.WriteLine("{0} {1} {2} - {3:0.0}/{4:0.0} = {5:0.0}", index, classCons.name, classCons.count, ((double)temp / sum) * 100, 100 - ((double)temp / sum) * 100, ((double)index / total) * 100);


                }

            }





        }

        private static double CalculateStdDev(IEnumerable<int> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }

        public static int getIndex(String s)
        {
            int hash = s.Replace("System.Threading.Tasks.", "").Replace("System.Threading.", "").Replace("System.Linq.", "").Replace("System.Collections.Concurrent.", "").GetHashCode();
            hash = Math.Abs(hash);

            return hash;
        }


        private static int getPercent(int a, int b)
        {
            if (b == 0)
            {
                return 0;
            }
            Double r = a * 100 / (Double)b;
            return (int)Math.Round(r);
        }

        private static void fillMembers()
        {
            SyntaxTree tree = SyntaxTree.ParseCompilationUnit(@"
using System;
using System.Threading.Tasks;
            namespace Roslyn.Compilers
{
     class Program
     {
          static void Main(string[] args)
          	{
               Console.WriteLine(""Hello, World!"");
		}
   }
}

            
            ");


            Project.tplUsage = new Dictionary<string, int>();
            Project.threadingUsage = new Dictionary<string, int>();
            Project.plinqUsage = new Dictionary<string, int>();
            Project.colConUsage = new Dictionary<string, int>();

            Project.fingerprints = new HashSet<string>();


            AssemblyFileReference mscorlib = new AssemblyFileReference(
                                          typeof(object).Assembly.Location);
            AssemblyFileReference systemCore = new AssemblyFileReference(Assembly.Load("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").Location);
            AssemblyFileReference system = new AssemblyFileReference(Assembly.Load("System, Version=4.0.0.0, Culture=neutral,PublicKeyToken=b77a5c561934e089").Location);


            Compilation compilation = Compilation.Create("HelloWorld")
                            .AddReferences(mscorlib, systemCore, system)
                            .AddSyntaxTrees(tree);

            NamespaceSymbol tpl = ((compilation.GlobalNamespace.GetMembers("System").First() as NamespaceSymbol)
                                    .GetMembers("Threading").First() as NamespaceSymbol)
                                    .GetMembers("Tasks").First() as NamespaceSymbol;
            NamespaceSymbol threading = ((compilation.GlobalNamespace.GetMembers("System").First() as NamespaceSymbol)
                                    .GetMembers("Threading").First() as NamespaceSymbol);

            NamespaceSymbol colcon = ((compilation.GlobalNamespace.GetMembers("System").First() as NamespaceSymbol)
                                    .GetMembers("Collections").First() as NamespaceSymbol)
                                    .GetMembers("Concurrent").First() as NamespaceSymbol;

            NamespaceSymbol linq = ((compilation.GlobalNamespace.GetMembers("System").First() as NamespaceSymbol)
                                   .GetMembers("Linq").First() as NamespaceSymbol);


            var members = threading.GetMembers().ToList().Union(tpl.GetMembers().ToList()).Union(linq.GetMembers().ToList()).Union(colcon.GetMembers().ToList());

            Dictionary<string, int> list = null;

            foreach (var classType in members)
            {
                String namespaceName = classType.ContainingNamespace.ToString();
                String className = classType.Name;
                if (namespaceName == "System.Threading")
                {
                    list = Project.threadingUsage;
                }
                else if (namespaceName == "System.Threading.Tasks")
                {
                    list = Project.tplUsage;
                }
                else if (namespaceName == "System.Linq")
                {
                    list = Project.plinqUsage;
                }
                else if (namespaceName == "System.Collections.Concurrent")
                {
                    list = Project.colConUsage;
                }


                if (!(classType is NamedTypeSymbol))
                    continue;
                //Console.WriteLine(className);
                if (className == "Timer" && namespaceName == "System.Threading")
                    continue;
                if (!className.Contains("Parallel") && namespaceName == "System.Linq")
                    continue;

                Project.fingerprints.Add(classType.Name);

                HashSet<int> set = new HashSet<int>();

                foreach (var method in (classType as NamedTypeSymbol).GetMembers())
                {
                    if (method is MethodSymbol)
                    {
                        if ((method.Name == "Sleep" && className == "Thread") || list.ContainsKey(method.OriginalDefinition.ToString()))
                        {
                            continue;
                        }

                        //Console.WriteLine(method.OriginalDefinition);
                        list.Add(method.OriginalDefinition.ToString(), 0);
                        int hash = method.OriginalDefinition.ToString().Replace("System.Threading.Tasks.", "").Replace("System.Threading.", "").Replace("System.Linq.", "").Replace("System.Collections.Concurrent.", "").GetHashCode();
                        hash = Math.Abs(hash);
                        if (set.Contains(hash))
                            Console.WriteLine("asd");
                        else
                            set.Add(hash);

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\asd.txt", true))
                        {

                            file.WriteLine(method.OriginalDefinition.ToString().Replace("System.Threading.Tasks.", "").Replace("System.Threading.", "").Replace("System.Linq.", "").Replace("System.Collections.Concurrent.", "") + " " + hash);
                        }

                        if (!Project.fingerprints.Contains(method.Name))
                            Project.fingerprints.Add(method.Name);
                    }

                }

            }










        }


    }

}