using DatabaseMiddleware.Core;
using Google.Apis.Analytics.v3;
using Google.Apis.Analytics.v3.Data;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Plus.v1;
using Google.Apis.Services;
using IntegrateGoogle.Core;
using IntegrateGoogle.Core.Manager;
using IntegrateGoogle.Core.Models;
using IntegrateGoogleSignIn.GoogleApi;
using IntegrateGoogleSignIn.Helpers;
using IntegrateGoogleSignIn.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace IntegrateGoogleSignIn.Controllers
{
    public class DashboardController : Controller
    {
        private IGoogleProfile profile = UnityFactory.ResolveObject<IGoogleProfile>();
        private IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();

        public DashboardController()
        {
            db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
        }

        public ActionResult PriceCard()
        {
            return View();
        }

        [HttpPost]
        [ActionName("PriceCard")]
        public ActionResult ProcessToSubscribe()
        {
            return RedirectToAction("SendPayment", "CreatePayPalSubscribe");
        }
     
        public async Task<ActionResult> Index(string userName)
        {
            //var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
            //	AuthorizeAsync(new CancellationToken());
            List<string> str = new List<string>();
            var eid = string.Empty;
            var history = UnityFactory.ResolveObject<ILoginHistoryManager>();
            UserProfile mymodel = await profile.GetuserProfile(HttpContext.Session["userToken"].ToString());
            mymodel.Link = HttpContext.Session["userToken"].ToString();
            string adminUser = (string)Session["AdminUser"];
            if (string.IsNullOrEmpty(adminUser))
            {

                var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(CancellationToken.None);
                if (result.Credential != null)
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

                    try
                    {
                        var rt = plusService.People.Get("me").Execute();
                        Session["Email"] = rt.Emails.FirstOrDefault().Value;
                        //check paid or not
                        eid = Session["Email"].ToString();
                        string sql1 = string.Format("SELECT * FROM paymentdetails Where emailid = '{0}'", eid);
                        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
                        SqlDataAdapter adp = new SqlDataAdapter(sql1, conn);

                        //DataSet ds = new DataSet();
                        DataTable dt = new DataTable();
                        adp.Fill(dt);
                        Session["payEmailId"] = userName;
                        if (dt.Rows.Count == 0)
                        {
                            //return RedirectToAction("PriceCard"); // return RedirectToAction("SendPayment", "CreatePayPalSubscribe");
                        }
                        else
                        {
                            DateTime date = DateTime.Parse(dt.Rows[0]["paydate"].ToString());
                            TimeSpan tm = DateTime.Now.Date.Subtract(date);
                            int days = Convert.ToInt32(tm.TotalDays);
                            if (days > 30)
                            {
                                //return RedirectToAction("PriceCard"); ;//return RedirectToAction("SendPayment", "CreatePayPalSubscribe");
                            }
                        }


                        history.AddLogin(new LoginHistory() { UserName = rt.Emails.FirstOrDefault().Value, Name = rt.DisplayName, AuthType = "Google" });
                    }
                    catch
                    {
                        // when not able to get profile.
                    }

                    MetadataResource.ColumnsResource.ListRequest request = service.Metadata.Columns.List("ga");
                    Columns col = request.Execute();
                    ViewBag.columns = col.Items.Select(x => string.Format("{0} {1} - {2}", x.Id, x.Kind, x.Attributes["type"]));

                    ManagementResource.AccountSummariesResource.ListRequest list = service.Management.AccountSummaries.List();
                    list.MaxResults = 1000;  // Maximum number of Account Summaries to return per request. 

                    AccountSummaries feed = list.Execute();
                    List<AccountSummary> allRows = new List<AccountSummary>();

                    //// Loop through until we arrive at an empty page
                    while (feed.Items != null)
                    {
                        allRows.AddRange(feed.Items);

                        // We will know we are on the last page when the next page token is
                        // null.
                        // If this is the case, break.
                        if (feed.NextLink == null)
                        {
                            break;
                        }

                        // Prepare the next page of results             
                        list.StartIndex = feed.StartIndex + list.MaxResults;
                        // Execute and process the next page request
                        feed = list.Execute();

                    }
                    IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                    db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
                    string sql = string.Format("SELECT * FROM loginhistory Where username = '{0}'", userName);
                    //DataSet ds = db.GetDataSetFromSql(sql);
                    //if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count == 0)
                    //{
                    //    return RedirectToAction("SendPayment", "CreatePayPalSubscribe");
                    //}

                    feed.Items = allRows;

                    //Get account summary and display them.
                    foreach (AccountSummary account in feed.Items)
                    {
                        // Account
                        str.Add("Account: " + account.Name + "(" + account.Id + ")");
                        string query = @"usp_Accounts_Insert";
                        List<IDbDataParameter> dataParameters = new List<IDbDataParameter>
                        {
                            new SqlParameter("username", Session["Email"]),
                            new SqlParameter("name", account.Name),
                            new SqlParameter("id", account.Id)
                        };

                        db.ExecuteProcedure(query, dataParameters.ToArray());
                        var dbEntity = new DBEntities();
                        foreach (WebPropertySummary wp in account.WebProperties)
                        {
                            account.ETag = string.Empty;
                            var googleSubscriptionPlan = dbEntity.GoogleUsers.FirstOrDefault(m => m.email.Equals(eid) && m.websiteId == wp.Id);
                            if (googleSubscriptionPlan != null)
                            {
                                account.ETag = googleSubscriptionPlan.Id.ToString();
                            }

                            // Web Properties within that account
                            //str.Add("\tWeb Property: " + wp.Name + "(" + wp.Id + ")");
                            query = @"usp_WebProperty_Insert";
                            dataParameters = new List<IDbDataParameter>
                            {
                                new SqlParameter("accountId", account.Id),
                                new SqlParameter("name", wp.Name),
                                new SqlParameter("id", wp.Id)
                            };

                            db.ExecuteProcedure(query, dataParameters.ToArray());


                            //Don't forget to check its not null. Believe it or not it could be.
                            if (wp.Profiles != null)
                            {
                                foreach (ProfileSummary profile in wp.Profiles)
                                {
                                    query = @"usp_Profile_Insert";
                                    dataParameters = new List<IDbDataParameter>
                                    {
                                        new SqlParameter("webPropertyId", wp.Id),
                                        new SqlParameter("name", profile.Name),
                                        new SqlParameter("id", profile.Id)
                                    };
                                    db.ExecuteProcedure(query, dataParameters.ToArray());
                                    // Profiles with in that web property.
                                    //str.Add("\t\tProfile: " + profile.Name + "(" + profile.Id + ")");
                                }
                            }
                        }
                    }

                    //ViewBag.listAc = str;

                    return View(model: feed.Items);
                }

            }
            else if (!string.IsNullOrEmpty(userName))
            {
                IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);

                ViewBag.SelectedUser = userName;
                ViewBag.Users = history.GetUsers();
                return View(model: GetAccounts(userName));
            }
            else
            {
                var userss = history.GetUsers();
                ViewBag.Users = userss;
                if (userss != null && userss.Any())
                {
                    var selUser = userss.FirstOrDefault();
                    ViewBag.SelectedUser = selUser.UserName;
                    return View(model: GetAccounts(selUser.UserName));
                }
            }

            ViewBag.listAc = str;

            return View(model: new List<AccountSummary>());
        }

        public ActionResult DomainList()
        {
            try
            {
                TempData["SwalErrorMessage"] = TempData["SwalErrorMessage"];
                TempData["SwalSuccessMessage"] = TempData["SwalSuccessMessage"];
                var db = new DBEntities();
                var domainList = db.AdminDomains.ToList();
                return View(domainList);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPost]
        public ActionResult DomainList(string domainName)
        {
            try
            {
                var db = new DBEntities();
                if (!string.IsNullOrEmpty(domainName))
                {
                    var adminDomain = db.AdminDomains.FirstOrDefault(m=> m.domainname.Equals(domainName));
                    if (adminDomain != null)
                    {
                        TempData["SwalErrorMessage"] = "Domain already available.";
                    }
                    else
                    {
                        var newDomain = new AdminDomain
                        {
                            domainname = domainName
                        };
                        db.AdminDomains.Add(newDomain);
                        db.SaveChanges();
                        TempData["SwalSuccessMessage"] = "Domain added successfully.";
                    }
                }
                else
                {
                    TempData["SwalErrorMessage"] = "Please enter domain name.";
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }

            return RedirectToAction("DomainList");
        }

        public ActionResult DeleteDomain(long id)
        {
            try
            {
                var db = new DBEntities();
                var adminDomain = db.AdminDomains.Find(id);
                if (adminDomain != null)
                {
                    db.AdminDomains.Remove(adminDomain);
                    db.SaveChanges();
                    TempData["SwalSuccessMessage"] = "Domain deleted successfully.";
                }
                else
                {
                    TempData["SwalErrorMessage"] = "Record not found.";
                }
            }
            catch (Exception e)
            {
                TempData["SwalErrorMessage"] = e.Message;
            }

            return RedirectToAction("DomainList");
        }

        public async Task<ActionResult> AddDomain(string userName)
        {
            //var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
            //	AuthorizeAsync(new CancellationToken());
            List<string> str = new List<string>();
            var eid = string.Empty;
            var history = UnityFactory.ResolveObject<ILoginHistoryManager>();
            UserProfile mymodel = await profile.GetuserProfile(HttpContext.Session["userToken"].ToString());
            mymodel.Link = HttpContext.Session["userToken"].ToString();
            string adminUser = (string)Session["AdminUser"];
            if (string.IsNullOrEmpty(adminUser))
            {

                var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(CancellationToken.None);
                if (result.Credential != null)
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

                    try
                    {
                        var rt = plusService.People.Get("me").Execute();
                        Session["Email"] = rt.Emails.FirstOrDefault().Value;
                        //check paid or not
                        eid = Session["Email"].ToString();
                        string sql1 = string.Format("SELECT * FROM paymentdetails Where emailid = '{0}'", eid);
                        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
                        SqlDataAdapter adp = new SqlDataAdapter(sql1, conn);

                        //DataSet ds = new DataSet();
                        DataTable dt = new DataTable();
                        adp.Fill(dt);
                        Session["payEmailId"] = userName;
                        if (dt.Rows.Count == 0)
                        {
                            //return RedirectToAction("PriceCard"); // return RedirectToAction("SendPayment", "CreatePayPalSubscribe");
                        }
                        else
                        {
                            DateTime date = DateTime.Parse(dt.Rows[0]["paydate"].ToString());
                            TimeSpan tm = DateTime.Now.Date.Subtract(date);
                            int days = Convert.ToInt32(tm.TotalDays);
                            if (days > 30)
                            {
                                //return RedirectToAction("PriceCard"); ;//return RedirectToAction("SendPayment", "CreatePayPalSubscribe");
                            }
                        }


                        history.AddLogin(new LoginHistory() { UserName = rt.Emails.FirstOrDefault().Value, Name = rt.DisplayName, AuthType = "Google" });
                    }
                    catch
                    {
                        // when not able to get profile.
                    }

                    MetadataResource.ColumnsResource.ListRequest request = service.Metadata.Columns.List("ga");
                    Columns col = request.Execute();
                    ViewBag.columns = col.Items.Select(x => string.Format("{0} {1} - {2}", x.Id, x.Kind, x.Attributes["type"]));

                    ManagementResource.AccountSummariesResource.ListRequest list = service.Management.AccountSummaries.List();
                    list.MaxResults = 1000;  // Maximum number of Account Summaries to return per request. 

                    AccountSummaries feed = list.Execute();
                    List<AccountSummary> allRows = new List<AccountSummary>();

                    //// Loop through until we arrive at an empty page
                    while (feed.Items != null)
                    {
                        allRows.AddRange(feed.Items);

                        // We will know we are on the last page when the next page token is
                        // null.
                        // If this is the case, break.
                        if (feed.NextLink == null)
                        {
                            break;
                        }

                        // Prepare the next page of results             
                        list.StartIndex = feed.StartIndex + list.MaxResults;
                        // Execute and process the next page request
                        feed = list.Execute();

                    }
                    IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                    db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
                    string sql = string.Format("SELECT * FROM loginhistory Where username = '{0}'", userName);
                    //DataSet ds = db.GetDataSetFromSql(sql);
                    //if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count == 0)
                    //{
                    //    return RedirectToAction("SendPayment", "CreatePayPalSubscribe");
                    //}

                    feed.Items = allRows;

                    //Get account summary and display them.
                    foreach (AccountSummary account in feed.Items)
                    {
                        // Account
                        str.Add("Account: " + account.Name + "(" + account.Id + ")");
                        string query = @"usp_Accounts_Insert";
                        List<IDbDataParameter> dataParameters = new List<IDbDataParameter>
                        {
                            new SqlParameter("username", Session["Email"]),
                            new SqlParameter("name", account.Name),
                            new SqlParameter("id", account.Id)
                        };

                        db.ExecuteProcedure(query, dataParameters.ToArray());
                        var dbEntity = new DBEntities();
                        foreach (WebPropertySummary wp in account.WebProperties)
                        {
                            account.ETag = string.Empty;
                            var googleSubscriptionPlan = dbEntity.GoogleUsers.FirstOrDefault(m => m.email.Equals(eid) && m.websiteId == wp.Id);
                            if (googleSubscriptionPlan != null)
                            {
                                account.ETag = googleSubscriptionPlan.Id.ToString();
                            }

                            // Web Properties within that account
                            //str.Add("\tWeb Property: " + wp.Name + "(" + wp.Id + ")");
                            query = @"usp_WebProperty_Insert";
                            dataParameters = new List<IDbDataParameter>
                            {
                                new SqlParameter("accountId", account.Id),
                                new SqlParameter("name", wp.Name),
                                new SqlParameter("id", wp.Id)
                            };

                            db.ExecuteProcedure(query, dataParameters.ToArray());


                            //Don't forget to check its not null. Believe it or not it could be.
                            if (wp.Profiles != null)
                            {
                                foreach (ProfileSummary profile in wp.Profiles)
                                {
                                    query = @"usp_Profile_Insert";
                                    dataParameters = new List<IDbDataParameter>
                                    {
                                        new SqlParameter("webPropertyId", wp.Id),
                                        new SqlParameter("name", profile.Name),
                                        new SqlParameter("id", profile.Id)
                                    };
                                    db.ExecuteProcedure(query, dataParameters.ToArray());
                                    // Profiles with in that web property.
                                    //str.Add("\t\tProfile: " + profile.Name + "(" + profile.Id + ")");
                                }
                            }
                        }
                    }

                    //ViewBag.listAc = str;

                    return View(model: feed.Items);
                }

            }
            else if (!string.IsNullOrEmpty(userName))
            {
                IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);

                ViewBag.SelectedUser = userName;
                ViewBag.Users = history.GetUsers();
                return View(model: GetAccounts(userName));
            }
            else
            {
                var userss = history.GetUsers();
                ViewBag.Users = userss;
                if (userss != null && userss.Any())
                {
                    var selUser = userss.FirstOrDefault();
                    ViewBag.SelectedUser = selUser.UserName;
                    return View(model: GetAccounts(selUser.UserName));
                }
            }

            ViewBag.listAc = str;

            return View(model: new List<AccountSummary>());
        }

        private List<AccountSummary> GetAccounts(string username)
        {
            List<AccountSummary> accounts = new List<AccountSummary>();
            var dbEntity = new DBEntities();
            string sql = string.Format("Select Id, username, name from Accounts Where username = '{0}'", username);
            DataSet ds = db.GetDataSetFromSql(sql);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    AccountSummary summary = new AccountSummary()
                    {
                        Id = row.Field<string>("Id"),
                        Name = row.Field<string>("name")
                    };

                    summary.WebProperties = GetWebPropertySummaries(summary.Id);
                    foreach (var webProperties in summary.WebProperties)
                    {
                        summary.ETag = string.Empty;
                        var googleSubscriptionPlan = dbEntity.GoogleUsers.FirstOrDefault(m => m.email.Equals(username) && m.websiteId == webProperties.Id);
                        if (googleSubscriptionPlan != null)
                        {
                            summary.ETag = googleSubscriptionPlan.Id.ToString();
                        }
                    }
                    accounts.Add(summary);
                }
            }

            return accounts;
        }

        private List<WebPropertySummary> GetWebPropertySummaries(string accountId)
        {
            List<WebPropertySummary> webPropertySummaries = new List<WebPropertySummary>();
            string sql = string.Format("Select Id, name FROM WebProperties Where AccountId = '{0}'", accountId);
            DataSet ds = db.GetDataSetFromSql(sql);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    WebPropertySummary summary = new WebPropertySummary()
                    {
                        Id = row.Field<string>("Id"),
                        Name = row.Field<string>("name")
                    };

                    summary.Profiles = GetProfiles(summary.Id);
                    webPropertySummaries.Add(summary);
                }
            }

            return webPropertySummaries;
        }

        private List<ProfileSummary> GetProfiles(string webPropertieId)
        {
            List<ProfileSummary> profiles = new List<ProfileSummary>();
            string sql = string.Format("Select Id, name FROM Profiles Where WebPropertyId = '{0}'", webPropertieId);
            DataSet ds = db.GetDataSetFromSql(sql);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    ProfileSummary summary = new ProfileSummary()
                    {
                        Id = row.Field<string>("Id"),
                        Name = row.Field<string>("name")
                    };

                    profiles.Add(summary);
                }
            }

            return profiles;
        }

        public string GetCountReports(string viewId, string startDate, string endDate, string city, string path)
        {
            string adminUser = (string)Session["AdminUser"];
            if (string.IsNullOrEmpty(adminUser))
            {
                List<Dictionary<string, string>> rest = new List<Dictionary<string, string>>();
                string dimension = null;// string startDate = "2019-01-01"; string endDate = "2019-01-01";
                #region Prepare Report Request object 
                // Create the DateRange object. Here we want data from last week.

                var dateRange = new DateRange
                {
                    // DateTime.UtcNow.AddDays(-7)
                    StartDate = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd") : Convert.ToDateTime(startDate).ToString("yyyy-MM-dd"),
                    EndDate = string.IsNullOrEmpty(endDate) ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd") : Convert.ToDateTime(endDate).ToString("yyyy-MM-dd"),
                };

                ViewBag.StartDate = dateRange.StartDate;
                ViewBag.EndDate = dateRange.EndDate;
                // Create the Metrics and dimensions object.
                var metrics = new List<Metric> { new Metric { Expression = "ga:sessions", Alias = "Sessions" } };
                var dimensions = new List<Dimension> { new Dimension { Name = "ga:pageTitle" } };


                var sessions = new List<Metric> {
                new Metric
                {
                    Expression = "ga:users",
                    Alias = "User"
                },
                new Metric
                {
                    Expression = "ga:timeOnPage",
                    Alias = "timeOnPage"
                }
            };
                var hostData = "ga:hostname"
                    .Split(',').Select(x => new Dimension { Name = x }).ToList();
                var reportHostRequest = new ReportRequest
                {
                    DateRanges = new List<DateRange> { dateRange },
                    Dimensions = hostData,
                    Metrics = sessions,
                    ViewId = viewId
                };
                var getHostReportsRequest = new GetReportsRequest
                {
                    ReportRequests = new List<ReportRequest> { reportHostRequest }
                };
                Dictionary<string, List<Dictionary<string, string>>> resHost = new Dictionary<string, List<Dictionary<string, string>>>();
                var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(CancellationToken.None).Result;
                ReportManager rm = new ReportManager();
                //List<AnalyticalData> res = new List<AnalyticalData>();
                Dictionary<string, List<Dictionary<string, string>>> res = new Dictionary<string, List<Dictionary<string, string>>>();
                if (result.Credential != null)
                {
                    //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                    var responseHost = rm.GetReport(getHostReportsRequest, new AnalyticsReportingService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = result.Credential,
                        ApplicationName = "UA-131388149-1"
                    }));


                    var restHost = PrintReport(responseHost);
                    if (restHost != null)
                    {
                        restHost.ForEach(host =>
                        {
                            var date = "ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:pageTitle,ga:pagePath,ga:source,ga:city,ga:sessionCount"
                    .Split(',').Select(x => new Dimension { Name = x }).ToList();
                            if (!string.IsNullOrEmpty(dimension))
                            {
                                date.AddRange(dimension.Split(',').Select(x => new Dimension { Name = x }));
                            }

                            ViewBag.Dimensions = "ga:pageTitle,ga:landingPagePath".Split(',').Select(x => x).ToList();

                            //Get required View Id from configuration
                            //var ViewId = "187023611";// ConfigurationManager.AppSettings["ViewId"];

                            var dimensionFilter = new DimensionFilter
                            {
                                DimensionName = "ga:hostname",
                                Expressions = new List<string> { host["ga:hostname"] }
                            };
                            var dimensionFilterClause = new DimensionFilterClause
                            {
                                Filters = new List<DimensionFilter> { dimensionFilter }
                            };

                            // Create the Request object.
                            var reportRequest = new ReportRequest
                            {
                                DateRanges = new List<DateRange> { dateRange },
                                Dimensions = date,
                                Metrics = sessions,
                                ViewId = viewId,
                                DimensionFilterClauses = new List<DimensionFilterClause> { dimensionFilterClause }
                            };
                            var getReportsRequest = new GetReportsRequest
                            {
                                ReportRequests = new List<ReportRequest> { reportRequest }
                            };
                            #endregion

                            //Invoke Google Analytics API call and get report
                            try
                            {
                                ViewBag.ActiveUsers = GetActiveUsers(viewId);

                                //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                                var response = rm.GetReport(getReportsRequest, new AnalyticsReportingService(new BaseClientService.Initializer
                                {
                                    HttpClientInitializer = result.Credential,
                                    ApplicationName = "UA-131388149-1"
                                }));

                                rest.AddRange(PrintReport(response, host["ga:hostname"]));
                            }
                            catch (Exception ex)
                            {
                                ViewBag.ex = ex.Message;
                                //res = new List<string>() { ex.Message };
                            }
                        });
                        try
                        {
                            if (rest != null)
                            {
                                string stringData = JsonConvert.SerializeObject(rest);
                                IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                                db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
                                string query = @"usp_VisitorData_Insert";
                                List<IDbDataParameter> dataParameters = new List<IDbDataParameter>
                                {
                                    new SqlParameter("date", dateRange.StartDate),
                                    new SqlParameter("profileId", viewId),
                                    new SqlParameter("data", stringData)
                                };

                                db.ExecuteProcedure(query, dataParameters.ToArray());

                            }

                            //.Where(c => c["ga:city"])
                            ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                            ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                            ViewBag.selectedCity = city;
                            ViewBag.selectedPath = path;
                            ViewBag.UserKeys =
                                rest
                                .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) && (string.IsNullOrEmpty(path) || c["ga:pagePath"] == path))
                                .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                .GroupBy(x => new
                                {
                                    latitude = x["ga:latitude"],
                                    longitude = x["ga:longitude"],
                                    userBucket = x["ga:userBucket"]
                                }).Where(z => z.Select(a => a["ga:timeOnPageNo"]).Select(float.Parse).Sum() > 0 && !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket)).ToList();

                            var data =
                                rest
                                .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                .GroupBy(x => new
                                {
                                    latitude = x["ga:latitude"],
                                    longitude = x["ga:longitude"],
                                    userBucket = x["ga:userBucket"]
                                }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).ToDictionary(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket), y => y.ToList());


                            ViewBag.ViewId = data;
                            ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                            int ret = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                            return ret.ToString();
                        }
                        catch (Exception ex)
                        {
                            ViewBag.ex = ex.Message;
                            //res = new List<string>() { ex.Message };
                        }
                    }
                    else
                    {
                        ViewBag.ConsentUrl = result.RedirectUri;
                        //var response = rm.GetReport(getReportsRequest);
                        //res = PrintReport(response);                      
                    }
                }

                ViewBag.ViewId = res;
            }
            else
            {
                DateTime date = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.Date.AddDays(-1) : Convert.ToDateTime(startDate);
                ViewBag.StartDate = date.ToString("yyyy-MM-dd");
                string s = GetVisitorData(viewId, date);
                var rest = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(s);
                if (rest != null)
                {
                    ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                    ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                    ViewBag.selectedCity = city;
                    ViewBag.selectedPath = path;
                    ViewBag.UserKeys =
                        rest
                        .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) && (string.IsNullOrEmpty(path) || c["ga:pagePath"] == path))
                        .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                        .GroupBy(x => new
                        {
                            latitude = x["ga:latitude"],
                            longitude = x["ga:longitude"],
                            userBucket = x["ga:userBucket"],
                            sessionCount = x["ga:sessionCount"]
                        }).Where(z => z.Select(a => a["ga:timeOnPageNo"]).Select(float.Parse).Sum() > 0 && !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket)).ToList();
                    var restD =
                        rest
                        .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) && (string.IsNullOrEmpty(path) || c["ga:pagePath"] == path))
                        .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                        .GroupBy(x => new
                        {
                            latitude = x["ga:latitude"],
                            longitude = x["ga:longitude"],
                            userBucket = x["ga:userBucket"],
                            sessionCount = x["ga:sessionCount"]
                        }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket)).ToList();

                    var data = rest.GroupBy(x => new
                    {
                        latitude = x["ga:latitude"],
                        longitude = x["ga:longitude"],
                        userBucket = x["ga:userBucket"]
                    }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).ToDictionary(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket), y => y.ToList());

                    //data.Select(a => a[""]).Distinct();

                    ViewBag.ViewId = data;
                    ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                    int ret = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                    return ret.ToString();//data.Select(d => d).Distinct().ToList().Count.ToString();//ret.ToString();
                }
            }

            //int ret = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
            return "0";
        }

        public ActionResult GetReports(string viewId, string startDate, string endDate, string city, string path)
        {

            var dbEntity = new DBEntities();
            var profiles = dbEntity.Profiles.FirstOrDefault(m => m.Id.Equals(viewId));
            if (profiles != null)
            {
                if (Session["AdminUser"] == null)
                {
                    var userEmail = Session["Email"].ToString();
                    var isSubscribe =
                        dbEntity.GoogleUsers.FirstOrDefault(m =>
                            m.email.Equals(userEmail) && m.websiteId == profiles.WebPropertyId);
                    if (isSubscribe != null)
                    {
                        ViewBag.OrgViewId = viewId;
                        string adminUser = (string)Session["AdminUser"];
                        string hostname = "";
                        if (string.IsNullOrEmpty(adminUser))
                        {
                            List<Dictionary<string, string>> rest = new List<Dictionary<string, string>>();
                            List<Dictionary<string, string>> hourlyUserData = new List<Dictionary<string, string>>();
                            string dimension = null; // string startDate = "2019-01-01"; string endDate = "2019-01-01";

                            #region Prepare Report Request object 

                            // Create the DateRange object. Here we want data from last week.

                            var dateRange = new DateRange
                            {
                                // DateTime.UtcNow.AddDays(-7)
                                StartDate = string.IsNullOrEmpty(startDate)
                                    ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
                                    : Convert.ToDateTime(startDate).ToString("yyyy-MM-dd"),
                                EndDate = string.IsNullOrEmpty(endDate)
                                    ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
                                    : Convert.ToDateTime(endDate).ToString("yyyy-MM-dd"),
                            };

                            ViewBag.StartDate = dateRange.StartDate;
                            ViewBag.EndDate = dateRange.EndDate;
                            // Create the Metrics and dimensions object.
                            var metrics = new List<Metric>
                                {new Metric {Expression = "ga:sessions", Alias = "Sessions"}};
                            var dimensions = new List<Dimension> { new Dimension { Name = "ga:pageTitle" } };


                            var sessions = new List<Metric>
                            {
                                new Metric
                                {
                                    Expression = "ga:users",
                                    Alias = "User"
                                },
                                new Metric
                                {
                                    Expression = "ga:timeOnPage",
                                    Alias = "timeOnPage"
                                }
                            };

                            var hostData = "ga:hostname"
                                .Split(',').Select(x => new Dimension { Name = x }).ToList();
                            var reportHostRequest = new ReportRequest
                            {
                                DateRanges = new List<DateRange> { dateRange },
                                Dimensions = hostData,
                                Metrics = sessions,
                                ViewId = viewId
                            };
                            var getHostReportsRequest = new GetReportsRequest
                            {
                                ReportRequests = new List<ReportRequest> { reportHostRequest }
                            };
                            Dictionary<string, List<Dictionary<string, string>>> resHost =
                                new Dictionary<string, List<Dictionary<string, string>>>();
                            var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata())
                                .AuthorizeAsync(CancellationToken.None).Result;
                            ReportManager rm = new ReportManager();
                            if (result.Credential != null)
                            {
                                //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                                var responseHost = rm.GetReport(getHostReportsRequest, new AnalyticsReportingService(
                                    new BaseClientService.Initializer
                                    {
                                        HttpClientInitializer = result.Credential,
                                        ApplicationName = "UA-131388149-1"
                                    }));

                                var restHost = PrintReport(responseHost);
                                if (restHost != null)
                                {
                                    //List<AnalyticalData> res = new List<AnalyticalData>();
                                    Dictionary<string, List<Dictionary<string, string>>> res =
                                        new Dictionary<string, List<Dictionary<string, string>>>();
                                    restHost.ForEach(host =>
                                    {
                                        var date =
                                            "ga:userBucket,ga:dateHourMinute,ga:networkLocation,ga:pageTitle,ga:pagePath,ga:source,ga:city,ga:sessionCount"
                                                .Split(',').Select(x => new Dimension { Name = x }).ToList();
                                        var date2 =
                                            "ga:userBucket,ga:dateHourMinute,ga:pageTitle,ga:pagePath,ga:browser,ga:browserSize,ga:browserVersion,ga:deviceCategory"
                                                .Split(',').Select(x => new Dimension { Name = x }).ToList();
                                        if (!string.IsNullOrEmpty(dimension))
                                        {
                                            date.AddRange(dimension.Split(',').Select(x => new Dimension { Name = x }));
                                        }

                                        ViewBag.Dimensions = "ga:pageTitle,ga:landingPagePath".Split(',').Select(x => x)
                                            .ToList();

                                        //Get required View Id from configuration
                                        //var ViewId = "187023611";// ConfigurationManager.AppSettings["ViewId"];

                                        var dimensionFilter = new DimensionFilter
                                        {
                                            DimensionName = "ga:hostname",
                                            Expressions = new List<string> { host["ga:hostname"] }
                                        };
                                        var dimensionFilterClause = new DimensionFilterClause
                                        {
                                            Filters = new List<DimensionFilter> { dimensionFilter }
                                        };

                                        // Create the Request object.
                                        var reportRequest = new ReportRequest
                                        {
                                            DateRanges = new List<DateRange> { dateRange },
                                            Dimensions = date,
                                            Metrics = sessions,
                                            ViewId = viewId,
                                            DimensionFilterClauses = new List<DimensionFilterClause>
                                                {dimensionFilterClause}
                                        };
                                        var reportRequest2 = new ReportRequest
                                        {
                                            DateRanges = new List<DateRange> { dateRange },
                                            Dimensions = date2,
                                            Metrics = sessions,
                                            ViewId = viewId,
                                            DimensionFilterClauses = new List<DimensionFilterClause>
                                                {dimensionFilterClause}
                                        };
                                        var getReportsRequest = new GetReportsRequest
                                        {
                                            ReportRequests = new List<ReportRequest> { reportRequest, reportRequest2 }
                                        };

                                        #endregion


                                        if (string.IsNullOrEmpty(hostname))
                                        {
                                            hostname = host["ga:hostname"];
                                        }

                                        //Invoke Google Analytics API call and get report
                                        try
                                        {
                                            ViewBag.ActiveUsers = GetActiveUsers(viewId);

                                            //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                                            var response = rm.GetReport(getReportsRequest,
                                                new AnalyticsReportingService(new BaseClientService.Initializer
                                                {
                                                    HttpClientInitializer = result.Credential,
                                                    ApplicationName = "UA-131388149-1"
                                                }));

                                            rest.AddRange(PrintReport(response, host["ga:hostname"]));
                                        }
                                        catch (Exception ex)
                                        {
                                            ViewBag.ex = ex.Message;
                                            //res = new List<string>() { ex.Message };
                                        }
                                    });
                                    try
                                    {
                                        if (rest != null)
                                        {
                                            string stringData = JsonConvert.SerializeObject(rest);
                                            IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                                            db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"]
                                                .ConnectionString);
                                            string query = @"usp_VisitorData_Insert";
                                            List<IDbDataParameter> dataParameters = new List<IDbDataParameter>
                                            {
                                                new SqlParameter("date", dateRange.StartDate),
                                                new SqlParameter("profileId", viewId),
                                                new SqlParameter("data", stringData)
                                            };

                                            db.ExecuteProcedure(query, dataParameters.ToArray());
                                        }

                                        //.Where(c => c["ga:city"])
                                        ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                                        ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                                        ViewBag.selectedCity = city;
                                        ViewBag.selectedPath = path;
                                        ViewBag.UserKeys =
                                            rest
                                                .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) &&
                                                            (string.IsNullOrEmpty(path) || c["ga:pagePath"] == path))
                                                .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                                .GroupBy(x => new
                                                {
                                                    userBucket = x["ga:userBucket"],
                                                    city = x["ga:city"],
                                                    networkDomain = x["ga:networkLocation"],
                                                    source = x["ga:source"],



                                                    browser = x["ga:browser"],
                                                    browserSize = x["ga:browserSize"],
                                                    browserVersion = x["ga:browserVersion"]


                                                })
                                                .Where(z =>
                                                    z.Select(a => a["ga:timeOnPageNo"]).Select(float.Parse).Sum() > 0 &&
                                                    !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                                .Select(y => string.Format("{0}_{1}_{2}_{3}", y.Key.userBucket,
                                                    y.Key.city, y.Key.networkDomain, y.Key.source)).Distinct()
                                                .ToList();

                                        var data =
                                            rest
                                                .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                                .GroupBy(x => new
                                                {
                                                    userBucket = x["ga:userBucket"],
                                                    city = x["ga:city"],
                                                    networkDomain = x["ga:networkLocation"],
                                                    source = x["ga:source"],



                                                    //browser = x["ga:browser"],
                                                    //browserSize = x["ga:browserSize"],
                                                    //browserVersion = x["ga:browserVersion"],
                                                    //deviceCategory = x["ga:deviceCategory"],
                                                    //networkDomain = x["ga:networkLocation"]
                                                })
                                                .Where(z => !(z.Count() == 1 &&
                                                              z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                                .ToDictionary(
                                                    y => string.Format("{0}_{1}_{2}_{3}", y.Key.userBucket, y.Key.city,
                                                        y.Key.networkDomain, y.Key.source), y => y.Distinct().ToList());

                                        ViewBag.hostname = hostname;
                                        ViewBag.ViewId = data;
                                        ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                                        return View();
                                    }
                                    catch (Exception ex)
                                    {
                                        ViewBag.ex = ex.Message;
                                        //res = new List<string>() { ex.Message };
                                    }

                                    ViewBag.ViewId = res;

                                    return View();
                                }
                            }
                            else
                            {
                                ViewBag.ConsentUrl = result.RedirectUri;
                                //var response = rm.GetReport(getReportsRequest);
                                //res = PrintReport(response);
                            }
                        }
                        else
                        {
                            DateTime date = string.IsNullOrEmpty(startDate)
                                ? DateTime.UtcNow.Date.AddDays(-1)
                                : Convert.ToDateTime(startDate);
                            ViewBag.StartDate = date.ToString("yyyy-MM-dd");
                            string s = GetVisitorData(viewId, date);
                            var rest = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(s);
                            if (rest != null)
                            {
                                ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                                ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                                ViewBag.selectedCity = city;
                                ViewBag.selectedPath = path;
                                ViewBag.UserKeys =
                                    rest
                                        .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) &&
                                                    (string.IsNullOrEmpty(path) || c["ga:pagePath"] == path))
                                        .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                        .GroupBy(x => new
                                        {
                                            latitude = x["ga:latitude"],
                                            longitude = x["ga:longitude"],
                                            userBucket = x["ga:userBucket"],
                                            sessionCount = x["ga:sessionCount"],

                                            browser = x["ga:browser"],
                                            browserSize = x["ga:browserSize"],
                                            browserVersion = x["ga:browserVersion"],
                                            deviceCategory = x["ga:deviceCategory"],
                                            networkDomain = x["ga:networkLocation"]
                                        })
                                        .Where(z => z.Select(a => a["ga:timeOnPageNo"])
                                                        .Select(float.Parse).Sum() > 0 &&
                                                    !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                        .Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude,
                                            y.Key.userBucket))
                                        .ToList();

                                var data =
                                    rest
                                        .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                        .GroupBy(x => new
                                        {
                                            latitude = x["ga:latitude"],
                                            longitude = x["ga:longitude"],
                                            userBucket = x["ga:userBucket"],

                                            browser = x["ga:browser"],
                                            browserSize = x["ga:browserSize"],
                                            browserVersion = x["ga:browserVersion"],
                                            deviceCategory = x["ga:deviceCategory"],
                                            networkDomain = x["ga:networkLocation"]
                                        }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                        .ToDictionary(
                                            y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude,
                                                y.Key.userBucket), y => y.ToList());

                                ViewBag.ViewId = data;
                                ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                                return View();
                            }
                        }

                        return View();
                    }
                    else
                    {
                        TempData["Message"] = "Plan is not subscribe.";
                        return RedirectToAction("index", "Dashboard");
                    }
                }
                else
                {
                    ViewBag.OrgViewId = viewId;
                    string adminUser = (string)Session["AdminUser"];
                    string hostname = "";
                    if (string.IsNullOrEmpty(adminUser))
                    {
                        List<Dictionary<string, string>> rest = new List<Dictionary<string, string>>();
                        List<Dictionary<string, string>> hourlyUserData = new List<Dictionary<string, string>>();
                        string dimension = null; // string startDate = "2019-01-01"; string endDate = "2019-01-01";

                        #region Prepare Report Request object 

                        // Create the DateRange object. Here we want data from last week.

                        var dateRange = new DateRange
                        {
                            // DateTime.UtcNow.AddDays(-7)
                            StartDate = string.IsNullOrEmpty(startDate)
                                ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
                                : Convert.ToDateTime(startDate).ToString("yyyy-MM-dd"),
                            EndDate = string.IsNullOrEmpty(endDate)
                                ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
                                : Convert.ToDateTime(endDate).ToString("yyyy-MM-dd"),
                        };

                        ViewBag.StartDate = dateRange.StartDate;
                        ViewBag.EndDate = dateRange.EndDate;
                        // Create the Metrics and dimensions object.
                        var metrics = new List<Metric>
                                {new Metric {Expression = "ga:sessions", Alias = "Sessions"}};
                        var dimensions = new List<Dimension> { new Dimension { Name = "ga:pageTitle" } };


                        var sessions = new List<Metric>
                            {
                                new Metric
                                {
                                    Expression = "ga:users",
                                    Alias = "User"
                                },
                                new Metric
                                {
                                    Expression = "ga:timeOnPage",
                                    Alias = "timeOnPage"
                                }
                            };

                        var hostData = "ga:hostname"
                            .Split(',').Select(x => new Dimension { Name = x }).ToList();
                        var reportHostRequest = new ReportRequest
                        {
                            DateRanges = new List<DateRange> { dateRange },
                            Dimensions = hostData,
                            Metrics = sessions,
                            ViewId = viewId
                        };
                        var getHostReportsRequest = new GetReportsRequest
                        {
                            ReportRequests = new List<ReportRequest> { reportHostRequest }
                        };
                        Dictionary<string, List<Dictionary<string, string>>> resHost =
                            new Dictionary<string, List<Dictionary<string, string>>>();
                        var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata())
                            .AuthorizeAsync(CancellationToken.None).Result;
                        ReportManager rm = new ReportManager();
                        if (result.Credential != null)
                        {
                            //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                            var responseHost = rm.GetReport(getHostReportsRequest, new AnalyticsReportingService(
                                new BaseClientService.Initializer
                                {
                                    HttpClientInitializer = result.Credential,
                                    ApplicationName = "UA-131388149-1"
                                }));

                            var restHost = PrintReport(responseHost);
                            if (restHost != null)
                            {
                                //List<AnalyticalData> res = new List<AnalyticalData>();
                                Dictionary<string, List<Dictionary<string, string>>> res =
                                    new Dictionary<string, List<Dictionary<string, string>>>();
                                restHost.ForEach(host =>
                                {
                                    var date =
                                        "ga:userBucket,ga:dateHourMinute,ga:networkLocation,ga:pageTitle,ga:pagePath,ga:source,ga:city,ga:sessionCount"
                                            .Split(',').Select(x => new Dimension { Name = x }).ToList();
                                    var date2 =
                                        "ga:userBucket,ga:dateHourMinute,ga:pageTitle,ga:pagePath,ga:browser,ga:browserSize,ga:browserVersion,ga:deviceCategory"
                                            .Split(',').Select(x => new Dimension { Name = x }).ToList();
                                    if (!string.IsNullOrEmpty(dimension))
                                    {
                                        date.AddRange(dimension.Split(',').Select(x => new Dimension { Name = x }));
                                    }

                                    ViewBag.Dimensions = "ga:pageTitle,ga:landingPagePath".Split(',').Select(x => x)
                                        .ToList();

                                    //Get required View Id from configuration
                                    //var ViewId = "187023611";// ConfigurationManager.AppSettings["ViewId"];

                                    var dimensionFilter = new DimensionFilter
                                    {
                                        DimensionName = "ga:hostname",
                                        Expressions = new List<string> { host["ga:hostname"] }
                                    };
                                    var dimensionFilterClause = new DimensionFilterClause
                                    {
                                        Filters = new List<DimensionFilter> { dimensionFilter }
                                    };

                                    // Create the Request object.
                                    var reportRequest = new ReportRequest
                                    {
                                        DateRanges = new List<DateRange> { dateRange },
                                        Dimensions = date,
                                        Metrics = sessions,
                                        ViewId = viewId,
                                        DimensionFilterClauses = new List<DimensionFilterClause>
                                            {dimensionFilterClause}
                                    };
                                    var reportRequest2 = new ReportRequest
                                    {
                                        DateRanges = new List<DateRange> { dateRange },
                                        Dimensions = date2,
                                        Metrics = sessions,
                                        ViewId = viewId,
                                        DimensionFilterClauses = new List<DimensionFilterClause>
                                            {dimensionFilterClause}
                                    };
                                    var getReportsRequest = new GetReportsRequest
                                    {
                                        ReportRequests = new List<ReportRequest> { reportRequest, reportRequest2 }
                                    };

                                    #endregion


                                    if (string.IsNullOrEmpty(hostname))
                                    {
                                        hostname = host["ga:hostname"];
                                    }

                                    //Invoke Google Analytics API call and get report
                                    try
                                    {
                                        ViewBag.ActiveUsers = GetActiveUsers(viewId);

                                        //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                                        var response = rm.GetReport(getReportsRequest,
                                        new AnalyticsReportingService(new BaseClientService.Initializer
                                        {
                                            HttpClientInitializer = result.Credential,
                                            ApplicationName = "UA-131388149-1"
                                        }));

                                        rest.AddRange(PrintReport(response, host["ga:hostname"]));
                                    }
                                    catch (Exception ex)
                                    {
                                        ViewBag.ex = ex.Message;
                                        //res = new List<string>() { ex.Message };
                                    }
                                });
                                try
                                {
                                    if (rest != null)
                                    {
                                        string stringData = JsonConvert.SerializeObject(rest);
                                        IDatabaseMiddleware db = UnityFactory.ResolveObject<IDatabaseMiddleware>();
                                        db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"]
                                            .ConnectionString);
                                        string query = @"usp_VisitorData_Insert";
                                        List<IDbDataParameter> dataParameters = new List<IDbDataParameter>
                                            {
                                                new SqlParameter("date", dateRange.StartDate),
                                                new SqlParameter("profileId", viewId),
                                                new SqlParameter("data", stringData)
                                            };

                                        db.ExecuteProcedure(query, dataParameters.ToArray());
                                    }

                                    //.Where(c => c["ga:city"])
                                    ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                                    ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                                    ViewBag.selectedCity = city;
                                    ViewBag.selectedPath = path;
                                    ViewBag.UserKeys =
                                        rest
                                            .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) &&
                                                        (string.IsNullOrEmpty(path) || c["ga:pagePath"] == path))
                                            .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                            .GroupBy(x => new
                                            {
                                                userBucket = x["ga:userBucket"],
                                                city = x["ga:city"],
                                                networkDomain = x["ga:networkLocation"],
                                                source = x["ga:source"],



                                                browser = x["ga:browser"],
                                                browserSize = x["ga:browserSize"],
                                                browserVersion = x["ga:browserVersion"]


                                            })
                                            .Where(z =>
                                                z.Select(a => a["ga:timeOnPageNo"]).Select(float.Parse).Sum() > 0 &&
                                                !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                            .Select(y => string.Format("{0}_{1}_{2}_{3}", y.Key.userBucket,
                                                y.Key.city, y.Key.networkDomain, y.Key.source)).Distinct()
                                            .ToList();

                                    var data =
                                        rest
                                            .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                            .GroupBy(x => new
                                            {
                                                userBucket = x["ga:userBucket"],
                                                city = x["ga:city"],
                                                networkDomain = x["ga:networkLocation"],
                                                source = x["ga:source"],



                                                //browser = x["ga:browser"],
                                                //browserSize = x["ga:browserSize"],
                                                //browserVersion = x["ga:browserVersion"],
                                                //deviceCategory = x["ga:deviceCategory"],
                                                //networkDomain = x["ga:networkLocation"]
                                            })
                                            .Where(z => !(z.Count() == 1 &&
                                                          z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                            .ToDictionary(
                                                y => string.Format("{0}_{1}_{2}_{3}", y.Key.userBucket, y.Key.city,
                                                    y.Key.networkDomain, y.Key.source), y => y.Distinct().ToList());

                                    ViewBag.hostname = hostname;
                                    ViewBag.ViewId = data;
                                    ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                                    return View();
                                }
                                catch (Exception ex)
                                {
                                    ViewBag.ex = ex.Message;
                                    //res = new List<string>() { ex.Message };
                                }

                                ViewBag.ViewId = res;

                                return View();
                            }
                        }
                        else
                        {
                            ViewBag.ConsentUrl = result.RedirectUri;
                            //var response = rm.GetReport(getReportsRequest);
                            //res = PrintReport(response);
                        }
                    }
                    else
                    {
                        DateTime date = string.IsNullOrEmpty(startDate)
                            ? DateTime.UtcNow.Date.AddDays(-1)
                            : Convert.ToDateTime(startDate);
                        ViewBag.StartDate = date.ToString("yyyy-MM-dd");
                        string s = GetVisitorData(viewId, date);
                        var rest = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(s);
                        if (rest != null)
                        {
                            ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                            ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                            ViewBag.selectedCity = city;
                            ViewBag.selectedPath = path;
                            ViewBag.UserKeys =
                                rest
                                    .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) &&
                                                (string.IsNullOrEmpty(path) || c["ga:pagePath"] == path))
                                    .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                    .GroupBy(x => new
                                    {
                                        latitude = x["ga:latitude"],
                                        longitude = x["ga:longitude"],
                                        userBucket = x["ga:userBucket"],
                                        sessionCount = x["ga:sessionCount"],

                                        browser = x["ga:browser"],
                                        browserSize = x["ga:browserSize"],
                                        browserVersion = x["ga:browserVersion"],
                                        deviceCategory = x["ga:deviceCategory"],
                                        networkDomain = x["ga:networkLocation"]
                                    })
                                    .Where(z => z.Select(a => a["ga:timeOnPageNo"])
                                                    .Select(float.Parse).Sum() > 0 &&
                                                !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                    .Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude,
                                        y.Key.userBucket))
                                    .ToList();

                            var data =
                                rest
                                    .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                    .GroupBy(x => new
                                    {
                                        latitude = x["ga:latitude"],
                                        longitude = x["ga:longitude"],
                                        userBucket = x["ga:userBucket"],

                                        browser = x["ga:browser"],
                                        browserSize = x["ga:browserSize"],
                                        browserVersion = x["ga:browserVersion"],
                                        deviceCategory = x["ga:deviceCategory"],
                                        networkDomain = x["ga:networkLocation"]
                                    }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec"))
                                    .ToDictionary(
                                        y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude,
                                            y.Key.userBucket), y => y.ToList());

                            ViewBag.ViewId = data;
                            ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                            return View();
                        }
                    }

                    return View();
                }
            }
            else
            {
                TempData["Message"] = "Profile not found.";
                return RedirectToAction("index", "Dashboard");
            }
        }

        private string GetVisitorData(string viewId, DateTime date)
        {
            string data = "";
            string sql = string.Format("Select * from VisitorData Where ProfileId = @profileId AND Date = @date");
            DataSet ds = db.GetDataSetFromSql(sql, new SqlParameter("profileId", viewId), new SqlParameter("date", date));
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    data = row.Field<string>("Data");

                }
            }

            return data;
        }

        private int GetActiveUsers(string viewId)
        {
            int activeUsers = 0;
            //Invoke Google Analytics API call and get report
            try
            {
                var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                AuthorizeAsync(CancellationToken.None).Result;
                ReportManager rm = new ReportManager();
                if (result.Credential != null)
                {
                    var service = new AnalyticsService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = result.Credential,
                        ApplicationName = "UA-131388149-1"
                    });

                    DataResource.RealtimeResource.GetRequest request = service.Data.Realtime.Get(string.Format("ga:{0}", viewId), "rt:activeUsers");
                    RealtimeData feed = request.Execute();
                    if (feed.Rows != null)
                    {
                        foreach (var row in feed.Rows)
                        {
                            foreach (string col in row)
                            {
                                activeUsers = int.Parse(col);  // writes the value of the column
                            }
                        }
                    }
                }
                else
                {
                    ViewBag.ConsentUrl = result.RedirectUri;
                    //var response = rm.GetReport(getReportsRequest);
                    //res = PrintReport(response);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ex = ex.Message;
                //res = new List<string>() { ex.Message };
            }

            return activeUsers;
        }

        public static int GetHashCodeUnique(string value)
        {
            int h = 0;
            for (int i = 0; i < value.Length; i++)
            {
                h += value[i] * 31 ^ value.Length - (i + 1);
            }

            return h;
        }

        private string ConvertToMinutes(string sec)
        {
            if (double.TryParse(sec, out double secodns))
            {
                TimeSpan t = TimeSpan.FromSeconds(secodns);

                //here backslash is must to tell that colon is
                //not the part of format, it just a character that we want in output
                return t.Minutes > 0 ? string.Format("{0:D2} min {1:D2} sec",
                t.Minutes,
                t.Seconds) : string.Format("{0} sec", t.TotalSeconds);
            }

            return string.Empty;
        }

        private List<Dictionary<string, string>> PrintReport(GetReportsResponse response, string hostname = "")
        {
            List<string> str = new List<string>();
            List<Dictionary<string, string>> adList = new List<Dictionary<string, string>>();
            var isHostName = false;
            //foreach (var report in response.Reports)
            //{
            var rows = response.Reports[0].Data.Rows;

            ColumnHeader header = response.Reports[0].ColumnHeader;

            var dimensionHeaders = header.Dimensions;

            var metricHeaders = header.MetricHeader.MetricHeaderEntries;

            //For: 1st response
            if (rows == null || !rows.Any())
            {
                str.Add("No data found!");
            }
            else
            {
                foreach (var row in rows)
                {
                    Dictionary<string, string> ad = new Dictionary<string, string>();

                    var dimensions = row.Dimensions;
                    var metrics = row.Metrics;
                    for (int i = 0; i < dimensionHeaders.Count && i < dimensions.Count; i++)
                    {
                        str.Add(dimensionHeaders[i] + ": " + dimensions[i]);
                        if (dimensionHeaders[i] == "ga:hostname")
                        {
                            isHostName = true;
                        }

                        if (dimensionHeaders[i] == "ga:dateHourMinute")
                        {
                            ad.Add(dimensionHeaders[i], new DateTime(int.Parse(dimensions[i].Substring(0, 4)),
                            int.Parse(dimensions[i].Substring(4, 2)),
                            int.Parse(dimensions[i].Substring(6, 2)),
                            int.Parse(dimensions[i].Substring(8, 2)),
                            int.Parse(dimensions[i].Substring(10, 2)),
                            0).ToString("HH:mm:ss"));
                        }
                        else
                        {
                            ad.Add(dimensionHeaders[i], dimensions[i]);
                        }
                    }
                    for (int j = 0; j < metrics.Count; j++)
                    {
                        DateRangeValues values = metrics[j];
                        for (int k = 0; k < values.Values.Count && k < metricHeaders.Count; k++)
                        {
                            //str.Add(metricHeaders[k].Name + ": " + values.Values[k]);

                            if (metricHeaders[k].Name.Equals("timeOnPage"))
                            {
                                ad.Add(metricHeaders[k].Name, ConvertToMinutes(values.Values[k]));
                                ad.Add("ga:timeOnPageNo", values.Values[k]);
                            }
                            else
                            {
                                ad.Add(metricHeaders[k].Name, values.Values[k]);
                            }
                        }
                    }
                    if (!isHostName)
                    {
                        ad.Add("ga:hostname", hostname);
                    }

                    adList.Add(ad);
                }
            }
            //}
            //For: 2nd response
            if (response.Reports.Count > 1)
            {
                if (adList.Count > 0 && adList != null)
                {
                    for (int i = 0; i < adList.Count; i++)
                    {
                        Dictionary<string, string> ad = adList[i];

                        ad.Add("ga:browser", response.Reports[1].Data.Rows[i].Dimensions[4]);
                        ad.Add("ga:browserSize", response.Reports[1].Data.Rows[i].Dimensions[5]);
                        ad.Add("ga:browserVersion", response.Reports[1].Data.Rows[i].Dimensions[6]);
                        ad.Add("ga:deviceCategory", response.Reports[1].Data.Rows[i].Dimensions[7]);
                        // ad.Add("ga:networkLocation", response.Reports[1].Data.Rows[i].Dimensions[8]);

                        adList[i] = ad;
                    }
                }
            }
            return adList;
        }

        public string GetListOfCountReports(string viewId, string startDate, string endDate, string city, string path)
        {
            string adminUser = (string)Session["AdminUser"];
            List<string> lstPath = new List<string>();
            if (!string.IsNullOrEmpty(path))
            {
                lstPath.AddRange(path.Split(',').Select(i => i.Trim()).ToList());
            }
            else
            {
                return "0";
            }
            if (string.IsNullOrEmpty(adminUser))
            {
                List<Dictionary<string, string>> rest = new List<Dictionary<string, string>>();
                string dimension = null;// string startDate = "2019-01-01"; string endDate = "2019-01-01";
                #region Prepare Report Request object 
                // Create the DateRange object. Here we want data from last week.

                var dateRange = new DateRange
                {
                    // DateTime.UtcNow.AddDays(-7)
                    StartDate = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd") : Convert.ToDateTime(startDate).ToString("yyyy-MM-dd"),
                    EndDate = string.IsNullOrEmpty(endDate) ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd") : Convert.ToDateTime(endDate).ToString("yyyy-MM-dd"),
                };

                ViewBag.StartDate = dateRange.StartDate;
                ViewBag.EndDate = dateRange.EndDate;
                // Create the Metrics and dimensions object.
                var metrics = new List<Metric> { new Metric { Expression = "ga:sessions", Alias = "Sessions" } };
                var dimensions = new List<Dimension> { new Dimension { Name = "ga:pageTitle" } };


                var sessions = new List<Metric> {
                new Metric
                {
                    Expression = "ga:users",
                    Alias = "User"
                },
                new Metric
                {
                    Expression = "ga:timeOnPage",
                    Alias = "timeOnPage"
                }
            };
                var hostData = "ga:hostname"
                    .Split(',').Select(x => new Dimension { Name = x }).ToList();
                var reportHostRequest = new ReportRequest
                {
                    DateRanges = new List<DateRange> { dateRange },
                    Dimensions = hostData,
                    Metrics = sessions,
                    ViewId = viewId
                };
                var getHostReportsRequest = new GetReportsRequest
                {
                    ReportRequests = new List<ReportRequest> { reportHostRequest }
                };
                Dictionary<string, List<Dictionary<string, string>>> resHost = new Dictionary<string, List<Dictionary<string, string>>>();
                var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(CancellationToken.None).Result;
                ReportManager rm = new ReportManager();
                //List<AnalyticalData> res = new List<AnalyticalData>();
                Dictionary<string, List<Dictionary<string, string>>> res = new Dictionary<string, List<Dictionary<string, string>>>();
                if (result.Credential != null)
                {
                    //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                    var responseHost = rm.GetReport(getHostReportsRequest, new AnalyticsReportingService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = result.Credential,
                        ApplicationName = "UA-131388149-1"
                    }));


                    var restHost = PrintReport(responseHost);
                    if (restHost != null)
                    {
                        restHost.ForEach(host =>
                        {
                            var date = "ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:pageTitle,ga:pagePath,ga:source,ga:city,ga:sessionCount,ga:hostname"
                    .Split(',').Select(x => new Dimension { Name = x }).ToList();
                            if (!string.IsNullOrEmpty(dimension))
                            {
                                date.AddRange(dimension.Split(',').Select(x => new Dimension { Name = x }));
                            }

                            ViewBag.Dimensions = "ga:pageTitle,ga:landingPagePath".Split(',').Select(x => x).ToList();

                            //Get required View Id from configuration
                            //var ViewId = "187023611";// ConfigurationManager.AppSettings["ViewId"];

                            var dimensionFilter = new DimensionFilter
                            {
                                DimensionName = "ga:hostname",
                                Expressions = new List<string> { host["ga:hostname"] }
                            };
                            var dimensionFilterClause = new DimensionFilterClause
                            {
                                Filters = new List<DimensionFilter> { dimensionFilter }
                            };

                            // Create the Request object.
                            var reportRequest = new ReportRequest
                            {
                                DateRanges = new List<DateRange> { dateRange },
                                Dimensions = date,
                                Metrics = sessions,
                                ViewId = viewId,
                                DimensionFilterClauses = new List<DimensionFilterClause> { dimensionFilterClause }
                            };
                            var getReportsRequest = new GetReportsRequest
                            {
                                ReportRequests = new List<ReportRequest> { reportRequest }
                            };
                            #endregion

                            //Invoke Google Analytics API call and get report
                            try
                            {
                                ViewBag.ActiveUsers = GetActiveUsers(viewId);

                                //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                                var response = rm.GetReport(getReportsRequest, new AnalyticsReportingService(new BaseClientService.Initializer
                                {
                                    HttpClientInitializer = result.Credential,
                                    ApplicationName = "UA-131388149-1"
                                }));

                                rest.AddRange(PrintReport(response, host["ga:hostname"]));
                            }
                            catch (Exception ex)
                            {
                                ViewBag.ex = ex.Message;
                                //res = new List<string>() { ex.Message };
                            }
                        });
                        try
                        {

                            //.Where(c => c["ga:city"])
                            ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                            ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                            ViewBag.selectedCity = city;
                            ViewBag.selectedPath = path;
                            ViewBag.UserKeys =
                                rest
                                .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) && (lstPath.Count() == 0 || lstPath.Any(x => c["ga:pagePath"].Contains(x))))
                                .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                                .GroupBy(x => new
                                {
                                    latitude = x["ga:latitude"],
                                    longitude = x["ga:longitude"],
                                    userBucket = x["ga:userBucket"]
                                }).Where(z => z.Select(a => a["ga:timeOnPageNo"]).Select(float.Parse).Sum() > 0 && !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket)).Distinct().ToList();

                            var data = rest.GroupBy(x => new
                            {
                                latitude = x["ga:latitude"],
                                longitude = x["ga:longitude"],
                                userBucket = x["ga:userBucket"]
                            }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).ToDictionary(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket), y => y.ToList());


                            ViewBag.ViewId = data;
                            ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                            int ret = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                            return ret.ToString();
                        }
                        catch (Exception ex)
                        {
                            ViewBag.ex = ex.Message;
                            //res = new List<string>() { ex.Message };
                        }
                    }
                    else
                    {
                        ViewBag.ConsentUrl = result.RedirectUri;
                        //var response = rm.GetReport(getReportsRequest);
                        //res = PrintReport(response);                      
                    }
                }

                ViewBag.ViewId = res;
            }
            else
            {
                DateTime date = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.Date.AddDays(-1) : Convert.ToDateTime(startDate);
                ViewBag.StartDate = date.ToString("yyyy-MM-dd");
                string s = GetVisitorData(viewId, date);
                var rest = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(s);
                if (rest != null)
                {
                    ViewBag.City = rest.Select(d => d["ga:city"]).Distinct();
                    ViewBag.PagePath = rest.Select(d => d["ga:pagePath"]).Distinct();
                    ViewBag.selectedCity = city;
                    ViewBag.selectedPath = path;
                    ViewBag.UserKeys =
                        rest
                        .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) && (lstPath.Count() == 0 || lstPath.Any(x => c["ga:pagePath"].Contains(x))))
                        .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                        .GroupBy(x => new
                        {
                            latitude = x["ga:latitude"],
                            longitude = x["ga:longitude"],
                            userBucket = x["ga:userBucket"],
                            sessionCount = x["ga:sessionCount"]
                        }).Where(z => z.Select(a => a["ga:timeOnPageNo"]).Select(float.Parse).Sum() > 0 && !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket)).ToList();

                    var restD =
                        rest
                        .Where(c => (string.IsNullOrEmpty(city) || c["ga:city"] == city) && (lstPath.Count() == 0 || lstPath.Any(x => c["ga:pagePath"].Contains(x))))
                        .OrderBy(r => Convert.ToDateTime(r["ga:dateHourMinute"]))
                        .GroupBy(x => new
                        {
                            latitude = x["ga:latitude"],
                            longitude = x["ga:longitude"],
                            userBucket = x["ga:userBucket"],
                            sessionCount = x["ga:sessionCount"]
                        }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).Select(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket)).ToList();

                    var data = rest.GroupBy(x => new
                    {
                        latitude = x["ga:latitude"],
                        longitude = x["ga:longitude"],
                        userBucket = x["ga:userBucket"]
                    }).Where(z => !(z.Count() == 1 && z.FirstOrDefault()["timeOnPage"] == "0 sec")).ToDictionary(y => string.Format("{0}_{1}_{2}", y.Key.latitude, y.Key.longitude, y.Key.userBucket), y => y.ToList());

                    //data.Select(a => a[""]).Distinct();

                    ViewBag.ViewId = data;
                    ViewBag.CountData = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                    int ret = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
                    return ret.ToString();//data.Select(d => d).Distinct().ToList().Count.ToString();//ret.ToString();
                }
            }

            //int ret = ViewBag.UserKeys != null ? ViewBag.UserKeys.Count : 0;
            return "0";
        }

        private List<Dictionary<string, string>> GetHourlyUsersList(GetReportsResponse response, string hostname = "")
        {
            List<string> str = new List<string>();
            List<Dictionary<string, string>> adList = new List<Dictionary<string, string>>();
            var isHostName = false;
            foreach (var report in response.Reports)
            {
                var rows = report.Data.Rows;
                ColumnHeader header = report.ColumnHeader;
                var dimensionHeaders = header.Dimensions;
                var metricHeaders = header.MetricHeader.MetricHeaderEntries;
                if (rows == null || !rows.Any())
                {
                    str.Add("No data found!");
                }
                else
                {
                    foreach (var row in rows)
                    {
                        Dictionary<string, string> ad = new Dictionary<string, string>();

                        var dimensions = row.Dimensions;
                        var metrics = row.Metrics;
                        for (int i = 0; i < dimensionHeaders.Count && i < dimensions.Count; i++)
                        {
                            str.Add(dimensionHeaders[i] + ": " + dimensions[i]);
                            if (dimensionHeaders[i] == "ga:dateHour")
                            {
                                ad.Add("DateHour", new DateTime(int.Parse(dimensions[i].Substring(0, 4)),
                                int.Parse(dimensions[i].Substring(4, 2)),
                                int.Parse(dimensions[i].Substring(6, 2)),
                                int.Parse(dimensions[i].Substring(8, 2)),
                                0, 0).ToString("HH:mm"));
                            }
                            else
                            {
                                ad.Add(dimensionHeaders[i], dimensions[i]);
                            }
                        }
                        for (int j = 0; j < metrics.Count; j++)
                        {
                            DateRangeValues values = metrics[j];
                            for (int k = 0; k < values.Values.Count && k < metricHeaders.Count; k++)
                            {
                                //str.Add(metricHeaders[k].Name + ": " + values.Values[k]);

                                if (metricHeaders[k].Name.Equals("users"))
                                {
                                    ad.Add(metricHeaders[k].Name, values.Values[k]);
                                }
                            }
                        }
                        if (!isHostName)
                        {
                            ad.Add("ga:hostname", hostname);
                        }

                        adList.Add(ad);
                    }
                }
            }

            return adList;
        }

        public JsonResult GetHourlyUsers(string viewId, string startDate, string endDate, string city, string path)
        {
            ViewBag.OrgViewId = viewId;
            string adminUser = (string)Session["AdminUser"];
            string hostname = "";
            List<Dictionary<string, string>> hourlyUserData = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(adminUser))
            {


                string dimension = null;// string startDate = "2019-01-01"; string endDate = "2019-01-01";

                // Create the DateRange object. Here we want data from last week.

                var dateRange = new DateRange
                {
                    // DateTime.UtcNow.AddDays(-7)
                    StartDate = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd") : Convert.ToDateTime(startDate).ToString("yyyy-MM-dd"),
                    EndDate = string.IsNullOrEmpty(endDate) ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd") : Convert.ToDateTime(endDate).ToString("yyyy-MM-dd"),
                };

                ViewBag.StartDate = dateRange.StartDate;
                ViewBag.EndDate = dateRange.EndDate;
                // Create the Metrics and dimensions object.
                var metrics = new List<Metric> { new Metric { Expression = "ga:users", Alias = "Users" } };
                var dimensions = new List<Dimension> { new Dimension { Name = "ga:dateHour" } };


                var sessions = new List<Metric> {
                new Metric
                {
                    Expression = "ga:users",
                    Alias = "users"
                }
            };

                var hostData = "ga:dateHour"
                   .Split(',').Select(x => new Dimension { Name = x }).ToList();
                var reportHostRequest = new ReportRequest
                {
                    DateRanges = new List<DateRange> { dateRange },
                    Dimensions = hostData,
                    Metrics = sessions,
                    ViewId = viewId
                };
                var getHostReportsRequest = new GetReportsRequest
                {
                    ReportRequests = new List<ReportRequest> { reportHostRequest }
                };
                Dictionary<string, List<Dictionary<string, string>>> resHost = new Dictionary<string, List<Dictionary<string, string>>>();
                var result = new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                    AuthorizeAsync(CancellationToken.None).Result;
                ReportManager rm = new ReportManager();
                if (result.Credential != null)
                {
                    //"ga:userBucket,ga:dateHourMinute,ga:latitude,ga:longitude,ga:city,ga:pageTitle,ga:pagePathLevel1,ga:landingPagePath"
                    var responseHost = rm.GetReport(getHostReportsRequest, new AnalyticsReportingService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = result.Credential,
                        ApplicationName = "UA-131388149-1"
                    }));

                    hourlyUserData = GetHourlyUsersList(responseHost);
                }
                else
                {
                    ViewBag.ConsentUrl = result.RedirectUri;
                    //var response = rm.GetReport(getReportsRequest);
                    //res = PrintReport(response);
                }
            }
            return Json(hourlyUserData, JsonRequestBehavior.AllowGet);
        }
    }
}