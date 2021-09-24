using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using FluentScheduler;


namespace IntegrateGoogleSignIn.Services
{
    public class DailyAnalyticJob : IJob
    {
        public void Execute()
        {
            JobFunctions.DailyAnalyticUpdated();
        }

    }

    public class MonthlyAnalyticJob : IJob
    {
        public void Execute()
        {
            JobFunctions.MonthlyAnalytic();
        }
    }

    public class TagVisitors : IJob
    {
        public void Execute()
        {
            JobFunctions.TagVisitorsUpdate();
        }
    }

    public class SourceReportData : IJob
    {
        public void Execute()
        {
            JobFunctions.SourceReportDataUpdate();
        }
    }
}