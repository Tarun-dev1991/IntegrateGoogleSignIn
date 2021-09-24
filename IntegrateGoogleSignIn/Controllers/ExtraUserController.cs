using IntegrateGoogleSignIn.Helpers;
using IntegrateGoogleSignIn.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace IntegrateGoogleSignIn.Controllers
{
    public class ExtraUserController : Controller
    {
        public ActionResult Index()
        {
            DBEntities db = new DBEntities();
            string email = Session["user"].ToString();
            TempData["Message"] = TempData["Message"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            TempData["SwalSubscribeSuccessMessage"] = TempData["SwalSubscribeSuccessMessage"];
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];

            //Timezone code
            if (Session["timezone"] != null)
            {
                var timeZoneValue = Session["timezone"].ToString();
                if (!string.IsNullOrEmpty(timeZoneValue))
                {
                    TempData["TimeZone"] = Session["timezone"].ToString();
                }
            }
            //Timezone code

            var domainDetails = new List<ExtraUserVm>();
            var domainList = db.ExtraUsers.Where(m => m.email.Equals(email)).OrderBy(m => m.Id).ToList();
            if (!domainList.Any())
            {
                domainList = new List<ExtraUser>();
            }
            else
            {
                var i = 0;
                foreach (var data in domainList)
                {
                    var domainDetail = new ExtraUserVm
                    {
                        packageId = data.Id,
                        domainname = data.domainname,
                        email = data.email,
                        name = data.name,
                        monthlyReportData = string.Empty,
                        isDefault = (i == 0)
                    };

                    var monthlyReportData = GetMonthlyVisitors(data.domainname);

                    if (monthlyReportData.Any())
                    {
                        domainDetail.totalVisitors = monthlyReportData.Sum(m => m.y);
                        domainDetail.monthlyReportData = JsonConvert.SerializeObject(monthlyReportData);
                    }

                    domainDetails.Add(domainDetail);
                    i = i + 1;
                }
            }
            return View(domainDetails);
        }

        [HttpPost]
        public ActionResult Index(string DomainName)
        {
            var db = new DBEntities();
            var userEmail = Session["user"].ToString();
            var userName = Session["userName"].ToString();
            var domainCollection = db.ExtraUsers.Where(m => m.email.Equals(userEmail)).ToList();
            if (domainCollection.FirstOrDefault(m => m.domainname.Equals(DomainName)) != null)
            {
                TempData["Message"] = "Domain already available.";
                return RedirectToAction("Index", "ExtraUser");
            }

            var subscribePlanAmount = domainCollection.Count * 9;
            var subscriberPlan = db.ExtraUserSubscriptionPlans.FirstOrDefault(m => m.planAmount == subscribePlanAmount);

            if (subscriberPlan != null)
            {
                Session["planId"] = subscriberPlan.planId;
                Session["userPackageId"] = string.Empty;
                Session["domainName"] = DomainName;
                Session["operationType"] = "Subscribe";

                var subscription = PayPalFunction.CreateBillingAgreement(subscriberPlan.planId,
                    userName, userEmail, DateTime.Now);

                return Redirect(subscription);
            }
            else
            {
                TempData["Message"] = "No subscription plan added, please contact administrator.";
            }

            return RedirectToAction("Index", "ExtraUser");
        }

        [HttpPost]
        public JsonResult ChangeTimeZone(string timezone)
        {
            try
            {
                string message;
                using (var db = new DBEntities())
                {
                    var userEmail = Session["user"].ToString();
                    var userDetails =
                        db.ExtraUsers.FirstOrDefault(m => m.email.Equals(userEmail));
                    if (userDetails != null)
                    {
                        userDetails.timezone = timezone;
                        db.SaveChanges();
                        Session["timezone"] = timezone;
                        message = "Timezone has been updated.";
                        return Json(new { status = true, Message = message }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        message = "User details not found.";
                        return Json(new { status = false, Message = message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch
            {
                return Json(new { status = false, Message = "Issue to update of timezone" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Temp()
        {
            return View();
        }

        public ActionResult Report(string domainName, string date)
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

            //Timezone code
            var userTimeZone = string.Empty;
            if (Session["timezone"] != null)
            {
                userTimeZone = Session["timezone"].ToString();
            }
            //Timezone code

            var db = new DBEntities();
            if (Session["AdminUser"] == null)
            {
                var userEmail = Session["user"].ToString();

                if (Session["SpecialUser"] == null)
                {
                    var domainAvailable =
                        db.ExtraUsers.FirstOrDefault(m =>
                            m.email.Equals(userEmail) &&
                            (m.domainname.Equals(wwwDomainName) || m.domainname.Equals(wwwWithoutDomainName)));

                    if (domainAvailable == null)
                    {
                        TempData["Message"] = "Domain accessibility failed.";
                        return RedirectToAction("Index", "ExtraUser");
                    }
                }
                else
                {
                    var domainAvailable =
                        db.specialusers.FirstOrDefault(m => m.email.Equals(userEmail));

                    if (domainAvailable == null)
                    {
                        TempData["Message"] = "Domain accessibility failed.";
                        return RedirectToAction("Index", "ExtraUser");
                    }
                    else
                    {
                        var domainList = domainAvailable.domainname.Split(',');
                        var isAvailable = false;
                        foreach (var domain in domainList)
                        {
                            if (domain.Equals(wwwDomainName) || domain.Equals(wwwWithoutDomainName))
                            {
                                isAvailable = true;
                            }
                        }

                        if (!isAvailable)
                        {
                            TempData["Message"] = "Domain accessibility failed.";
                            return RedirectToAction("Index", "ExtraUser");
                        }
                    }
                }
            }

            //todo : date parse exact
            var reportDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(date))
            {
                reportDate = Convert.ToDateTime(date);
            }

            //Timezone code
            var startDate = new DateTime(reportDate.Year, reportDate.Month, reportDate.Day, 0, 0, 0);
            var endDate = new DateTime(reportDate.Year, reportDate.Month, reportDate.Day, 23, 59, 59);
            var startDateUk = CommonFunctions.ConvertTimeZone(userTimeZone, StaticValues.TzUk, startDate);
            var endDateUk = CommonFunctions.ConvertTimeZone(userTimeZone, StaticValues.TzUk, endDate);

            var reportPageVm = new ReportPageVm
            {
                userListing = new List<UserVm>(),
                domainName = domainName,
                reportCurrentDate = reportDate
            };

            var userDetailsCollection = db.UserDetails
                .Where(m => (m.User.DomainName.Equals(wwwDomainName) || m.User.DomainName.Equals(wwwWithoutDomainName)) && m.SessionDateTime.HasValue &&
                            (m.SessionDateTime.Value >= startDateUk && m.SessionDateTime.Value <= endDateUk))
                .OrderBy(m => m.UserId).ThenBy(k => k.UserSessionId).ToList();

            //Timezone code
            foreach (var item in userDetailsCollection)
            {
                if (item.SessionDateTime != null)
                {
                    item.SessionDateTime =
                        CommonFunctions.ConvertTimeZone(StaticValues.TzUk, userTimeZone, item.SessionDateTime.Value);
                }
            }
            //Timezone code

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
                    userInteractions = new List<EarlierInteractionVm>()
                };

                var totalDuration = 0;
                foreach (var earlierCollection in userCol.userInteractions)
                {
                    var earlierVisitSessions = new List<EarlierUserInteractionVm>();
                    if (earlierCollection.Visits.Count <= 1)
                    {
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

            return View(reportPageVm);
        }

        public ActionResult ChangePassword()
        {
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(string oldPassword, string newPassword)
        {
            try
            {
                var db = new DBEntities();
                if (!string.IsNullOrEmpty(oldPassword))
                {
                    var email = Session["user"].ToString();
                    var userDetails = db.ExtraUsers.FirstOrDefault(m => m.email.Equals(email));
                    if (userDetails != null)
                    {
                        if (userDetails.password.Equals(oldPassword))
                        {
                            userDetails.password = newPassword;
                            db.SaveChanges();
                            TempData["SwalSuccessMessage"] = "Password has been change successfully";
                            return RedirectToAction("Index", "ExtraUser");
                        }
                        else
                        {
                            TempData["SwalErrorMessage"] = "Old password does not match! Please try other";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.InnerException;
            }
            return View();
        }

        [HttpGet]
        public ActionResult Dcscript(long id)
        {
            try
            {
                var db = new DBEntities();
                var scriptDetails = db.ExtraUsers.Find(id);
                return View(scriptDetails);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public ActionResult SuccessPaypal()
        {
            TempData["SwalSubscribeSuccessMessage"] = "Please add our tracking code to your website. You can access this code via your dashboard. Please note it make take up to an hour before data appears in your report.";
            return View();
        }

        #region --> Paypal Normal Plan

        public ActionResult SubscribeDashboardSuccess(string token)
        {
            var type = Session["operationType"].ToString();
            var packageId = Session["userPackageId"].ToString();
            var planId = Session["planId"].ToString();
            var userEmail = Session["user"].ToString();

            if (type == "Subscribe")
            {
                var domainName = Session["domainName"].ToString();
                ExecuteBillingAgreementDashboard(token, userEmail, planId, domainName, type, packageId);
                //TempData["SwalSuccessMessage"] = "Thank you for subscription.";
            }
            else
            {
                var domainName = Session["domainName"].ToString();
                ExecuteBillingAgreementDashboard(token, userEmail, planId, string.Empty, type, packageId);
                var db = new DBEntities();
                var specialUsers = db.specialusers
                    .Where(m => m.propackageEmail.Equals(userEmail) && m.domainname.Contains(domainName) && m.isExtra).ToList();
                if (specialUsers.Any())
                {
                    foreach (var singleUser in specialUsers)
                    {
                        var domainCollection = singleUser.domainname.Split(',');
                        if (domainCollection.Length == 1)
                        {
                            db.specialusers.Remove(singleUser);
                        }
                        else
                        {
                            var newCollection = domainCollection.Where(val => !val.Equals(domainName)).ToArray();
                            singleUser.domainname = string.Join(",", newCollection);
                        }
                        db.SaveChanges();
                    }
                }
                TempData["SwalSubscribeSuccessMessage"] = "Plan unsubscribe successfully.";
                return RedirectToAction("Index", "ExtraUser");
            }

            return RedirectToAction("SuccessPaypal", "ExtraUser");
        }

        public static void ExecuteBillingAgreementDashboard(string token, string userEmail, string planId, string domainName, string type, string packageId)
        {
            using (var db = new DBEntities())
            {
                CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement] ==>  " + token);

                var xData = PayPalFunction.ExecuteAgreement(token);

                if (xData != null)
                {
                    if (type == "Subscribe")
                    {
                        var unSubscribePlan = db.ExtraUserAgreementCollections.FirstOrDefault(m => !m.isSpecialPlan && m.email.Equals(userEmail));
                        if (unSubscribePlan != null)
                        {
                            PayPalFunction.CancelBillingAgreement(unSubscribePlan.agreementId);
                            unSubscribePlan.planId = planId;
                            unSubscribePlan.agreementId = xData.id;
                        }
                        else
                        {
                            var subscribeCollection = new ExtraUserAgreementCollection
                            {
                                email = userEmail,
                                agreementId = xData.id,
                                isSpecialPlan = false,
                                planId = planId
                            };
                            db.ExtraUserAgreementCollections.Add(subscribeCollection);
                        }

                        //Get UserDetails
                        var userDetails = db.ExtraUsers.FirstOrDefault(m => m.email.Equals(userEmail));
                        if (userDetails != null)
                        {
                            //Add new domain
                            var newDomainUser = new ExtraUser
                            {
                                domainname = domainName,
                                password = userDetails.password,
                                email = userDetails.email,
                                name = userDetails.name,
                                mobile_no = userDetails.mobile_no
                            };
                            db.ExtraUsers.Add(newDomainUser);
                        }

                        db.SaveChanges();

                        if (!string.IsNullOrEmpty(userEmail))
                        {
                            CommonFunctions.SendMail(userEmail, true);
                        }

                    }
                    else
                    {
                        var unSubscribePlan = db.ExtraUserAgreementCollections.FirstOrDefault(m => !m.isSpecialPlan && m.email.Equals(userEmail));
                        if (unSubscribePlan != null)
                        {
                            PayPalFunction.CancelBillingAgreement(unSubscribePlan.agreementId);
                            unSubscribePlan.agreementId = xData.id;
                            unSubscribePlan.planId = planId;
                        }

                        //Remove existing user domain
                        var userPackageId = Convert.ToInt64(packageId);
                        var userDetails = db.ExtraUsers.Find(userPackageId);
                        if (userDetails != null)
                        {
                            db.ExtraUsers.Remove(userDetails);
                        }

                        db.SaveChanges();
                    }

                }
            }

        }

        public ActionResult SubscribeDashboardCancel(string token)
        {
            TempData["SwalErrorMessage"] = "Paypal subscription failed or canceled.";
            return RedirectToAction("Index", "ExtraUser");
        }

        public ActionResult Unsubscribe(long id, string domain)
        {
            try
            {
                var db = new DBEntities();
                Session["domainName"] = domain;
                Session["operationType"] = "UnSubscribe";
                Session["userPackageId"] = id;
                var userEmail = Session["user"].ToString();
                var userName = Session["userName"].ToString();
                var domainCollection = db.ExtraUsers.Where(m => m.email.Equals(userEmail)).ToList();
                if (domainCollection.Count > 2)
                {
                    var subscribePlanAmount = (domainCollection.Count - 2) * 9;
                    var subscriberPlan = db.ExtraUserSubscriptionPlans.FirstOrDefault(m => m.planAmount == subscribePlanAmount);
                    if (subscriberPlan != null)
                    {
                        Session["planId"] = subscriberPlan.planId;
                        var subscription = PayPalFunction.CreateBillingAgreement(subscriberPlan.planId,
                            userName, userEmail, DateTime.Now);

                        return Redirect(subscription);
                    }
                }
                else
                {
                    var unSubscribePlan = db.ExtraUserAgreementCollections.FirstOrDefault(m => !m.isSpecialPlan && m.email.Equals(userEmail));
                    if (unSubscribePlan != null)
                    {
                        PayPalFunction.CancelBillingAgreement(unSubscribePlan.agreementId);
                        db.ExtraUserAgreementCollections.Remove(unSubscribePlan);
                    }

                    var specialUsers = db.specialusers
                        .Where(m => m.propackageEmail.Equals(userEmail) && m.isExtra).ToList();
                    if (specialUsers.Any())
                    {
                        db.specialusers.RemoveRange(specialUsers);
                    }

                    var extraUserDetails =
                        db.ExtraUsers.FirstOrDefault(m => m.email.Equals(userEmail) && m.domainname.Equals(domain));
                    if (extraUserDetails != null)
                    {
                        db.ExtraUsers.Remove(extraUserDetails);
                    }

                    db.SaveChanges();

                    TempData["SwalSuccessMessage"] = "Plan unsubscribe successfully.";
                    return RedirectToAction("Index", "ExtraUser");
                }
            }
            catch (Exception e)
            {
                TempData["Message"] = e.Message;
            }

            return RedirectToAction("Index", "ExtraUser");
        }

        #endregion

        #region --> Search Functionality

        public JsonResult GetColorList(string v)
        {
            var db = new DBEntities();
            var blueList = db.searchdomainMasters.Where(m => m.searcgclass == "blue" && m.domainname == v).ToList();
            var redList = db.searchdomainMasters.Where(m => m.searcgclass == "red" && m.domainname == v).ToList();
            var greenList = db.searchdomainMasters.Where(m => m.searcgclass == "green" && m.domainname == v).ToList();
            return Json(new { blueList, redList, greenList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RedListSearchExists(string v, string searchText)
        {
            var db = new DBEntities();
            var data = db.searchdomainMasters.FirstOrDefault(m => m.searcgclass == "red" && m.domainname == v && m.searchtext == searchText);
            return Json(data != null ? 1 : 0, JsonRequestBehavior.AllowGet);
        }

        public JsonResult BlueListSearchExists(string v, string searchText)
        {
            var db = new DBEntities();
            var data = db.searchdomainMasters.FirstOrDefault(m => m.searcgclass == "blue" && m.domainname == v && m.searchtext == searchText);
            return Json(data != null ? 1 : 0, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GreenListSearchExists(string v, string searchText)
        {
            var db = new DBEntities();
            var data = db.searchdomainMasters.FirstOrDefault(m => m.searcgclass == "green" && m.domainname == v && m.searchtext == searchText);
            return Json(data != null ? 1 : 0, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public void AddRed(string st, string v)
        {
            var db = new DBEntities();
            var searchMastersVm = new searchdomainMaster
            {
                searchtext = st,
                domainname = v,
                searcgclass = "red"
            };
            db.searchdomainMasters.Add(searchMastersVm);
            db.SaveChanges();
        }

        [HttpPost]
        public void AddGreen(string st, string v)
        {
            var db = new DBEntities();
            var searchMastersVm = new searchdomainMaster
            {
                searchtext = st,
                domainname = v,
                searcgclass = "green"
            };
            db.searchdomainMasters.Add(searchMastersVm);
            db.SaveChanges();
        }

        [HttpPost]
        public void AddBlue(string st, string v)
        {
            var db = new DBEntities();
            var searchMastersVm = new searchdomainMaster
            {
                searchtext = st,
                domainname = v,
                searcgclass = "blue"
            };
            db.searchdomainMasters.Add(searchMastersVm);
            db.SaveChanges();
        }

        public void DeleteIt(int sid)
        {
            var db = new DBEntities();
            var searchMastersVm = db.searchdomainMasters.Find(sid);
            if (searchMastersVm != null)
            {
                db.searchdomainMasters.Remove(searchMastersVm);
                db.SaveChanges();
            }
        }

        #endregion

        #region --> Chart Functionality

        public List<ReportVm> GetMonthlyVisitors(string domain)
        {
            var reportVm = new List<ReportVm>();
            using (var db = new DBEntities())
            {

                string wwwDomainName;
                string wwwWithoutDomainName;
                if (domain.Contains("www."))
                {
                    wwwDomainName = domain;
                    wwwWithoutDomainName = domain.Replace("www.", string.Empty);
                }
                else
                {
                    wwwDomainName = "www." + domain;
                    wwwWithoutDomainName = domain;
                }

                //Timezone code
                var userTimeZone = string.Empty;
                if (Session["timezone"] != null)
                {
                    userTimeZone = Session["timezone"].ToString();
                }
                //Timezone code

                //Timezone code

                var endDate = DateTime.UtcNow;
                var startDate = DateTime.UtcNow.AddDays(-31);
                var startDateUk = CommonFunctions.ConvertTimeZone(userTimeZone, StaticValues.TzUk, startDate);
                var endDateUk = CommonFunctions.ConvertTimeZone(userTimeZone, StaticValues.TzUk, endDate);

                var query =
                    "SELECT distinct UserSession.UserId as userid, CONVERT(varchar, UserSession.SessionVistTime, 112) AS sessionDate FROM Users INNER JOIN UserSession ON Users.Id = UserSession.UserId and (Users.DomainName='" +
                    wwwDomainName + "' or Users.DomainName='" +
                    wwwWithoutDomainName + "') and UserSession.SessionVistTime between '" + startDateUk.Year + "/" +
                    startDateUk.Month + "/" + startDateUk.Day + "' and '" + endDateUk.Year + "/" + endDateUk.Month + "/" +
                    endDateUk.Day +
                    "' order by UserSession.UserId";

                var userSessionDetails = db.Database.SqlQuery<ReportVm>(query).ToList();

                for (var date = startDateUk; date.Date <= endDateUk.Date; date = date.AddDays(1))
                {
                    var cDate = date.ToString("yyyyMMdd");
                    var visitorCount = userSessionDetails.Count(m => m.sessionDate.Equals(cDate));
                    var startDateTime = new DateTime(date.Year, date.Month, date.Day);
                    var endDateTime = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
                    var startDateInnerUk = CommonFunctions.ConvertTimeZone(userTimeZone, StaticValues.TzUk, startDateTime);
                    var endDateInnerUk = CommonFunctions.ConvertTimeZone(userTimeZone, StaticValues.TzUk, endDateTime);

                    var dbUserParalytics =
                        db.UserDailyAnalytics.FirstOrDefault(m =>
                            (m.domainname.Equals(wwwWithoutDomainName) || m.domainname.Equals(wwwDomainName)) &&
                            m.collectiondate >= startDateInnerUk && m.collectiondate <= endDateInnerUk);

                    if (dbUserParalytics != null)
                    {
                        var multipageuser = dbUserParalytics.multivisitors;
                        var redbrick = dbUserParalytics.redtickvisitors;
                        var disagreement = dbUserParalytics.greentickvisitors;
                        var bluejacket = dbUserParalytics.bluetickvisitors;
                        reportVm.Add(new ReportVm
                        {
                            x = date.ToString("dd MMM"),
                            xDate = date,
                            y = visitorCount,
                            bluetik = bluejacket,
                            greentik = disagreement,
                            redtik = redbrick,
                            multipage = multipageuser
                        });
                    }
                    else
                    {

                        reportVm.Add(new ReportVm
                        {
                            x = date.ToString("dd MMM"),
                            xDate = date,
                            y = visitorCount,
                            bluetik = 0,
                            greentik = 0,
                            redtik = 0,
                            multipage = 0
                        });
                    }
                }
            }
            return reportVm;
        }

        #endregion

        #region --> Special Users

        public ActionResult SpecialUsers()
        {
            List<specialuser> specialUsersList;
            try
            {
                TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
                TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
                var db = new DBEntities();
                string email = Session["user"].ToString();
                specialUsersList = db.specialusers.Where(m => m.propackageEmail.Equals(email)).ToList();
                if (!specialUsersList.Any())
                {
                    specialUsersList = new List<specialuser>();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return View(specialUsersList);
        }

        public ActionResult AddSpecialUsers()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddSpecialUsers(string email, string username, string[] domain, string password)
        {
            try
            {
                string prepackageEmail = Session["user"].ToString();
                var db = new DBEntities();
                var domainCollection = string.Join(",", domain);
                var specialUser = new specialuser
                {
                    email = email,
                    domainname = domainCollection,
                    name = username,
                    password = password,
                    propackageEmail = prepackageEmail,
                    isExtra = true
                };
                db.specialusers.Add(specialUser);
                db.SaveChanges();
                CommonFunctions.SendMail1(prepackageEmail, true, email, true, password, true);

                TempData["SwalSuccessMessage"] = "User details added successfully.";
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }

            return RedirectToAction("SpecialUsers");
        }

        public ActionResult UpdateSpecialUsers(long id)
        {
            var db = new DBEntities();
            var specialUser = db.specialusers.Find(id);
            if (specialUser != null)
            {
                return View(specialUser);
            }
            else
            {
                TempData["SwalErrorMessage"] = "User details not found.";
                return RedirectToAction("SpecialUsers");
            }
        }

        [HttpPost]
        public ActionResult UpdateSpecialUsers(long id, string email, string username, string[] domain)
        {
            try
            {
                var db = new DBEntities();
                var specialUser = db.specialusers.Find(id);
                var domainCollection = string.Join(",", domain);
                if (specialUser != null)
                {
                    specialUser.domainname = domainCollection;
                    specialUser.name = username;
                    specialUser.email = email;
                    specialUser.isExtra = true;
                    db.SaveChanges();
                    TempData["SwalSuccessMessage"] = "User details updated successfully.";
                }
                else
                {
                    TempData["SwalErrorMessage"] = "User details not found.";
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }

            return RedirectToAction("SpecialUsers");
        }

        public ActionResult DeleteSpecialUsers(long id)
        {
            try
            {
                var db = new DBEntities();
                var specialUser = db.specialusers.Find(id);
                if (specialUser != null)
                {
                    db.specialusers.Remove(specialUser);
                    db.SaveChanges();
                    TempData["SwalSuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    TempData["SwalErrorMessage"] = "User details not found.";
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }

            return RedirectToAction("SpecialUsers");
        }

        #endregion
    }
}