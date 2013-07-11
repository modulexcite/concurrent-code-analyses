using Microsoft.Win32;
using Roslyn.Services;
using Roslyn.Services.CSharp;
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
        public static void execute()
        {
            string dir = @"Z:\C#PROJECTS\Unused\wphonecommands";
            

            

            //var app = new AsyncAnalysis(@"C:\Users\Semih\Desktop\facebook-windows-phone-sample-master","facebook"); 
            
            var solutionPaths = Directory.GetFiles(dir, "*.sln", SearchOption.AllDirectories);
            foreach (var solutionPath in solutionPaths)
            {

                    var solution = Solution.Load(solutionPath);
                    int a = 0;
                    foreach (var project in solution.Projects)
                    {
                        a++;
                        Console.WriteLine(a+ " project: " + project.FilePath);
                        if (project.Documents == null)
                            Console.WriteLine("****************");
                        //IDocument doc = project.Documents.First();
                        //doc.LanguageServices.GetService<IExtractMethodService>();
                        
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
