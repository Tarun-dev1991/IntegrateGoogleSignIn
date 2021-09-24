using System;
using System.Configuration;
using System.Linq;
using System.Net;
using IntegrateGoogleSignIn.Models;
using Newtonsoft.Json;
using PayPal.Api;
using RestSharp;
using RestSharp.Authenticators;

namespace IntegrateGoogleSignIn.Helpers
{
    public static class PayPalFunction
    {

        public static readonly string PaypalUrl = ConfigurationManager.AppSettings["UrlPaypalBaseURL"];

        public static ExecuteAgreementResVm ExecuteAgreement(string returnToken)
        {
            var res = new ExecuteAgreementResVm();
            try
            {
                CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement]: method called");
                var token = GenerateAccessToken();
                CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement]: token generated ==> " + token);
                
                var client = new RestClient(PaypalUrl + "v1/payments/billing-agreements/" + returnToken + "/agreement-execute");
                var request = new RestRequest(Method.POST);
               
                request.AddHeader("Authorization", "Bearer " + token);
                request.AddHeader("Content-Type", "application/json");

                var response = client.Execute(request);
                var content = response.Content; // raw content as string 

                CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement]: response received ==> " + JsonConvert.SerializeObject(response));

                res = JsonConvert.DeserializeObject<ExecuteAgreementResVm>(content);

            }
            catch (Exception ex)
            {
                CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement]: exception ==> " + ex.Message + " #STACK TRACE# ==> " + ex.StackTrace);

                if (ex.InnerException != null)
                    CommonFunctions.SaveAuditLog("RestAPI [ExecuteAgreement]: Inner Exception ==> " + ex.InnerException.Message);

                throw;
            }

            return res;
        }
         
        public static Currency GetCurrency(string value)
        {
            return new Currency
            {
                currency = "GBP",
                value = value
            };
        }

        public static void UpdateBillingPlan(string planId, string path, object value)
        {
            try
            {
                // PayPal Authentication tokens
                var apiContext = PaypalConfiguration.GetAPIContext();

                // Retrieve Plan
                var plan = PayPal.Api.Plan.Get(apiContext, planId);

                // Activate the plan
                var patchRequest = new PatchRequest()
                {
                    new Patch()
                    {
                        op = "replace",
                        path = path,
                        value = value
                    }
                };
                plan.Update(apiContext, patchRequest);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
        public static string GenerateAccessToken()
        {
            try
            {

                var client = new RestClient(PaypalUrl + "v1/oauth2/token")
                {
                    Authenticator = new HttpBasicAuthenticator(ConfigurationManager.AppSettings["paypalclientId"], ConfigurationManager.AppSettings["paypalclientSecret"])
                };
                var request = new RestRequest(Method.POST);
                request.AddParameter("grant_type", "client_credentials");
                var response = client.Execute(request);

                var tokenData = CommonFunctions.JsonToClass(new TokenVm(), response.Content);
                return tokenData.access_token;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return string.Empty;
            }

        }

        public static string CreateBillingAgreement(string planId, string name, string description, DateTime startDate)
        {
            var url = string.Empty;
            CommonFunctions.SaveAuditLog("RestAPI [CreateAgreement]: CreateBillingAgreement method called");
            try
            {
                var client = new RestClient(PaypalUrl + "v1/payments/billing-agreements");
                var request = new RestRequest(Method.POST);

                var token = GenerateAccessToken();

                CommonFunctions.SaveAuditLog("RestAPI [CreateAgreement]: token generated ==> " + token);

                request.AddHeader("Authorization", "Bearer " + token);



                var requestData = new CreateAgreementReqVm
                {
                    description = description,
                    name = name,
                    payer = new PayerVm
                    {
                        payment_method = "paypal"
                    },
                    plan = new PlanVm
                    {
                        id = planId
                    },
                    start_date = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss") + "Z"
                };

                request.AddJsonBody(requestData);

                CommonFunctions.SaveAuditLog("RestAPI [CreateAgreement]: JSON BODY ==> " + JsonConvert.SerializeObject(requestData));

                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string 

                CommonFunctions.SaveAuditLog("RestAPI [CreateAgreement]: Response received ==> " + JsonConvert.SerializeObject(response));

                if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Accepted)
                {
                    var obj = JsonConvert.DeserializeObject<CreateAgreementResVm>(content);

                    if (obj?.links != null && obj.links.Any())
                    {
                        var approveUrl = obj.links.FirstOrDefault(s =>
                            s.rel.Equals("approval_url", StringComparison.InvariantCultureIgnoreCase));

                        if (approveUrl != null)
                        {
                            url = approveUrl.href;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.SaveAuditLog("RestAPI [CreateAgreement]: exception ==> " + ex.Message + " #STACK TRACE# ==> " + ex.StackTrace);

                if (ex.InnerException != null)
                    CommonFunctions.SaveAuditLog("RestAPI [CreateAgreement]: Inner Exception ==> " + ex.InnerException.Message);

                url = string.Empty;
            }

            return url;
        }

          
        public static PlanList GetPlanList()
        {
            var apiContext = PaypalConfiguration.GetAPIContext();
            var plans = PayPal.Api.Plan.List(apiContext, "0", "ALL");
            return plans;
        }

        public static void CancelBillingAgreement(string agreementId)
        {
            var apiContext = PaypalConfiguration.GetAPIContext();

            var agreement = new Agreement() { id = agreementId };
            agreement.Cancel(apiContext, new AgreementStateDescriptor()
            { note = "Cancelling the profile." });
        }

        public static void SuspendBillingAgreement(string agreementId)
        {
            var apiContext = PaypalConfiguration.GetAPIContext();

            var agreement = new Agreement() { id = agreementId };
            agreement.Suspend(apiContext, new AgreementStateDescriptor()
            { note = "Suspending the profile." });
        }

        public static void ReActivateBillingAgreement(string agreementId)
        {
            var apiContext = PaypalConfiguration.GetAPIContext();

            var agreement = new Agreement() { id = agreementId };
            agreement.ReActivate(apiContext, new AgreementStateDescriptor()
            { note = "ReActivate the agreement" });
        }

        public static string GetSubscribeStatus(string email)
        {
            DBEntities db = new DBEntities();
            var loginHistoryObj = db.loginhistories.FirstOrDefault(m => m.username.Equals(email));
            if (loginHistoryObj == null)
            {
                return "false";
            }
            else
            {
                if (loginHistoryObj.hasSubscribed)
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
        }
    }
}