using IntegrateGoogleSignIn.Helpers;
using IntegrateGoogleSignIn.Models;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Plan = PayPal.Api.Plan;

namespace IntegrateGoogleSignIn.Controllers
{
    [AllowAnonymous]
    public class PaypalController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                var db = new DBEntities();
                var subscriptionPlans = db.ProSubscriptionPlans.Select(m => new PaypalSubscriptionVm
                {
                    planId = m.planId,
                    planAmount = m.planAmount.ToString(),
                    planTitle = m.planTitle,
                    isSpecialPlan = m.isSpecialPlan
                }).ToList();

                var proAgreementCollections = db.ProAgreementCollections.ToList();
                foreach (var plan in subscriptionPlans)
                {
                    plan.users = new List<string>();
                    if (!plan.isSpecialPlan)
                    {
                        var userList = proAgreementCollections.Where(m => !m.isSpecialPlan && m.planId == plan.planId).Select(m => m.email).ToList();
                        if (userList.Any())
                        {
                            plan.users = userList;
                        }
                    }
                    else
                    {
                        plan.planAmount = (Convert.ToInt64(plan.planAmount) + 14).ToString();
                        var userList = proAgreementCollections.Where(m => m.isSpecialPlan && m.planId == plan.planId).Select(m => m.email).ToList();
                        if (userList.Any())
                        {
                            plan.users = userList;
                        }
                    }
                }

                var subscriptionGooglePlans = db.GoogleSubscribePlans.Select(m => new PaypalSubscriptionVm
                {
                    planId = m.planId,
                    planAmount = m.planAmount.ToString(),
                    planTitle = m.planTitle,
                    isGoogle = true
                }).ToList();

                var googleAgreementCollections = db.GoogleAgreementCollections.ToList();
                foreach (var googlePlans in subscriptionGooglePlans)
                {
                    googlePlans.users = new List<string>();
                    var userList = googleAgreementCollections.Where(m => m.planId == googlePlans.planId)
                        .Select(m => m.email).ToList();

                    if (userList.Any())
                    {
                        googlePlans.users = userList;
                    }
                }

                if (subscriptionGooglePlans.Any())
                {
                    subscriptionPlans.AddRange(subscriptionGooglePlans);
                }

