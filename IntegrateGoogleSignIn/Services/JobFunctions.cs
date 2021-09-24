using IntegrateGoogleSignIn.Helpers;
using IntegrateGoogleSignIn.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace IntegrateGoogleSignIn.Services
{
    public static class JobFunctions
    {
        public static void DailyAnalytic()
        {
            try
            {
                using (var db = new DBEntities())
                {
                    //1.0 function called
                    CommonFunctions.SaveAuditLog("1.0: Function => DailyAnalytic() called");

                    var currentDate = DateTime.UtcNow.AddDays(-1);
                    //var currentDate = new DateTime(DateTime.Now.Year, 10, 01);
                    var proPackageDomainList = db.propackageusers.Select(m => m.domainname).Distinct().ToList();
                    var domainCollection = new List<string>();
                    if (proPackageDomainList != null && proPackageDomainList.Any())
                    {
                        //2.0 proPackageDomainList data found
                        CommonFunctions.SaveAuditLog("2.0:  proPackageDomainList data found. Total Data Count == " + proPackageDomainList.Count);

                        foreach (var domainName in proPackageDomainList.Where(m => !string.IsNullOrEmpty(m)).ToList())
                        {
                            string wwwDomainName;
                            string wwwWithoutDomainName;
                            if (domainName.Contains("www."))
                            {
                                wwwDomainName = domainName;
                                wwwWithoutDomainName = domainName.Replace("www.", string.Empty);
                            }
                            else
                            {
                                wwwDomainName = "www." + domainName;
                                wwwWithoutDomainName = domainName;
                            }

                            var domainAvailable = domainCollection.FirstOrDefault(m =>
                                m.Equals(wwwDomainName) || m.Equals(wwwWithoutDomainName));

                            if (domainAvailable == null)
                            {
                                var dateWiseAvailable = db.UserDailyAnalytics.FirstOrDefault(m => DbFunctions.TruncateTime(m.collectiondate) == currentDate.Date
                                    && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName)));

                                if (dateWiseAvailable == null || (dateWiseAvailable != null && dateWiseAvailable.bluetickvisitors == 0 && dateWiseAvailable.redtickvisitors == 0 && dateWiseAvailable.greentickvisitors == 0))
                                {
                                    var collection = CommonFunctions.GetDailyCount(domainName, currentDate);
                                    if (collection != null)
                                    {
                                        CommonFunctions.SaveAuditLog(
                                            "3.0:  collection data for doamin => " + domainName);

                                        var userDailyAnalytic = new UserDailyAnalytic
                                        {
                                            bluetickvisitors = collection.blueVisitors,
                                            collectiondate = currentDate,
                                            domainname = wwwDomainName,
                                            greentickvisitors = collection.greenVisitors,
                                            multivisitors = collection.multiVisitors,
                                            redtickvisitors = collection.redVisitors,
                                            pinktickvisitors = collection.pinkVisitors,
                                            visitors = collection.visitors
                                        };
                                        db.UserDailyAnalytics.Add(userDailyAnalytic);

                                        //Remove existing data for previous date is exists
                                        if (dateWiseAvailable != null && dateWiseAvailable.bluetickvisitors == 0 && dateWiseAvailable.redtickvisitors == 0 && dateWiseAvailable.greentickvisitors == 0 && dateWiseAvailable.pinktickvisitors == 0)
                                        {
                                            db.UserDailyAnalytics.Remove(dateWiseAvailable);
                                        }
                                        db.SaveChanges();

                                        CommonFunctions.SaveAuditLog(
                                            "4.0: userDailyAnalytic data saved for domain => " + wwwDomainName);
                                    }
                                    else
                                    {
                                        CommonFunctions.SaveAuditLog(
                                            "3.0:  collection data not found for domain => " + wwwDomainName);
                                    }
                                }
                                else
                                {
                                    CommonFunctions.SaveAuditLog("3.0:  data already available in database => " + wwwDomainName);
                                }
                            }
                            else
                            {
                                CommonFunctions.SaveAuditLog("3.0:  domain already available => " + wwwDomainName);
                            }
                        }
                    }
                    else
                    {
                        //2.0 proPackageDomainList data not available
                        CommonFunctions.SaveAuditLog("2.0:  proPackageDomainList data not available");
                    }
                }

                CommonFunctions.SaveAuditLog("5.0: Function return successfully!");
            }
            catch (Exception e)
            {
                CommonFunctions.SaveAuditLog("0.0: Exception => " + e.Message);
            }
        }

        public static void MonthlyAnalytic()
        {
            try
            {
                using (var db = new DBEntities())
                {
                    CommonFunctions.SaveAuditLog("1.0: Function => MonthlyAnalytic() called");

                    // send email HERE
                    var currentDate = DateTime.UtcNow;
                    var userDailyAnalytic = db.UserDailyAnalytics.Where(m => m.collectiondate.Month == currentDate.Month && m.collectiondate.Year == currentDate.Year).OrderBy(p => p.collectiondate).ToList();

                    if (userDailyAnalytic.Any())
                    {
                        CommonFunctions.SaveAuditLog("2.0: userDailyAnalytic data found. Total records: => " + userDailyAnalytic.Count);

                        var domainCollections = userDailyAnalytic.Select(m => m.domainname).Distinct().ToList();

                        if (domainCollections != null && domainCollections.Any())
                        {
                            CommonFunctions.SaveAuditLog("3.0: domainCollections data found. Total records: => " + domainCollections.Count);

                            foreach (var domainname in domainCollections.Where(m => !string.IsNullOrEmpty(m)).ToList())
                            {
                                var monthlyDomainCollection =
                                    userDailyAnalytic.Where(m => m.domainname.Equals(domainname)).ToList();

                                string emailTemplate = "<!DOCTYPE html><html><head><link href='https://fonts.googleapis.com/css?family=Roboto+Condensed:700' rel='stylesheet' type='text/css'><link href='https://fonts.googleapis.com/css?family=Roboto:100' rel='stylesheet' type='text/css'><style>table {font-family: arial, sans-serif;border-collapse: collapse;width: 100%;}td, th {border: 1px solid #dddddd;text-align: left;padding: 8px;}td:not(:first-child) {background-color: #f6fbff;}</style></head><body><p style='text-align: center; '><img src='https://digital-crumbs.com/content/blacklogo.png' /></p><h2 style='text-align: center; font-size: 40px; color: #2B547E;margin-bottom: 0px;font-family: Roboto, sans-serif;'>Monthly Analytic Report</h2><h3 style='text-align: center; font-size: 18px;font-weight: bold;color: #143f6c;margin-top: 8px;'>#domainname#</h3><h3 style='text-align: center; font-size: 16px;font-weight: inherit;'>#reportmonth#</h3><h2 style='text-align: center; font-size: 30px;color: #2B547E;margin-bottom: 15px;margin-top: 40px;font-family: Roboto, sans-serif;'>Average Daily Traffic</h2><table style='margin: 0 auto'><tr><td width='20%' style='border: none; background: none;'>Visitors<br /><br /><span style='color: #084a7e; font-size: 33px;font-weight: bold;'>#totalvisitor#</span></td><td width='20%' style='border: none;background:none'>Multi-Page V.<br /><br /><span style='color: #084a7e; font-size: 33px;font-weight: bold;'>#totalmvisitor#</span></td><td width='20%' style='border: none;background:none'>Blue Visitors <br /><br /><span style='color: #084a7e; font-size: 33px;font-weight: bold;'>#totalblue#</span></td><td width='20%' style='border: none;background:none'>Red Visitors<br /><br /><span style='color: #084a7e; font-size: 33px;font-weight: bold;'>#totalred#</span></td><td width='20%' style='border: none;background:none'>Green Visitors<br /><br /><span style='color: #084a7e; font-size: 33px;font-weight: bold;'>#totalgreen#</span></td></tr></table><h2 style='text-align: center; font-size: 30px;color: #2B547E;margin-bottom: 15px;margin-top: 40px;font-family: Roboto, sans-serif;'>Daily Traffic Breakdown</h2><table><thead><tr><th>&nbsp;</th><th style='font-weight: normal'>Visitors</th><th style='font-weight: normal'>Multi-Page <br />Visitors</th><th style='font-weight: normal'>Blue<br />Visitors</th><th style='font-weight: normal'>Red<br />Visitors</th><th style='font-weight: normal'>Green<br />Visitors</th></tr></thead><tbody>#bodypart#</tbody></table><br/><br /><br /></body></html>";
                                string bodyPart = string.Empty;
                                foreach (var singleDetails in monthlyDomainCollection)
                                {
                                    bodyPart = bodyPart + "<tr><td>" + singleDetails.collectiondate.ToString("MMM, dd yyyy") + "</td><td>" + singleDetails.visitors + "</td><td>" + singleDetails.multivisitors + "</td><td>" + singleDetails.bluetickvisitors + "</td><td>" + singleDetails.redtickvisitors + "</td><td>" + singleDetails.greentickvisitors + "</td></tr>";
                                }

                                emailTemplate = emailTemplate.Replace("#domainname#", domainname);
                                var lastdate = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                                var reportMonth = "01 " + currentDate.ToString("MMM") + " - " + lastdate.ToString() +
                                                  " " + currentDate.ToString("MMM") + " " + currentDate.Year;
                                emailTemplate = emailTemplate.Replace("#reportmonth#", reportMonth);

                                var totalVisitors = monthlyDomainCollection.Sum(m => m.visitors);
                                var totalMVisitors = monthlyDomainCollection.Sum(m => m.multivisitors);
                                var totalRed = monthlyDomainCollection.Sum(m => m.redtickvisitors);
                                var totalGreen = monthlyDomainCollection.Sum(m => m.greentickvisitors);
                                var totalBlue = monthlyDomainCollection.Sum(m => m.bluetickvisitors);

                                emailTemplate = emailTemplate.Replace("#totalvisitor#", Convert.ToString(totalVisitors));
                                emailTemplate = emailTemplate.Replace("#totalmvisitor#", Convert.ToString(totalMVisitors));
                                emailTemplate = emailTemplate.Replace("#totalblue#", Convert.ToString(totalBlue));
                                emailTemplate = emailTemplate.Replace("#totalred#", Convert.ToString(totalRed));
                                emailTemplate = emailTemplate.Replace("#totalgreen#", Convert.ToString(totalGreen));
                                emailTemplate = emailTemplate.Replace("#bodypart#", bodyPart);

                                string wwwDomainName;
                                string wwwWithoutDomainName;
                                if (domainname.Contains("www."))
                                {
                                    wwwDomainName = domainname;
                                    wwwWithoutDomainName = domainname.Replace("www.", string.Empty);
                                }
                                else
                                {
                                    wwwDomainName = "www." + domainname;
                                    wwwWithoutDomainName = domainname;
                                }

                                var prepackageUsers = db.propackageusers.Where(m => m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))
                                    .ToList();

                                foreach (var userDetail in prepackageUsers)
                                {
                                    var agreementDetails = db.ProAgreementCollections.FirstOrDefault(m => m.email.Equals(userDetail.email));
                                    if (agreementDetails != null)
                                    {
                                        //Send Email Code   
                                        MailMessage msg = new MailMessage();
                                        msg.To.Add(new MailAddress(userDetail.email, "Dear User"));
                                        msg.From = new MailAddress("info@digital-crumbs.com", "Digital-Crumbs");
                                        msg.Subject = "Monthly Analytic Report For " + domainname;
                                        msg.Body = emailTemplate;
                                        msg.IsBodyHtml = true;

                                        SmtpClient client = new SmtpClient
                                        {
                                            UseDefaultCredentials = false,
                                            Credentials = new NetworkCredential("info@digital-crumbs.com", "Admin@987"),
                                            Port = 587, // You can use Port 25 if 587 is blocked (mine is!)
                                            Host = "smtp.ionos.co.uk",
                                            DeliveryMethod = SmtpDeliveryMethod.Network,
                                            EnableSsl = true
                                        };
                                        client.Send(msg);

                                        CommonFunctions.SaveAuditLog("4.0 Mail send to => " + userDetail.email);
                                    }
                                    else
                                    {
                                        CommonFunctions.SaveAuditLog("5.0 Mail not send to => " + userDetail.email);
                                    }
                                }
                            }
                        }
                        else
                        {
                            CommonFunctions.SaveAuditLog("3.0:  domainCollections data not available");
                        }
                    }
                    else
                    {
                        CommonFunctions.SaveAuditLog("2.0:  proPackageDomainList data not available");
                    }
                }

                CommonFunctions.SaveAuditLog("5.0: Function return successfully!");
            }
            catch (Exception e)
            {
                CommonFunctions.SaveAuditLog("0.0: Exception => " + e.Message);
            }
        }

        public static void TagVisitors()
        {
            var reportScheduledDataVm = new List<TagReportData>();
            try
            {
                using (var db = new DBEntities())
                {
                    CommonFunctions.SaveAuditLog("1.0: Function => TagVisitors() called");
                    var currentDate = DateTime.UtcNow.AddDays(-1);
                   // var currentDate = new DateTime(2021, 04, 13);
                    var proPackageDomainList = db.propackageusers.Select(m => m.domainname).Distinct().ToList();
                    var domainCollection = new List<string>();
                    if (proPackageDomainList != null && proPackageDomainList.Any())
                    {
                        var existingRecords = db.TagReportDatas.Where(m => DbFunctions.TruncateTime(m.createdDate) == currentDate.Date).ToList();
                        if (existingRecords.Any())
                        {
                            db.TagReportDatas.RemoveRange(existingRecords);
                            db.SaveChanges();
                        }

                        //2.0 proPackageDomainList data found
                        CommonFunctions.SaveAuditLog("2.0:  proPackageDomainList data found. Total Data Count == " + proPackageDomainList.Count);
                        foreach (var domainName in proPackageDomainList.Where(m => !string.IsNullOrEmpty(m)).ToList())
                        {
                            string wwwDomainName;
                            string wwwWithoutDomainName;
                            if (domainName.Contains("www."))
                            {
                                wwwDomainName = domainName;
                                wwwWithoutDomainName = domainName.Replace("www.", string.Empty);
                            }
                            else
                            {
                                wwwDomainName = "www." + domainName;
                                wwwWithoutDomainName = domainName;
                            }

                            var blueTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "blue" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();
                            var redTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "red" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();
                            var pinkTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "pink" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();

                            var greenTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "green" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();

                            if (blueTagCollection.Any())
                            {
                                var greenTagVisitor = 0;
                                var multiPageVisitor = 0;
                                var allPageVisitor = 0;
                                var reportCollections = CommonFunctions.GetReportData(wwwDomainName, wwwWithoutDomainName, currentDate);
                                if (reportCollections != null && reportCollections.userCollection.Any())
                                {
                                    foreach (var blueTag in blueTagCollection)
                                    {
                                        greenTagVisitor = 0;
                                        multiPageVisitor = 0;
                                        allPageVisitor = 0;
                                        foreach (var userData in reportCollections.userCollection)
                                        {
                                            var isFound = false;
                                            if (userData.userListing.Any())
                                            {
                                                foreach (var visitData in userData.userListing)
                                                {
                                                    if (!isFound)
                                                    {
                                                        var splitURL = visitData.url.Split('/');
                                                        var totallength = splitURL.Length;
                                                        var urlToDisplay = "Home Page";
                                                        if (totallength == 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                        }
                                                        else if (totallength > 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                            else
                                                            {
                                                                urlToDisplay = splitURL[totallength - 2];
                                                            }
                                                        }

                                                        if (urlToDisplay.Length > 39)
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay);
                                                        }

                                                        var urlTitle = visitData.title;
                                                        var searchtext1 = '?' + blueTag.searchtext;
                                                        CommonFunctions.SaveAuditLog("8.0:  proPackageDomainList data found. == " + searchtext1);
                                                        if (urlToDisplay.Contains(searchtext1) || urlTitle.Contains(searchtext1))
                                                        {
                                                            isFound = true;
                                                            if (greenTagCollection.Any())
                                                            {
                                                                var isGreenTagFound = false;
                                                                foreach (var greenTag in greenTagCollection)
                                                                {
                                                                    if (!isGreenTagFound)
                                                                    {
                                                                        if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                        {
                                                                            isGreenTagFound = true;
                                                                            greenTagVisitor = greenTagVisitor + 1;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                if (isFound)
                                                {
                                                    allPageVisitor = allPageVisitor + 1;
                                                    if (!userData.isSinglePageUser)
                                                    {
                                                        multiPageVisitor = multiPageVisitor + 1;
                                                    }
                                                }
                                            }
                                        }

                                        reportScheduledDataVm.Add(new TagReportData
                                        {
                                            createdDate = currentDate,
                                            tagName = blueTag.searchtext,
                                            greenTagVisitor = greenTagVisitor,
                                            multipageVisitor = multiPageVisitor,
                                            allVisitor = allPageVisitor,
                                            wwwDomainName = wwwDomainName,
                                            wwwWithoutDomainName = wwwWithoutDomainName,
                                            websiteurl = wwwDomainName,
                                        });
                                    }
                                }
                            }
                            if (redTagCollection.Any())
                            {
                                var greenTagVisitor = 0;
                                var multiPageVisitor = 0;
                                var allPageVisitor = 0;
                                var reportCollections = CommonFunctions.GetReportData(wwwDomainName, wwwWithoutDomainName, currentDate);
                                if (reportCollections != null && reportCollections.userCollection.Any())
                                {
                                    foreach (var redTag in redTagCollection)
                                    {
                                        greenTagVisitor = 0;
                                        multiPageVisitor = 0;
                                        allPageVisitor = 0;
                                        foreach (var userData in reportCollections.userCollection)
                                        {
                                            var isFound = false;
                                            if (userData.userListing.Any())
                                            {
                                                foreach (var visitData in userData.userListing)
                                                {
                                                    if (!isFound)
                                                    {
                                                        var splitURL = visitData.url.Split('/');
                                                        var totallength = splitURL.Length;
                                                        var urlToDisplay = "Home Page";
                                                        if (totallength == 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                        }
                                                        else if (totallength > 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                            else
                                                            {
                                                                urlToDisplay = splitURL[totallength - 2];
                                                            }
                                                        }

                                                        if (urlToDisplay.Length > 39)
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay);
                                                        }

                                                        var urlTitle = visitData.title;
                                                        if (urlToDisplay.Contains(redTag.searchtext) || urlTitle.Contains(redTag.searchtext))
                                                        {
                                                            isFound = true;
                                                            if (greenTagCollection.Any())
                                                            {
                                                                var isGreenTagFound = false;
                                                                foreach (var greenTag in greenTagCollection)
                                                                {
                                                                    if (!isGreenTagFound)
                                                                    {
                                                                        if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                        {
                                                                            isGreenTagFound = true;
                                                                            greenTagVisitor = greenTagVisitor + 1;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                if (isFound)
                                                {
                                                    allPageVisitor = allPageVisitor + 1;
                                                    if (!userData.isSinglePageUser)
                                                    {
                                                        multiPageVisitor = multiPageVisitor + 1;
                                                    }
                                                }
                                            }
                                        }

                                        reportScheduledDataVm.Add(new TagReportData
                                        {
                                            createdDate = currentDate,
                                            tagName = redTag.searchtext,
                                            greenTagVisitor = greenTagVisitor,
                                            multipageVisitor = multiPageVisitor,
                                            allVisitor = allPageVisitor,
                                            wwwDomainName = wwwDomainName,
                                            wwwWithoutDomainName = wwwWithoutDomainName,
                                            websiteurl = wwwDomainName,
                                        });
                                    }
                                }
                            }
                            if (pinkTagCollection.Any())
                            {
                                var greenTagVisitor = 0;
                                var multiPageVisitor = 0;
                                var allPageVisitor = 0;
                                var reportCollections = CommonFunctions.GetReportData(wwwDomainName, wwwWithoutDomainName, currentDate);
                                if (reportCollections != null && reportCollections.userCollection.Any())
                                {
                                    foreach (var pinkTag in pinkTagCollection)
                                    {
                                        greenTagVisitor = 0;
                                        multiPageVisitor = 0;
                                        allPageVisitor = 0;
                                        foreach (var userData in reportCollections.userCollection)
                                        {
                                            var isFound = false;
                                            if (userData.userListing.Any())
                                            {
                                                foreach (var visitData in userData.userListing)
                                                {
                                                    if (!isFound)
                                                    {
                                                        var splitURL = visitData.url.Split('/');
                                                        var totallength = splitURL.Length;
                                                        var urlToDisplay = "Home Page";
                                                        if (totallength == 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                        }
                                                        else if (totallength > 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                            else
                                                            {
                                                                urlToDisplay = splitURL[totallength - 2];
                                                            }
                                                        }

                                                        if (urlToDisplay.Length > 39)
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay);
                                                        }

                                                        var urlTitle = visitData.title;
                                                        if (urlToDisplay.Contains(pinkTag.searchtext) || urlTitle.Contains(pinkTag.searchtext))
                                                        {
                                                            isFound = true;
                                                            if (greenTagCollection.Any())
                                                            {
                                                                var isGreenTagFound = false;
                                                                foreach (var greenTag in greenTagCollection)
                                                                {
                                                                    if (!isGreenTagFound)
                                                                    {
                                                                        if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                        {
                                                                            isGreenTagFound = true;
                                                                            greenTagVisitor = greenTagVisitor + 1;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                if (isFound)
                                                {
                                                    allPageVisitor = allPageVisitor + 1;
                                                    if (!userData.isSinglePageUser)
                                                    {
                                                        multiPageVisitor = multiPageVisitor + 1;
                                                    }
                                                }
                                            }
                                        }

                                        reportScheduledDataVm.Add(new TagReportData
                                        {
                                            createdDate = currentDate,
                                            tagName = pinkTag.searchtext,
                                            greenTagVisitor = greenTagVisitor,
                                            multipageVisitor = multiPageVisitor,
                                            allVisitor = allPageVisitor,
                                            wwwDomainName = wwwDomainName,
                                            wwwWithoutDomainName = wwwWithoutDomainName,
                                            websiteurl = wwwDomainName,
                                        });
                                    }
                                }
                            }

                            if (greenTagCollection.Any())
                            {
                                var greenTagVisitor = 0;
                                var multiPageVisitor = 0;
                                var allPageVisitor = 0;
                                var reportCollections = CommonFunctions.GetReportData(wwwDomainName, wwwWithoutDomainName, currentDate);
                                if (reportCollections != null && reportCollections.userCollection.Any())
                                {
                                    foreach (var greenTag in greenTagCollection)
                                    {
                                        greenTagVisitor = 0;
                                        multiPageVisitor = 0;
                                        allPageVisitor = 0;
                                        foreach (var userData in reportCollections.userCollection)
                                        {
                                            var isFound = false;
                                            if (userData.userListing.Any())
                                            {
                                                foreach (var visitData in userData.userListing)
                                                {
                                                    if (!isFound)
                                                    {
                                                        var splitURL = visitData.url.Split('/');
                                                        var totallength = splitURL.Length;
                                                        var urlToDisplay = "Home Page";
                                                        if (totallength == 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                        }
                                                        else if (totallength > 4)
                                                        {
                                                            if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                            {
                                                                urlToDisplay = splitURL[totallength - 1];
                                                            }
                                                            else
                                                            {
                                                                urlToDisplay = splitURL[totallength - 2];
                                                            }
                                                        }

                                                        if (urlToDisplay.Length > 39)
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = string.Concat("/", urlToDisplay);
                                                        }

                                                        var urlTitle = visitData.title;
                                                        if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                        {
                                                            isFound = true;
                                                            if (greenTagCollection.Any())
                                                            {
                                                                var isGreenTagFound = false;
                                                                
                                                                    if (!isGreenTagFound)
                                                                    {
                                                                        if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                        {
                                                                            isGreenTagFound = true;
                                                                            greenTagVisitor = greenTagVisitor + 1;
                                                                        }
                                                                    
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                if (isFound)
                                                {
                                                    allPageVisitor = allPageVisitor + 1;
                                                    if (!userData.isSinglePageUser)
                                                    {
                                                        multiPageVisitor = multiPageVisitor + 1;
                                                    }
                                                }
                                            }
                                        }

                                        reportScheduledDataVm.Add(new TagReportData
                                        {
                                            createdDate = currentDate,
                                            tagName = greenTag.searchtext,
                                            greenTagVisitor = greenTagVisitor,
                                            multipageVisitor = multiPageVisitor,
                                            allVisitor = allPageVisitor,
                                            wwwDomainName = wwwDomainName,
                                            wwwWithoutDomainName = wwwWithoutDomainName,
                                            websiteurl = wwwDomainName,
                                        });
                                    }
                                }
                            }

                        }
                    }

                    foreach (var dbData in reportScheduledDataVm)
                    {
                        db.TagReportDatas.Add(dbData);
                    }
                    db.SaveChanges();
                }
                CommonFunctions.SaveAuditLog("5.0: Function return successfully!");
            }
            catch (Exception e)
            {
                CommonFunctions.SaveAuditLog("0.0: Exception => " + e.Message);
            }
        }

        public static void SourceReportData()
        {
            var reportScheduledDataVm = new List<TagReportData>();
            try
            {
                using (var db = new DBEntities())
                {
                    CommonFunctions.SaveAuditLog("1.0: Function => SourceReportData() called");
                    var currentDate = DateTime.UtcNow.AddDays(-1);
                    //var currentDate = new DateTime(2019, 08, 09);
                    var proPackageDomainList = db.propackageusers.Select(m => m.domainname).Distinct().ToList();
                    var domainCollection = new List<string>();
                    CommonFunctions.SaveAuditLog("2.0: Function => Created Date : " + currentDate);
                    if (proPackageDomainList != null && proPackageDomainList.Any())
                    {
                        var existingRecords = db.sourceReportDatas.Where(m => DbFunctions.TruncateTime(m.createdDate) == currentDate.Date).ToList();
                        if (existingRecords.Any())
                        {
                            db.sourceReportDatas.RemoveRange(existingRecords);
                            db.SaveChanges();
                        }

                        //2.0 proPackageDomainList data found
                        CommonFunctions.SaveAuditLog("3.0:  proPackageDomainList data found. Total Data Count == " + proPackageDomainList.Count);
                        foreach (var domainName in proPackageDomainList.Where(m => !string.IsNullOrEmpty(m)).ToList())
                        {
                            CommonFunctions.SaveAuditLog("4.0: Domain name == " + domainName);
                            string wwwDomainName;
                            string wwwWithoutDomainName;
                            if (domainName.Contains("www."))
                            {
                                wwwDomainName = domainName;
                                wwwWithoutDomainName = domainName.Replace("www.", string.Empty);
                            }
                            else
                            {
                                wwwDomainName = "www." + domainName;
                                wwwWithoutDomainName = domainName;
                            }

                            var greenTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "green" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();
                            var reportCollections = CommonFunctions.GetReportData(wwwDomainName, wwwWithoutDomainName, currentDate);
                            if (reportCollections == null)
                            {
                                CommonFunctions.SaveAuditLog("5.0: Domain based data is null");
                            }

                            if (reportCollections != null && reportCollections.userCollection.Any())
                            {
                                CommonFunctions.SaveAuditLog("5.0: Domain based data cont " + reportCollections.userCollection.Count);
                            }
                            var srourceCollection = new List<string>();
                            if (reportCollections != null && reportCollections.userCollection.Any())
                            {
                                foreach (var userCollection in reportCollections.userCollection)
                                {
                                    CommonFunctions.SaveAuditLog("6.0 User Collection source" + userCollection.source);

                                    if (!string.IsNullOrEmpty(userCollection.source))
                                    {
                                        Uri myUri = new Uri(userCollection.source);
                                        var host = myUri.Host;
                                        if (!string.IsNullOrEmpty(host))
                                        {
                                            var hostArray = host.Split('.');
                                            if (hostArray[0].Length == 1 || hostArray[0] == "www")
                                            {
                                                if (hostArray.Length == 2)
                                                {
                                                    var finalURL = string.Join(".", hostArray);
                                                    srourceCollection.Add(finalURL);
                                                }
                                                else
                                                {
                                                    var newHostArray = hostArray.Skip(1).ToArray();
                                                    var finalURL = string.Join(".", newHostArray);
                                                    srourceCollection.Add(finalURL);
                                                }
                                            }
                                            else
                                            {
                                                var finalURL = string.Join(".", hostArray);
                                                srourceCollection.Add(finalURL);
                                            }
                                        }
                                    }
                                }

                                if (srourceCollection.Any())
                                {
                                    var disctinctSrourceCollection = srourceCollection.Distinct().ToList();
                                    foreach (var source in disctinctSrourceCollection)
                                    {
                                        CommonFunctions.SaveAuditLog("7.0: Source Loop == " + source);

                                        var greenTagVisitor = 0;
                                        var multiPageVisitor = 0;
                                        var allPageVisitor = 0;
                                        foreach (var userCollection in reportCollections.userCollection)
                                        {
                                            var isFound = false;
                                            if (!string.IsNullOrEmpty(userCollection.source))
                                            {
                                                if (userCollection.source.Contains(source))
                                                {
                                                    CommonFunctions.SaveAuditLog("8.0: Source Found in UserDetails");
                                                    isFound = true;
                                                    allPageVisitor = allPageVisitor + 1;
                                                    if (!userCollection.isSinglePageUser)
                                                    {
                                                        multiPageVisitor = multiPageVisitor + 1;
                                                    }
                                                }
                                            }

                                            if (userCollection.userListing.Any() && isFound)
                                            {
                                                if (greenTagCollection.Any())
                                                {
                                                    var isGreenTagFound = false;
                                                    foreach (var greenTag in greenTagCollection)
                                                    {
                                                        if (!isGreenTagFound)
                                                        {
                                                            foreach (var userListing in userCollection.userListing)
                                                            {
                                                                if (!isGreenTagFound)
                                                                {
                                                                    if (userListing.url.Contains(greenTag.searchtext) || userListing.title.Contains(greenTag.searchtext))
                                                                    {
                                                                        CommonFunctions.SaveAuditLog("9.0: Source Found Considered As Green Tag");
                                                                        isGreenTagFound = true;
                                                                        greenTagVisitor = greenTagVisitor + 1;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        var sourceReportDataVM = new sourceReportData
                                        {
                                            allVisitor = allPageVisitor,
                                            multipageVisitor = multiPageVisitor,
                                            greenTagVisitor = greenTagVisitor,
                                            createdDate = currentDate,
                                            sourceName = source,
                                            wwwDomainName = wwwDomainName,
                                            wwwWithoutDomainName = wwwWithoutDomainName
                                        };

                                        db.sourceReportDatas.Add(sourceReportDataVM);

                                        CommonFunctions.SaveAuditLog("10.0: Entered Data == so" + sourceReportDataVM.sourceName);
                                    }
                                    db.SaveChanges();
                                }
                                else
                                {
                                    CommonFunctions.SaveAuditLog("5.0: No Source Found...");
                                }
                            }
                        }
                    }
                }
                CommonFunctions.SaveAuditLog("11.0: Function return successfully!");
            }
            catch (Exception e)
            {
                CommonFunctions.SaveAuditLog("0.0: Exception => " + e.Message);
            }
        }

        public static void DailyAnalyticUpdated()
        {
            try
            {
                using (var db = new DBEntities())
                {
                    //1.0 function called
                    CommonFunctions.SaveAuditLog("1.0: Function => DailyAnalytic() called");

                    var currentDate = DateTime.UtcNow.AddDays(-1);
                    //var currentDate = new DateTime(DateTime.Now.Year, 10, 01);
                    var proPackageDomainList = db.propackageusers.Select(m => m.domainname).Distinct().ToList();
                    var domainCollection = new List<string>();
                    if (proPackageDomainList != null && proPackageDomainList.Any())
                    {
                        //2.0 proPackageDomainList data found
                        CommonFunctions.SaveAuditLog("2.0:  proPackageDomainList data found. Total Data Count == " + proPackageDomainList.Count);

                        foreach (var domainName in proPackageDomainList.Where(m => !string.IsNullOrEmpty(m)).ToList())
                        {
                            string wwwDomainName;
                            string wwwWithoutDomainName;
                            if (domainName.Contains("www."))
                            {
                                wwwDomainName = domainName;
                                wwwWithoutDomainName = domainName.Replace("www.", string.Empty);
                            }
                            else
                            {
                                wwwDomainName = "www." + domainName;
                                wwwWithoutDomainName = domainName;
                            }

                            var domainAvailable = domainCollection.FirstOrDefault(m =>
                                m.Equals(wwwDomainName) || m.Equals(wwwWithoutDomainName));

                            if (domainAvailable == null)
                            {
                                var dateWiseAvailable = db.UserDailyAnalytics.FirstOrDefault(m => DbFunctions.TruncateTime(m.collectiondate) == currentDate.Date
                                    && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName)));

                                if (dateWiseAvailable == null || (dateWiseAvailable != null && dateWiseAvailable.bluetickvisitors == 0 && dateWiseAvailable.redtickvisitors == 0 && dateWiseAvailable.greentickvisitors == 0))
                                {
                                    var collection = CommonFunctions.GetDailyCount(domainName, currentDate);
                                    if (collection != null)
                                    {
                                        CommonFunctions.SaveAuditLog(
                                            "3.0:  collection data for doamin => " + domainName);

                                        var userDailyAnalytic = new UserDailyAnalytic
                                        {
                                            bluetickvisitors = collection.blueVisitors,
                                            collectiondate = currentDate,
                                            domainname = wwwDomainName,
                                            greentickvisitors = collection.greenVisitors,
                                            multivisitors = collection.multiVisitors,
                                            redtickvisitors = collection.redVisitors,
                                            pinktickvisitors = collection.pinkVisitors,
                                            visitors = collection.visitors
                                        };
                                        db.UserDailyAnalytics.Add(userDailyAnalytic);

                                        //Remove existing data for previous date is exists
                                        if (dateWiseAvailable != null && dateWiseAvailable.bluetickvisitors == 0 && dateWiseAvailable.redtickvisitors == 0 && dateWiseAvailable.greentickvisitors == 0 && dateWiseAvailable.pinktickvisitors == 0)
                                        {
                                            db.UserDailyAnalytics.Remove(dateWiseAvailable);
                                        }
                                        db.SaveChanges();

                                        CommonFunctions.SaveAuditLog(
                                            "4.0: userDailyAnalytic data saved for domain => " + wwwDomainName);
                                    }
                                    else
                                    {
                                        CommonFunctions.SaveAuditLog(
                                            "3.0:  collection data not found for domain => " + wwwDomainName);
                                    }
                                }
                                else
                                {
                                    CommonFunctions.SaveAuditLog("3.0:  data already available in database => " + wwwDomainName);
                                }
                            }
                            else
                            {
                                CommonFunctions.SaveAuditLog("3.0:  domain already available => " + wwwDomainName);
                            }
                        }
                    }
                    else
                    {
                        //2.0 proPackageDomainList data not available
                        CommonFunctions.SaveAuditLog("2.0:  proPackageDomainList data not available");
                    }
                }

                CommonFunctions.SaveAuditLog("5.0: Function return successfully!");
            }
            catch (Exception e)
            {
                CommonFunctions.SaveAuditLog("0.0: Exception => " + e.Message);
            }
        }

        public static void TagVisitorsUpdate()
        {
            var reportScheduledDataVm = new List<TagReportData>();
            var databaseCollection = new List<TagSchedulerVm>();
            var currentDate = DateTime.UtcNow.AddDays(-1);
            //var currentDate = new DateTime(2019, 08, 09);

            CommonFunctions.SaveAuditLog("0.0: Call Scheduler TagVisitorsUpdate() => " + currentDate.ToString("ddMMyyyy"));
            try
            {
                var reportDataCollection = CommonFunctions.GetReportDataUpdated(currentDate);

                using (var db = new DBEntities())
                {
                    var proPackageDomainList = db.propackageusers.Select(m => m.domainname).Distinct().ToList();

                    if (proPackageDomainList.Any())
                    {
                        foreach (var proPackageDomain in proPackageDomainList)
                        {
                            string wwwDomainName;
                            string wwwWithoutDomainName;
                            if (proPackageDomain.Contains("www."))
                            {
                                wwwDomainName = proPackageDomain;
                                wwwWithoutDomainName = proPackageDomain.Replace("www.", string.Empty);
                            }
                            else
                            {
                                wwwDomainName = "www." + proPackageDomain;
                                wwwWithoutDomainName = proPackageDomain;
                            }

                            var currentCollection = new TagSchedulerVm
                            {
                                blueTagCollection = new List<searchdomainMaster>(),
                                redTagCollection = new List<searchdomainMaster>(),
                                pinkTagCollection = new List<searchdomainMaster>(),
                                
                                greenTagCollection = new List<searchdomainMaster>(),
                                wwwDomainName = wwwDomainName,
                                wwwWithoutDomainName = wwwWithoutDomainName
                            };

                            //Collection of blue tag data
                            var blueTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "blue" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();


                            if (blueTagCollection.Any())
                            {
                                currentCollection.blueTagCollection = blueTagCollection;
                            }

                            var redTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "red" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();
                            if (redTagCollection.Any())
                            {
                                currentCollection.redTagCollection = redTagCollection;
                            }
                            var pinkTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "pink" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();
                            if (pinkTagCollection.Any())
                            {
                                currentCollection.pinkTagCollection = pinkTagCollection;
                            }

                            //Collection of green tag data
                            var greenTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "green" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();
                            if (greenTagCollection.Any())
                            {
                                currentCollection.greenTagCollection = greenTagCollection;
                            }

                            databaseCollection.Add(currentCollection);
                        }
                    }
                }

                if (databaseCollection.Any())
                {
                    var newDataVm = new List<TagReportData>();
                    var removeDataVm = new List<TagReportData>();

                    foreach (var dbData in databaseCollection)
                    {
                        if (dbData.blueTagCollection.Any())
                        {
                            var greenTagVisitor = 0;
                            var multiPageVisitor = 0;
                            var allPageVisitor = 0;

                            foreach (var blueTag in dbData.blueTagCollection)
                            {
                                greenTagVisitor = 0;
                                multiPageVisitor = 0;
                                allPageVisitor = 0;

                                if (reportDataCollection != null && reportDataCollection.userCollection.Any())
                                {
                                    var exactReportDataCollection = reportDataCollection.userCollection.Where(m => (m.domainName.Equals(dbData.wwwDomainName) || m.domainName.Equals((dbData.wwwWithoutDomainName)))).ToList();
                                    foreach (var userData in exactReportDataCollection)
                                    {
                                        var isFound = false;
                                        if (userData.userListing.Any())
                                        {
                                            foreach (var visitData in userData.userListing)
                                            {
                                                if (!isFound)
                                                {
                                                    var splitURL = visitData.url.Split('/');
                                                    var totallength = splitURL.Length;
                                                    var urlToDisplay = "Home Page";
                                                    if (totallength == 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                    }
                                                    else if (totallength > 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = splitURL[totallength - 2];
                                                        }
                                                    }

                                                    if (urlToDisplay.Length > 39)
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                    }
                                                    else
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                                    }

                                                    var urlTitle = visitData.title;
                                                    var searchtext1 = '?' + blueTag.searchtext;
                                                    if (urlToDisplay.Contains(searchtext1) || urlTitle.Contains(searchtext1))
                                                    {
                                                        isFound = true;

                                                        if (dbData.greenTagCollection.Any())
                                                        {
                                                            foreach (var greenTag in dbData.greenTagCollection)
                                                            {
                                                                if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                {
                                                                    greenTagVisitor = greenTagVisitor + 1;
                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    }
                                                }
                                            }

                                            if (isFound)
                                            {
                                                allPageVisitor = allPageVisitor + 1;
                                                if (!userData.isSinglePageUser)
                                                {
                                                    multiPageVisitor = multiPageVisitor + 1;
                                                }
                                            }
                                        }
                                    }

                                    newDataVm.Add(new TagReportData
                                    {
                                        createdDate = currentDate,
                                        tagName = blueTag.searchtext,
                                        greenTagVisitor = greenTagVisitor,
                                        multipageVisitor = multiPageVisitor,
                                        allVisitor = allPageVisitor,
                                        wwwDomainName = dbData.wwwDomainName,
                                        wwwWithoutDomainName = dbData.wwwWithoutDomainName,
                                        websiteurl = dbData.wwwDomainName,
                                    });
                                }
                            }
                        }
                        if (dbData.redTagCollection.Any())
                        {
                            var greenTagVisitor = 0;
                            var multiPageVisitor = 0;
                            var allPageVisitor = 0;

                            foreach (var redTag in dbData.redTagCollection)
                            {
                                greenTagVisitor = 0;
                                multiPageVisitor = 0;
                                allPageVisitor = 0;

                                if (reportDataCollection != null && reportDataCollection.userCollection.Any())
                                {
                                    var exactReportDataCollection = reportDataCollection.userCollection.Where(m => (m.domainName.Equals(dbData.wwwDomainName) || m.domainName.Equals((dbData.wwwWithoutDomainName)))).ToList();
                                    foreach (var userData in exactReportDataCollection)
                                    {
                                        var isFound = false;
                                        if (userData.userListing.Any())
                                        {
                                            foreach (var visitData in userData.userListing)
                                            {
                                                if (!isFound)
                                                {
                                                    var splitURL = visitData.url.Split('/');
                                                    var totallength = splitURL.Length;
                                                    var urlToDisplay = "Home Page";
                                                    if (totallength == 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                    }
                                                    else if (totallength > 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = splitURL[totallength - 2];
                                                        }
                                                    }

                                                    if (urlToDisplay.Length > 39)
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                    }
                                                    else
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                                    }

                                                    var urlTitle = visitData.title;
                                                    if (urlToDisplay.Contains(redTag.searchtext) || urlTitle.Contains(redTag.searchtext))
                                                    {
                                                        isFound = true;

                                                        if (dbData.greenTagCollection.Any())
                                                        {
                                                            foreach (var greenTag in dbData.greenTagCollection)
                                                            {
                                                                if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                {
                                                                    greenTagVisitor = greenTagVisitor + 1;
                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    }
                                                }
                                            }

                                            if (isFound)
                                            {
                                                allPageVisitor = allPageVisitor + 1;
                                                if (!userData.isSinglePageUser)
                                                {
                                                    multiPageVisitor = multiPageVisitor + 1;
                                                }
                                            }
                                        }
                                    }

                                    newDataVm.Add(new TagReportData
                                    {
                                        createdDate = currentDate,
                                        tagName = redTag.searchtext,
                                        greenTagVisitor = greenTagVisitor,
                                        multipageVisitor = multiPageVisitor,
                                        allVisitor = allPageVisitor,
                                        wwwDomainName = dbData.wwwDomainName,
                                        wwwWithoutDomainName = dbData.wwwWithoutDomainName,
                                        websiteurl = dbData.wwwDomainName,
                                    });
                                }
                            }
                        }
                        if (dbData.pinkTagCollection.Any())
                        {
                            var greenTagVisitor = 0;
                            var multiPageVisitor = 0;
                            var allPageVisitor = 0;

                            foreach (var pinkTag in dbData.pinkTagCollection)
                            {
                                greenTagVisitor = 0;
                                multiPageVisitor = 0;
                                allPageVisitor = 0;

                                if (reportDataCollection != null && reportDataCollection.userCollection.Any())
                                {
                                    var exactReportDataCollection = reportDataCollection.userCollection.Where(m => (m.domainName.Equals(dbData.wwwDomainName) || m.domainName.Equals((dbData.wwwWithoutDomainName)))).ToList();
                                    foreach (var userData in exactReportDataCollection)
                                    {
                                        var isFound = false;
                                        if (userData.userListing.Any())
                                        {
                                            foreach (var visitData in userData.userListing)
                                            {
                                                if (!isFound)
                                                {
                                                    var splitURL = visitData.url.Split('/');
                                                    var totallength = splitURL.Length;
                                                    var urlToDisplay = "Home Page";
                                                    if (totallength == 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                    }
                                                    else if (totallength > 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = splitURL[totallength - 2];
                                                        }
                                                    }

                                                    if (urlToDisplay.Length > 39)
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                    }
                                                    else
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                                    }

                                                    var urlTitle = visitData.title;
                                                    if (urlToDisplay.Contains(pinkTag.searchtext) || urlTitle.Contains(pinkTag.searchtext))
                                                    {
                                                        isFound = true;

                                                        if (dbData.greenTagCollection.Any())
                                                        {
                                                            foreach (var greenTag in dbData.greenTagCollection)
                                                            {
                                                                if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                {
                                                                    greenTagVisitor = greenTagVisitor + 1;
                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    }
                                                }
                                            }

                                            if (isFound)
                                            {
                                                allPageVisitor = allPageVisitor + 1;
                                                if (!userData.isSinglePageUser)
                                                {
                                                    multiPageVisitor = multiPageVisitor + 1;
                                                }
                                            }
                                        }
                                    }

                                    newDataVm.Add(new TagReportData
                                    {
                                        createdDate = currentDate,
                                        tagName = pinkTag.searchtext,
                                        greenTagVisitor = greenTagVisitor,
                                        multipageVisitor = multiPageVisitor,
                                        allVisitor = allPageVisitor,
                                        wwwDomainName = dbData.wwwDomainName,
                                        wwwWithoutDomainName = dbData.wwwWithoutDomainName,
                                        websiteurl = dbData.wwwDomainName,
                                    });
                                }
                            }
                        }
                        if (dbData.greenTagCollection.Any())
                        {
                            var greenTagVisitor = 0;
                            var multiPageVisitor = 0;
                            var allPageVisitor = 0;

                            foreach (var greenTag in dbData.greenTagCollection)
                            {
                                greenTagVisitor = 0;
                                multiPageVisitor = 0;
                                allPageVisitor = 0;

                                if (reportDataCollection != null && reportDataCollection.userCollection.Any())
                                {
                                    var exactReportDataCollection = reportDataCollection.userCollection.Where(m => (m.domainName.Equals(dbData.wwwDomainName) || m.domainName.Equals((dbData.wwwWithoutDomainName)))).ToList();
                                    foreach (var userData in exactReportDataCollection)
                                    {
                                        var isFound = false;
                                        if (userData.userListing.Any())
                                        {
                                            foreach (var visitData in userData.userListing)
                                            {
                                                if (!isFound)
                                                {
                                                    var splitURL = visitData.url.Split('/');
                                                    var totallength = splitURL.Length;
                                                    var urlToDisplay = "Home Page";
                                                    if (totallength == 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                    }
                                                    else if (totallength > 4)
                                                    {
                                                        if (!string.IsNullOrEmpty(splitURL[totallength - 1]))
                                                        {
                                                            urlToDisplay = splitURL[totallength - 1];
                                                        }
                                                        else
                                                        {
                                                            urlToDisplay = splitURL[totallength - 2];
                                                        }
                                                    }

                                                    if (urlToDisplay.Length > 39)
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 40));
                                                    }
                                                    else
                                                    {
                                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                                    }

                                                    var urlTitle = visitData.title;
                                                    if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                    {
                                                        isFound = true;

                                                        if (dbData.greenTagCollection.Any())
                                                        {
                                                            
                                                                if (urlToDisplay.Contains(greenTag.searchtext) || urlTitle.Contains(greenTag.searchtext))
                                                                {
                                                                    greenTagVisitor = greenTagVisitor + 1;
                                                                    break;
                                                                }
                                                            
                                                        }

                                                        break;
                                                    }
                                                }
                                            }

                                            if (isFound)
                                            {
                                                allPageVisitor = allPageVisitor + 1;
                                                if (!userData.isSinglePageUser)
                                                {
                                                    multiPageVisitor = multiPageVisitor + 1;
                                                }
                                            }
                                        }
                                    }

                                    newDataVm.Add(new TagReportData
                                    {
                                        createdDate = currentDate,
                                        tagName = greenTag.searchtext,
                                        greenTagVisitor = greenTagVisitor,
                                        multipageVisitor = multiPageVisitor,
                                        allVisitor = allPageVisitor,
                                        wwwDomainName = dbData.wwwDomainName,
                                        wwwWithoutDomainName = dbData.wwwWithoutDomainName,
                                        websiteurl= dbData.wwwDomainName
                                    });
                                }
                            }
                        }

                    }

                    if (newDataVm.Any())
                    {
                        using (var db = new DBEntities())
                        {
                            var existingRecords = db.TagReportDatas.Where(m => DbFunctions.TruncateTime(m.createdDate) == currentDate.Date).ToList();
                            if (existingRecords.Any())
                            {
                                db.TagReportDatas.RemoveRange(existingRecords);
                            }
                            db.TagReportDatas.AddRange(newDataVm);
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CommonFunctions.SaveAuditLog("0.0: Exception => " + e.Message);
                }
        }

        public static void SourceReportDataUpdate()
        {
            var reportScheduledDataVm = new List<TagReportData>();
            var databaseCollection = new List<TagSchedulerVm>();
            var currentDate = DateTime.UtcNow.AddDays(-1);
            //var currentDate = new DateTime(2019, 08, 09);

            CommonFunctions.SaveAuditLog("0.0: Call Scheduler SourceReportDataUpdate() => " + currentDate.ToString("ddMMyyyy"));
            try
            {
                var reportDataCollection = CommonFunctions.GetReportDataUpdated(currentDate);

                using (var db = new DBEntities())
                {
                    var proPackageDomainList = db.propackageusers.Select(m => m.domainname).Distinct().ToList();

                    if (proPackageDomainList.Any())
                    {
                        foreach (var proPackageDomain in proPackageDomainList)
                        {
                            string wwwDomainName;
                            string wwwWithoutDomainName;
                            if (proPackageDomain.Contains("www."))
                            {
                                wwwDomainName = proPackageDomain;
                                wwwWithoutDomainName = proPackageDomain.Replace("www.", string.Empty);
                            }
                            else
                            {
                                wwwDomainName = "www." + proPackageDomain;
                                wwwWithoutDomainName = proPackageDomain;
                            }

                            var currentCollection = new TagSchedulerVm
                            {
                                blueTagCollection = new List<searchdomainMaster>(),
                                greenTagCollection = new List<searchdomainMaster>(),
                                wwwDomainName = wwwDomainName,
                                wwwWithoutDomainName = wwwWithoutDomainName
                            };

                            //Collection of green tag data
                            var greenTagCollection = db.searchdomainMasters.Where(m => m.searcgclass == "green" && (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName))).ToList();
                            if (greenTagCollection.Any())
                            {
                                currentCollection.greenTagCollection = greenTagCollection;
                            }

                            databaseCollection.Add(currentCollection);
                        }
                    }
                }

                if (databaseCollection.Any())
                {
                    var newDataVm = new List<sourceReportData>();
                    var removeDataVm = new List<sourceReportData>();
                    var sourceList = new List<string>();
                    var newReportCollection = new List<UserScheduleVm>();
                    foreach (var dbData in databaseCollection)
                    {
                        if (reportDataCollection != null && reportDataCollection.userCollection.Any())
                        {
                            foreach (var item in reportDataCollection.userCollection)
                            {
                                if (!string.IsNullOrEmpty(item.source) && !string.IsNullOrEmpty(item.domainName) && (item.domainName.Equals(dbData.wwwDomainName) || item.domainName.Equals((dbData.wwwWithoutDomainName))))
                                {
                                    Uri myUri = new Uri(item.source);
                                    var host = myUri.Host;
                                    if (!string.IsNullOrEmpty(host))
                                    {
                                        var hostArray = host.Split('.');
                                        if (hostArray[0].Length == 1 || hostArray[0] == "www")
                                        {
                                            if (hostArray.Length == 2)
                                            {
                                                var finalURL = string.Join(".", hostArray);
                                                sourceList.Add(finalURL);
                                            }
                                            else
                                            {
                                                var newHostArray = hostArray.Skip(1).ToArray();
                                                var finalURL = string.Join(".", newHostArray);
                                                sourceList.Add(finalURL);
                                            }
                                        }
                                        else
                                        {
                                            var finalURL = string.Join(".", hostArray);
                                            sourceList.Add(finalURL);
                                        }
                                    }

                                    newReportCollection.Add(item);
                                }
                            }
                        }

                        if (sourceList.Any() && newReportCollection.Any())
                        {
                            var distinctSources = sourceList.Distinct().ToList();
                            if (distinctSources.Any())
                            {
                                foreach (var source in distinctSources)
                                {
                                    var greenTagVisitor = 0;
                                    var multiPageVisitor = 0;
                                    var allPageVisitor = 0;
                                    foreach (var userCollection in newReportCollection)
                                    {
                                        var isFound = false;
                                        if (!string.IsNullOrEmpty(userCollection.source))
                                        {
                                            if (userCollection.source.Contains(source))
                                            {
                                                isFound = true;
                                                allPageVisitor = allPageVisitor + 1;
                                                if (!userCollection.isSinglePageUser)
                                                {
                                                    multiPageVisitor = multiPageVisitor + 1;
                                                }
                                            }
                                        }

                                        if (userCollection.userListing.Any() && isFound)
                                        {
                                            if (dbData.greenTagCollection.Any())
                                            {
                                                foreach (var greenTag in dbData.greenTagCollection)
                                                {
                                                    foreach (var userListing in userCollection.userListing)
                                                    {
                                                        if (userListing.url.Contains(greenTag.searchtext) || userListing.title.Contains(greenTag.searchtext))
                                                        {
                                                            greenTagVisitor = greenTagVisitor + 1;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (allPageVisitor != 0)
                                    {
                                        newDataVm.Add(new sourceReportData
                                        {
                                            allVisitor = allPageVisitor,
                                            multipageVisitor = multiPageVisitor,
                                            greenTagVisitor = greenTagVisitor,
                                            createdDate = currentDate,
                                            sourceName = source,
                                            wwwDomainName = dbData.wwwDomainName,
                                            wwwWithoutDomainName = dbData.wwwWithoutDomainName
                                        });
                                    }
                                }
                            }

                            if (newDataVm.Any())
                            {
                                using (var db = new DBEntities())
                                {
                                    var existingRecords = db.sourceReportDatas.Where(m => DbFunctions.TruncateTime(m.createdDate) == currentDate.Date).ToList();
                                    if (existingRecords.Any())
                                    {
                                        db.sourceReportDatas.RemoveRange(existingRecords);
                                    }
                                    db.sourceReportDatas.AddRange(newDataVm);
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CommonFunctions.SaveAuditLog("0.0: Exception => " + e.Message);
            }
        }
    }
}