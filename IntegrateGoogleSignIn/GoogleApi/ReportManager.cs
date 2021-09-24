using Google.Apis.Analytics.v3;
using Google.Apis.Analytics.v3.Data;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Services;
using IntegrateGoogleSignIn.Controllers;
using System;
using System.Linq;
using System.Threading;
using System.Web;

namespace IntegrateGoogleSignIn.GoogleApi
{
	public class ReportManager
	{
		/// <summary>
		/// Intializes and returns Analytics Reporting Service Instance using the parameters stored in key file
		/// </summary>
		/// <param name="keyFileName"></param>
		/// <returns></returns>
		public AnalyticsReportingService GetAnalyticsReportingServiceInstance()
		{
			//string[] scopes = { AnalyticsReportingService.Scope.Analytics, StorageService.Scope.CloudPlatform, DataprocService.Scope.CloudPlatform }; //Read-only access to Google Analytics
			GoogleCredential credential;
			string[] scopes = new string[] {
				AnalyticsService.Scope.Analytics,               // view and manage your Google Analytics data
				AnalyticsService.Scope.AnalyticsEdit,           // Edit and manage Google Analytics Account
				AnalyticsService.Scope.AnalyticsManageUsers,    // Edit and manage Google Analytics Users
				AnalyticsService.Scope.AnalyticsReadonly};      // View Google Analytics Data

			//var clientId = "299321306288-t89mkhenotrtau21ei5nfffdj9u7cn8q.apps.googleusercontent.com";      // From https://console.developers.google.com
			//var clientSecret = "eJNLX3ZvnZ496eJ1dR2cDT4P";          // From https://console.developers.google.com
			//								   // here is where we Request the user to give us access, or use the Refresh Token that was previously stored in %AppData%
			//var usercredential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
			//{
			//	ClientId = clientId,
			//	ClientSecret = clientSecret
			//},
			//															scopes,
			//															Environment.UserName,
			//															CancellationToken.None,
			//															new FileDataStore("Daimto.GoogleAnalytics.Auth.Store")).Result;
			//string token = HttpContext.Current.Session["user"].ToString();
			//credential = GoogleCredential.FromAccessToken(token).CreateScoped(scopes);
			//using (var stream = new FileStream(@"C:\Users\hp\Downloads\IntegrateGoogleSignIn\IntegrateGoogleSignIn\IntegrateGoogleSignIn\IntegrateGoogleSignIn\demodev-bb90a15f525a.json", FileMode.Open, FileAccess.Read))
			//{
			//	credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
			//}
			// Create the  Analytics service.

			var result = new AuthorizationCodeMvcApp(new HomeController(), new AppFlowMetadata()).
				AuthorizeAsync(CancellationToken.None).Result;

			if (result.Credential != null)
			{
				return new AnalyticsReportingService(new BaseClientService.Initializer
				{
					HttpClientInitializer = result.Credential,
					ApplicationName = "UA-131388149-1"
				});
				
			}

			string token = HttpContext.Current.Session["user"].ToString();
			credential = GoogleCredential.FromAccessToken(token).CreateScoped(scopes);
			return new AnalyticsReportingService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "UA-131388149-1"
			});
		}

		public AnalyticsService GetAnalyticsServiceInstance()
		{
			//string[] scopes = { AnalyticsReportingService.Scope.Analytics, StorageService.Scope.CloudPlatform, DataprocService.Scope.CloudPlatform }; //Read-only access to Google Analytics
			GoogleCredential credential;
			string[] scopes = new string[] {
				AnalyticsService.Scope.Analytics,               // view and manage your Google Analytics data
				AnalyticsService.Scope.AnalyticsEdit,           // Edit and manage Google Analytics Account
				AnalyticsService.Scope.AnalyticsManageUsers,    // Edit and manage Google Analytics Users
				AnalyticsService.Scope.AnalyticsReadonly};      // View Google Analytics Data
			
			var result = new AuthorizationCodeMvcApp(new HomeController(), new AppFlowMetadata()).
				AuthorizeAsync(CancellationToken.None).Result;

			if (result.Credential != null)
			{
				return new AnalyticsService(new BaseClientService.Initializer
				{
					HttpClientInitializer = result.Credential,
					ApplicationName = "UA-131388149-1"
				});

			}

			string token = HttpContext.Current.Session["user"].ToString();
			credential = GoogleCredential.FromAccessToken(token).CreateScoped(scopes);
			return new AnalyticsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "UA-131388149-1"
			});
		}

		/// <summary>
		/// Fetches all required reports from Google Analytics
		/// </summary>
		/// <param name="reportRequests"></param>
		/// <returns></returns>
		public GetReportsResponse GetReport(GetReportsRequest getReportsRequest)
		{
			var analyticsService = GetAnalyticsReportingServiceInstance();
			return analyticsService.Reports.BatchGet(getReportsRequest).Execute();
		}

		public GetReportsResponse GetReport(GetReportsRequest getReportsRequest, AnalyticsReportingService analyticsService)
		{
			return analyticsService.Reports.BatchGet(getReportsRequest).Execute();
		}

		public GaData GetServiceData(AnalyticsService analyticsService, string profileId)
		{
			var segments = analyticsService.Management.Segments.List().Execute();

			var engagedTeamsSegment = segments.Items.FirstOrDefault(x => x.Name.Equals("New Users", StringComparison.OrdinalIgnoreCase));

			var accountSummary = analyticsService.Management.AccountSummaries.List().Execute();
			var d = analyticsService.Management.CustomDimensions.List(accountSummary.Items[0].Id, accountSummary.Items[0].WebProperties[0].Id).Execute();

			analyticsService.Management.CustomDimensions.Insert(d.Items.Last(),accountSummary.Items[0].Id, accountSummary.Items[0].WebProperties[0].Id);
			var format = "yyyy-MM-dd";
			var today = DateTime.UtcNow.Date;
			var thirtyDaysAgo = today.Subtract(TimeSpan.FromDays(7));

			var webSiteId = accountSummary.Items[0].WebProperties[0].Id;
			//analyticsService.Management.RemarketingAudience.Insert(d.Items.FirstOrDefault(), accountSummary.Items[0].Id, accountSummary.Items[0].WebProperties[0].Id);
			var columns = analyticsService.Metadata.Columns.List("ga").Execute();
			var gaDataRequest = analyticsService
				.Data.Ga
				.Get($"ga:{profileId}", thirtyDaysAgo.ToString(format), today.ToString(format), "ga:users");

			gaDataRequest.Dimensions = "ga:dimension1,ga:sourceMedium,ga:city,ga:pagePathLevel1,ga:pagePathLevel2";// engagedTeamsSegment.Definition;
			var gaData = gaDataRequest.Execute();
			return gaData;
		}
	}
}