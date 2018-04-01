using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Visit_Manager.Models;
using Visit_Manager.Repository;

namespace Visit_Manager.Controllers
{
    public class BaseController : Controller
    {
        // GET: Base

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        public class CultureAttribute : ActionFilterAttribute
        {
            private const String CookieLangEntry = "language";

            public String Name { get; set; }
            public static String CookieName
            {
                get { return "_Culture"; }
            }

            public override void OnActionExecuting(ActionExecutingContext filterContext)
            {
                var culture = Name;
                if (String.IsNullOrEmpty(culture))
                    culture = GetSavedCultureOrDefault(filterContext.RequestContext.HttpContext.Request);

                // Set culture on current thread
                SetCultureOnThread(culture);

                // Proceed as usual
                base.OnActionExecuting(filterContext);
            }

            public static void SavePreferredCulture(HttpResponseBase response, String language,
                                                    Int32 expireDays = 1)
            {
                var cookie = new HttpCookie(CookieName) { Expires = DateTime.Now.AddDays(expireDays) };
                cookie.Values[CookieLangEntry] = language;
                response.Cookies.Add(cookie);
            }

            public static String GetSavedCultureOrDefault(HttpRequestBase httpRequestBase)
            {
                var culture = "";
                var cookie = httpRequestBase.Cookies[CookieName];
                if (cookie != null)
                    culture = cookie.Values[CookieLangEntry];
                return culture;
            }

            private static void SetCultureOnThread(String language)
            {
                var cultureInfo = CultureInfo.CreateSpecificCulture(language);
                Thread.CurrentThread.CurrentCulture = cultureInfo;
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
            }
        }

        public bool Is_user_logged_in()
        {
            EmployeeRepository employee_repo = new EmployeeRepository();
            bool result = false;

            try
            {
                if (Session["user_session"] != null)
                {
                    Session.Timeout = 1;
                    result = true;
                }
                else if (Request.Cookies["user_cookie"] != null)
                {
                    string cookie = Request.Cookies["user_cookie"].Value;
                    Session["user_session"] = employee_repo.Get_by_guid(cookie);

                    if (Session["user_session"] != null)
                    {
                        Session.Timeout = 1;
                        result = true;
                    }
                    else
                    {
                        Response.Cookies["user_cookie"].Expires = DateTime.Today.AddDays(-1);
                        result = false;
                    }
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return result;
        }






    }
}