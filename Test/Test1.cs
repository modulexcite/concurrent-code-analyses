using Analysis;
using Roslyn.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Test
{
    class Test1
    {
        static void Main(string[] args)
        {
            string dir = @"C:\Users\Semih\Desktop\WindowsPhoneTestFramework-master";

            //var app = new AsyncAnalysis(@"C:\Users\Semih\Desktop\facebook-windows-phone-sample-master","facebook"); 
            
            var solutionPaths = Directory.GetFiles(dir, "*.sln", SearchOption.AllDirectories);
            foreach (var solutionPath in solutionPaths)
            {

                    var solution = Solution.Load(solutionPath);
                    foreach (var project in solution.Projects)
                    {

                        Console.WriteLine(IsWindowsPhoneProject(project));

                        
                    }
                
            }
            Console.ReadLine();
        }
        public static int IsWindowsPhoneProject(IProject project)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(project.FilePath);

            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            var node = doc.SelectSingleNode("//x:TargetFrameworkIdentifier", mgr);
            if (node != null)
            {
                if (node.InnerText.ToString().Equals("WindowsPhone"))
                    return 2;
                else if (node.InnerText.ToString().Equals("Silverlight"))
                {
                    var profileNode = doc.SelectSingleNode("//x:TargetFrameworkProfile", mgr);
                    if (profileNode != null && profileNode.InnerText.ToString().Contains("WindowsPhone"))
                        return 1;
                } 
            }
            return 0;

        }
    }
}
