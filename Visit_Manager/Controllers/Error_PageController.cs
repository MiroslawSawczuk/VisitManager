using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Visit_Manager.Controllers
{
    public class Error_PageController : BaseController
    {
        // GET: Error_Page

        public ActionResult Error_404()
        {
            return View();
        }




    }
}