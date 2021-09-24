using IntegrateGoogleSignIn.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace IntegrateGoogleSignIn.Helpers
{
    public static class CommonFunctions
    {
        public static DateTime ConvertTimeZone(string fromZone, string toZone, DateTime inputDate)
        {
            var response = inputDate;

            try
            {
                var fromTimeZone = TimeZoneInfo.FindSystemTimeZoneById(fromZone);
                var isDaylightFrom = fromTimeZone.IsDaylightSavingTime(inputDate);

                var utcDateTime = Convert.ToDateTime(inputDate).Subtract(fromTimeZone.BaseUtcOffset);
                if (isDaylightFrom)
                {
                    utcDateTime = utcDateTime.AddHours(-1);
                }

                var toTimeZone = TimeZoneInfo.FindSystemTimeZoneById(toZone);
                response = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, toTimeZone);

                var xx = toTimeZone.IsDaylightSavingTime(response) ? toTimeZone.DaylightName : toTimeZone.StandardName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return response;
        }

        //public static DateTime ConvertTimeZone(string fromZone, string toZone, DateTime inputDate)
        //{
        //    var response = inputDate;

        //    try
        //    { 
        //        var fromTimeZone = TimeZoneInfo.FindSystemTimeZoneById(fromZone);

        //        if (fromTimeZone != null)
        //        {
        //            //Check day light time period
        //            var isDaylightFrom = fromTimeZone.IsDaylightSavingTime(inputDate);

        //            //if (isDaylightFrom)
        //            //    inputDate = inputDate.AddHours(-1);

        //            // 1.0 => Convert input date to UTC date with respect to from time zone
        //            var utcTime = Convert.ToDateTime(inputDate).Subtract(fromTimeZone.BaseUtcOffset);

        //            var toTimeZone = TimeZoneInfo.FindSystemTimeZoneById(toZone);

        //            if (toTimeZone != null)
        //            {
        //                // 2.0 => Convert utc date to required time with respect to given time zone format 
        //                response = TimeZoneInfo.ConvertTimeFromUtc(utcTime, toTimeZone);

        //                //Check day light time period
        //                var isDaylightTo = toTimeZone.IsDaylightSavingTime(response);

        //                //if (isDaylightFrom && !isDaylightTo)
        //                //    response = response.AddHours(-1);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }

        //    return response;
        //}

        public static DateTime GetCurrentUkTime()
        {
            var toTimeZone = TimeZoneInfo.FindSystemTimeZoneById(StaticValues.TzUk);
            var response = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, toTimeZone);
            return response;
        }
        public static T JsonToClass<T>(T response, string content)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                response = JsonConvert.DeserializeObject<T>(content, settings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return response;
        }

        public static void SaveAuditLog(string description)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    var audit = new AuditLog
                    {
                        CreatedDate = DateTime.Now,
                        Description = description
                    };

                    db.AuditLogs.Add(audit);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void SendMail(string email, bool isSubscribe)
        {
            string emailTemplate;
            var msg = new MailMessage();
            msg.To.Add(new MailAddress(email, email));
            msg.From = new MailAddress("info@digital-crumbs.com", "Digital-Crumbs");
            if (isSubscribe)
            {
                msg.Subject = "Domain subscribed successfully";
                emailTemplate = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN'><html><head><meta http-equiv='Content-Type' content='text/html; charset=UTF-8'><!--[if !mso]><!--><meta http-equiv='X-UA-Compatible' content='IE=edge'><!--<![endif]--><meta name='viewport' content='width=device-width, initial-scale=1.0'><title></title><style type='text/css'>* {-webkit-font-smoothing: antialiased;}body {Margin: 0;padding: 0;min-width: 100%;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;mso-line-height-rule: exactly;}table {border-spacing: 0;color: #333333;font-family: Arial, sans-serif;}img {border: 0;}.wrapper {width: 100%;table-layout: fixed;-webkit-text-size-adjust: 100%;-ms-text-size-adjust: 100%;}.webkit {max-width: 600px;}.outer {Margin: 0 auto;width: 100%;max-width: 600px;}.full-width-image img {width: 100%;max-width: 600px;height: auto;}.inner {padding: 10px;}p {Margin: 0;padding-bottom: 10px;}.h1 {font-size: 21px;font-weight: bold;Margin-top: 15px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.h2 {font-size: 18px;font-weight: bold;Margin-top: 10px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column .contents {text-align: left;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column p {font-size: 14px;Margin-bottom: 10px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.two-column {text-align: center;font-size: 0;}.two-column .column {width: 100%;max-width: 300px;display: inline-block;vertical-align: top;}.contents {width: 100%;}.two-column .contents {font-size: 14px;text-align: left;}.two-column img {width: 100%;max-width: 280px;height: auto;}.two-column .text {padding-top: 10px;}.three-column {text-align: center;font-size: 0;padding-top: 10px;padding-bottom: 10px;}.three-column .column {width: 100%;max-width: 200px;display: inline-block;vertical-align: top;}.three-column .contents {font-size: 14px;text-align: center;}.three-column img {width: 100%;max-width: 180px;height: auto;}.three-column .text {padding-top: 10px;}.img-align-vertical img {display: inline-block;vertical-align: middle;}@media only screen and (max-device-width: 480px) {table[class=hide], img[class=hide], td[class=hide] {display: none !important;}.contents1 {width: 100%;}.contents1 {width: 100%;}}</style><!--[if (gte mso 9)|(IE)]><style type='text/css'>table {border-collapse: collapse !important;}</style><![endif]--></head><body style='Margin:0;padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;min-width:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><center class='wrapper' style='width:100%;table-layout:fixed;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><table style='background:linear-gradient(to left,#f77408, #ffbe00);' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#f3f2f0;'><tbody><tr><td width='100%'><div class='webkit' style='max-width:600px;Margin:0 auto;'><!--[if (gte mso 9)|(IE)]><table width='600' align='center' cellpadding='0' cellspacing='0' border='0' style='border-spacing:0' ><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;' ><![endif]--><!-- ======= start main body ======= --><table class='outer' style='border-spacing:0;Margin:0 auto;width:100%;max-width:600px;' cellspacing='0' cellpadding='0' border='0' align='center'><tbody><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;'><!-- ======= start header ======= --><table width='100%' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td><table style='width:100%;' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td class='vervelogoplaceholder' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;height:143px;vertical-align:middle;' valign='middle' height='143' align='center'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22160%22%2C%22height%22%3A34%2C%22alt_text%22%3A%22Verve%20Wine%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/79d8f4f889362f0c7effb2c26e08814bb12f5eb31c053021ada3463c7b35de6fb261440fc89fa804edbd11242076a81c8f0a9daa443273da5cb09c1a4739499f.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><a href='#' target='_blank'><img alt='Digital-crumbs' src='http://digital-crumbs.co.uk/Content/logo.png' style='border-width: 0px; width: 160px; height: 34px;' width='160' height='34'></a></span></td></tr></tbody></table></td></tr></tbody></table><!-- ======= end header ======= --><!-- ======= start hero ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #fdac02; border-right:1px solid #fdac02; border-bottom:1px solid #fdac02; border-top:1px solid #fdac02;' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 20px 50px;background: linear-gradient(to left,#f77408, #ffbe00);' width='600' valign='top' height='80' align='center'><!--[if gte mso 9]><v:rect xmlns:v='urn:schemas-microsoft-com:vml' fill='true' stroke='false' style='width:600px;height:303px;'><v:fill type='tile' src='https://gallery.mailchimp.com/fdcaf86ecc5056741eb5cbc18/images/42ba8b72-65d6-4dea-b8ab-3ecc12676337.jpg' color='#2f9780' /><v:textbox inset='0,0,0,0'><![endif]--><div><br><br><p style='color:#ffffff; font-size:30px; text-align:center; font-family: Verdana, Geneva, sans-serif'>Subscribed successfully</p></div><!--[if gte mso 9]></v:textbox></v:rect><![endif]--></td></tr></tbody></table><!-- ======= end hero  ======= --><!-- ======= start article ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #e8e7e5; border-right:1px solid #e8e7e5; border-bottom:1px solid #e8e7e5; border-top:1px solid #e8e7e5' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 10px 50px' align='center'><p style='color:#262626; font-size:16px; text-align:justify; font-family: Verdana, Geneva, sans-serif; line-height:22px '>Dear User,<br><br>Thank you for subscribe domain.</p><p></p></td></tr></tbody></table><!-- ======= end article ======= --><!-- ======= start footer ======= --></td></tr></tbody></table><!--[if (gte mso 9)|(IE)]></td></tr></table><![endif]--></div></td></tr></tbody></table></center></body></html>";
            }
            else
            {
                msg.Subject = "Your Digital-crumbs account created successfully";
                emailTemplate = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN'><html><head><meta http-equiv='Content-Type' content='text/html; charset=UTF-8'><!--[if !mso]><!--><meta http-equiv='X-UA-Compatible' content='IE=edge'><!--<![endif]--><meta name='viewport' content='width=device-width, initial-scale=1.0'><title></title><style type='text/css'>* {-webkit-font-smoothing: antialiased;}body {Margin: 0;padding: 0;min-width: 100%;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;mso-line-height-rule: exactly;}table {border-spacing: 0;color: #333333;font-family: Arial, sans-serif;}img {border: 0;}.wrapper {width: 100%;table-layout: fixed;-webkit-text-size-adjust: 100%;-ms-text-size-adjust: 100%;}.webkit {max-width: 600px;}.outer {Margin: 0 auto;width: 100%;max-width: 600px;}.full-width-image img {width: 100%;max-width: 600px;height: auto;}.inner {padding: 10px;}p {Margin: 0;padding-bottom: 10px;}.h1 {font-size: 21px;font-weight: bold;Margin-top: 15px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.h2 {font-size: 18px;font-weight: bold;Margin-top: 10px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column .contents {text-align: left;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column p {font-size: 14px;Margin-bottom: 10px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.two-column {text-align: center;font-size: 0;}.two-column .column {width: 100%;max-width: 300px;display: inline-block;vertical-align: top;}.contents {width: 100%;}.two-column .contents {font-size: 14px;text-align: left;}.two-column img {width: 100%;max-width: 280px;height: auto;}.two-column .text {padding-top: 10px;}.three-column {text-align: center;font-size: 0;padding-top: 10px;padding-bottom: 10px;}.three-column .column {width: 100%;max-width: 200px;display: inline-block;vertical-align: top;}.three-column .contents {font-size: 14px;text-align: center;}.three-column img {width: 100%;max-width: 180px;height: auto;}.three-column .text {padding-top: 10px;}.img-align-vertical img {display: inline-block;vertical-align: middle;}@media only screen and (max-device-width: 480px) {table[class=hide], img[class=hide], td[class=hide] {display: none !important;}.contents1 {width: 100%;}.contents1 {width: 100%;}}</style><!--[if (gte mso 9)|(IE)]><style type='text/css'>table {border-collapse: collapse !important;}</style><![endif]--></head><body style='Margin:0;padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;min-width:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><center class='wrapper' style='width:100%;table-layout:fixed;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><table style='background:linear-gradient(to left,#f77408, #ffbe00);' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#f3f2f0;'><tbody><tr><td width='100%'><div class='webkit' style='max-width:600px;Margin:0 auto;'><!--[if (gte mso 9)|(IE)]><table width='600' align='center' cellpadding='0' cellspacing='0' border='0' style='border-spacing:0' ><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;' ><![endif]--><!-- ======= start main body ======= --><table class='outer' style='border-spacing:0;Margin:0 auto;width:100%;max-width:600px;' cellspacing='0' cellpadding='0' border='0' align='center'><tbody><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;'><!-- ======= start header ======= --><table width='100%' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td><table style='width:100%;' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td class='vervelogoplaceholder' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;height:143px;vertical-align:middle;' valign='middle' height='143' align='center'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22160%22%2C%22height%22%3A34%2C%22alt_text%22%3A%22Verve%20Wine%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/79d8f4f889362f0c7effb2c26e08814bb12f5eb31c053021ada3463c7b35de6fb261440fc89fa804edbd11242076a81c8f0a9daa443273da5cb09c1a4739499f.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><a href='#' target='_blank'><img alt='Digital-crumbs' src='http://digital-crumbs.co.uk/Content/logo.png' style='border-width: 0px; width: 160px; height: 34px;' width='160' height='34'></a></span></td></tr></tbody></table></td></tr></tbody></table><!-- ======= end header ======= --><!-- ======= start hero ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #fdac02; border-right:1px solid #fdac02; border-bottom:1px solid #fdac02; border-top:1px solid #fdac02;' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 20px 50px;background: linear-gradient(to left,#f77408, #ffbe00);' width='600' valign='top' height='80' align='center'><!--[if gte mso 9]><v:rect xmlns:v='urn:schemas-microsoft-com:vml' fill='true' stroke='false' style='width:600px;height:303px;'><v:fill type='tile' src='https://gallery.mailchimp.com/fdcaf86ecc5056741eb5cbc18/images/42ba8b72-65d6-4dea-b8ab-3ecc12676337.jpg' color='#2f9780' /><v:textbox inset='0,0,0,0'><![endif]--><div><br><br><p style='color:#ffffff; font-size:30px; text-align:center; font-family: Verdana, Geneva, sans-serif'>Account created successfully</p></div><!--[if gte mso 9]></v:textbox></v:rect><![endif]--></td></tr></tbody></table><!-- ======= end hero  ======= --><!-- ======= start article ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #e8e7e5; border-right:1px solid #e8e7e5; border-bottom:1px solid #e8e7e5; border-top:1px solid #e8e7e5' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 10px 50px' align='center'><p style='color:#262626; font-size:16px; text-align:justify; font-family: Verdana, Geneva, sans-serif; line-height:22px '>Dear User,<br><br>Thank you for registering on <a href='http://digital-crumbs.com'><b>Digital-crumbs</b></a></p><p></p></td></tr></tbody></table><!-- ======= end article ======= --><!-- ======= start footer ======= --></td></tr></tbody></table><!--[if (gte mso 9)|(IE)]></td></tr></table><![endif]--></div></td></tr></tbody></table></center></body></html>";
            }

            msg.Body = emailTemplate;
            msg.IsBodyHtml = true;

            var client = new SmtpClient
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("info@digital-crumbs.com", "Ishapatel@1998"),
                Port = 587, // You can use Port 25 if 587 is blocked (mine is!)
                Host = "smtp.ionos.co.uk",
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true
            };
            client.Send(msg);
        }

        public static List<string> GetDomainList(string email, string domain)
        {
            var db = new DBEntities();
            var domainList = db.propackageusers.Where(m => m.email.Equals(email)).Select(k => k.domainname).Distinct()
                .ToList();

            var domainCollection = new List<string>();
            if (domainList.Any())
            {
                foreach (var domainDetail in domainList)
                {
                    domainCollection.Add(domainDetail);
                }
            }

            return domainCollection;
        }

        public static List<string> GetDomainListForExtraUser(string email, string domain)
        {
            var db = new DBEntities();
            var domainList = db.ExtraUsers.Where(m => m.email.Equals(email)).Select(k => k.domainname).Distinct()
                .ToList();

            var domainCollection = new List<string>();
            if (domainList.Any())
            {
                foreach (var domainDetail in domainList)
                {
                    domainCollection.Add(domainDetail);
                }
            }

            return domainCollection;
        }

        public static DailyUserInteractionVm GetDailyCount(string domainName, DateTime reportDate)
        {
          //  domainName = "www.phileasfoggsworldofadventures.co.uk";
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
            var db = new DBEntities();

            var reportPageVm = new ReportPageVm
            {
                userListing = new List<UserVm>(),
                domainName = domainName,
                reportCurrentDate = reportDate
            };

            var currentDate = reportDate.ToString("yyyyMMdd");
            var query =
                "SELECT distinct UserDetail.UserId as userid, CONVERT(varchar, UserDetail.SessionDateTime, 112) AS sessionDate FROM Users INNER JOIN UserDetail ON Users.Id = UserDetail.UserId and (Users.DomainName='" +
                wwwDomainName + "' or Users.DomainName='" +
                wwwWithoutDomainName + "') and CONVERT(varchar, UserDetail.SessionDateTime, 112) = " + currentDate +
                " order by UserDetail.UserId";

            var userSessionDetails = db.Database.SqlQuery<ReportVm>(query).ToList();
            var dailyUserInteractionVm = new DailyUserInteractionVm
            {
                visitors = userSessionDetails.Count()
            };

            var userDetailsCollection = db.UserDetails
                .Where(m => (m.User.DomainName.Equals(wwwDomainName) || m.User.DomainName.Equals(wwwWithoutDomainName)) && m.SessionDateTime.HasValue &&
                            DbFunctions.TruncateTime(m.SessionDateTime.Value) == reportDate.Date)
                .OrderBy(m => m.UserId).ThenBy(k => k.UserSessionId).ToList();

            var userCollection = new List<UserVm>();
            foreach (var item in userDetailsCollection.OrderBy(k => k.SessionDateTime))
            {
                var userData = userCollection.FirstOrDefault(m => m.id == item.UserId);
                if (userData == null)
                {
                    var userDetails = new UserVm
                    {
                        location = item.User.Location,
                        source = item.User.Source,
                        id = item.UserId,
                        mobile = item.User.mobile,
                        isSinglePageUser = true,
                        userInteractions = new List<EarlierInteractionVm>()
                    };

                    var sessionInteraction = new EarlierInteractionVm
                    {
                        UserSessionId = item.UserSessionId,
                        Visits = new List<EarlierUserInteractionVm>()
                    };

                    var earlierInteraction = new EarlierUserInteractionVm
                    {
                        url = item.StartUrl,
                        title = item.PageTitle,
                        sessionDatetime = item.SessionDateTime,
                        sessionId = item.UserSessionId
                    };

                    sessionInteraction.Visits.Add(earlierInteraction);
                    userDetails.userInteractions.Add(sessionInteraction);
                    userCollection.Add(userDetails);
                }
                else
                {
                    var userSessionData =
                        userData.userInteractions.FirstOrDefault(m => m.UserSessionId == item.UserSessionId);
                    if (userSessionData == null)
                    {
                        var sessionInteraction = new EarlierInteractionVm
                        {
                            UserSessionId = item.UserSessionId,
                            Visits = new List<EarlierUserInteractionVm>()
                        };

                        var earlierInteraction = new EarlierUserInteractionVm
                        {
                            url = item.StartUrl,
                            title = item.PageTitle,
                            sessionDatetime = item.SessionDateTime,
                            sessionId = item.UserSessionId
                        };

                        sessionInteraction.Visits.Add(earlierInteraction);
                        userData.userInteractions.Add(sessionInteraction);
                    }
                    else
                    {
                        userData.isSinglePageUser = false;

                        var earlierInteraction = new EarlierUserInteractionVm
                        {
                            url = item.StartUrl,
                            title = item.PageTitle,
                            sessionDatetime = item.SessionDateTime,
                            sessionId = item.UserSessionId
                        };

                        userSessionData.Visits.Add(earlierInteraction);
                    }
                }
            }

            var newCollection = new List<UserVm>();
            foreach (var userCol in userCollection)
            {
                var userDetails = new UserVm
                {
                    location = userCol.location,
                    source = userCol.source,
                    mobile = userCol.mobile,
                    id = userCol.id,
                    isSinglePageUser = userCol.isSinglePageUser,
                    userInteractions = new List<EarlierInteractionVm>()
                };

                var totalDuration = 0;
                foreach (var earlierCollection in userCol.userInteractions)
                {
                    var earlierVisitSessions = new List<EarlierUserInteractionVm>();
                    if (earlierCollection.Visits.Count <= 1)
                    {
                        earlierCollection.Visits = earlierCollection.Visits;
                        userDetails.userInteractions.Add(earlierCollection);
                        continue;
                    }

                    for (var i = 0; i < earlierCollection.Visits.Count; i++)
                    {
                        var needToAdd = false;
                        if (i == (earlierCollection.Visits.Count - 1))
                        {
                            needToAdd = true;
                            earlierCollection.isSessionEnd = true;
                        }
                        else
                        {
                            var sessionDatetime = earlierCollection.Visits[i].sessionDatetime;
                            if (sessionDatetime == null)
                            {
                                continue;
                            }

                            var dateTime = earlierCollection.Visits[i + 1].sessionDatetime;
                            if (dateTime != null)
                            {
                                earlierCollection.Visits[i].durationTime =
                                    (dateTime.Value - sessionDatetime.Value).TotalSeconds;
                            }

                            if (earlierCollection.Visits[i].durationTime > 0)
                            {
                                needToAdd = true;
                                totalDuration = (int)(totalDuration + earlierCollection.Visits[i].durationTime);
                            }
                        }

                        if (needToAdd)
                        {
                            earlierVisitSessions.Add(earlierCollection.Visits[i]);
                        }
                    }

                    earlierCollection.Visits = earlierVisitSessions;
                    userDetails.userInteractions.Add(earlierCollection);
                }

                userDetails.duration = totalDuration;
                newCollection.Add(userDetails);

                reportPageVm.userListing = newCollection;
            }


            if (reportPageVm != null && reportPageVm.userListing.Any())
            {
                dailyUserInteractionVm.visitors = reportPageVm.userListing.Count();
                dailyUserInteractionVm.multiVisitors = reportPageVm.userListing.Where(m => !m.isSinglePageUser && m.userInteractions.Any(p => p.Visits.Count > 1 && p.Visits.Any(k => k.durationTime > 0))).Count();
            }

            var blueList = db.searchdomainMasters.Where(m => m.searcgclass == "blue" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();
            var redList = db.searchdomainMasters.Where(m => m.searcgclass == "red" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();
            var greenList = db.searchdomainMasters.Where(m => m.searcgclass == "green" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();
            var pinkList = db.searchdomainMasters.Where(m => m.searcgclass == "pink" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();

            foreach (var singUserDetail in reportPageVm.userListing)
            {
                var isRedCounted = false;
                var isGreenCounted = false;
                var isBlueCounted = false;
                var isPinkCounted = false;

                foreach (var bluetag in blueList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }
                                    var searchtext1 = '?' + bluetag.searchtext;

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(searchtext1) && !isBlueCounted)
                                    {
                                        dailyUserInteractionVm.blueVisitors = dailyUserInteractionVm.blueVisitors + 1;
                                        isBlueCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var redtag in redList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(redtag.searchtext) && !isRedCounted)
                                    {
                                        dailyUserInteractionVm.redVisitors = dailyUserInteractionVm.redVisitors + 1;
                                        isRedCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var greentag in greenList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(greentag.searchtext) && !isGreenCounted)
                                    {
                                        dailyUserInteractionVm.greenVisitors = dailyUserInteractionVm.greenVisitors + 1;
                                        isGreenCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var pinktag in pinkList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(pinktag.searchtext) && !isPinkCounted)
                                    {
                                        dailyUserInteractionVm.pinkVisitors = dailyUserInteractionVm.pinkVisitors + 1;
                                        isPinkCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            
            return dailyUserInteractionVm;
        }

        public static ReportPageScedulerVm GetReportData(string wwwDomainName, string wwwWithoutDomainName, DateTime date)
        {
            var db = new DBEntities();

            //Timezone code
            var startDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            var endDate = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
            var startDateUk = CommonFunctions.ConvertTimeZone(string.Empty, StaticValues.TzUk, startDate);
            var endDateUk = CommonFunctions.ConvertTimeZone(string.Empty, StaticValues.TzUk, endDate);

            var reportPageVm = new ReportPageScedulerVm
            {
                userCollection = new List<UserScheduleVm>(),
                domainName = wwwDomainName
            };

            var userDetailsCollection = db.UserDetails
                .Where(m => (m.User.DomainName.Equals(wwwDomainName) || m.User.DomainName.Equals(wwwWithoutDomainName)) && m.SessionDateTime.HasValue &&
                            (m.SessionDateTime.Value >= startDateUk && m.SessionDateTime.Value <= endDateUk))
                .OrderBy(m => m.UserId).ThenBy(k => k.UserSessionId).ToList();

            var userCollection = new List<UserVm>();
            foreach (var item in userDetailsCollection.OrderBy(k => k.SessionDateTime))
            {
                var userData = userCollection.FirstOrDefault(m => m.id == item.UserId);
                if (userData == null)
                {
                    var userDetails = new UserVm
                    {
                        id = item.UserId,
                        source = item.User.Source,
                        isSinglePageUser = true,
                        userInteractionCollections = new List<UserSceduledInteractionVm>(),
                        userInteractions = new List<EarlierInteractionVm>(),
                    };

                    var sessionInteraction = new EarlierInteractionVm
                    {
                        UserSessionId = item.UserSessionId
                    };

                    userDetails.userInteractions.Add(sessionInteraction);
                    userDetails.userInteractionCollections.Add(new UserSceduledInteractionVm()
                    {
                        title = item.PageTitle,
                        url = item.StartUrl
                    });

                    userCollection.Add(userDetails);
                }
                else
                {
                    var userSessionData =
                        userData.userInteractions.FirstOrDefault(m => m.UserSessionId == item.UserSessionId);
                    if (userSessionData == null)
                    {
                        var sessionInteraction = new EarlierInteractionVm
                        {
                            UserSessionId = item.UserSessionId
                        };

                        userData.userInteractions.Add(sessionInteraction);
                        userData.userInteractionCollections.Add(new UserSceduledInteractionVm()
                        {
                            title = item.PageTitle,
                            url = item.StartUrl
                        });
                    }
                    else
                    {
                        userData.isSinglePageUser = false;
                        userData.userInteractionCollections.Add(new UserSceduledInteractionVm()
                        {
                            title = item.PageTitle,
                            url = item.StartUrl
                        });
                    }
                }
            }

            foreach (var item in userCollection)
            {
                reportPageVm.userCollection.Add(new UserScheduleVm
                {
                    isSinglePageUser = item.isSinglePageUser,
                    source = item.source,
                    id = item.id,
                    userListing = item.userInteractionCollections
                });
            }

            return reportPageVm;
        }

        public static ReportPageScedulerVm GetReportDataUpdated(DateTime date)
        {
            //Timezone code
            var startDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            var endDate = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
            var startDateUk = CommonFunctions.ConvertTimeZone(string.Empty, StaticValues.TzUk, startDate);
            var endDateUk = CommonFunctions.ConvertTimeZone(string.Empty, StaticValues.TzUk, endDate);

            var reportPageVm = new ReportPageScedulerVm
            {
                userCollection = new List<UserScheduleVm>()
            };

            var userDetailsCollection = new List<UserDetail>();

            using (var db = new DBEntities())
            {
                userDetailsCollection = db.UserDetails
                    .Where(m => m.SessionDateTime.HasValue &&
                                (m.SessionDateTime.Value >= startDateUk && m.SessionDateTime.Value <= endDateUk))
                    .OrderBy(m => m.UserId).ThenBy(k => k.UserSessionId).Include(m => m.User).ToList();
            }

            var userCollection = new List<UserVm>();
            foreach (var item in userDetailsCollection.OrderBy(k => k.SessionDateTime))
            {
                var userData = userCollection.FirstOrDefault(m => m.id == item.UserId);
                if (userData == null)
                {
                    var userDetails = new UserVm
                    {
                        id = item.UserId,
                        domainName = item.User.DomainName,
                        source = item.User.Source,
                        isSinglePageUser = true,
                        userInteractionCollections = new List<UserSceduledInteractionVm>(),
                        userInteractions = new List<EarlierInteractionVm>(),
                    };

                    var sessionInteraction = new EarlierInteractionVm
                    {
                        UserSessionId = item.UserSessionId
                    };

                    userDetails.userInteractions.Add(sessionInteraction);
                    userDetails.userInteractionCollections.Add(new UserSceduledInteractionVm()
                    {
                        title = item.PageTitle,
                        url = item.StartUrl
                    });

                    userCollection.Add(userDetails);
                }
                else
                {
                    var userSessionData =
                        userData.userInteractions.FirstOrDefault(m => m.UserSessionId == item.UserSessionId);
                    if (userSessionData == null)
                    {
                        var sessionInteraction = new EarlierInteractionVm
                        {
                            UserSessionId = item.UserSessionId
                        };

                        userData.userInteractions.Add(sessionInteraction);
                        userData.userInteractionCollections.Add(new UserSceduledInteractionVm()
                        {
                            title = item.PageTitle,
                            url = item.StartUrl
                        });
                    }
                    else
                    {
                        userData.isSinglePageUser = false;
                        userData.userInteractionCollections.Add(new UserSceduledInteractionVm()
                        {
                            title = item.PageTitle,
                            url = item.StartUrl
                        });
                    }
                }
            }

            foreach (var item in userCollection)
            {
                reportPageVm.userCollection.Add(new UserScheduleVm
                {
                    domainName = item.domainName,
                    isSinglePageUser = item.isSinglePageUser,
                    source = item.source,
                    id = item.id,
                    userListing = item.userInteractionCollections
                });
            }

            return reportPageVm;
        }

        public static DailyUserInteractionVm GetDailyCountUpdated(string domainName, DateTime reportDate)
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

            var reportPageVm = new ReportPageVm
            {
                userListing = new List<UserVm>(),
                domainName = domainName,
                reportCurrentDate = reportDate
            };

            var dailyUserInteractionVm = new DailyUserInteractionVm();
            var userDetailsCollection = new List<UserDetail>();
            var blueList = new List<searchdomainMaster>();
            var redList = new List<searchdomainMaster>();
            var greenList = new List<searchdomainMaster>();
            var pinkList = new List<searchdomainMaster>();

            using (var db = new DBEntities())
            {
                userDetailsCollection = db.UserDetails
                .Where(m => (m.User.DomainName.Equals(wwwDomainName) || m.User.DomainName.Equals(wwwWithoutDomainName)) && m.SessionDateTime.HasValue &&
                            DbFunctions.TruncateTime(m.SessionDateTime.Value) == reportDate.Date)
                .OrderBy(m => m.UserId).ThenBy(k => k.UserSessionId).ToList();

                blueList = db.searchdomainMasters.Where(m => m.searcgclass == "blue" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();
                redList = db.searchdomainMasters.Where(m => m.searcgclass == "red" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();
                greenList = db.searchdomainMasters.Where(m => m.searcgclass == "green" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();
                pinkList = db.searchdomainMasters.Where(m => m.searcgclass == "pink" && (m.domainname == wwwDomainName || m.domainname == wwwWithoutDomainName)).ToList();
            }

            var userCollection = new List<UserVm>();
            foreach (var item in userDetailsCollection.OrderBy(k => k.SessionDateTime))
            {
                var userData = userCollection.FirstOrDefault(m => m.id == item.UserId);
                if (userData == null)
                {
                    var userDetails = new UserVm
                    {
                        location = item.User.Location,
                        source = item.User.Source,
                        id = item.UserId,
                        mobile = item.User.mobile,
                        isSinglePageUser = true,
                        userInteractions = new List<EarlierInteractionVm>()
                    };

                    var sessionInteraction = new EarlierInteractionVm
                    {
                        UserSessionId = item.UserSessionId,
                        Visits = new List<EarlierUserInteractionVm>()
                    };

                    var earlierInteraction = new EarlierUserInteractionVm
                    {
                        url = item.StartUrl,
                        title = item.PageTitle,
                        sessionDatetime = item.SessionDateTime,
                        sessionId = item.UserSessionId
                    };

                    sessionInteraction.Visits.Add(earlierInteraction);
                    userDetails.userInteractions.Add(sessionInteraction);
                    userCollection.Add(userDetails);
                }
                else
                {
                    var userSessionData =
                        userData.userInteractions.FirstOrDefault(m => m.UserSessionId == item.UserSessionId);
                    if (userSessionData == null)
                    {
                        var sessionInteraction = new EarlierInteractionVm
                        {
                            UserSessionId = item.UserSessionId,
                            Visits = new List<EarlierUserInteractionVm>()
                        };

                        var earlierInteraction = new EarlierUserInteractionVm
                        {
                            url = item.StartUrl,
                            title = item.PageTitle,
                            sessionDatetime = item.SessionDateTime,
                            sessionId = item.UserSessionId
                        };

                        sessionInteraction.Visits.Add(earlierInteraction);
                        userData.userInteractions.Add(sessionInteraction);
                    }
                    else
                    {
                        userData.isSinglePageUser = false;

                        var earlierInteraction = new EarlierUserInteractionVm
                        {
                            url = item.StartUrl,
                            title = item.PageTitle,
                            sessionDatetime = item.SessionDateTime,
                            sessionId = item.UserSessionId
                        };

                        userSessionData.Visits.Add(earlierInteraction);
                    }
                }
            }

            var newCollection = new List<UserVm>();
            foreach (var userCol in userCollection)
            {
                var userDetails = new UserVm
                {
                    location = userCol.location,
                    source = userCol.source,
                    mobile = userCol.mobile,
                    id = userCol.id,
                    isSinglePageUser = userCol.isSinglePageUser,
                    userInteractions = new List<EarlierInteractionVm>()
                };

                var totalDuration = 0;
                foreach (var earlierCollection in userCol.userInteractions)
                {
                    var earlierVisitSessions = new List<EarlierUserInteractionVm>();
                    if (earlierCollection.Visits.Count <= 1)
                    {
                        earlierCollection.Visits = earlierCollection.Visits;
                        userDetails.userInteractions.Add(earlierCollection);
                        continue;
                    }

                    for (var i = 0; i < earlierCollection.Visits.Count; i++)
                    {
                        var needToAdd = false;
                        if (i == (earlierCollection.Visits.Count - 1))
                        {
                            needToAdd = true;
                            earlierCollection.isSessionEnd = true;
                        }
                        else
                        {
                            var sessionDatetime = earlierCollection.Visits[i].sessionDatetime;
                            if (sessionDatetime == null)
                            {
                                continue;
                            }

                            var dateTime = earlierCollection.Visits[i + 1].sessionDatetime;
                            if (dateTime != null)
                            {
                                earlierCollection.Visits[i].durationTime =
                                    (dateTime.Value - sessionDatetime.Value).TotalSeconds;
                            }

                            if (earlierCollection.Visits[i].durationTime > 0)
                            {
                                needToAdd = true;
                                totalDuration = (int)(totalDuration + earlierCollection.Visits[i].durationTime);
                            }
                        }

                        if (needToAdd)
                        {
                            earlierVisitSessions.Add(earlierCollection.Visits[i]);
                        }
                    }

                    earlierCollection.Visits = earlierVisitSessions;
                    userDetails.userInteractions.Add(earlierCollection);
                }

                userDetails.duration = totalDuration;
                newCollection.Add(userDetails);

                reportPageVm.userListing = newCollection;
            }

            if (reportPageVm != null && reportPageVm.userListing.Any())
            {
                dailyUserInteractionVm.visitors = reportPageVm.userListing.Count();
                dailyUserInteractionVm.multiVisitors = reportPageVm.userListing.Where(m => !m.isSinglePageUser && m.userInteractions.Any(p => p.Visits.Count > 1 && p.Visits.Any(k => k.durationTime > 0))).Count();
            }

            foreach (var singUserDetail in reportPageVm.userListing)
            {
                var isRedCounted = false;
                var isGreenCounted = false;
                var isBlueCounted = false;
                var isPinkCounted = false;

                foreach (var bluetag in blueList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(bluetag.searchtext) && !isBlueCounted)
                                    {
                                        dailyUserInteractionVm.blueVisitors = dailyUserInteractionVm.blueVisitors + 1;
                                        isBlueCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var redtag in redList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(redtag.searchtext) && !isRedCounted)
                                    {
                                        dailyUserInteractionVm.redVisitors = dailyUserInteractionVm.redVisitors + 1;
                                        isRedCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var greentag in greenList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(greentag.searchtext) && !isGreenCounted)
                                    {
                                        dailyUserInteractionVm.greenVisitors = dailyUserInteractionVm.greenVisitors + 1;
                                        isGreenCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var pinktag in pinkList)
                {
                    if (singUserDetail.userInteractions.Any())
                    {
                        foreach (var innerDetails in singUserDetail.userInteractions)
                        {
                            if (innerDetails.Visits.Any())
                            {
                                foreach (var item in innerDetails.Visits)
                                {
                                    var splitURL = item.url.Split('/');
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

                                    if (urlToDisplay.Length > 29)
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay.Substring(0, 30));
                                    }
                                    else
                                    {
                                        urlToDisplay = string.Concat("/", urlToDisplay);
                                    }

                                    if (!string.IsNullOrEmpty(urlToDisplay) &&
                                        urlToDisplay.Contains(pinktag.searchtext) && !isPinkCounted)
                                    {
                                        dailyUserInteractionVm.pinkVisitors = dailyUserInteractionVm.pinkVisitors + 1;
                                        isPinkCounted = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dailyUserInteractionVm;
        }

        public static void SendMail1(string prepackageEmail, bool v, string email, bool v1, string password, bool v2)
        {
            var emailTemplate = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN'><html><head><meta http-equiv='Content-Type' content='text/html; charset=UTF-8'><!--[if !mso]><!--><meta http-equiv='X-UA-Compatible' content='IE=edge'><!--<![endif]--><meta name='viewport' content='width=device-width, initial-scale=1.0'><title></title><style type='text/css'>* {-webkit-font-smoothing: antialiased;}body {Margin: 0;padding: 0;min-width: 100%;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;mso-line-height-rule: exactly;}table {border-spacing: 0;color: #333333;font-family: Arial, sans-serif;}img {border: 0;}.wrapper {width: 100%;table-layout: fixed;-webkit-text-size-adjust: 100%;-ms-text-size-adjust: 100%;}.webkit {max-width: 600px;}.outer {Margin: 0 auto;width: 100%;max-width: 600px;}.full-width-image img {width: 100%;max-width: 600px;height: auto;}.inner {padding: 10px;}p {Margin: 0;padding-bottom: 10px;}.h1 {font-size: 21px;font-weight: bold;Margin-top: 15px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.h2 {font-size: 18px;font-weight: bold;Margin-top: 10px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column .contents {text-align: left;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column p {font-size: 14px;Margin-bottom: 10px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.two-column {text-align: center;font-size: 0;}.two-column .column {width: 100%;max-width: 300px;display: inline-block;vertical-align: top;}.contents {width: 100%;}.two-column .contents {font-size: 14px;text-align: left;}.two-column img {width: 100%;max-width: 280px;height: auto;}.two-column .text {padding-top: 10px;}.three-column {text-align: center;font-size: 0;padding-top: 10px;padding-bottom: 10px;}.three-column .column {width: 100%;max-width: 200px;display: inline-block;vertical-align: top;}.three-column .contents {font-size: 14px;text-align: center;}.three-column img {width: 100%;max-width: 180px;height: auto;}.three-column .text {padding-top: 10px;}.img-align-vertical img {display: inline-block;vertical-align: middle;}@media only screen and (max-device-width: 480px) {table[class=hide], img[class=hide], td[class=hide] {display: none !important;}.contents1 {width: 100%;}.contents1 {width: 100%;}}</style><!--[if (gte mso 9)|(IE)]><style type='text/css'>table {border-collapse: collapse !important;}</style><![endif]--></head><body style='Margin:0;padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;min-width:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><center class='wrapper' style='width:100%;table-layout:fixed;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><table style='background:linear-gradient(to left,#f77408, #ffbe00);' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#f3f2f0;'><tbody><tr><td width='100%'><div class='webkit' style='max-width:600px;Margin:0 auto;'><!--[if (gte mso 9)|(IE)]><table width='600' align='center' cellpadding='0' cellspacing='0' border='0' style='border-spacing:0' ><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;' ><![endif]--><!-- ======= start main body ======= --><table class='outer' style='border-spacing:0;Margin:0 auto;width:100%;max-width:600px;' cellspacing='0' cellpadding='0' border='0' align='center'><tbody><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;'><!-- ======= start header ======= --><table width='100%' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td><table style='width:100%;' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td class='vervelogoplaceholder' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;height:143px;vertical-align:middle;' valign='middle' height='143' align='center'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22160%22%2C%22height%22%3A34%2C%22alt_text%22%3A%22Verve%20Wine%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/79d8f4f889362f0c7effb2c26e08814bb12f5eb31c053021ada3463c7b35de6fb261440fc89fa804edbd11242076a81c8f0a9daa443273da5cb09c1a4739499f.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><a href='#' target='_blank'><img alt='Digital-crumbs' src='http://digital-crumbs.co.uk/Content/logo.png' style='border-width: 0px; width: 160px; height: 34px;' width='160' height='34'></a></span></td></tr></tbody></table></td></tr></tbody></table><!-- ======= end header ======= --><!-- ======= start hero ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #fdac02; border-right:1px solid #fdac02; border-bottom:1px solid #fdac02; border-top:1px solid #fdac02;' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 20px 50px;background: linear-gradient(to left,#f77408, #ffbe00);' width='600' valign='top' height='80' align='center'><!--[if gte mso 9]><v:rect xmlns:v='urn:schemas-microsoft-com:vml' fill='true' stroke='false' style='width:600px;height:303px;'><v:fill type='tile' src='https://gallery.mailchimp.com/fdcaf86ecc5056741eb5cbc18/images/42ba8b72-65d6-4dea-b8ab-3ecc12676337.jpg' color='#2f9780' /><v:textbox inset='0,0,0,0'><![endif]--><div><br><br><p style='color:#ffffff; font-size:40px; text-align:center; font-family: Verdana, Geneva, sans-serif'>New user added</p></div><!--[if gte mso 9]></v:textbox></v:rect><![endif]--></td></tr></tbody></table><!-- ======= end hero  ======= --><!-- ======= start article ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #e8e7e5; border-right:1px solid #e8e7e5; border-bottom:1px solid #e8e7e5; border-top:1px solid #e8e7e5' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 10px 50px' align='center'><p style='color:#262626; font-size:16px; text-align:justify; font-family: Verdana, Geneva, sans-serif; line-height:22px '>Dear Admin,<br><br>You have successfully added a new user to your account. Your users email id is " + email + ", and password " + password + ". Your user may login using the following login page:https://digital-crumbs.com/Auth/ProLogin<br> And login as Addtional User.</p><p style='color:#262626; font-size:16px; text-align:justify; font-family: Verdana, Geneva, sans-serif; line-height:22px '></p><pre style='text-align: left'></pre><p></p></td></tr></tbody></table><!-- ======= end article ======= --><!-- ======= start footer ======= --></td></tr></tbody></table><!--[if (gte mso 9)|(IE)]></td></tr></table><![endif]--></div></td></tr></tbody></table></center></body></html>";

            var msg = new MailMessage();
            msg.To.Add(new MailAddress(prepackageEmail, prepackageEmail));
            msg.From = new MailAddress("info@digital-crumbs.com", "Digital-Crumbs");
            msg.Subject = "Your Digital-crumbs Addtional User Add successfully";

            msg.Body = emailTemplate;


            msg.IsBodyHtml = true;

            var client = new SmtpClient
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("info@digital-crumbs.com", "Ishapatel@1998"),
                Port = 587, // You can use Port 25 if 587 is blocked (mine is!)
                Host = "smtp.ionos.co.uk",
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true
            };
            client.Send(msg);
        }
    }
}