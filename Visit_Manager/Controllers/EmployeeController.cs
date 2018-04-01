using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Visit_Manager.Models;
using Visit_Manager.Repository;
using System.ComponentModel.DataAnnotations;

namespace Visit_Manager.Controllers
{
    public class EmployeeController : BaseController
    {
        // GET: Employee

        public ActionResult Login()
        {
            if (Is_user_logged_in())
            {
                return RedirectToAction("Index", "Visit");
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult Login_user(Employee_model user)
        {
            if (ModelState.IsValid)
            {
                EmployeeRepository employee_repo = new EmployeeRepository();
                Employee user_from_db = new Employee();
                try
                {
                    user_from_db = employee_repo.Get_by_login(user.Login);

                    if (user_from_db != null)
                    {
                        if (user_from_db.password == user.Password.ToString())
                        {
                            if (user_from_db.guid == null) // If the given user logs in for the first time and does not have his guid, he adds guid in db
                            {
                                employee_repo.Add_guid(user_from_db.id);
                            }

                            Session["user_session"] = user_from_db;
                            Session.Timeout = 1;

                            if (user.Remember_me == true)
                            {
                                HttpCookie ciasteczko = new HttpCookie("user_cookie", user_from_db.guid);
                                ciasteczko.Expires = DateTime.Today.AddDays(1);
                                Response.Cookies.Add(ciasteczko);
                            }

                            return RedirectToAction("Index", "Visit");
                        }
                        else
                        {
                            user.Password_valid_message = Resources.Global.Password_incorrect;
                        }
                    }
                    else
                    {
                        user.Name_valid_message = Resources.Global.Login_incorrect;
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                }
            }

            return View("Login", user);
        }

        public ActionResult Log_out()
        {
            try
            {
                Session.Remove("user_session");

                if (Response.Cookies["user_cookie"] != null)
                {
                    Response.Cookies["user_cookie"].Expires = DateTime.Today.AddDays(-1);
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return RedirectToAction("Login", "Employee");
        }

        public ActionResult Return_true_if_user_logged()
        {
            Visit_model model = new Visit_model()
            {
                If_session_expired = false
            };

            if (!Is_user_logged_in())
            {
                model.If_session_expired = true;
            }

            return Json(model, JsonRequestBehavior.AllowGet);
        }











    }
}