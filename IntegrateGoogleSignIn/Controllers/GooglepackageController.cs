using IntegrateGoogleSignIn.Helpers;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using IntegrateGoogleSignIn.Models;

namespace IntegrateGoogleSignIn.Controllers
{
    public class GooglepackageController : Controller
    {
        #region --> Google Subscription

        [HttpPost]
        public ActionResult SubscribeGooglePlan(List<string> googleIds)
        {
            try
            {
                if (googleIds.Any())
                {
                    var db = new DBEntities();
                    var userEmail = Session["Email"].ToString();
                    Session["isDirect"] = "Google";
                    var getSubscribedDetails = PayPalFunction.GetSubscribeStatus(userEmail);
                    if (getSubscribedDetails == "true")
                    {
                        var googleUsers = db.GoogleUsers.ToList();
                        var googleIdCollection = new List<string>();
                        foreach (var item in googleIds)
                        {
                            var isExists =
                                googleUsers.FirstOrDefault(m => m.websiteId == item && m.email.Equals(userEmail));
                            if (isExists == null)
                            {
                                googleIdCollection.Add(item);
                            }
                        }

                        if (googleIdCollection.Any())
                        {
                            Session["websiteIds"] = string.Join(",", googleIdCollection);

                            var previousCount = googleUsers.Count();
                            var currentCount = googleIdCollection.Count();

                            //Subscribe New Plan
                            var subscribePlan = db.GoogleSubscribePlans.FirstOrDefault(m =>
                                m.planAmount == ((previousCount + currentCount) * 19));

                            if (subscribePlan != null)
                            {
                                Session["operationType"] = "Subscribe";
                                Session["planId"] = subscribePlan.planId;
                                var subscription = PayPalFunction.CreateBillingAgreement(subscribePlan.planId,
                                    userEmail, userEmail, DateTime.Now);

                                return Redirect(subscription);
                            }
                            else
                            {
                                TempData["Message"] = "Subscription plan not found, Please contact administrator";
                            }
                        }
                        else
                        {
                            TempData["Message"] = "Selected entry already subscribed.";
                        }
                    }
                    else
                    {
                        var googleId = googleIds.FirstOrDefault();
                        if (!string.IsNullOrEmpty(googleId))
                        {
                            Session["websiteIds"] = googleId;
                            Session["operationType"] = "Subscribe";

                            //Subscribe Zero Plan
                            var subscribePlan = db.GoogleSubscribePlans.FirstOrDefault(m => m.planAmount == 0);
                            if (subscribePlan != null)
                            {
                                Session["planId"] = subscribePlan.planId;

                                var subscription = PayPalFunction.CreateBillingAgreement(subscribePlan.planId,
                                    userEmail, userEmail, DateTime.Now);

                                return Redirect(subscription);
                            }
                            else
                            {
                                TempData["Message"] = "Subscription plan not found.";
                            }
                        }
                        else
                        {
                            TempData["Message"] = "All selected entries already subscribed.";
                        }
                    }

                }
            }
            catch (Exception e)
            {
                TempData["Message"] = e.Message;
            }
            return RedirectToAction("Index", "Dashboard");
        }

