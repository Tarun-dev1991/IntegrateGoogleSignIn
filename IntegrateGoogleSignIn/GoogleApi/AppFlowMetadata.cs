using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using Google.Apis.Analytics.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Plus.v1;
using Google.Apis.Util.Store;

namespace IntegrateGoogleSignIn.GoogleApi 
{
	public class AppFlowMetadata : FlowMetadata
	{
		public static string ClientId = ConfigurationManager.AppSettings["Google.ClientID"];
		public static string SecretKey = ConfigurationManager.AppSettings["Google.SecretKey"];
		private static string RedirectUrl = ConfigurationManager.AppSettings["Google.RedirectUrl"];

		private static readonly IAuthorizationCodeFlow flow =
			new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
			{
				ClientSecrets = new ClientSecrets
				{
					ClientId = AppFlowMetadata.ClientId,
					ClientSecret = AppFlowMetadata.SecretKey
				},
				Scopes = new List<string>(){ AnalyticsService.Scope.Analytics,               // view and manage your Google Analytics data
				AnalyticsService.Scope.AnalyticsEdit,           // Edit and manage Google Analytics Account
				AnalyticsService.Scope.AnalyticsManageUsers,    // Edit and manage Google Analytics Users
				AnalyticsService.Scope.AnalyticsReadonly,
				AnalyticsService.Scope.AnalyticsProvision,
				PlusService.Scope.UserinfoProfile,
				PlusService.Scope.UserinfoEmail
				},
				RevokeTokenUrl = AppFlowMetadata.RedirectUrl,

                DataStore = new FileDataStore(@"~\App_Data\Drive.Api.Auth.Store")
               // DataStore = new FileDataStore(@"C:\Inetpub\vhosts\digital-crumbs.co.uk\httpdocs\App_Data\Drive.Api.Auth.Store")
                                               
            });

		public override string GetUserId(Controller controller)
		{
			// In this sample we use the session to store the user identifiers.
			// That's not the best practice, because you should have a logic to identify
			// a user. You might want to use "OpenID Connect".
			// You can read more about the protocol in the following link:
			// https://developers.google.com/accounts/docs/OAuth2Login.
			var user = controller.Session["user"];
			if (user == null)
			{
				user = Guid.NewGuid();
				controller.Session["user"] = user;
			}
			return user.ToString();

		}

		public override IAuthorizationCodeFlow Flow
		{
			get { return flow; }
		}
	}

}