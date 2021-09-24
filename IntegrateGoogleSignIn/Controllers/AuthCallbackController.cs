using IntegrateGoogleSignIn.GoogleApi;

namespace IntegrateGoogleSignIn.Controllers
{
	public class AuthCallbackController : Google.Apis.Auth.OAuth2.Mvc.Controllers.AuthCallbackController
	{
		protected override Google.Apis.Auth.OAuth2.Mvc.FlowMetadata FlowData
		{
			get { return new AppFlowMetadata(); }
		}

		//public override async Task<ActionResult> IndexAsync(AuthorizationCodeResponseUrl authorizationCode, CancellationToken taskCancellationToken)
		//{
		//	return RedirectToAction("SaveGoogleUser", "Home", new { code = authorizationCode.Code, state = authorizationCode.State, session_state = "" });
		//}
	}
}