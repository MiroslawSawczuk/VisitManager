using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Visit_Manager.Models;

namespace Visit_Manager.Repository
{
    public class EmployeeRepository
    {
        public Employee Get_by_id(long id)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee.Find(id);
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return employee;
        }

        public Employee Get_by_id_and_password(long id, string password)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee
                                .Where(a => a.is_removed == false &&
                                    a.id == id &&
                                a.password == password)
                                .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return employee;
        }

        public Employee Get_by_guid(string guid)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee.Where(a => a.guid == guid).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return employee;
        }

        public Employee Get_by_login(string login)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee.Where(a => a.is_removed == false &&
                            (a.name.ToLower() + "." + a.surname.ToLower()) == login)
                            .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return employee;
        }

        public Employee Get_by_name_surname(string name, string surname)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee
                                 .Where(a => a.is_removed == false &&
                                             a.name == name &&
                                             a.surname == surname)
                                             .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return employee;
        }

        public List<Employee> Get_all_except_admin()
        {
            List<Employee> list_of_employees = new List<Employee>();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_employees = db.Employee
                                .Where(a => a.is_removed == false &&
                                    a.id != 1)
                                .ToList();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return list_of_employees;
        }

        public void Add(Employee employee)
        {
            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    db.Employee.Add(employee);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }

        public Employee Add_guid(long id)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee.Find(id);
                    employee.guid = Guid.NewGuid().ToString();
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return employee;
        }

        public void Update(Employee employee_to_edit)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee.Find(employee_to_edit.id);

                    if (employee != null)
                    {
                        employee.name = employee_to_edit.name;
                        employee.surname = employee_to_edit.surname;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }

        public void Update_password(long id, string password)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee.Find(id);

                    if (employee != null)
                    {
                        employee.password = password;
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
                    Employee employee = db.Employee.Find(id);
                    employee.is_removed = true;

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }

        public bool Check_if_removed(long id)
        {
            bool is_removed = true;

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    is_removed = db.Employee.Find(id).is_removed;
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return is_removed;
        }

        public bool Return_true_if_employee_not_repeted_in_db(Employee employee_to_check)
        {
            Employee employee = new Employee();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    employee = db.Employee
                                .Where(a => a.is_removed == false &&
                                    a.id != employee_to_check.id &&
                                    a.name == employee_to_check.name &&
                                    a.surname == employee_to_check.surname)
                                    .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            if (employee == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }







    }
}