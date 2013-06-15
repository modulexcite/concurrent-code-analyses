using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Services;

namespace Analysis
{
    public static class Extensions
    {
        public static bool IsCSProject(this IProject project)
        {
            return project.LanguageServices.Language.Equals("C#");
        }

        public static bool IsWPProject(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone") || a.Display.Contains("WindowsPhone"));
        }

        public static bool IsWP8Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone\\v8"));
        }

        public static bool IsAzureProject(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Azure"));
        }

        public static bool IsNet40Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.0"));
        }

        public static bool IsNet45Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.5") || a.Display.Contains(".NETCore\\v4.5"));
        }
    }
}
