using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Visit_Manager.Models;

namespace Visit_Manager.Controllers
{
    public class LanguageController : BaseController
    {
        // GET: Language

        [HttpGet]
        public void Set(String lang)
        {
            try
            {
                // Set culture to use next
                CultureAttribute.SavePreferredCulture(HttpContext.Response, lang);

                // Return to the calling URL (or go to the site's home page)
                HttpContext.Response.Redirect(HttpContext.Request.UrlReferrer.AbsolutePath);
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }




    }
}