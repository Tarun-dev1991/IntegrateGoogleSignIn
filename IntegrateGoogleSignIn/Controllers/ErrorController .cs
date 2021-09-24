using System.Web.Mvc;

namespace IntegrateGoogleSignIn.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        public ActionResult Index(string message)
        {
            return View(message);
        }

        public ActionResult HttpError404(string message)
        {
            Response.StatusCode = 404;
            return View(message);
        }

        public ActionResult HttpError500(string message)
        {
            Response.StatusCode = 500;
            return View(message);
        }
    }
}