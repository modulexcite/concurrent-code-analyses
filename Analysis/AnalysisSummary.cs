using Utilities;
using System;

namespace Analysis
{
    public abstract class AnalysisSummary
    {
        public int NumPhoneProjects;
        public int NumPhone8Projects;
        public int NumAzureProjects;
        public int NumNet4Projects;
        public int NumNet45Projects;
        public int NumOtherNetProjects;

        private string CommonText()
        {
            throw new NotImplementedException();
        }

        protected abstract string AnalysisSpecificText();

        public void Write(string outputFilename)
        {
            var text = CommonText() + AnalysisSpecificText();

            Helper.WriteLogger(outputFilename, text);
        }

    }
}
