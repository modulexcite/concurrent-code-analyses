using System.IO;
using Roslyn.Services;
using Utilities;
using System;
using System.Configuration;
using NLog;

namespace Analysis
{
    public abstract class AnalysisResultBase
    {
        

        public readonly string _appName;

        protected static readonly Logger SummaryLog = LogManager.GetLogger("SummaryLog");
        protected static readonly Logger phoneProjectListLog = LogManager.GetLogger("PhoneProjectListLog");
        protected static readonly Logger phoneSolutionListLog = LogManager.GetLogger("PhoneSolutionListLog");


        public int NumPhone7Projects;
        public int NumPhone8Projects;
        protected int NumNet4Projects;
        protected int NumNet45Projects;
        protected int NumOtherNetProjects;

        protected int NumTotalProjects;
        protected int NumUnanalyzedProjects;

        public int NumTotalSLOC;

        public AnalysisResultBase(string appName)
        {
            _appName = appName;
        }

        public void AddAnalyzedProject(Enums.ProjectType type)
        {
            switch (type)
            { 
                case Enums.ProjectType.WP7:
                     NumPhone7Projects++;
                    break;
                case Enums.ProjectType.WP8:
                    NumPhone8Projects++;
                    break;
                case Enums.ProjectType.NET4:
                    NumNet4Projects++;
                    break;
                case Enums.ProjectType.NET45:
                    NumNet45Projects++;
                    break;
                case Enums.ProjectType.NETOther:
                    NumOtherNetProjects++;
                    break;
            }
        }

        public void AddUnanalyzedProject()
        {
            NumUnanalyzedProjects++;
        }

        public void AddProject()
        {
            NumTotalProjects++;
        }

        public void WritePhoneProjects(IProject project)
        {
            phoneProjectListLog.Info(project.FilePath);
            //if (!hasPhoneProjectInThisSolution)
            phoneSolutionListLog.Info(project.Solution.FilePath);
            //hasPhoneProjectInThisSolution = true;

        }



        public abstract void WriteSummaryLog();

        
    }
}
