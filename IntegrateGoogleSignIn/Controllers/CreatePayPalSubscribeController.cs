using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

using DatabaseMiddleware.Core;
using System.Data.SqlClient;
using IntegrateGoogleSignIn;
using System.Configuration;

namespace paypalIntegration.Controllers
{
    static class APIConfig
    {
        public const string user = "tarun431991_api1.gmail.com";
        public const string clientId = "AT5V84J-0pXZLTUgre6aL_3WEszAyPEQIWo-ZBptHBNnvtHReX1LPUKD6x7AkFHZHDaNOJSKrR6nNvsZ";
        public const string password = "4MAYB5GRTQCVNHUA";
        public const string secret = "EIgaRgvNZ4D3nHf5Ew_6CDLrFlAhY8t71RCg2_nEHFsofRPQqc3xGnOz9NGBRsKWOl2oPdWBt400o81o";
        public const string signature = "AXD2H7YLacgJvlUVv8QIbyV7zqP5Ab9BC8HqCYsn0bcSssAG0H5bZ2z9";
        public const string urlNVP = "https://api-3t.paypal.com/nvp";
    }
    public class CreatePayPalSubscribeController : Controller
    {


        // GET: CreatePayPalSubscribe
        public ActionResult Index(string id = "")
        {
            ViewBag.message = id;

            return View();
        }

        public async Task<ActionResult> SendPayment()
        {
            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri(APIConfig.urlNVP);

                var values = new Dictionary<string, string>
{
   { "USER", APIConfig.user },
   { "PWD", APIConfig.password },
   { "SIGNATURE", APIConfig.signature},
   { "METHOD", "SetExpressCheckout" },
   { "VERSION", "86" },
   { "L_BILLINGTYPE0", "RecurringPayments" },
   { "L_BILLINGAGREEMENTDESCRIPTION0", "GoogleAPI" },
   { "cancelUrl","http://localhost:51809/Home/AdminLogin" },
   { "returnUrl", "http://localhost:51809/Home/Index"}
};

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(APIConfig.urlNVP, content);

                var responseString = await response.Content.ReadAsStringAsync();

                string token = "";
                if (responseString.Contains("Error"))
                {
                    return RedirectToAction("ErrorAction", new { message = responseString });
                }
                else
                {
                    string[] arr = responseString.Split('&');
                    foreach (var item in arr)
                    {
                        if (item.Contains("TOKEN"))
                        {
                            string[] strArr = item.Split('=');
                            token = strArr[1];
                            Session["token"] = token;
                            var obj = new Dictionary<string, string>();
                            obj["token"] = token;
                            string json = JsonConvert.SerializeObject(obj);

                            System.IO.File.WriteAllText(Server.MapPath("../Content/tokens.json"), json);

                            /*https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token=InsertTokenHere
*/
                            //var res = await client.PostAsync("https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token=InsertTokenHere", null);

                            return Redirect("https://www.paypal.com/checkoutnow?token=" + token);//"https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token="+token);

                        }
                    }
                }
                ViewBag.Token = token;
                ViewBag.respo = response;

                return View("Index");
            }

        }

        public async Task<ActionResult> ProcessPayment()
        {
            StreamReader r = new StreamReader(Server.MapPath("../Content/tokens.json"));
            string strjson = r.ReadToEnd();
            var obj = new Dictionary<string, string>();
            obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(strjson);
            string token = Session["token"].ToString();
            token = token.Replace("%2D", "-");
            token = token.Replace("%2d", "-");
            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri(APIConfig.urlNVP);

                var values = new Dictionary<string, string>
{
   { "USER", APIConfig.user },
   { "PWD", APIConfig.password },
   { "SIGNATURE", APIConfig.signature},
   { "METHOD", "GetExpressCheckoutDetails" },
   { "VERSION", "86" },
   { "TOKEN", token }
};

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(APIConfig.urlNVP, content);

                var responseString = await response.Content.ReadAsStringAsync();
                if (responseString.Contains("PAYERID"))
                {
                    string[] strArr = responseString.Split('&');
                    foreach (var item in strArr)
                    {
                        if (item.Contains("PAYERID"))
                        {
                            Session["payerid"] = item.Split('=')[1];
                            return RedirectToAction("CreateProfile", new { payeriid = Session["payerid"].ToString() });
                        }

                    }
                }

                return RedirectToAction("Index", new { id = responseString });
            }

        }
        public async Task<ActionResult> CreateProfile(string payeriid)
        {
            string token = Session["token"].ToString();
            token = token.Replace("%2D", "-");
            token = token.Replace("%2d", "-");
            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri(APIConfig.urlNVP);

                var values = new Dictionary<string, string>
{
   { "USER", APIConfig.user },
   { "PWD", APIConfig.password },
   { "SIGNATURE", APIConfig.signature},
   { "METHOD", "CreateRecurringPaymentsProfile" },
   { "VERSION", "86" },
   { "TOKEN", token },
   { "PAYERID", payeriid },
   { "PROFILESTARTDATE", DateTime.Now.AddDays(1).Date.ToUniversalTime()
                         .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")
            },
   { "DESC", "GOOGLEAPI" },
   { "BILLINGPERIOD", "Month" },
   { "BILLINGFREQUENCY", "1" },
   { "AMT", "0.1" },
   { "CURRENCYCODE", "GBP" },
   { "COUNTRYCODE", "POUND" },
   { "MAXFAILEDPAYMENTS", "3" }
};

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(APIConfig.urlNVP, content);

                var responseString = await response.Content.ReadAsStringAsync();
                TempData["resp"] = responseString;

                IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
                SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
                string eid = Session["Email"].ToString(); ;
                string cmdtext = "insert into paymentdetails(token,profileid,paydate,emailid,ispaid) values(@token,@profileid,@paydate,@emailid,@ispaid)";

                SqlCommand cmd = new SqlCommand(cmdtext, conn);
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@profileid", payeriid);
                cmd.Parameters.AddWithValue("@paydate", DateTime.Now.Date);
                cmd.Parameters.AddWithValue("@emailid", eid);
                cmd.Parameters.AddWithValue("@ispaid", true);
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
                //db.ExecuteSql("Insert into paymentdetails (token,profileid,paydate) values('"+token+"','"+payeriid+"','"+DateTime.Now+"')");
                return RedirectToAction("Index", "Dashboard");
            }

        }
        public ActionResult ErrorAction(string message)
        {
            ViewBag.Error = message;
            return View();
        }

    }
}