                return View(subscriptionPlans);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public ActionResult UserInformation()
        {
            var userInformationVm = new List<UserInformationVm>();
            try
            {
                var db = new DBEntities();
                var propackageusers = db.propackageusers.ToList();
                var proAgreementCollections = db.ProAgreementCollections.ToList();
                var proSubscriptionPlans = db.ProSubscriptionPlans.ToList();
                foreach (var propackageuser in propackageusers)
                {
                    var isExist = userInformationVm.FirstOrDefault(m => m.email.Equals(propackageuser.email));
                    if (isExist == null)
                    {
                        userInformationVm.Add(new UserInformationVm
                        {
                            email = propackageuser.email,
                            domainList = new List<string>
                            {
                                propackageuser.domainname
                            }
                        });
                    }
                    else
                    {
                        isExist.domainList.Add(propackageuser.domainname);
                    }
                }

                foreach (var userCollection in userInformationVm)
                {
                    var isPaid = proAgreementCollections.FirstOrDefault(m => m.email.Equals(userCollection.email));
                    if (isPaid == null)
                    {
                        userCollection.price = "Not Paying";
                    }
                    else
                    {
                        var planDetails = proSubscriptionPlans.FirstOrDefault(m => m.planId.Equals(isPaid.planId));
                        if (planDetails != null && planDetails.planAmount == 0)
                        {
                            userCollection.price = "Paying - £14";
                        }
                        else
                        {
                            if (planDetails != null)
                            {
                                userCollection.price = "Paying - £" + planDetails.planAmount;
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
            return View(userInformationVm);
        }

        #region --> PayPal Functions For Pro Packahge User

        public ActionResult ResetAll()
        {
            var apiContext = PaypalConfiguration.GetAPIContext();
            var allList = PayPalFunction.GetPlanList();
            if (allList?.plans != null && allList.plans.Any())
            {
                foreach (var plan in allList.plans)
                {
                    PayPal.Api.Plan.Delete(apiContext, plan.id);
                }
            }

            var db = new DBEntities();

            var proSubscriptionPlanCollection = db.ProSubscriptionPlans.ToList();
            db.ProSubscriptionPlans.RemoveRange(proSubscriptionPlanCollection);

            var userCollection = db.propackageusers.ToList();
            db.propackageusers.RemoveRange(userCollection);

            var proAgreementCollections = db.ProAgreementCollections.ToList();
            db.ProAgreementCollections.RemoveRange(proAgreementCollections);

            var prepackage = new propackageuser
            {
                password = "Password123",
                name = "tapan",
                email = "tdpatel43@outlook.com",
                domainname = "www.phileasfoggsworldofadventures.co.uk",
                mobile_no = string.Empty
            };
            db.propackageusers.Add(prepackage);
            db.SaveChanges();

            var prepackagePlans = new int[] { 0, 14, 18, 22, 26, 30, 34, 38, 42, 46, 50, 54, 58 };
            foreach (var plan in prepackagePlans)
            {
                CreateCustomBillingPlan(plan + "£ plan", plan + "£ plan", plan.ToString());
            }

            return RedirectToAction("Index", "Home");
        }

        public void CreateCustomBillingPlan(string title, string description, string amount)
        {
            try
            {
                var exactUrl = System.Web.HttpContext.Current.Request.Url;
                var customUr = exactUrl.Scheme + "://" + exactUrl.Authority + "/";
                var db = new DBEntities();
                var planExists = db.ProSubscriptionPlans.FirstOrDefault();

                var returnUrl = customUr + "ProPackage/SubscribeDashboardSuccess";
                var cancelUrl = customUr + "ProPackage/SubscribeDashboardCancel";

                if (planExists == null)
                {
                    returnUrl = customUr + "ProPackage/SubscribeSuccess";
                    cancelUrl = customUr + "ProPackage/SubscribeCancel";
                }

                Plan billingPlan;
                if (amount == "0")
                {
                    billingPlan = new PayPal.Api.Plan
                    {
                        name = title,
                        description = description,
                        type = "infinite",
                        merchant_preferences = new MerchantPreferences()
                        {
                            setup_fee = PayPalFunction.GetCurrency("0"),
                            return_url = returnUrl, // Retrieve from config
                            cancel_url = cancelUrl, // Retrieve from config
                            auto_bill_amount = "YES",
                            initial_fail_amount_action = "CONTINUE",
                            max_fail_attempts = "0"
                        },
                        payment_definitions = new List<PaymentDefinition>
                        {
                            new PaymentDefinition()
                            {
                                name = "Trial Plan",
                                type = "TRIAL",
                                frequency = "WEEK",
                                frequency_interval = "2",
                                amount = PayPalFunction.GetCurrency("0"), // Free for the 1st month
                                cycles = "1",
                                charge_models = new List<ChargeModel>
                                {
                                    new ChargeModel()
                                    {
                                        type = "TAX",
                                        amount = PayPalFunction.GetCurrency("0") // If we need to charge Tax
                                    },
                                    new ChargeModel()
                                    {
                                        type = "SHIPPING",
                                        amount = PayPalFunction.GetCurrency("0") // If we need to charge for Shipping
                                    }
                                }
                            },
                            new PaymentDefinition
                            {
                                name = "Standard Plan",
                                type = "REGULAR",
                                frequency = "MONTH",
                                frequency_interval = "1",
                                amount = PayPalFunction.GetCurrency("14"),
                                cycles = "0",
                                charge_models = new List<ChargeModel>
                                {
                                    new ChargeModel
                                    {
                                        type = "TAX",
                                        amount = PayPalFunction.GetCurrency("0")
                                    },
                                    new ChargeModel()
                                    {
                                        type = "SHIPPING",
                                        amount = PayPalFunction.GetCurrency("0")
                                    }
                                }
                            }
                        }
                    };
                }
                else
                {
                    billingPlan = new PayPal.Api.Plan
                    {
                        name = title,
                        description = description,
                        type = "infinite",
                        merchant_preferences = new MerchantPreferences()
                        {
                            setup_fee = PayPalFunction.GetCurrency("0"),
                            return_url = returnUrl, // Retrieve from config
                            cancel_url = cancelUrl, // Retrieve from config
                            auto_bill_amount = "YES",
                            initial_fail_amount_action = "CONTINUE",
                            max_fail_attempts = "0"
                        },
                        payment_definitions = new List<PaymentDefinition>
                        {
                            new PaymentDefinition
                            {
                                name = "Standard Plan",
                                type = "REGULAR",
                                frequency = "MONTH",
                                frequency_interval = "1",
                                amount = PayPalFunction.GetCurrency(amount),
                                cycles = "0",
                                charge_models = new List<ChargeModel>
                                {
                                    new ChargeModel
                                    {
                                        type = "TAX",
                                        amount = PayPalFunction.GetCurrency("0")
                                    },
                                    new ChargeModel()
                                    {
                                        type = "SHIPPING",
                                        amount = PayPalFunction.GetCurrency("0")
                                    }
                                }
                            }
                        }
                    };
                }

                // Get PayPal Config
                var apiContext = PaypalConfiguration.GetAPIContext();

                // Create Plan
                var plan = billingPlan.Create(apiContext);

                var proSubscriptionPlans = new ProSubscriptionPlan
                {
                    planId = plan.id,
                    planTitle = title,
                    planDesription = description,
                    planAmount = Convert.ToInt16(amount),
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                };
                db.ProSubscriptionPlans.Add(proSubscriptionPlans);
                db.SaveChanges();

                //Active Newly Created Plan
                PayPalFunction.UpdateBillingPlan(plan.id, "/", new PayPal.Api.Plan { state = "ACTIVE" });

                TempData["message"] = "subscription added successfully";
            }
            catch (Exception e)
            {
                TempData["message"] = e.Message;
            }
        }

        #endregion

        #region --> PayPal Functions For Extra User

        public ActionResult ResetAllExtraUsers()
        {
            var db = new DBEntities();

            var extraSubscriptionPlanCollection = db.ExtraUserSubscriptionPlans.ToList();
            db.ExtraUserSubscriptionPlans.RemoveRange(extraSubscriptionPlanCollection);

            var userCollection = db.ExtraUsers.ToList();
            db.ExtraUsers.RemoveRange(userCollection);

            var extraAgreementCollections = db.ExtraUserAgreementCollections.ToList();
            db.ExtraUserAgreementCollections.RemoveRange(extraAgreementCollections);

            var prepackage = new ExtraUser
            {
                password = "Password123",
                name = "tapan",
                email = "tdpatel43@outlook.com",
                domainname = "www.phileasfoggsworldofadventures.co.uk",
                mobile_no = string.Empty
            };
            db.ExtraUsers.Add(prepackage);
            db.SaveChanges();

            var prepackagePlans = new int[] { 9, 18, 27, 36, 45, 54, 63, 72, 81, 90, 99, 108 };
            foreach (var plan in prepackagePlans)
            {
                CreateCustomBillingPlanForExtraUser(plan + "£ plan", plan + "£ plan of extra user", plan.ToString());
            }

            return RedirectToAction("Index", "Home");
        }

        public void CreateCustomBillingPlanForExtraUser(string title, string description, string amount)
        {
            try
            {
                var exactUrl = System.Web.HttpContext.Current.Request.Url;
                var customUr = exactUrl.Scheme + "://" + exactUrl.Authority + "/";
                var db = new DBEntities();
                var returnUrl = customUr + "ExtraUser/SubscribeDashboardSuccess";
                var cancelUrl = customUr + "ExtraUser/SubscribeDashboardCancel";

                Plan billingPlan;
                billingPlan = new PayPal.Api.Plan
                {
                    name = title,
                    description = description,
                    type = "infinite",
                    merchant_preferences = new MerchantPreferences()
                    {
                        setup_fee = PayPalFunction.GetCurrency("0"),
                        return_url = returnUrl, // Retrieve from config
                        cancel_url = cancelUrl, // Retrieve from config
                        auto_bill_amount = "YES",
                        initial_fail_amount_action = "CONTINUE",
                        max_fail_attempts = "0"
                    },
                    payment_definitions = new List<PaymentDefinition>
                        {
                            new PaymentDefinition
                            {
                                name = "Standard Plan",
                                type = "REGULAR",
                                frequency = "MONTH",
                                frequency_interval = "1",
                                amount = PayPalFunction.GetCurrency(amount),
                                cycles = "0",
                                charge_models = new List<ChargeModel>
                                {
                                    new ChargeModel
                                    {
                                        type = "TAX",
                                        amount = PayPalFunction.GetCurrency("0")
                                    },
                                    new ChargeModel()
                                    {
                                        type = "SHIPPING",
                                        amount = PayPalFunction.GetCurrency("0")
                                    }
                                }
                            }
                        }
                };

                // Get PayPal Config
                var apiContext = PaypalConfiguration.GetAPIContext();

                // Create Plan
                var plan = billingPlan.Create(apiContext);

                var extraSubscriptionPlans = new ExtraUserSubscriptionPlan
                {
                    planId = plan.id,
                    planTitle = title,
                    planDesription = description,
                    planAmount = Convert.ToInt16(amount),
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                };
                db.ExtraUserSubscriptionPlans.Add(extraSubscriptionPlans);
                db.SaveChanges();

                //Active Newly Created Plan
                PayPalFunction.UpdateBillingPlan(plan.id, "/", new PayPal.Api.Plan { state = "ACTIVE" });

                TempData["message"] = "subscription added successfully";
            }
            catch (Exception e)
            {
                TempData["message"] = e.Message;
            }
        }

        #endregion
    }
}