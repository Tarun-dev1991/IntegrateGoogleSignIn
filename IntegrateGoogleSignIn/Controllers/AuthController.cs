using DatabaseMiddleware.Core;
using Google.Apis.Analytics.v3;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Plus.v1;
using Google.Apis.Services;
using IntegrateGoogleSignIn.GoogleApi;
using IntegrateGoogleSignIn.Helpers;
using IntegrateGoogleSignIn.Models;
using RestSharp;
using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Principal;
using System.Threading;
using System.Web.Mvc;
using System.Web.Security;

namespace IntegrateGoogleSignIn.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        #region Admin 

        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
                HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            }

            Session.Clear();
            System.Web.HttpContext.Current.Session.RemoveAll();

            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            return View();
        }

        [HttpPost]
        public ActionResult Index(AdminUser user)
        {
            IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
            db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
            string sql = string.Format("SELECT TOP 1 username FROM UserTable Where username = '{0}' AND [password] = '{1}'", user.UserName, user.Password);
            DataSet ds = db.GetDataSetFromSql(sql);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                string userName = ds.Tables[0].Rows[0].Field<string>("username");
                Session["AdminUser"] = userName;
                Session["SpecialUser"] = null;
                Session["userToken"] = Guid.NewGuid().ToString();
                Session["user"] = Session["userToken"];
                return Redirect("/Dashboard/Index");
            }
            else
            {
                TempData["SwalErrorMessage"] = "User Validation Failed";
                return RedirectToAction("Index", "Auth", new { area = "" });
            }
        }

        #endregion

        #region GooglePackage

        public ActionResult GoogleSignIn()
        {
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            return View();
        }

        public ActionResult GoogleSignUp()
        {
            #region --> Signout Code
            var token = CookieHelper.Get("GAuth");

            if (string.IsNullOrWhiteSpace(token) && Session["userToken"] != null)
            {
                token = Session["userToken"].ToString();
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                var client = new RestClient("https://accounts.google.com/o/oauth2/revoke?token=" + token);
                var request = new RestRequest(Method.GET);
                var response = client.Execute(request);
            }

            Session.Clear();
            System.Web.HttpContext.Current.Session.RemoveAll();
            CookieHelper.Delete("GAuth");

            if (Request.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
                HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            }
            #endregion 

            return View();
        }

        public void LoginUsingGoogle()
        {
            var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                AuthorizeAsync(CancellationToken.None).Result;
            if (result.Credential == null)
            {
                Response.Redirect(result.RedirectUri);
            }
            else
            {
                var token = result.Credential.Token;
                Session["userToken"] = token.AccessToken;
                CookieHelper.Set("GAuth", token.AccessToken, false, 365);
                Response.Redirect("/Dashboard/Index");
            }
        }

        public void SignUpUsingGoogle()
        {
            var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                AuthorizeAsync(CancellationToken.None).Result;

            if (result.Credential == null)
            {
                Response.Redirect(result.RedirectUri);
            }
            else
            {
                var service = new AnalyticsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = result.Credential,
                    ApplicationName = "Analytics API Sample",
                });

                var plusService = new PlusService(
                    new Google.Apis.Services.BaseClientService.Initializer()
                    {
                        ApplicationName = "mtslive-225112",
                        HttpClientInitializer = result.Credential
                    });

                var rt = plusService.People.Get("me").Execute();
                var userEmail = rt.Emails.FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var db = new DBEntities();
                    var isExists = db.loginhistories.FirstOrDefault(m => m.username.Equals(userEmail));
                    var token = result.Credential.Token;
                    Session["userToken"] = token.AccessToken;
                    if (isExists == null)
                    {
                        TempData["SwalSuccessMessage"] = "Welcome to dashboard!";
                        Response.Redirect("/Dashboard/AddDomain");
                    }
                    else
                    {
                        TempData["SwalErrorMessage"] = "User already registered!";
                        Response.Redirect("/Dashboard/Index");
                    }
                }
            }
        }

        [HttpGet]
        public ActionResult GoogleSignOut()
        {
            FormsAuthentication.SignOut();
            HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            Session.Clear();
            System.Web.HttpContext.Current.Session.RemoveAll();
            return Redirect("/Home/Index");
        }

        #endregion

        #region ProPackage

        public ActionResult ProLogin(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                TempData["SwalErrorMessage"] = "Paypal subscription failed or canceled.";
            }
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            return View();
        }

        [HttpPost]
        public ActionResult ProLogin(string email, string password)
        {

            try
            {
                var db2 = new DBEntities();
                var usercheck = db2.propackageusers.FirstOrDefault(m => m.email.Equals(email) && m.password.Equals(password));
                var usercheck1 = db2.specialusers.FirstOrDefault(m => m.email.Equals(email) && m.password.Equals(password));
                if (usercheck !=null && usercheck1 !=null)
                {
                    string userName = usercheck.email;
                    Session["userToken"] = Guid.NewGuid().ToString();
                    Session["user"] = userName;
                    Session["userName"] = usercheck.name;
                    Session["timezone"] = usercheck.timezone;
                    Session["SpecialUser"] = null;
                    Session["AdminUser"] = null;
                    var agreementDetails = db2.ProAgreementCollections.FirstOrDefault(m => m.email.Equals(email));

                    return RedirectToAction("checkuser", "Auth");
                    //if (agreementDetails == null)
                    //{
                    //    return RedirectToAction("Temp", "Propackage");
                    //}
                    //else
                    //{
                    //    return RedirectToAction("checkuser", "Auth");
                    //}
                    
                }
                else {
                    var db = new DBEntities();
                    var userExists =
                        db.propackageusers.FirstOrDefault(m => m.email.Equals(email) && m.password.Equals(password));
                    if (userExists != null)
                    {
                        string userName = userExists.email;
                        Session["userToken"] = Guid.NewGuid().ToString();
                        Session["user"] = userName;
                        Session["userName"] = userExists.name;
                        Session["timezone"] = userExists.timezone;
                        Session["SpecialUser"] = null;
                        Session["AdminUser"] = null;
                        var agreementDetails = db.ProAgreementCollections.FirstOrDefault(m => m.email.Equals(email));
                        return RedirectToAction("Index", "Propackage");
                        //if (agreementDetails == null)
                        //{
                        //    return RedirectToAction("Temp", "Propackage");
                        //}
                        //else
                        //{
                        //    return RedirectToAction("Index", "Propackage");
                        //}
                    }

                    else {
                        var db1 = new DBEntities();
                        var userExists1 =
                            db1.specialusers.FirstOrDefault(m => m.email.Equals(email) && m.password.Equals(password));
                        if (userExists1 != null)
                        {
                            string userName = userExists1.email;
                            Session["userToken"] = Guid.NewGuid().ToString();
                            Session["user"] = userName;
                            Session["AdminUser"] = null;
                            Session["SpecialUser"] = userName;
                            Session["userName"] = userExists1.name;
                            Session["timezone"] = userExists1.timezone;
                            return RedirectToAction("Dashboard", "SpecialUser");
                        }
                    }

                    TempData["SwalErrorMessage"] = "User Validation Failed";
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }
            return RedirectToAction("ProLogin", "Auth", new { area = "" });
        }

        public ActionResult Checkuser()
        {
            

            return View();
        }
        public ActionResult Username(string email)
        {
            
                Session["SpecialUser"] = "xyz";
                return RedirectToAction("Dashboard", "SpecialUser");
            
           
        }



        public ActionResult ProSignUp()
        {
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];

            return View();
        }

        [HttpPost]
        public ActionResult ProSignUp(string name, string email, string domain, string password)
        {
            try
            {
                DBEntities db = new DBEntities();
                var userExists =
                    db.propackageusers.FirstOrDefault(m => m.email.Equals(email) || m.domainname.Equals(domain));

                if (userExists == null)
                {
                    if (domain.Contains("www."))
                    {
                        domain = domain.Replace("www.", string.Empty);
                    }

                    var propPackageUser = new propackageuser
                    {
                        email = email,
                        password = password,
                        domainname = domain,    
                        name = name
                    };

                    db.propackageusers.Add(propPackageUser);
                    db.SaveChanges();

                    if(!string.IsNullOrEmpty(email))
                        CommonFunctions.SendMail(email, false);

                    TempData["SwalSuccessMessage"] = "Register Successfully.";
                    return RedirectToAction("ProLogin", "Auth", new { area = "" });
                }
                else
                {
                    TempData["SwalErrorMessage"] = "Email address or Domain name already register.";
                    return RedirectToAction("ProSignUp", "Auth", new { area = "" });
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }
            return RedirectToAction("ProSignUp", "Auth", new { area = "" });
        }

        [HttpGet]
        public ActionResult ProSignOut()
        {
            if (Request.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
                HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            }

            Session.Clear();
            System.Web.HttpContext.Current.Session.RemoveAll();
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public ActionResult ForgotPassword()
        {
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            try
            {
                var emailTemplate = "<head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><!--[if !mso]><!--><meta http-equiv='X-UA-Compatible' content='IE=edge'><!--<![endif]--><meta name='viewport' content='width=device-width, initial-scale=1.0'><title></title><!--[if !mso]><!--><style type='text/css'>@font-face {font-family: 'flama-condensed';font-weight: 100;src: url('http://assets.vervewine.com/fonts/FlamaCond-Medium.eot');src: url('http://assets.vervewine.com/fonts/FlamaCond-Medium.eot?#iefix') format('embedded-opentype'),url('http://assets.vervewine.com/fonts/FlamaCond-Medium.woff') format('woff'),url('http://assets.vervewine.com/fonts/FlamaCond-Medium.ttf') format('truetype');}@font-face {font-family: 'Muli';font-weight: 100;src: url('http://assets.vervewine.com/fonts/muli-regular.eot');src: url('http://assets.vervewine.com/fonts/muli-regular.eot?#iefix') format('embedded-opentype'),url('http://assets.vervewine.com/fonts/muli-regular.woff2') format('woff2'),url('http://assets.vervewine.com/fonts/muli-regular.woff') format('woff'),url('http://assets.vervewine.com/fonts/muli-regular.ttf') format('truetype');}.address-description a {color: #000000 ; text-decoration: none;}@media (max-device-width: 480px) {.vervelogoplaceholder {height:83px ;}}</style><!--<![endif]--><!--[if (gte mso 9)|(IE)]><style type='text/css'>.address-description a {color: #000000 ; text-decoration: none;}table {border-collapse: collapse ;}</style><![endif]--></head><body bgcolor='#e1e5e8' style='margin-top:0 ;margin-bottom:0 ;margin-right:0 ;margin-left:0 ;padding-top:0px;padding-bottom:0px;padding-right:0px;padding-left:0px;background:linear-gradient(to right, #0062E6, #33AEFF);'><!--[if gte mso 9]><center><table width='600' cellpadding='0' cellspacing='0'><tr><td valign='top'><![endif]--><center style='width:100%;table-layout:fixed;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;'><div style='max-width:600px;margin-top:0;margin-bottom:0;margin-right:auto;margin-left:auto;'><table align='center' cellpadding='0' style='border-spacing:0;font-family:'Muli',Arial,sans-serif;color:#333333;Margin:0 auto;width:100%;max-width:600px;'><tbody><tr><td align='center' class='vervelogoplaceholder' height='143' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;height:143px;vertical-align:middle;' valign='middle'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22160%22%2C%22height%22%3A34%2C%22alt_text%22%3A%22Verve%20Wine%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/79d8f4f889362f0c7effb2c26e08814bb12f5eb31c053021ada3463c7b35de6fb261440fc89fa804edbd11242076a81c8f0a9daa443273da5cb09c1a4739499f.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><a href='#' target='_blank'><img alt='Digital-crumbs' height='34' src='http://digital-crumbs.co.uk/Content/logo.png' style='border-width: 0px; width: 160px; height: 34px;' width='160'></a></span></td></tr><!-- Start of Email Body--><tr><td class='one-column' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;background-color:#ffffff;'><!--[if gte mso 9]><center><table width='80%' cellpadding='20' cellspacing='30'><tr><td valign='top'><![endif]--><table style='border-spacing:0;' width='100%'><tbody><tr><td align='center' class='inner' style='padding-top:15px;padding-bottom:15px;padding-right:30px;padding-left:30px;' valign='middle'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22255%22%2C%22height%22%3A93%2C%22alt_text%22%3A%22Forgot%20Password%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/35c763626fdef42b2197c1ef7f6a199115df7ff779f7c2d839bd5c6a8c2a6375e92a28a01737e4d72f42defcac337682878bf6b71a5403d2ff9dd39d431201db.png%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><img alt='Forgot Password' class='banner' height='93' src='https://marketing-image-production.s3.amazonaws.com/uploads/35c763626fdef42b2197c1ef7f6a199115df7ff779f7c2d839bd5c6a8c2a6375e92a28a01737e4d72f42defcac337682878bf6b71a5403d2ff9dd39d431201db.png' style='border-width: 0px; margin-top: 30px; width: 255px; height: 93px;' width='255'></span></td></tr><tr><td class='inner contents center' style='padding-top:15px;padding-bottom:15px;padding-right:30px;padding-left:30px;text-align:left;'><center><p class='h1 center' style='Margin:0;text-align:center;font-family:'flama-condensed','Arial Narrow',Arial;font-weight:100;font-size:30px;Margin-bottom:26px;'>Forgot your password?</p><!--[if (gte mso 9)|(IE)]><![endif]--><p class='description center' style='font-family:'Muli','Arial Narrow',Arial;Margin:0;text-align:center;max-width:320px;color:#a1a8ad;line-height:24px;font-size:15px;Margin-bottom:10px;margin-left: auto; margin-right: auto;'><span style='color: rgb(161, 168, 173); font-family: Muli, &quot;Arial Narrow&quot;, Arial; font-size: 15px; text-align: center; background-color: rgb(255, 255, 255);'>That's okay, it happens! copy your password.</span></p><br /><br /><!--[if (gte mso 9)|(IE)]><br>&nbsp;<![endif]--><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22260%22%2C%22height%22%3A54%2C%22alt_text%22%3A%22Reset%20your%20Password%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/c1e9ad698cfb27be42ce2421c7d56cb405ef63eaa78c1db77cd79e02742dd1f35a277fc3e0dcad676976e72f02942b7c1709d933a77eacb048c92be49b0ec6f3.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><span  style='border-width: 0px; margin-top: 30px; margin-bottom: 50px;  background-color:black;color:white;padding:20px;' width='100%'>##Password##</span><br /><br /><!--[if (gte mso 9)|(IE)]><br>&nbsp;<![endif]--></center></td></tr></tbody></table><!--[if (gte mso 9)|(IE)]></td></tr></table></center><![endif]--></td></tr><!-- End of Email Body--></tbody></table></div></center><!--[if gte mso 9]></td></tr></table><br /><br /></center><![endif]--></body>";
                using (var db = new DBEntities())
                {
                    var emailExists = db.propackageusers.FirstOrDefault(m => m.email.Equals(email));
                    {
                        if (emailExists == null)
                        {
                            TempData["SwalErrorMessage"] = "User not found in the system.";
                            return RedirectToAction("ForgotPassword", "Auth");
                        }
                        else
                        {
                            MailMessage msg = new MailMessage();
                            msg.To.Add(new MailAddress(email, "Dear User"));
                            msg.From = new MailAddress("info@digital-crumbs.com", "Digital-Crumbs");
                            msg.Subject = "Forgot Password";
                            msg.Body = emailTemplate.Replace("##Password##", emailExists.password);
                            msg.IsBodyHtml = true;

                            SmtpClient client = new SmtpClient
                            {
                                UseDefaultCredentials = false,
                                Credentials = new NetworkCredential("info@digital-crumbs.com", "Ishapatel@1998"),
                                Port = 587, // You can use Port 25 if 587 is blocked (mine is!)
                                Host = "smtp.ionos.co.uk",
                                DeliveryMethod = SmtpDeliveryMethod.Network,
                                EnableSsl = true
                            };
                            client.Send(msg);
                            TempData["SwalSuccessMessage"] = "Password has been sent to your registered email address.";
                            return RedirectToAction("ProLogin", "Auth");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }
            return RedirectToAction("ForgotPassword", "Auth");
        }
        #endregion

        public ActionResult Test()
        {
            return View();
        }

        #region Extra User

        public ActionResult ExtraUserLogin(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                TempData["SwalErrorMessage"] = "Paypal subscription failed or canceled.";
            }
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            return View();
        }

        [HttpPost]
        public ActionResult ExtraUserLogin(string email, string password)
        {
            try
            {
                var db = new DBEntities();
                var userExists =
                    db.ExtraUsers.FirstOrDefault(m => m.email.Equals(email) && m.password.Equals(password));
                if (userExists != null)
                {
                    string userName = userExists.email;
                    Session["userToken"] = Guid.NewGuid().ToString();
                    Session["user"] = userName;
                    Session["userName"] = userExists.name;
                    Session["timezone"] = userExists.timezone;
                    Session["SpecialUser"] = null;
                    Session["AdminUser"] = null;
                    return RedirectToAction("Index", "ExtraUser");
                }

                TempData["SwalErrorMessage"] = "User Validation Failed";
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }
            return RedirectToAction("ExtraUserLogin", "Auth", new { area = "" });
        }

        public ActionResult ExtraUserSignUp()
        {
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];

            return View();
        }

        [HttpPost]
        public ActionResult ExtraUserSignUp(string name, string email, string domain, string password)
        {
            try
            {
                DBEntities db = new DBEntities();
                var userExists =
                    db.ExtraUsers.FirstOrDefault(m => m.email.Equals(email) || m.domainname.Equals(domain));

                if (userExists == null)
                {
                    if (domain.Contains("www."))
                    {
                        domain = domain.Replace("www.", string.Empty);
                    }

                    var propPackageUser = new ExtraUser
                    {
                        email = email,
                        password = password,
                        domainname = domain,
                        name = name
                    };

                    db.ExtraUsers.Add(propPackageUser);
                    db.SaveChanges();

                    if (!string.IsNullOrEmpty(email))
                        //CommonFunctions.SendMail(email, false);

                    TempData["SwalSuccessMessage"] = "Register Successfully.";
                    return RedirectToAction("ExtraUserLogin", "Auth", new { area = "" });
                }
                else
                {
                    TempData["SwalErrorMessage"] = "Email address or Domain name already register.";
                    return RedirectToAction("ExtraUserSignUp", "Auth", new { area = "" });
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }
            return RedirectToAction("ExtraUserSignUp", "Auth", new { area = "" });
        }

        [HttpGet]
        public ActionResult ExtraUserSignOut()
        {
            if (Request.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
                HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            }

            Session.Clear();
            System.Web.HttpContext.Current.Session.RemoveAll();
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public ActionResult ExtraUserForgotPassword()
        {
            TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
            TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
            return View();
        }

        [HttpPost]
        public ActionResult ExtraUserForgotPassword(string email)
        {
            try
            {
                var emailTemplate = "<head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><!--[if !mso]><!--><meta http-equiv='X-UA-Compatible' content='IE=edge'><!--<![endif]--><meta name='viewport' content='width=device-width, initial-scale=1.0'><title></title><!--[if !mso]><!--><style type='text/css'>@font-face {font-family: 'flama-condensed';font-weight: 100;src: url('http://assets.vervewine.com/fonts/FlamaCond-Medium.eot');src: url('http://assets.vervewine.com/fonts/FlamaCond-Medium.eot?#iefix') format('embedded-opentype'),url('http://assets.vervewine.com/fonts/FlamaCond-Medium.woff') format('woff'),url('http://assets.vervewine.com/fonts/FlamaCond-Medium.ttf') format('truetype');}@font-face {font-family: 'Muli';font-weight: 100;src: url('http://assets.vervewine.com/fonts/muli-regular.eot');src: url('http://assets.vervewine.com/fonts/muli-regular.eot?#iefix') format('embedded-opentype'),url('http://assets.vervewine.com/fonts/muli-regular.woff2') format('woff2'),url('http://assets.vervewine.com/fonts/muli-regular.woff') format('woff'),url('http://assets.vervewine.com/fonts/muli-regular.ttf') format('truetype');}.address-description a {color: #000000 ; text-decoration: none;}@media (max-device-width: 480px) {.vervelogoplaceholder {height:83px ;}}</style><!--<![endif]--><!--[if (gte mso 9)|(IE)]><style type='text/css'>.address-description a {color: #000000 ; text-decoration: none;}table {border-collapse: collapse ;}</style><![endif]--></head><body bgcolor='#e1e5e8' style='margin-top:0 ;margin-bottom:0 ;margin-right:0 ;margin-left:0 ;padding-top:0px;padding-bottom:0px;padding-right:0px;padding-left:0px;background:linear-gradient(to right, #0062E6, #33AEFF);'><!--[if gte mso 9]><center><table width='600' cellpadding='0' cellspacing='0'><tr><td valign='top'><![endif]--><center style='width:100%;table-layout:fixed;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;'><div style='max-width:600px;margin-top:0;margin-bottom:0;margin-right:auto;margin-left:auto;'><table align='center' cellpadding='0' style='border-spacing:0;font-family:'Muli',Arial,sans-serif;color:#333333;Margin:0 auto;width:100%;max-width:600px;'><tbody><tr><td align='center' class='vervelogoplaceholder' height='143' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;height:143px;vertical-align:middle;' valign='middle'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22160%22%2C%22height%22%3A34%2C%22alt_text%22%3A%22Verve%20Wine%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/79d8f4f889362f0c7effb2c26e08814bb12f5eb31c053021ada3463c7b35de6fb261440fc89fa804edbd11242076a81c8f0a9daa443273da5cb09c1a4739499f.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><a href='#' target='_blank'><img alt='Digital-crumbs' height='34' src='http://digital-crumbs.co.uk/Content/logo.png' style='border-width: 0px; width: 160px; height: 34px;' width='160'></a></span></td></tr><!-- Start of Email Body--><tr><td class='one-column' style='padding-top:0;padding-bottom:0;padding-right:0;padding-left:0;background-color:#ffffff;'><!--[if gte mso 9]><center><table width='80%' cellpadding='20' cellspacing='30'><tr><td valign='top'><![endif]--><table style='border-spacing:0;' width='100%'><tbody><tr><td align='center' class='inner' style='padding-top:15px;padding-bottom:15px;padding-right:30px;padding-left:30px;' valign='middle'><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22255%22%2C%22height%22%3A93%2C%22alt_text%22%3A%22Forgot%20Password%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/35c763626fdef42b2197c1ef7f6a199115df7ff779f7c2d839bd5c6a8c2a6375e92a28a01737e4d72f42defcac337682878bf6b71a5403d2ff9dd39d431201db.png%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><img alt='Forgot Password' class='banner' height='93' src='https://marketing-image-production.s3.amazonaws.com/uploads/35c763626fdef42b2197c1ef7f6a199115df7ff779f7c2d839bd5c6a8c2a6375e92a28a01737e4d72f42defcac337682878bf6b71a5403d2ff9dd39d431201db.png' style='border-width: 0px; margin-top: 30px; width: 255px; height: 93px;' width='255'></span></td></tr><tr><td class='inner contents center' style='padding-top:15px;padding-bottom:15px;padding-right:30px;padding-left:30px;text-align:left;'><center><p class='h1 center' style='Margin:0;text-align:center;font-family:'flama-condensed','Arial Narrow',Arial;font-weight:100;font-size:30px;Margin-bottom:26px;'>Forgot your password?</p><!--[if (gte mso 9)|(IE)]><![endif]--><p class='description center' style='font-family:'Muli','Arial Narrow',Arial;Margin:0;text-align:center;max-width:320px;color:#a1a8ad;line-height:24px;font-size:15px;Margin-bottom:10px;margin-left: auto; margin-right: auto;'><span style='color: rgb(161, 168, 173); font-family: Muli, &quot;Arial Narrow&quot;, Arial; font-size: 15px; text-align: center; background-color: rgb(255, 255, 255);'>That's okay, it happens! copy your password.</span></p><br /><br /><!--[if (gte mso 9)|(IE)]><br>&nbsp;<![endif]--><span class='sg-image' data-imagelibrary='%7B%22width%22%3A%22260%22%2C%22height%22%3A54%2C%22alt_text%22%3A%22Reset%20your%20Password%22%2C%22alignment%22%3A%22%22%2C%22border%22%3A0%2C%22src%22%3A%22https%3A//marketing-image-production.s3.amazonaws.com/uploads/c1e9ad698cfb27be42ce2421c7d56cb405ef63eaa78c1db77cd79e02742dd1f35a277fc3e0dcad676976e72f02942b7c1709d933a77eacb048c92be49b0ec6f3.png%22%2C%22link%22%3A%22%23%22%2C%22classes%22%3A%7B%22sg-image%22%3A1%7D%7D'><span  style='border-width: 0px; margin-top: 30px; margin-bottom: 50px;  background-color:black;color:white;padding:20px;' width='100%'>##Password##</span><br /><br /><!--[if (gte mso 9)|(IE)]><br>&nbsp;<![endif]--></center></td></tr></tbody></table><!--[if (gte mso 9)|(IE)]></td></tr></table></center><![endif]--></td></tr><!-- End of Email Body--></tbody></table></div></center><!--[if gte mso 9]></td></tr></table><br /><br /></center><![endif]--></body>";
                using (var db = new DBEntities())
                {
                    var emailExists = db.ExtraUsers.FirstOrDefault(m => m.email.Equals(email));
                    {
                        if (emailExists == null)
                        {
                            TempData["SwalErrorMessage"] = "User not found in the system.";
                            return RedirectToAction("ExtraUserForgotPassword", "Auth");
                        }
                        else
                        {
                            MailMessage msg = new MailMessage();
                            msg.To.Add(new MailAddress(email, "Dear User"));
                            msg.From = new MailAddress("info@digital-crumbs.com", "Digital-Crumbs");
                            msg.Subject = "Forgot Password";
                            msg.Body = emailTemplate.Replace("##Password##", emailExists.password);
                            msg.IsBodyHtml = true;

                            SmtpClient client = new SmtpClient
                            {
                                UseDefaultCredentials = false,
                                Credentials = new NetworkCredential("info@digital-crumbs.com", "Ishapatel@1998"),
                                Port = 587, // You can use Port 25 if 587 is blocked (mine is!)
                                Host = "smtp.ionos.co.uk",
                                DeliveryMethod = SmtpDeliveryMethod.Network,
                                EnableSsl = true
                            };
                            client.Send(msg);
                            TempData["SwalSuccessMessage"] = "Password has been sent to your registered email address.";
                            return RedirectToAction("ExtraUserLogin", "Auth");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }
            return RedirectToAction("ExtraUserForgotPassword", "Auth");
        }

        #endregion
    }
}