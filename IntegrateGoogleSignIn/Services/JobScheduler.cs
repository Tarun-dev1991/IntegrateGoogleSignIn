using System;
using FluentScheduler;

namespace IntegrateGoogleSignIn.Services
{
    public class JobScheduler : Registry
    {
        public JobScheduler()
        {
            //Daily
            var startForDaily = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 01, 00, 00);
            Schedule<DailyAnalyticJob>().ToRunOnceAt(startForDaily).AndEvery(1).Days();

            //Monthly
            var lastDayOfMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            var startFotMonthly = new DateTime(DateTime.Now.Year, DateTime.Now.Month, lastDayOfMonth, 01, 00, 00);
            Schedule<MonthlyAnalyticJob>().ToRunOnceAt(startFotMonthly).AndEvery(1).Months();

            //Daily Tag Counting
            var startDailyTagVisitor = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 01, 00, 00);
            Schedule<TagVisitors>().ToRunOnceAt(startDailyTagVisitor).AndEvery(1).Days();

            //Daily Source Counting
            var startDailySource = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 01, 00, 00);
            Schedule<SourceReportData>().ToRunOnceAt(startDailySource).AndEvery(1).Days();
        }

    }
}