        public ActionResult UnsubscribeGooglePlan(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var db = new DBEntities();
                    var userEmail = Session["Email"].ToString();
                    Session["operationType"] = "UnSubscribe";
                    Session["websiteIds"] = id;
                    var googleUserCollection = db.GoogleUsers.ToList();

                    if (googleUserCollection.Count == 1)
                    {
                        var cancelGoogleAgreementCollections =
                            db.GoogleAgreementCollections.FirstOrDefault(m => m.email.Equals(userEmail));
                        if (cancelGoogleAgreementCollections != null)
                        {
                            PayPalFunction.CancelBillingAgreement(cancelGoogleAgreementCollections.agreementId);
                            db.GoogleAgreementCollections.Remove(cancelGoogleAgreementCollections);

                            db.GoogleUsers.RemoveRange(googleUserCollection);
                            db.SaveChanges();

                            TempData["Message"] = "Plan unSubscribe successfully.";
                        }
                    }
                    else
                    {
                        var planAmount = (googleUserCollection.Count() - 1) * 19;
                        var subscribePlan = db.GoogleSubscribePlans.FirstOrDefault(m => m.planAmount == planAmount);
                        Session["planId"] = subscribePlan.planId;
                        var subscription = PayPalFunction.CreateBillingAgreement(subscribePlan.planId,
                            userEmail, userEmail, DateTime.Now);

                        return Redirect(subscription);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return RedirectToAction("Index", "Dashboard");
        }

        public ActionResult SuspendGooglePlan(string id, string userEmail)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var db = new DBEntities();
                    var googleUserCollection = db.GoogleUsers.Where(m => m.email.Equals(userEmail)).ToList();
                    if (googleUserCollection.Any())
                    {
                        db.GoogleUsers.RemoveRange(googleUserCollection);
                    }

                    var agreementDetails = db.GoogleAgreementCollections.FirstOrDefault(m => m.email.Equals(userEmail));
                    if (agreementDetails != null)
                    {
                        PayPalFunction.CancelBillingAgreement(agreementDetails.agreementId);
                        db.GoogleAgreementCollections.Remove(agreementDetails);
                    }

                    if (googleUserCollection.Any())
                    {
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            TempData["Message"] = "Plan suspended successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        #endregion

        #region --> Google Script

        public ActionResult PackageUsers()
        {
            var db = new DBEntities();
            var prepackageUsers = db.propackageusers.Select(m => m.email).Distinct().ToList();
            if (!prepackageUsers.Any())
            {
                prepackageUsers = new List<string>();
            }

            return View(prepackageUsers);
        }

        public ActionResult GoogleScript(string email)
        {
            var db = new DBEntities();
            var prepackageUsers = db.propackageusers.Where(m => m.email.Equals(email)).ToList();
            if (!prepackageUsers.Any())
            {
                prepackageUsers = new List<propackageuser>();
            }
            return View(prepackageUsers);
        }

      
      

        #endregion

        #region --> Google Plan

        [AllowAnonymous]
        public ActionResult SuccessPaypal()
        {
            return View();
        }

        public ActionResult SubscribeGooglePlanSuccess(string token)
        {
            var websiteIds = Session["websiteIds"].ToString();
            var type = Session["operationType"].ToString();
            var planId = Session["planId"].ToString();
            var userEmail = Session["email"].ToString();
            //if (type == "Subscribe")
            //{
            //    TempData["SwalSuccessMessage"] = "Thank you for subscription.";
            //}
            ExecuteBillingGoogleAgreementDirect(token, userEmail, planId, websiteIds, type);
            return RedirectToAction("SuccessPaypal", "Googlepackage");
        }

        public static void ExecuteBillingGoogleAgreement(string token, string userEmail, string planId, string websiteId)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement] ==>  " + token);

                    var xData = PayPalFunction.ExecuteAgreement(token);

                    if (xData != null)
                    {
                        #region --> Database interactions

                        //Add new google user for plan
                        var googleUser = new GoogleUser
                        {
                            email = userEmail,
                            websiteId = websiteId
                        };
                        db.GoogleUsers.Add(googleUser);

                        //Add New Google Package Agreement
                        var googleAgreementCollection = new GoogleAgreementCollection
                        {
                            planId = planId,
                            agreementId = xData.id,
                            email = userEmail
                        };
                        db.GoogleAgreementCollections.Add(googleAgreementCollection);
                        db.SaveChanges();

                        #endregion
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void ExecuteBillingGoogleAgreementDirect(string token, string userEmail, string planId, string websiteIds, string type)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement] ==>  " + token);

                    var xData = PayPalFunction.ExecuteAgreement(token);

                    if (xData != null)
                    {
                        if (type == "Subscribe")
                        {
                            #region --> Paypal Interaction + Database interactions

                            var defaultFreePlan = db.GoogleSubscribePlans.FirstOrDefault(m => m.planAmount == 0);
                            if (defaultFreePlan != null && defaultFreePlan.planId == planId)
                            {
                                var loginHistoryObj = db.loginhistories.FirstOrDefault(m => m.username.Equals(userEmail));
                                if (loginHistoryObj != null) loginHistoryObj.hasSubscribed = true;
                            }

                            //Unsubscribe Plan
                            var unSubscribeAgreementDetails = db.GoogleAgreementCollections.FirstOrDefault(m => m.email.Equals(userEmail));
                            if (unSubscribeAgreementDetails != null)
                            {
                                PayPalFunction.SuspendBillingAgreement(unSubscribeAgreementDetails.agreementId);
                                unSubscribeAgreementDetails.planId = planId;
                                unSubscribeAgreementDetails.agreementId = xData.id;
                            }
                            else
                            {
                                //Add New Google Package Agreement
                                var googleAgreementCollection = new GoogleAgreementCollection
                                {
                                    planId = planId,
                                    agreementId = xData.id,
                                    email = userEmail
                                };
                                db.GoogleAgreementCollections.Add(googleAgreementCollection);
                            }

                            var websiteIdList = websiteIds.Split(',').Select(m => m.Trim()).ToList();
                            var googleUserList = new List<GoogleUser>();
                            if (websiteIdList.Any())
                            {
                                foreach (var websiteId in websiteIdList)
                                {
                                    //Add new google user for plan
                                    var googleUser = new GoogleUser
                                    {
                                        email = userEmail,
                                        websiteId = websiteId
                                    };
                                    googleUserList.Add(googleUser);
                                }

                                if (googleUserList.Any())
                                {
                                    db.GoogleUsers.AddRange(googleUserList);
                                }
                            }

                            db.SaveChanges();

                            #endregion
                        }
                        else
                        {
                            var googleAgreementDetails =
                                db.GoogleAgreementCollections.FirstOrDefault(m => m.email.Equals(userEmail));
                            if (googleAgreementDetails != null)
                            {
                                PayPalFunction.CancelBillingAgreement(googleAgreementDetails.agreementId);
                                googleAgreementDetails.planId = planId;
                                googleAgreementDetails.agreementId = xData.id;

                                if (!string.IsNullOrEmpty(websiteIds))
                                {
                                    var googleUserId = Convert.ToInt32(websiteIds);
                                    var googleUserDetails = db.GoogleUsers.Find(googleUserId);
                                    if (googleUserDetails != null)
                                    {
                                        db.GoogleUsers.Remove(googleUserDetails);
                                    }

                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public ActionResult SubscribeGooglePlanCancel(string token)
        {
            TempData["SwalErrorMessage"] = "Paypal subscription failed or canceled.";
            return RedirectToAction("index", "Dashboard");
        }

        #endregion
    }
}