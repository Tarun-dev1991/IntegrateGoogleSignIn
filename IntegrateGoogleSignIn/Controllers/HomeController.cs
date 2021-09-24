using DatabaseMiddleware.Core;
using Google.Apis.Analytics.v3;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Plus.v1;
using Google.Apis.Services;
using IntegrateGoogle.Core;
using IntegrateGoogleSignIn.GoogleApi;
using IntegrateGoogleSignIn.Helpers;
using IntegrateGoogleSignIn.Models;
using IntegrateGoogleSignIn.Services;
using LogInUsingLinkedinApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;


namespace IntegrateGoogleSignIn.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly string ClientId = ConfigurationManager.AppSettings["Google.ClientID"];
        private readonly string SecretKey = ConfigurationManager.AppSettings["Google.SecretKey"];
        private readonly string RedirectUrl = ConfigurationManager.AppSettings["Google.RedirectUrl"];
        private IGoogleProfile profile = UnityFactory.ResolveObject<IGoogleProfile>();

        public ActionResult Index()
        {
            //JobFunctions.SourceReportData();
          IntegrateGoogleSignIn.Services.JobFunctions.DailyAnalytic();
            //IntegrateGoogleSignIn.Services.JobScheduler();

            return View();
        }

        public ActionResult ContactUs()
        {
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            return View();
        }

        [HttpPost]
        public ActionResult ContactUs(string name, string email, string query)
        {
            try
            {
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(query))
                {
                    var emailTemplate = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN'><html><head><meta http-equiv='Content-Type' content='text/html; charset=UTF-8'><!--[if !mso]><!--><meta http-equiv='X-UA-Compatible' content='IE=edge'><!--<![endif]--><meta name='viewport' content='width=device-width, initial-scale=1.0'><title></title><style type='text/css'>* {-webkit-font-smoothing: antialiased;}body {Margin: 0;padding: 0;min-width: 100%;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;mso-line-height-rule: exactly;}table {border-spacing: 0;color: #333333;font-family: Arial, sans-serif;}img {border: 0;}.wrapper {width: 100%;table-layout: fixed;-webkit-text-size-adjust: 100%;-ms-text-size-adjust: 100%;}.webkit {max-width: 600px;}.outer {Margin: 0 auto;width: 100%;max-width: 600px;}.full-width-image img {width: 100%;max-width: 600px;height: auto;}.inner {padding: 10px;}p {Margin: 0;padding-bottom: 10px;}.h1 {font-size: 21px;font-weight: bold;Margin-top: 15px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.h2 {font-size: 18px;font-weight: bold;Margin-top: 10px;Margin-bottom: 5px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column .contents {text-align: left;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.one-column p {font-size: 14px;Margin-bottom: 10px;font-family: Arial, sans-serif;-webkit-font-smoothing: antialiased;}.two-column {text-align: center;font-size: 0;}.two-column .column {width: 100%;max-width: 300px;display: inline-block;vertical-align: top;}.contents {width: 100%;}.two-column .contents {font-size: 14px;text-align: left;}.two-column img {width: 100%;max-width: 280px;height: auto;}.two-column .text {padding-top: 10px;}.three-column {text-align: center;font-size: 0;padding-top: 10px;padding-bottom: 10px;}.three-column .column {width: 100%;max-width: 200px;display: inline-block;vertical-align: top;}.three-column .contents {font-size: 14px;text-align: center;}.three-column img {width: 100%;max-width: 180px;height: auto;}.three-column .text {padding-top: 10px;}.img-align-vertical img {display: inline-block;vertical-align: middle;}@media only screen and (max-device-width: 480px) {table[class=hide], img[class=hide], td[class=hide] {display: none !important;}.contents1 {width: 100%;}.contents1 {width: 100%;}}</style><!--[if (gte mso 9)|(IE)]><style type='text/css'>table {border-collapse: collapse !important;}</style><![endif]--></head><body style='Margin:0;padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;min-width:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><center class='wrapper' style='width:100%;table-layout:fixed;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;background:linear-gradient(to left,#f77408, #ffbe00);'><table style='background:linear-gradient(to left,#f77408, #ffbe00);' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#f3f2f0;'><tbody><tr><td width='100%'><div class='webkit' style='max-width:600px;Margin:0 auto;'><!--[if (gte mso 9)|(IE)]><table width='600' align='center' cellpadding='0' cellspacing='0' border='0' style='border-spacing:0' ><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;' ><![endif]--><!-- ======= start main body ======= --><table class='outer' style='border-spacing:0;Margin:0 auto;width:100%;max-width:600px;' cellspacing='0' cellpadding='0' border='0' align='center'><tbody><tr><td style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;'><!-- ======= start header ======= --><table width='100%' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td><table style='width:100%;' cellspacing='0' cellpadding='0' border='0'><tbody><tr><td class='vervelogoplaceholder' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;height:143px;vertical-align:middle;' valign='middle' height='143' align='center'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22160%22%2C%22height%22%3A34%2C%22alt_text%22%3A%22Verve%20Wine%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/79d8f4f889362f0c7effb2c26e08814bb12f5eb31c053021ada3463c7b35de6fb261440fc89fa804edbd11242076a81c8f0a9daa443273da5cb09c1a4739499f.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><a href='#' target='_blank'><img alt='Digital-crumbs' src='http://digital-crumbs.co.uk/Content/logo.png' style='border-width: 0px; width: 160px; height: 34px;' width='160' height='34'></a></span></td></tr></tbody></table></td></tr></tbody></table><!-- ======= end header ======= --><!-- ======= start hero ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #fdac02; border-right:1px solid #fdac02; border-bottom:1px solid #fdac02; border-top:1px solid #fdac02;' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 20px 50px;background: linear-gradient(to left,#f77408, #ffbe00);' width='600' valign='top' height='80' align='center'><!--[if gte mso 9]><v:rect xmlns:v='urn:schemas-microsoft-com:vml' fill='true' stroke='false' style='width:600px;height:303px;'><v:fill type='tile' src='https://gallery.mailchimp.com/fdcaf86ecc5056741eb5cbc18/images/42ba8b72-65d6-4dea-b8ab-3ecc12676337.jpg' color='#2f9780' /><v:textbox inset='0,0,0,0'><![endif]--><div><br><br><p style='color:#ffffff; font-size:40px; text-align:center; font-family: Verdana, Geneva, sans-serif'>Contact Inquiry</p></div><!--[if gte mso 9]></v:textbox></v:rect><![endif]--></td></tr></tbody></table><!-- ======= end hero  ======= --><!-- ======= start article ======= --><table class='one-column' style='border-spacing:0; border-left:1px solid #e8e7e5; border-right:1px solid #e8e7e5; border-bottom:1px solid #e8e7e5; border-top:1px solid #e8e7e5' width='100%' cellspacing='0' cellpadding='0' border='0' bgcolor='#FFFFFF'><tbody><tr><td style='padding:20px 50px 10px 50px' align='center'><p style='color:#262626; font-size:16px; text-align:justify; font-family: Verdana, Geneva, sans-serif; line-height:22px '>Dear Admin,<br><br>You got an inquiry from "+name+" with email "+email+", basically user sendsbelow message so requested to followed up on his/her subject.</p><p style='color:#262626; font-size:16px; text-align:justify; font-family: Verdana, Geneva, sans-serif; line-height:22px '></p><pre style='text-align: left'>"+query+"</pre><p></p></td></tr></tbody></table><!-- ======= end article ======= --><!-- ======= start footer ======= --></td></tr></tbody></table><!--[if (gte mso 9)|(IE)]></td></tr></table><![endif]--></div></td></tr></tbody></table></center></body></html>";
                    MailMessage msg = new MailMessage();
                    msg.To.Add(new MailAddress("info@digital-crumbs.com", "Digital-Crumbs"));
                    msg.From = new MailAddress(email, name);
                    msg.Subject = "Contact Inquiry";
                    emailTemplate.Replace("##name##", name);
                    emailTemplate.Replace("##email##", email);
                    emailTemplate.Replace("##query##", query);
                    msg.Body = emailTemplate;
                    msg.IsBodyHtml = true;

                    SmtpClient client = new SmtpClient();
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("info@digital-crumbs.com", "Ishapatel@1998");
                    client.Port = 587; // You can use Port 25 if 587 is blocked (mine is!)
                    client.Host = "smtp.ionos.co.uk";
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = true;
                    client.Send(msg);
                    TempData["SwalSuccessMessage"] = "Thank you for contact with us!";
                }
                else
                {
                    TempData["SwalErrorMessage"] = "Please fill all required fields.";
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }
            return RedirectToAction("ContactUs", "Home");
        }

        public ActionResult FAQs()
        {
            return View();
        }

        public ActionResult Pricing()
        {
            return View();
        }

        public ActionResult TermsAndCondition()
        {
            return View();
        }

        public ActionResult PrivacyPolicy()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SaveGoogleUser(string code, string state, string session_state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return View("Error");
            }

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://www.googleapis.com")
            };
            var requestUrl = $"oauth2/v4/token?code={code}&client_id={ClientId}&client_secret={SecretKey}&redirect_uri={RedirectUrl}&grant_type=authorization_code";

            var dict = new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" }
            };
            var req = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = new FormUrlEncodedContent(dict) };
            var response = await httpClient.SendAsync(req);
            var token = JsonConvert.DeserializeObject<GmailToken>(await response.Content.ReadAsStringAsync());
            Session["userToken"] = token.AccessToken;
            var obj = await profile.GetuserProfile(token.AccessToken);

            return RedirectToAction("Index", "Dashboard");
        }


        public ActionResult Crumbs80()
        {
            var db = new DBEntities();
            List<propackageuser> Propackageusers = db.propackageusers.ToList();
            return View(Propackageusers);
        }

    }
}