using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Net;
using System.Net.Mail;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using Visit_Manager.Models;
using Visit_Manager.Repository;


namespace Visit_Manager.Controllers
{
    public class VisitController : BaseController
    {

        // GET: Visit

        #region Methods in Index view

        public ActionResult Index()
        {
            if (Is_user_logged_in())
            {
                Visit_typeRepository visit_type_repo = new Visit_typeRepository();
                EmployeeRepository employee_repo = new EmployeeRepository();
                List<Employee> list_of_employees = new List<Employee>();
                List<Visit_type> list_of_visit_types = new List<Visit_type>();
                Visit_model model = new Visit_model();

                model.List_of_employees_to_ddl = new List<SelectListItem>();
                model.List_of_visit_types_to_ddl = new List<SelectListItem>();

                try
                {
                    list_of_employees = employee_repo.Get_all_except_admin();
                    list_of_visit_types = visit_type_repo.Get_all_not_removed();

                    foreach (var user in list_of_employees)
                    {
                        model.List_of_employees_to_ddl.Add(new SelectListItem
                        {
                            Text = user.name + " " + user.surname,
                            Value = user.id.ToString()
                        });
                    }
                    foreach (var type in list_of_visit_types)
                    {
                        model.List_of_visit_types_to_ddl.Add(new SelectListItem
                        {
                            Text = type.name,
                            Value = type.id.ToString()
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                }

                return View(model);
            }
            else
            {
                return RedirectToAction("Login", "Employee");
            }
        }

        [HttpGet]
        public ActionResult Get_visits_from_db(string date)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                try
                {
                    DateTime visit_date = Convert_string_to_datetime(date);
                    model.List_of_visits_to_view = Return_visits(visit_date);
                    model.If_session_expired = false;
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                }

                // Sorting must be in return
                return Json(model.List_of_visits_to_view
                         .OrderBy(a => a.Employee_id)
                         .ThenBy(a => a.Start_time.TimeOfDay)
                         .ToList(),
                         JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Get_visit_to_edit(long id)
        {
            Is_user_logged_in(); // Checks whether the session has not expired yet and possibly updates it

            VisitRepository visit_repo = new VisitRepository();
            Visit_typeRepository visit_type_repo = new Visit_typeRepository();
            EmployeeRepository employee_repo = new EmployeeRepository();
            Visit visit_from_db = new Visit();
            Visit_in_view visit_to_edit = new Visit_in_view();
            bool if_employee_removed = false;
            bool if_visit_type_removed = false;

            try
            {
                visit_from_db = visit_repo.Get_by_id(id);
                if_employee_removed = employee_repo.Check_if_removed(visit_from_db.employee_id);
                if_visit_type_removed = visit_type_repo.Check_if_removed(visit_from_db.type_id);// db.Visit_type.Find(visit_from_db.type_id).is_removed;

                if (visit_from_db != null)
                {
                    visit_to_edit = new Visit_in_view()
                    {
                        Id = visit_from_db.id,
                        Client_name = visit_from_db.client_name,
                        Client_surname = visit_from_db.client_surname,
                        Client_email = visit_from_db.client_email,
                        Client_tel_number = visit_from_db.client_tel_number,
                        Describe = visit_from_db.describe,
                        Employee_id = visit_from_db.employee_id,
                        Start_time = visit_from_db.start_time,
                        End_time = visit_from_db.end_time,
                        Price = visit_from_db.price,
                        Type_id = visit_from_db.type_id,
                        Type_unclassified = visit_from_db.type_unclassified
                    };

                    if (if_employee_removed)
                    {
                        // This if is needed to edit visit when the doctor to whom the visit
                        // was previously arranged has been removed, in modal-edit_visit_
                        // when choosing the default value visit was placeholder = 'select doctor' and not empty field as null

                        visit_to_edit.Employee_id = 0;
                    }
                    if (if_visit_type_removed)
                    {
                        visit_to_edit.Type_id = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return Json(visit_to_edit, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Add_new_visit(Visit_from_view new_visit)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        VisitRepository visit_repo = new VisitRepository();
                        EmployeeRepository employee_repo = new EmployeeRepository();
                        List<Visit> list_of_visits = new List<Visit>();
                        List<Employee> list_of_employees = new List<Employee>();
                        bool if_email_sent = false;

                        model.List_of_results = new List<Return_result>();
                        model.If_action_successed = true;
                        model.If_session_expired = false;

                        DateTime date = Convert_string_to_datetime(new_visit.Visit_date);

                        DateTime start_time_visit = new DateTime(date.Year, date.Month, date.Day, new_visit.Start_time.Hours, new_visit.Start_time.Minutes, new_visit.Start_time.Seconds);
                        DateTime end_time_visit = new DateTime(date.Year, date.Month, date.Day, new_visit.End_time.Hours, new_visit.End_time.Minutes, new_visit.End_time.Seconds);


                        list_of_visits = visit_repo.Get_list_by_date_and_employee_id(start_time_visit, new_visit.Employee_id);

                        if (list_of_visits.Count > 0)
                        {
                            // Checking if the added visit does not interfere with other visits on an hourly basis
                            model.List_of_results = Validate_time_of_visit(start_time_visit, end_time_visit, list_of_visits);
                        }
                        if (model.List_of_results.Count == 0)
                        {
                            // Converting data to the Visit class so that this object can be added to the database
                            Visit visit_to_db = new Visit();
                            visit_to_db.client_name = new_visit.Client_name;
                            visit_to_db.client_surname = new_visit.Client_surname;
                            visit_to_db.client_tel_number = new_visit.Client_tel_number;
                            visit_to_db.client_email = new_visit.Client_email;
                            visit_to_db.employee_id = new_visit.Employee_id;
                            visit_to_db.start_time = start_time_visit;
                            visit_to_db.end_time = end_time_visit;
                            visit_to_db.describe = new_visit.Describe;
                            visit_to_db.is_removed = false;
                            visit_to_db.price = new_visit.Price;
                            visit_to_db.type_id = new_visit.Type_id;
                            if (visit_to_db.type_id == 1)
                            {
                                visit_to_db.type_unclassified = new_visit.Type_unclassified;
                            }

                            visit_repo.Add(visit_to_db);


                            if (model.If_action_successed)
                            {
                                model.List_of_results.Add(new Return_result
                                {
                                    Result = true,
                                    Content = Resources.Global.Result_visit_added_correct

                                });

                                list_of_employees = employee_repo.Get_all_except_admin();
                                // Sending an email
                                Visit_mail visit = new Visit_mail()
                                {
                                    Client_name = new_visit.Client_name,
                                    Client_surname = new_visit.Client_surname,
                                    Client_email = new_visit.Client_email,
                                    Start_time = start_time_visit,
                                    Employee_name = list_of_employees
                                                  .Where(a => a.id == new_visit.Employee_id)
                                                  .Select(a => a.name)
                                                  .FirstOrDefault(),
                                    Employee_surname = list_of_employees
                                                  .Where(a => a.id == new_visit.Employee_id)
                                                  .Select(a => a.surname)
                                                  .FirstOrDefault(),
                                };

                                if_email_sent = Send_mail_ordered_visit(visit);

                                if (if_email_sent)
                                {
                                    model.List_of_results.Add(new Return_result
                                    {
                                        Result = true,
                                        Content = Resources.Global.Result_email_visit_added_correct
                                    });
                                }
                                else
                                {
                                    model.List_of_results.Add(new Return_result
                                    {
                                        Result = false,
                                        Content = Resources.Global.Result_email_visit_added_error
                                    });
                                }
                            }
                            else
                            {
                                model.List_of_results.Add(new Return_result
                               {
                                   Result = false,
                                   Content = Resources.Global.Result_visit_added_error
                               });
                            }
                        }
                        else
                        {
                            model.If_action_successed = false;
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_visit_added_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_visit_added_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Edit_visit(Visit_from_view new_visit)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        VisitRepository visit_repo = new VisitRepository();
                        EmployeeRepository employee_repo = new EmployeeRepository();
                        Visit edited_visit = new Visit();
                        List<Visit> list_of_visits = new List<Visit>();
                        List<Employee> list_of_employees = new List<Employee>();
                        bool if_needed_any_validation = true;
                        bool if_needed_time_validation = true;
                        bool if_email_sent = false;

                        model.List_of_results = new List<Return_result>();
                        model.If_action_successed = true;
                        model.If_session_expired = false;


                        DateTime date = Convert_string_to_datetime(new_visit.Visit_date);

                        DateTime start_time_visit = new DateTime(date.Year, date.Month, date.Day, new_visit.Start_time.Hours, new_visit.Start_time.Minutes, new_visit.Start_time.Seconds);
                        DateTime end_time_visit = new DateTime(date.Year, date.Month, date.Day, new_visit.End_time.Hours, new_visit.End_time.Minutes, new_visit.End_time.Seconds);

                        list_of_visits = visit_repo.Get_list_by_date_and_employee_id(start_time_visit, new_visit.Employee_id);

                        // Here it find visit if we edit the visit in the area of ​​the same doctor
                        edited_visit = list_of_visits.Where(a => a.id == new_visit.Visit_id).FirstOrDefault();

                        if (edited_visit != null)
                        {
                            // Checking if any data in the form has been changed
                            if (edited_visit.start_time.Date == start_time_visit.Date &&
                                edited_visit.start_time == start_time_visit &&
                                edited_visit.end_time == end_time_visit &&
                                edited_visit.employee_id == new_visit.Employee_id &&
                                edited_visit.client_name == new_visit.Client_name &&
                                edited_visit.client_surname == new_visit.Client_surname &&
                                edited_visit.client_tel_number == new_visit.Client_tel_number &&
                                edited_visit.client_email == new_visit.Client_email &&
                                edited_visit.describe == new_visit.Describe &&
                                edited_visit.price == new_visit.Price &&
                                edited_visit.type_id == new_visit.Type_id &&
                                edited_visit.type_unclassified == new_visit.Type_unclassified)
                            {

                                if_needed_any_validation = false;

                                if (edited_visit.start_time == start_time_visit &&
                                    edited_visit.end_time == end_time_visit)
                                {
                                    if_needed_time_validation = false;
                                }
                            }
                        }

                        if (if_needed_any_validation)
                        {
                            if (if_needed_time_validation)
                            {
                                if (list_of_visits.Count > 0 && edited_visit != null)
                                {
                                    list_of_visits.Remove(edited_visit);
                                    // Removes a visit from the list which is currently being edited. Needed when for example,
                                    // the time of the visit is changed to the one that is included, or it is caught by the time
                                    // frame of the old visit, then the currently edited visit will not be detected 
                                    // and it will be possible to change the visit time from 10: 00-11: 00 to 9:30 -10: 30
                                }
                                if (list_of_visits.Count > 0)
                                {
                                    model.List_of_results = Validate_time_of_visit(start_time_visit, end_time_visit, list_of_visits);
                                }
                            }

                            if (model.List_of_results.Count == 0)
                            {
                                // If no conditions have been returned from the Validate_visit() method
                                edited_visit = new Visit();
                                edited_visit.id = new_visit.Visit_id;
                                edited_visit.client_name = new_visit.Client_name;
                                edited_visit.client_surname = new_visit.Client_surname;
                                edited_visit.client_tel_number = new_visit.Client_tel_number;
                                edited_visit.client_email = new_visit.Client_email;
                                edited_visit.employee_id = new_visit.Employee_id;
                                edited_visit.start_time = start_time_visit;
                                edited_visit.end_time = end_time_visit;
                                edited_visit.describe = new_visit.Describe;
                                edited_visit.is_removed = false;
                                edited_visit.price = new_visit.Price;
                                edited_visit.type_id = new_visit.Type_id;
                                edited_visit.type_unclassified = new_visit.Type_unclassified;

                                visit_repo.Update(edited_visit);

                                if (model.If_action_successed)
                                {
                                    model.List_of_results.Add(new Return_result
                                    {
                                        Result = true,
                                        Content = Resources.Global.Result_visit_edit_correct
                                    });

                                    list_of_employees = employee_repo.Get_all_except_admin();

                                    // Sending an email
                                    Visit_mail visit = new Visit_mail()
                                    {
                                        Client_name = new_visit.Client_name,
                                        Client_surname = new_visit.Client_surname,
                                        Client_email = new_visit.Client_email,
                                        Start_time = start_time_visit,
                                        Employee_name = list_of_employees
                                                      .Where(a => a.id == new_visit.Employee_id)
                                                      .Select(a => a.name)
                                                      .FirstOrDefault(),
                                        Employee_surname = list_of_employees
                                                      .Where(a => a.id == new_visit.Employee_id)
                                                      .Select(a => a.surname)
                                                      .FirstOrDefault(),
                                    };

                                    if_email_sent = Send_mail_edited_visit(visit);

                                    if (if_email_sent)
                                    {
                                        model.List_of_results.Add(new Return_result
                                        {
                                            Result = true,
                                            Content = Resources.Global.Result_email_visit_edited_correct
                                        });
                                    }
                                    else
                                    {
                                        model.List_of_results.Add(new Return_result
                                        {
                                            Result = false,
                                            Content = Resources.Global.Result_email_visit_edited_error
                                        });
                                    }
                                }
                                else
                                {
                                    model.List_of_results.Add(new Return_result
                                     {
                                         Result = false,
                                         Content = Resources.Global.Result_visit_edit_error
                                     });
                                }
                            }
                            else
                            {
                                model.If_action_successed = false;
                            }
                        }
                        else
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                                   {
                                       Result = false,
                                       Content = Resources.Global.Result_no_changes_in_form
                                   });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_visit_edit_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_visit_edit_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Remove_visit(long id)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                VisitRepository visit_repo = new VisitRepository();
                EmployeeRepository employee_repo = new EmployeeRepository();
                Visit visit_to_remove = new Visit();
                List<Employee> list_of_employees = new List<Employee>();

                model.List_of_results = new List<Return_result>();
                model.If_action_successed = true;
                model.If_session_expired = false;

                try
                {
                    if (ModelState.IsValid)
                    {
                        list_of_employees = employee_repo.Get_all_except_admin();
                        visit_to_remove = visit_repo.Get_by_id(id);
                        visit_repo.Remove(id);

                        if (model.If_action_successed)
                        {
                            model.List_of_results.Add(new Return_result
                             {
                                 Result = true,
                                 Content = Resources.Global.Result_visit_removed_correct
                             });

                            // Sending an email
                            Visit_mail visit = new Visit_mail()
                            {
                                Client_name = visit_to_remove.client_name,
                                Client_surname = visit_to_remove.client_surname,
                                Client_email = visit_to_remove.client_email,
                                Start_time = visit_to_remove.start_time,
                                Employee_name = list_of_employees
                                              .Where(a => a.id == visit_to_remove.employee_id)
                                              .Select(a => a.name)
                                              .FirstOrDefault(),
                                Employee_surname = list_of_employees
                                              .Where(a => a.id == visit_to_remove.employee_id)
                                              .Select(a => a.surname)
                                              .FirstOrDefault(),
                            };

                            bool if_email_sent = Send_mail_removed_visit(visit);

                            if (if_email_sent)
                            {
                                model.List_of_results.Add(new Return_result
                                {
                                    Result = true,
                                    Content = Resources.Global.Result_email_visit_removed_correct
                                });
                            }
                            else
                            {
                                model.List_of_results.Add(new Return_result
                                    {
                                        Result = false,
                                        Content = Resources.Global.Result_email_visit_removed_error
                                    });
                            }
                        }
                        else
                        {
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_visit_removed_error
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_visit_removed_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_visit_removed_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public FileStreamResult Download_visits_in_xlsx(Visit_model model)
        {
            Is_user_logged_in();

            try
            {
                if (ModelState.IsValid)
                {
                    DateTime visit_date = Convert_string_to_datetime(model.Visit_date);
                    model.List_of_visits_to_view = Return_visits(visit_date);

                    MemoryStream ms = new MemoryStream();
                    SpreadsheetDocument xl = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook);
                    WorkbookPart wbp = xl.AddWorkbookPart();
                    WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();
                    Workbook wb = new Workbook();
                    Worksheet ws = new Worksheet();
                    FileVersion fv = new FileVersion();
                    fv.ApplicationName = "Microsoft Office Excel";

                    // Set columns width
                    Columns columns = new Columns();
                    columns.Append(new Column() { Min = 4, Max = 5, Width = 10, CustomWidth = true });
                    columns.Append(new Column() { Min = 6, Max = 8, Width = 12, CustomWidth = true });
                    columns.Append(new Column() { Min = 9, Max = 9, Width = 15, CustomWidth = true });
                    columns.Append(new Column() { Min = 10, Max = 11, Width = 20, CustomWidth = true });
                    columns.Append(new Column() { Min = 12, Max = 12, Width = 12, CustomWidth = true });

                    ws.Append(columns);

                    // Set styles to Cells
                    WorkbookStylesPart stylesPart = xl.WorkbookPart.AddNewPart<WorkbookStylesPart>();
                    stylesPart.Stylesheet = Generate_style_sheet();
                    stylesPart.Stylesheet.Save();

                    SheetData sd = new SheetData();
                    Row row = new Row();
                    Cell cell = new Cell();

                    uint u = Convert.ToUInt32(2);

                    // Table title cell
                    row = new Row()
                    {
                        RowIndex = (UInt32Value)u,// Number of row in specyfic unit
                        Height = 7,               // Row height
                    };

                    cell = new Cell()
                    {
                        CellReference = "H2",
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Visits_on + " "
                            + Convert_date_to_string(visit_date) + " " + Resources.Global.y),
                        StyleIndex = 1,

                    };
                    row.Append(cell);// Add cell to row
                    sd.Append(row);  // Add row to SheetData

                    int r = 4;
                    u = Convert.ToUInt32(r);
                    row = new Row()
                    {
                        RowIndex = (UInt32Value)u
                    };

                    // Write columns titles
                    cell = new Cell()
                    {
                        CellReference = "D" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Start_visit),
                        StyleIndex = 3,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "E" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.End_visit),
                        StyleIndex = 2,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "F" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Employee),
                        StyleIndex = 2,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "G" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Client_name),
                        StyleIndex = 2,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "H" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Client_surname),
                        StyleIndex = 2,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "I" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Visit_type),
                        StyleIndex = 2,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "J" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Unclassified_type),
                        StyleIndex = 2,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "K" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Additional_information),
                        StyleIndex = 2,
                    };
                    row.Append(cell);

                    cell = new Cell()
                    {
                        CellReference = "L" + r,
                        DataType = CellValues.String,
                        CellValue = new CellValue(Resources.Global.Tel_number),
                        StyleIndex = 4,
                    };
                    row.Append(cell);

                    sd.Append(row);

                    // Write columns content
                    if (model.List_of_visits_to_view.Count != 0)
                    {
                        model.List_of_visits_to_view = model.List_of_visits_to_view
                         .OrderBy(a => a.Employee_id)
                         .ThenBy(a => a.Start_time.TimeOfDay)
                         .ToList();

                        int extreme_left_index = 5;
                        int extreme_right_index = 6;
                        int extreme_bottom_index = 10;

                        Visit_in_view last = model.List_of_visits_to_view.Last(); // Set last element in list to later check it

                        foreach (var item in model.List_of_visits_to_view) // Writing out the contents of the columns
                        {
                            if (item.Equals(last)) // If is it the last element set another style to cells to allow set bottom border
                            {
                                extreme_left_index = 7;
                                extreme_right_index = 8;
                                extreme_bottom_index = 9;
                            }

                            r++;
                            u = Convert.ToUInt32(r);

                            row = new Row()
                            {
                                RowIndex = (UInt32Value)u
                            };

                            cell = new Cell()
                            {
                                CellReference = "D" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(Convert_time_to_string(item.Start_time)),
                                StyleIndex = Convert.ToUInt32(extreme_left_index),
                            };
                            row.Append(cell);

                            cell = new Cell()
                            {
                                CellReference = "E" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(Convert_time_to_string(item.End_time)),
                                StyleIndex = Convert.ToUInt32(extreme_bottom_index),
                            };
                            row.Append(cell);

                            cell = new Cell()
                            {
                                CellReference = "F" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(item.Employee_name + " " + item.Employee_surname),
                                StyleIndex = Convert.ToUInt32(extreme_bottom_index),
                            };
                            row.Append(cell);

                            cell = new Cell()
                            {
                                CellReference = "G" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(item.Client_name),
                                StyleIndex = Convert.ToUInt32(extreme_bottom_index),
                            };
                            row.Append(cell);

                            cell = new Cell()
                            {
                                CellReference = "H" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(item.Client_surname),
                                StyleIndex = Convert.ToUInt32(extreme_bottom_index),
                            };
                            row.Append(cell);

                            cell = new Cell()
                            {
                                CellReference = "I" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(item.Type_name),
                                StyleIndex = Convert.ToUInt32(extreme_bottom_index),
                            };
                            row.Append(cell);

                            cell = new Cell()
                            {
                                CellReference = "J" + r,
                                DataType = CellValues.String,
                                StyleIndex = Convert.ToUInt32(extreme_bottom_index),
                            };
                            if (item.Type_id == 1)
                            {
                                cell.CellValue = new CellValue(item.Type_unclassified);
                                row.Append(cell);
                            }
                            else
                            {
                                cell.CellValue = new CellValue("");
                                row.Append(cell);
                            }

                            cell = new Cell()
                            {
                                CellReference = "K" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(item.Describe),
                                StyleIndex = Convert.ToUInt32(extreme_bottom_index),
                            };
                            row.Append(cell);

                            cell = new Cell()
                            {
                                CellReference = "L" + r,
                                DataType = CellValues.String,
                                CellValue = new CellValue(item.Client_tel_number),
                                StyleIndex = Convert.ToUInt32(extreme_right_index),
                            };
                            row.Append(cell);

                            sd.Append(row);
                        }
                    }
                    else
                    {
                        u = Convert.ToUInt32(9);

                        row = new Row()
                        {
                            RowIndex = (UInt32Value)u
                        };

                        cell = new Cell()
                        {
                            CellReference = "H" + 9,
                            DataType = CellValues.String,
                            CellValue = new CellValue(Resources.Global.No_visits_to_display),
                        };
                        row.Append(cell);

                        sd.Append(row);
                    }

                    ws.Append(sd);
                    wsp.Worksheet = ws;
                    wsp.Worksheet.Save();
                    Sheets sheets = new Sheets();
                    Sheet sheet = new Sheet();
                    sheet.Name = Resources.Global.Visits;
                    sheet.SheetId = 1;
                    sheet.Id = wbp.GetIdOfPart(wsp);
                    sheets.Append(sheet);
                    wb.Append(fv);
                    wb.Append(sheets);
                    xl.WorkbookPart.Workbook = wb;
                    xl.WorkbookPart.Workbook.Save();
                    xl.Close();
                    string fileName = model.Visit_date + ".xlsx";
                    Response.Clear();
                    byte[] dt = ms.ToArray();

                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("Content-Disposition", string.Format("attachment; filename={0}", fileName));
                    Response.BinaryWrite(dt);
                    Response.End();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return new FileStreamResult(Response.OutputStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        [HttpGet]
        public FileStreamResult Download_visits_in_pdf(Visit_model model)
        {
            Is_user_logged_in();

            try
            {
                if (ModelState.IsValid)
                {
                    DateTime visit_date = Convert_string_to_datetime(model.Visit_date);
                    model.List_of_visits_to_view = Return_visits(visit_date);

                    MemoryStream stream = new MemoryStream();
                    Document document = new Document(iTextSharp.text.PageSize.LETTER, 10, 10, 42, 35);
                    PdfWriter writer = PdfWriter.GetInstance(document, stream);
                    document.Open();

                    Paragraph doc_title = new Paragraph(Resources.Global.Visits_on + " "
                        + Convert_date_to_string(visit_date) + " " + Resources.Global.y);
                    doc_title.Alignment = Element.ALIGN_CENTER;
                    document.Add(doc_title);
                    document.Add(new Chunk("\n"));
                    document.Add(new Chunk("\n"));

                    string[] headers = new string[]
                { 
                    Resources.Global.Start_visit,
                    Resources.Global.End_visit,
                    Resources.Global.Employee,
                    Resources.Global.Client_name,
                    Resources.Global.Client_surname,
                    Resources.Global.Visit_type,
                    Resources.Global.Type+"\n"+Resources.Global.Unclassified,
                    Resources.Global.Additional_info,
                    Resources.Global.Tel_number
                };

                    // Download file with times new roman font and set character encoding to UTF-8
                    string times_ttf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "times.ttf");

                    // Create base font
                    BaseFont bf = BaseFont.CreateFont(times_ttf, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                    // Font in the header
                    iTextSharp.text.Font font_header = new iTextSharp.text.Font(bf, 10, iTextSharp.text.Font.NORMAL);
                    font_header.Color = BaseColor.RED;

                    // The document's content font
                    iTextSharp.text.Font font_content = new iTextSharp.text.Font(bf, 10, iTextSharp.text.Font.NORMAL);

                    // Creating a table and setting the width
                    PdfPTable table = new PdfPTable(headers.Length) { WidthPercentage = 100 };
                    table.SetWidths(Get_header_widths(font_header, headers));

                    // List column headers
                    for (int i = 0; i < headers.Length; ++i)
                    {
                        table.AddCell(new PdfPCell(new Phrase(headers[i], font_header)) { VerticalAlignment = Element.ALIGN_MIDDLE, HorizontalAlignment = Element.ALIGN_CENTER });
                    }

                    if (model.List_of_visits_to_view.Count != 0)
                    {
                        model.List_of_visits_to_view = model.List_of_visits_to_view
                         .OrderBy(a => a.Employee_id)
                         .ThenBy(a => a.Start_time.TimeOfDay)
                         .ToList();

                        foreach (var visit in model.List_of_visits_to_view)
                        {
                            table.AddCell(new PdfPCell(new Phrase(Convert_time_to_string(visit.Start_time), font_content)));
                            table.AddCell(new PdfPCell(new Phrase(Convert_time_to_string(visit.End_time), font_content)));
                            table.AddCell(new PdfPCell(new Phrase(visit.Employee_name + " " + visit.Employee_surname, font_content)));
                            table.AddCell(new PdfPCell(new Phrase(visit.Client_name, font_content)));
                            table.AddCell(new PdfPCell(new Phrase(visit.Client_surname, font_content)));
                            table.AddCell(new PdfPCell(new Phrase(visit.Type_name, font_content)));

                            if (visit.Type_id == 1)
                            {
                                table.AddCell(new PdfPCell(new Phrase(visit.Type_unclassified, font_content)));
                            }
                            else
                            {
                                table.AddCell(new PdfPCell(new Phrase("", font_content)));
                            }

                            table.AddCell(new PdfPCell(new Phrase(visit.Describe, font_content)));
                            table.AddCell(new PdfPCell(new Phrase(visit.Client_tel_number, font_content)));
                        }

                        document.Add(table);
                    }
                    else
                    {
                        document.Add(table);
                        document.Add(new Chunk("\n"));

                        Paragraph no_visits = new Paragraph(Resources.Global.No_visits_to_display);
                        no_visits.Alignment = Element.ALIGN_CENTER;
                        document.Add(no_visits);
                    }

                    document.Close();

                    Response.ContentType = "application/pdf";
                    Response.AddHeader("content-disposition", "attachment;filename=" + model.Visit_date + ".pdf");
                    Response.Buffer = true;
                    Response.Clear();
                    Response.OutputStream.Write(stream.GetBuffer(), 0, stream.GetBuffer().Length);
                    Response.OutputStream.Flush();
                    Response.End();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return new FileStreamResult(Response.OutputStream, "application/pdf");
        }



        private List<Return_result> Validate_time_of_visit(DateTime start_time_visit, DateTime end_time_visit, List<Visit> list_of_visits_from_db)
        {
            Visit_model model = new Visit_model();
            model.List_of_results = new List<Return_result>();
            bool if_validation_error = false;

            try
            {
                // VERIFYING CONDITIONS

                //<--1 condition--> If the start time is an hour earlier than the end time.
                // You can do it on the client side but by checking all the conditions in one place the code is clearer
                if (start_time_visit.TimeOfDay >= end_time_visit.TimeOfDay)
                {
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_start_visit_must_be_earlier
                    });
                }

                //<--2 condition--> Whether the start of a new visit is between the beginning and the end of a visit in the database
                else if (list_of_visits_from_db
                    .Any(a => a.start_time.TimeOfDay <= start_time_visit.TimeOfDay &&
                              a.end_time.TimeOfDay > start_time_visit.TimeOfDay))
                {
                    if_validation_error = true;
                }

                //<--3 condition-->  Whether the end of a new visit is between the beginning of a visit in the base
                else if (list_of_visits_from_db
                    .Any(a => a.start_time.TimeOfDay < end_time_visit.TimeOfDay &&
                              a.end_time.TimeOfDay >= end_time_visit.TimeOfDay))
                {
                    if_validation_error = true;
                }

                //<--4 condition--> If the new visit is not included in the middle of some visit in the database
                else if (list_of_visits_from_db
                    .Any(a => a.start_time.TimeOfDay <= start_time_visit.TimeOfDay &&
                              a.end_time.TimeOfDay > end_time_visit.TimeOfDay))
                {
                    if_validation_error = true;
                }

                //<--5 condition--> Whether the new visit does not include any visit from the base in the middle
                else if (list_of_visits_from_db
                    .Any(a => a.start_time.TimeOfDay >= start_time_visit.TimeOfDay &&
                              a.end_time.TimeOfDay < end_time_visit.TimeOfDay))
                {
                    if_validation_error = true;
                }

                if (if_validation_error)
                {
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_visit_frame_time_incorrect
                    });
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return model.List_of_results;
        }

        private List<Visit_in_view> Return_visits(DateTime visit_date)
        {
            VisitRepository visit_repo = new VisitRepository();
            EmployeeRepository employee_repo = new EmployeeRepository();
            Visit_typeRepository visit_type_repo = new Visit_typeRepository();
            List<Visit> list_of_visits_from_db = new List<Visit>();
            List<Employee> list_of_employees_from_db = new List<Employee>();
            List<Visit_type> list_of_type_of_visits_from_db = new List<Visit_type>();
            Visit_model model = new Visit_model();

            model.List_of_visits_to_view = new List<Visit_in_view>();

            try
            {
                list_of_visits_from_db = visit_repo.Get_list_by_date(visit_date);
                list_of_employees_from_db = employee_repo.Get_all_except_admin();
                list_of_type_of_visits_from_db = visit_type_repo.Get_all_not_removed();

                if (list_of_visits_from_db.Count > 0)
                {
                    foreach (var visit in list_of_visits_from_db)
                    {
                        model.List_of_visits_to_view.Add(new Visit_in_view
                        {
                            Id = visit.id,
                            Client_name = visit.client_name,
                            Client_surname = visit.client_surname,
                            Client_tel_number = visit.client_tel_number,
                            Client_email = visit.client_email,
                            Employee_id = visit.employee_id,
                            Start_time = visit.start_time,
                            End_time = visit.end_time,
                            Describe = visit.describe,
                            Employee_name = list_of_employees_from_db
                                  .Where(a => a.id == visit.employee_id)
                                  .Select(a => a.name)
                                  .FirstOrDefault(),
                            Employee_surname = list_of_employees_from_db
                                          .Where(a => a.id == visit.employee_id)
                                          .Select(a => a.surname)
                                          .FirstOrDefault(),
                            Price = visit.price,
                            Type_id = visit.type_id,
                            Type_unclassified = visit.type_unclassified,
                            Type_name = list_of_type_of_visits_from_db
                                  .Where(a => a.id == visit.type_id)
                                  .Select(a => a.name)
                                  .FirstOrDefault()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return model.List_of_visits_to_view;
        }


        private bool Send_mail_ordered_visit(Visit_mail visit)
        {
            bool if_msg_sent = true;

            try
            {
                var msg = new MailMessage();

                msg.From = new MailAddress("visitmanagermail@gmail.com", Resources.Global.Your_dentist);
                msg.To.Add(new MailAddress(visit.Client_email));

                msg.Subject = Resources.Global.Dentist_visit;

                msg.Body = "<div style='background-color:#F6F6F6; padding:15px'>";
                msg.Body += "<h3>" + Resources.Global.Welcome + " " + visit.Client_name + " " + visit.Client_surname + " !</h3>";
                msg.Body += "<br/>";
                msg.Body += "<p>" + Resources.Global.We_are_pleased_to_inform + "</p>";
                msg.Body += "<p>" + Resources.Global.Your_visit + "</p>";
                msg.Body += "<table><tr><td>" + Resources.Global.Date + "</td>";
                msg.Body += "<td><b>" + Convert_date_to_string(visit.Start_time) + " r.</b></td></tr>";
                msg.Body += "<tr><td>" + Resources.Global.Hour + "</td><td> <b>" + Convert_time_to_string(visit.Start_time) + "</b></td></tr>";
                msg.Body += "<tr><td>" + Resources.Global.Doctor + "</td><td><b>" + visit.Employee_name + " " + visit.Employee_surname + "</b></td></tr></table>";
                msg.Body += "<br/>";
                msg.Body += "<p>" + Resources.Global.If_you_have_any_questions + " <b>555 555 555</b></p>";
                msg.Body += "<p>" + Resources.Global.See_you_later + "</p>";
                msg.Body += "<br/>";
                msg.Body += "<img width='600' src='https://maps.googleapis.com/maps/api/staticmap?center=Białystok+Lipowa+123,+PL&zoom=16&scale=false&size=600x300&maptype=roadmap&format=png&visual_refresh=true&markers=size:mid%7Ccolor:0xff0000%7Clabel:%7CBiałystok+Lipowa+123,+PL' alt='Google Map of Białystok Lipowa 123, PL'>";
                msg.Body += "<br />";
                msg.Body += "<p>" + Resources.Global.Our_adress + "</p>";
                msg.Body += "<p><b>" + Resources.Global.Street + " Lipowa 123 lok. 12</b></p>";
                msg.Body += "<p><b>15-062 Białystok </b></p>";
                msg.Body += "<br/>";
                msg.Body += "<br/>";
                msg.Body += "<p>" + Resources.Global.Message_was_generated_automatically + " ";
                msg.Body += "<a href='http://visitmanager.pl'>www.visitmanager.pl</a> ";
                msg.Body += Resources.Global.Please_do_not_answer_it + "</p>";
                msg.Body += "</div>";

                msg.IsBodyHtml = true;
                msg.SubjectEncoding = System.Text.Encoding.UTF8;
                msg.BodyEncoding = System.Text.Encoding.UTF8;

                var smtp = new SmtpClient("smtp.gmail.com");
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("visitmanagermail@gmail.com", "Haslo123!");
                smtp.EnableSsl = true;
                smtp.Port = 587;
                smtp.Send(msg);
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
                if_msg_sent = false;
            }

            return if_msg_sent;
        }

        private bool Send_mail_edited_visit(Visit_mail visit)
        {
            bool if_msg_sent = true;

            try
            {
                var msg = new MailMessage();

                msg.From = new MailAddress("visitmanagermail@gmail.com", Resources.Global.Your_dentist);
                msg.To.Add(new MailAddress(visit.Client_email));

                msg.Subject = Resources.Global.Your_visit_edited;

                msg.Body = "<div style='background-color:#F6F6F6; padding:15px'>";
                msg.Body += "<h3>" + Resources.Global.Welcome + " " + visit.Client_name + " " + visit.Client_surname + " !</h3>";
                msg.Body += "<br/>";
                msg.Body += "<br/>";
                msg.Body += "<p style='color:red'>" + Resources.Global.Your_visit_edited + "</p>";
                msg.Body += "<p>" + Resources.Global.Your_visit + "</p>";
                msg.Body += "<table><tr><td>" + Resources.Global.Date + "</td>";
                msg.Body += "<td><b>" + Convert_date_to_string(visit.Start_time) + " r.</b></td></tr>";
                msg.Body += "<tr><td>" + Resources.Global.Hour + "</td><td> <b>" + Convert_time_to_string(visit.Start_time) + "</b></td></tr>";
                msg.Body += "<tr><td>" + Resources.Global.Doctor + "</td><td><b>" + visit.Employee_name + " " + visit.Employee_surname + "</b></td></tr></table>";
                msg.Body += "<br/>";
                msg.Body += "<p>" + Resources.Global.If_you_have_any_questions + " <b>555 555 555</b></p>";
                msg.Body += "<p>" + Resources.Global.See_you_later + "</p>";
                msg.Body += "<br/>";
                msg.Body += "<img width='600' src='https://maps.googleapis.com/maps/api/staticmap?center=Białystok+Lipowa+123,+PL&zoom=16&scale=false&size=600x300&maptype=roadmap&format=png&visual_refresh=true&markers=size:mid%7Ccolor:0xff0000%7Clabel:%7CBiałystok+Lipowa+123,+PL' alt='Google Map of Białystok Lipowa 123, PL'>";
                msg.Body += "<br />";
                msg.Body += "<p>" + Resources.Global.Our_adress + "</p>";
                msg.Body += "<p><b>" + Resources.Global.Street + " Lipowa 123 lok. 12</b></p>";
                msg.Body += "<p><b>15-062 Białystok </b></p>";
                msg.Body += "<br/>";
                msg.Body += "<br/>";
                msg.Body += "<p>" + Resources.Global.Message_was_generated_automatically + " ";
                msg.Body += "<a href='http://visitmanager.pl'>www.visitmanager.pl</a> ";
                msg.Body += Resources.Global.Please_do_not_answer_it + "</p>";
                msg.Body += "</div>";

                msg.IsBodyHtml = true;
                msg.SubjectEncoding = System.Text.Encoding.UTF8;
                msg.BodyEncoding = System.Text.Encoding.UTF8;

                var smtp = new SmtpClient("smtp.gmail.com");
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("visitmanagermail@gmail.com", "Haslo123!");
                smtp.EnableSsl = true;
                smtp.Port = 587;
                smtp.Send(msg);
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
                if_msg_sent = false;
            }

            return if_msg_sent;
        }

        private bool Send_mail_removed_visit(Visit_mail visit)
        {
            bool if_msg_sent = true;

            try
            {
                var msg = new MailMessage();

                msg.From = new MailAddress("visitmanagermail@gmail.com", Resources.Global.Your_dentist);
                msg.To.Add(new MailAddress(visit.Client_email));

                msg.Subject = Resources.Global.Your_visit_canceled;

                msg.Body = "<div style='background-color:#F6F6F6; padding:15px'>";
                msg.Body += "<h3>" + Resources.Global.Welcome + " " + visit.Client_name + " " + visit.Client_surname + " !</h3>";
                msg.Body += "<br/>";
                msg.Body += "<p style='color:red'>" + Resources.Global.Your_visit_canceled + "</p>";
                msg.Body += "<p>" + Resources.Global.Your_visit + "</p>";
                msg.Body += "<table><tr><td>" + Resources.Global.Date + "</td>";
                msg.Body += "<td><b>" + Convert_date_to_string(visit.Start_time) + " r.</b></td></tr>";
                msg.Body += "<tr><td>" + Resources.Global.Hour + "</td><td> <b>" + Convert_time_to_string(visit.Start_time) + "</b></td></tr>";
                msg.Body += "<tr><td>" + Resources.Global.Doctor + "</td><td><b>" + visit.Employee_name + " " + visit.Employee_surname + "</b></td></tr></table>";
                msg.Body += "<br/>";
                msg.Body += "<p>" + Resources.Global.If_you_have_any_questions + " <b>555 555 555</b></p>";
                msg.Body += "<p>" + Resources.Global.See_you_later + "</p>";
                msg.Body += "<br/>";
                msg.Body += "<img width='600' src='https://maps.googleapis.com/maps/api/staticmap?center=Białystok+Lipowa+123,+PL&zoom=16&scale=false&size=600x300&maptype=roadmap&format=png&visual_refresh=true&markers=size:mid%7Ccolor:0xff0000%7Clabel:%7CBiałystok+Lipowa+123,+PL' alt='Google Map of Białystok Lipowa 123, PL'>";
                msg.Body += "<br />";
                msg.Body += "<p>" + Resources.Global.Our_adress + "</p>";
                msg.Body += "<p><b>" + Resources.Global.Street + " Lipowa 123 lok. 12</b></p>";
                msg.Body += "<p><b>15-062 Białystok </b></p>";
                msg.Body += "<br/>";
                msg.Body += "<br/>";
                msg.Body += "<p>" + Resources.Global.Message_was_generated_automatically + " ";
                msg.Body += "<a href='http://visitmanager.pl'>www.visitmanager.pl</a> ";
                msg.Body += Resources.Global.Please_do_not_answer_it + "</p>";
                msg.Body += "</div>";

                msg.IsBodyHtml = true;
                msg.SubjectEncoding = System.Text.Encoding.UTF8;
                msg.BodyEncoding = System.Text.Encoding.UTF8;

                var smtp = new SmtpClient("smtp.gmail.com");
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("visitmanagermail@gmail.com", "Haslo123!");
                smtp.EnableSsl = true;
                smtp.Port = 587;
                smtp.Send(msg);
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
                if_msg_sent = false;
            }

            return if_msg_sent;
        }


        private Stylesheet Generate_style_sheet()
        {
            return new Stylesheet(
                new Fonts(
                    new DocumentFormat.OpenXml.Spreadsheet.Font(                        // Index 0 - The default font.
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new DocumentFormat.OpenXml.Spreadsheet.Font(                        // Index 1 - The bold font.
                        new Bold(),
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new DocumentFormat.OpenXml.Spreadsheet.Font(                        // Index 2 - The font color: red.
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "fd4000" } },
                        new FontName() { Val = "Calibri" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),

                    new Border(                                                         // Index 1 - Applies a Top, Bottom border to a cell
                        new TopBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),

                    new Border(                                                         // Index 2 - Applies a Top, Bottom, Left border to a cell
                        new LeftBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),

                    new Border(                                                         // Index 3 - Applies a Top, Bottom, Right border to a cell
                        new RightBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),

                    new Border(                                                         // Index 4 - Applies a Left border to a cell
                        new LeftBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),

                    new Border(                                                         // Index 5 - Applies a Right border to a cell
                        new RightBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),

                    new Border(                                                         // Index 6 - Applies a Left, Bottom border to a cell
                        new LeftBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),

                    new Border(                                                         // Index 7 - Applies a Right, Bottom,  border to a cell
                        new RightBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder()),

                    new Border(                                                         // Index 8 - Applies a Bottom  border to a cell
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),

                //Cell formats to set w SetIndex while creating cells
                new CellFormats(

                    new CellFormat(                                                     // Index 0 - The default cell style.  
                        new Alignment()                                                 // If a cell does not have a style index applied it will use this style combination instead
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Top,
                            Horizontal = HorizontalAlignmentValues.Center
                        }) { FontId = 0, FillId = 0, BorderId = 0 },

                    new CellFormat(                                                     // Index 1 - Bold -table title
                        new Alignment()
                        {
                            //WrapText = true,
                            //Vertical = VerticalAlignmentValues.Center,
                            //Horizontal = HorizontalAlignmentValues.Center
                        }) { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },

                    new CellFormat(                                                     // Index 2 - Red font color, Top, Bottom border -Column titles
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Center,
                            Horizontal = HorizontalAlignmentValues.Center
                        }) { FontId = 2, FillId = 0, BorderId = 1, ApplyFont = true },

                    new CellFormat(                                                     // Index 3 - Red font color, Top, Bottom left border -Extreme left column title
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Center,
                            Horizontal = HorizontalAlignmentValues.Center
                        }) { FontId = 2, FillId = 0, BorderId = 2, ApplyFont = true },

                    new CellFormat(                                                     // Index 4 - Red font color, Top, Bottom right border -Extreme right column title
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Center,
                            Horizontal = HorizontalAlignmentValues.Center
                        }) { FontId = 2, FillId = 0, BorderId = 3, ApplyFill = true },

                    new CellFormat(                                                     // Index 5 - Left border -Extreme left content cell
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Top
                        }) { FontId = 0, FillId = 0, BorderId = 4, ApplyFill = true },

                    new CellFormat(                                                     // Index 6 - Right border -Extreme right content cell
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Top
                        }) { FontId = 0, FillId = 0, BorderId = 5, ApplyFill = true },

                    new CellFormat(                                                     // Index 7 - Left, Bottom border -Extreme bottom, left content cell
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Top
                        }) { FontId = 0, FillId = 0, BorderId = 6, ApplyFill = true },

                    new CellFormat(                                                     // Index 8 - Right, Bottom border -Extreme bottom, right content cell
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Top
                        }) { FontId = 0, FillId = 0, BorderId = 7, ApplyFill = true },

                    new CellFormat(                                                     // Index 9 - Bottom border -Extreme bottom content cell
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Top
                        }) { FontId = 0, FillId = 0, BorderId = 8, ApplyFill = true },
                    new CellFormat(                                                     // Index 10 - Bottom border - Normal cell inside table
                        new Alignment()
                        {
                            WrapText = true,
                            Vertical = VerticalAlignmentValues.Top
                        }) { FontId = 0, FillId = 0, BorderId = 0, ApplyFill = true }
                )
            );
        }

        private float[] Get_header_widths(iTextSharp.text.Font font, params string[] headers)
        {
            var total = 0;
            var columns = headers.Length;
            var widths = new int[columns];

            for (var i = 0; i < columns; ++i)
            {
                var w = font.GetCalculatedBaseFont(true).GetWidth(headers[i]);
                total += w;
                widths[i] = w;
            }

            var result = new float[columns];

            for (var i = 0; i < columns; ++i)
            {
                result[i] = (float)widths[i] / total * 100;
            }

            return result;
        }

        private string Convert_date_to_string(DateTime date)
        {
            string string_date = "";

            try
            {
                string yyyy = date.Year.ToString(); // Add 1 month - January is [0]
                string mm = date.Month.ToString();
                string dd = date.Day.ToString();

                if (date.Month < 10)  // Adding 0 in front of a one-digit number
                {
                    mm = "0" + mm;
                }
                if (date.Day < 10)
                {
                    dd = "0" + dd;
                }
                string_date = dd + "." + mm + "." + yyyy;
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return string_date;
        }

        private string Convert_time_to_string(DateTime date)
        {
            string string_time = "";

            try
            {
                string hh = date.Hour.ToString();
                string mm = date.Minute.ToString();

                if (date.Hour < 10)  // Adding 0 in front of a one-digit number
                {
                    hh = "0" + hh;
                }
                if (date.Minute < 10)
                {
                    mm = "0" + mm;
                }
                string_time = hh + ':' + mm;
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return string_time;
        }

        private DateTime Convert_string_to_datetime(string string_date)
        {
            DateTime date = new DateTime();

            try
            {
                string[] date_array = string_date.Split('.');

                int dd = int.Parse(date_array[0]);
                int mm = int.Parse(date_array[1]);
                int yyyy = int.Parse(date_array[2]);

                date = new DateTime(yyyy, mm, dd, 0, 0, 0);
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return date;
        }


        #endregion




        #region Methods in Admin view

        public ActionResult Admin()
        {
            if (Is_user_logged_in())
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Employee");
            }
        }

        [HttpGet]
        public ActionResult Get_data_to_top_chart()
        {
            // This method is run with other methods that already check if the user is logged in,
            // hence the Is_user_logged_in () method is not needed

            VisitRepository visit_repo = new VisitRepository();
            List<Top_chart> list_of_data_to_top_chart = new List<Top_chart>();
            List<Visit> list_of_visits = new List<Visit>();

            DateTime date_today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day,
                                               DateTime.Now.Hour, DateTime.Now.Minute, 0);
            DateTime before_11_months = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            before_11_months = before_11_months.AddMonths(-11);

            try
            {
                list_of_visits = visit_repo.Get_list_to_top_chart(date_today);

                DateTime start_range_date = new DateTime(date_today.Year, date_today.Month, 1);
                start_range_date = start_range_date.AddMonths(-11);
                DateTime end_range_date = start_range_date.AddMonths(1);

                while (start_range_date <= DateTime.Today)// Execute as long as the condition is met
                {
                    list_of_data_to_top_chart.Add(new Top_chart
                    {
                        Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(start_range_date.Month),
                        Year = start_range_date.Year,
                        Visit_count = list_of_visits.Where(a => a.start_time >= start_range_date
                        && a.start_time < end_range_date)
                        .Count()
                    });

                    start_range_date = start_range_date.AddMonths(1);
                    end_range_date = end_range_date.AddMonths(1);
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return Json(list_of_data_to_top_chart, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Get_data_to_bottom_charts(string date_range)
        {
            if (Is_user_logged_in())
            {
               // VisitRepository visit_repo = new VisitRepository();
                EmployeeRepository employee_repo = new EmployeeRepository();
                //Visit_typeRepository visit_type_repo = new Visit_typeRepository();

                List<Visit> list_of_visits = new List<Visit>();
                List<Employee> list_of_employees = new List<Employee>();
                List<Visit_type> list_of_visit_types = new List<Visit_type>();
                List<Bottom_charts> list_of_employee_data = new List<Bottom_charts>();
                List<List<Bottom_charts>> lists_of_data_to_horizontalBar_charts = new List<List<Bottom_charts>>();

                try
                {
                    if (ModelState.IsValid)
                    {
                        string[] start_end_date = date_range.Split('-');

                        string[] str_date = start_end_date[0].Split('.');
                        int start_dd = int.Parse(str_date[0]);
                        int start_mm = int.Parse(str_date[1]);
                        int start_yyyy = int.Parse(str_date[2]);

                        string[] en_date = start_end_date[1].Split('.');
                        int end_dd = int.Parse(en_date[0]);
                        int end_mm = int.Parse(en_date[1]);
                        int end_yyyy = int.Parse(en_date[2]);

                        DateTime start_range_date = new DateTime(start_yyyy, start_mm, start_dd);
                        DateTime end_range_date = new DateTime(end_yyyy, end_mm, end_dd, 23, 59, 59);

                        if (end_range_date.Date >= DateTime.Today.Date)
                        {
                            // If the end date chosen is today or later to the one that is now
                            // and not the hour of midnight this is because I only display completed / past visits

                            end_range_date = DateTime.Now;
                        }

                        //list_of_visits = visit_repo.Get_list_to_bottom_charts(start_range_date, end_range_date);
                        list_of_employees = employee_repo.Get_all_except_admin();
                        //list_of_visit_types = visit_type_repo.Get_all();

                        using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                        {
                            list_of_visits = db.Visit.Where(a =>
                                a.is_removed == false &&
                                a.Employee.is_removed == false &&
                                a.employee_id != 1 &&
                                a.start_time >= start_range_date &&
                                a.start_time <= end_range_date)
                                .ToList();

                            //list_of_employees = db.Employee
                            //    .Where(a => a.is_removed == false &&
                            //        a.id != 1)
                            //    .ToList();

                            // //It also downloads from these types of visits that have been deleted
                            list_of_visit_types = db.Visit_type.ToList();
                        }

                        foreach (var employee in list_of_employees)// The list iterates for employees
                        {
                            list_of_employee_data = new List<Bottom_charts>();

                            foreach (var visit_type in list_of_visit_types)// The list iterates for visit types
                            {
                                // Adds a new visit type for a given user and counts the number of such visits
                                list_of_employee_data.Add(new Bottom_charts
                                {
                                    Employee_name = employee.name + " " + employee.surname,
                                    Visit_type = visit_type.name,
                                    Visit_count = list_of_visits.Where(a => a.employee_id == employee.id &&
                                        a.Visit_type.id == visit_type.id)
                                        .Count(),
                                    Visit_type_total_price = list_of_visits.Where(a => a.employee_id == employee.id &&
                                    a.Visit_type.id == visit_type.id).Select(a => a.price).Sum(),
                                });
                            }

                            lists_of_data_to_horizontalBar_charts.Add(list_of_employee_data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                }

                return Json(lists_of_data_to_horizontalBar_charts, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Visit_model model = new Visit_model();
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult Add_new_employee(Employee new_employee)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                EmployeeRepository employee_repo = new EmployeeRepository();
                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        new_employee.is_removed = false;

                        Employee employee_from_db = employee_repo.Get_by_name_surname(new_employee.name, new_employee.surname);

                        if (employee_from_db == null) // If in db there is no employee with the same name and surname
                        {
                            employee_repo.Add(new_employee);
                        }
                        else
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_employee_already_exist_in_db
                            });
                        }

                        if (model.If_action_successed)
                        {
                            model.List_of_results.Add(new Return_result
                            {
                                Result = true,
                                Content = Resources.Global.Result_employee_added_correct
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_employee_added_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_employee_added_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }
      
        [HttpPost]
        public ActionResult Add_new_visit_type(Visit_type new_visit_type)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                Visit_typeRepository visit_type_repo = new Visit_typeRepository();

                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        new_visit_type.is_removed = false;

                        Visit_type visit_type_from_db = visit_type_repo.Get_by_name(new_visit_type.name);

                        if (visit_type_from_db == null)
                        {
                            visit_type_repo.Add(new_visit_type);
                        }
                        else
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_visit_type_already_exist_in_db
                            });
                        }

                        if (model.If_action_successed)
                        {
                            model.List_of_results.Add(new Return_result
                            {
                                Result = true,
                                Content = Resources.Global.Result_visit_type_added_correct
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = true,
                            Content = Resources.Global.Result_visit_type_added_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = true,
                        Content = Resources.Global.Result_visit_type_added_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult Edit_employee(Employee employee_to_edit)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                EmployeeRepository employee_repo = new EmployeeRepository();
                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        if (employee_repo.Return_true_if_employee_not_repeted_in_db(employee_to_edit)) // If in db there is no user with the same name and surname
                        {
                            Employee employee = employee_repo.Get_by_id(employee_to_edit.id);

                            if (employee.name != employee_to_edit.name ||
                            employee.surname != employee_to_edit.surname) // If any changes have been made to the form
                            {
                                employee_repo.Update(employee_to_edit);
                            }
                            else
                            {
                                model.If_action_successed = false;
                                model.List_of_results.Add(new Return_result
                                {
                                    Result = false,
                                    Content = Resources.Global.Result_no_changes_in_form
                                });
                            }
                        }
                        else
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_employee_already_exist_in_db
                            });
                        }

                        if (model.If_action_successed)
                        {
                            model.List_of_results.Add(new Return_result
                            {
                                Result = true,
                                Content = Resources.Global.Result_employee_edit_correct
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_employee_edit_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_employee_edit_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Edit_visit_type(Visit_type visit_type_to_edit)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                Visit_typeRepository visit_type_repo = new Visit_typeRepository();

                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        if (visit_type_repo.Return_true_if_name_not_repeted_in_db(visit_type_to_edit))// If there is no type of visit with the same name
                        {
                            Visit_type visit_type = visit_type_repo.Get_by_id(visit_type_to_edit.id);

                            if (visit_type.name != visit_type_to_edit.name)
                            {
                                visit_type_repo.Update(visit_type_to_edit);
                            }
                            else
                            {
                                model.If_action_successed = false;
                                model.List_of_results.Add(new Return_result
                                {
                                    Result = false,
                                    Content = Resources.Global.Result_no_changes_in_form
                                });
                            }
                        }
                        else
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_visit_type_already_exist_in_db
                            });
                        }

                        if (model.If_action_successed)
                        {
                            model.List_of_results.Add(new Return_result
                            {
                                Result = true,
                                Content = Resources.Global.Result_visit_type_edit_correct
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_visit_type_edit_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_visit_type_edit_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult Remove_employee(long id)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                EmployeeRepository employee_repo = new EmployeeRepository();
                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        employee_repo.Remove(id);

                        if (model.If_action_successed)
                        {
                            model.List_of_results.Add(new Return_result
                            {
                                Result = true,
                                Content = Resources.Global.Result_employee_removed_correct
                            });
                        }
                        else
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_employee_removed_error
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_employee_removed_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_employee_removed_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Remove_visit_type(long id)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                Visit_typeRepository visit_type_repo = new Visit_typeRepository();

                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        visit_type_repo.Remove(id);

                        if (model.If_action_successed)
                        {
                            model.List_of_results.Add(new Return_result
                            {
                                Result = true,
                                Content = Resources.Global.Result_visit_type_removed_correct
                            });
                        }
                        else
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_visit_type_removed_error
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_visit_type_removed_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_visit_type_removed_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;

                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult Change_password(string old_pass, string new_pass)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                EmployeeRepository employee_repo = new EmployeeRepository();
                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        long id = ((Employee)Session["user_session"]).id;

                        Employee employee_from_db = employee_repo.Get_by_id_and_password(id, old_pass);

                        if (employee_from_db != null)
                        {
                            employee_repo.Update_password(id, new_pass);
                        }
                        else
                        {
                            model.If_action_successed = false;
                        }

                        if (!model.If_action_successed)
                        {
                            model.If_action_successed = false;
                            model.List_of_results.Add(new Return_result
                            {
                                Result = false,
                                Content = Resources.Global.Result_password_changing_error
                            });
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_password_changing_error
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_password_changing_error
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public ActionResult Get_users_to_ddl()
        {
            EmployeeRepository employee_repo = new EmployeeRepository();
            List<Employee> list_of_employees_from_db = new List<Employee>();
            List<Employee> list_of_employees_to_view = new List<Employee>();

            try
            {
                list_of_employees_from_db = employee_repo.Get_all_except_admin();

                if (list_of_employees_from_db.Count > 0)
                {
                    // It must be converted, otherwise it will not send the list to the view
                    foreach (var user in list_of_employees_from_db)
                    {
                        list_of_employees_to_view.Add(new Employee
                        {
                            id = user.id,
                            name = user.name,
                            surname = user.surname
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return Json(list_of_employees_to_view, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Get_visit_type_to_ddl()
        {
            Is_user_logged_in(); // Checks if the session has not expired yet and possibly updates it

            List<Visit_type> list_of_visit_types_from_db = new List<Visit_type>();
            List<Visit_type> list_of_visit_types_to_view = new List<Visit_type>();
            Visit_typeRepository visit_type_repo = new Visit_typeRepository();

            try
            {
                list_of_visit_types_from_db = visit_type_repo.Get_all_except_first();

                if (list_of_visit_types_from_db.Count > 0)
                {
                    // It must be converted, otherwise it will not send the list to the view
                    foreach (var type in list_of_visit_types_from_db)
                    {
                        list_of_visit_types_to_view.Add(new Visit_type
                        {
                            id = type.id,
                            name = type.name,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return Json(list_of_visit_types_to_view, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult Set_employee_data_to_inputs(long id)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                EmployeeRepository employee_repo = new EmployeeRepository();
                Employee employee_from_db = new Employee();
                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        employee_from_db = employee_repo.Get_by_id(id);

                        if (employee_from_db != null) // It must be converted, otherwise it will not send model.Employee to view
                        {
                            model.Employee = new Employee()
                            {
                                id = employee_from_db.id,
                                name = employee_from_db.name,
                                surname = employee_from_db.surname,
                                is_removed = employee_from_db.is_removed
                            };
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_enable_to_get_employee
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_enable_to_get_employee
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Set_visit_type_to_input(long id)
        {
            Visit_model model = new Visit_model();

            if (Is_user_logged_in())
            {
                Visit_typeRepository visit_type_repo = new Visit_typeRepository();
                Visit_type visit_type_from_db = new Visit_type();

                model.List_of_results = new List<Return_result>();
                model.If_session_expired = false;
                model.If_action_successed = true;

                try
                {
                    if (ModelState.IsValid)
                    {
                        visit_type_from_db = visit_type_repo.Get_by_id(id);

                        if (visit_type_from_db != null) // It must be converted, otherwise it will not send model.Visit_type to view
                        {
                            model.Visit_type = new Visit_type()
                            {
                                id = visit_type_from_db.id,
                                name = visit_type_from_db.name,
                                is_removed = visit_type_from_db.is_removed
                            };
                        }
                    }
                    else
                    {
                        model.If_action_successed = false;
                        model.List_of_results.Add(new Return_result
                        {
                            Result = false,
                            Content = Resources.Global.Result_enable_to_get_visit_type
                        });
                    }
                }
                catch (Exception ex)
                {
                    NLog_logger.Loger.Error(ex);
                    model.If_action_successed = false;
                    model.List_of_results.Add(new Return_result
                    {
                        Result = false,
                        Content = Resources.Global.Result_enable_to_get_visit_type
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            else
            {
                model.If_session_expired = true;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

    }
}