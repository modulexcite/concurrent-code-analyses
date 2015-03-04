using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analysis;

namespace AnalysisRunner
{
    class ResultDatabaseContext : DbContext
    {
        public DbSet<Result> Results { get; set; }
    }

    public class Result
    {
        public DateTime Date { get; set; }
        public List<App> Apps { get; set; }
        public AnalysisType[] AnalysisTypes { get; set; }
    }
}
