using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Visit_Manager.Models;

namespace Visit_Manager.Repository
{
    public class VisitRepository
    {
        public Visit Get_by_id(long id)
        {
            Visit visit = new Visit();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    visit = db.Visit.Find(id);
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return visit;
        }

        public List<Visit> Get_list_by_date_and_employee_id(DateTime date, long id)
        {
            List<Visit> list_of_visits = new List<Visit>();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_visits = db.Visit
                                    .Where(a => a.is_removed == false
                                        && a.employee_id == id
                                        && a.start_time.Year == date.Year
                                        && a.start_time.Month == date.Month
                                        && a.start_time.Day == date.Day)
                                        .ToList();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return list_of_visits;
        }

        public List<Visit> Get_list_by_date(DateTime date)
        {
            List<Visit> list_of_visits = new List<Visit>();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_visits = db.Visit.Where(a => a.start_time.Day == date.Day
                         && a.start_time.Month == date.Month
                         && a.start_time.Year == date.Year
                         && a.is_removed == false)
                         .ToList();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return list_of_visits;
        }

        public List<Visit> Get_list_to_top_chart(DateTime date_today)
        {
            List<Visit> list_of_visits = new List<Visit>();
            DateTime before_11_months = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            before_11_months = before_11_months.AddMonths(-11);

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_visits = db.Visit
                        .Where(a => a.start_time > before_11_months
                        && a.end_time <= date_today
                        && a.Employee.is_removed == false
                        && a.is_removed == false)
                        .OrderBy(a => a.start_time)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return list_of_visits;
        }

        public List<Visit> Get_list_to_bottom_charts(DateTime start_range_date, DateTime end_range_date)
        {
            List<Visit> list_of_visits = new List<Visit>();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_visits = db.Visit.Where(a =>
                                a.is_removed == false &&
                                a.Employee.is_removed == false &&
                                a.employee_id != 1 &&
                                a.start_time >= start_range_date &&
                                a.start_time <= end_range_date)
                                .ToList();
                    
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return list_of_visits;
        }


        public void Add(Visit visit)
        {
            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    db.Visit.Add(visit);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }

        public void Update(Visit new_visit)
        {
            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    Visit visit_from_db = db.Visit.Find(new_visit.id);

                    if (visit_from_db != null)
                    {
                        visit_from_db.client_name = new_visit.client_name;
                        visit_from_db.client_surname = new_visit.client_surname;
                        visit_from_db.client_tel_number = new_visit.client_tel_number;
                        visit_from_db.client_email = new_visit.client_email;
                        visit_from_db.employee_id = new_visit.employee_id;
                        visit_from_db.start_time = new_visit.start_time;
                        visit_from_db.end_time = new_visit.end_time;
                        visit_from_db.describe = new_visit.describe;
                        visit_from_db.is_removed = false;
                        visit_from_db.price = new_visit.price;
                        visit_from_db.type_id = new_visit.type_id;
                        visit_from_db.type_unclassified = new_visit.type_unclassified;

                        db.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }

        public void Remove(long id)
        {
            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    Visit visit = db.Visit.Find(id);
                    visit.is_removed = true;

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }













    }
}