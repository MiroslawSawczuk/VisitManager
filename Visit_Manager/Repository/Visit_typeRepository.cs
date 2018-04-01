using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Visit_Manager.Models;

namespace Visit_Manager.Repository
{
    public class Visit_typeRepository
    {
        public Visit_type Get_by_id(long id)
        {
            Visit_type visit_type = new Visit_type();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    visit_type = db.Visit_type.Find(id);
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return visit_type;
        }

        public Visit_type Get_by_name(string name)
        {
            Visit_type visit_type = new Visit_type();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    visit_type = db.Visit_type
                               .Where(a => a.is_removed == false &&
                               a.name == name)
                               .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return visit_type;
        }

        public List<Visit_type> Get_all()
        {
            List<Visit_type> list_of_visit_types = new List<Visit_type>();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_visit_types = db.Visit_type.ToList();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return list_of_visit_types;
        }

        public List<Visit_type> Get_all_not_removed()
        {
            List<Visit_type> list_of_visit_types = new List<Visit_type>();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_visit_types = db.Visit_type
                            .Where(a => a.is_removed == false)
                            .ToList();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return list_of_visit_types;
        }

        public List<Visit_type> Get_all_except_first()
        {
            List<Visit_type> list_of_visit_types = new List<Visit_type>();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    list_of_visit_types = db.Visit_type
                       .Where(a => a.is_removed == false &&
                           a.id != 1) // id=1 it is 'Other' which cannot edit
                       .ToList();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            return list_of_visit_types;
        }

        public void Add(Visit_type visit_type)
        {
            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    db.Visit_type.Add(visit_type);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }


        public void Update(Visit_type visit_type)
        {
            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    Visit_type visit_type_from_db = db.Visit_type.Find(visit_type.id);

                    if (visit_type_from_db != null)
                    {
                        visit_type_from_db.name = visit_type.name;

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
                    Visit_type visit_type = db.Visit_type.Find(id);
                    visit_type.is_removed = true;

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
        }

        public bool Return_true_if_name_not_repeted_in_db(Visit_type visit_type_to_check)
        {
            Visit_type visit_type = new Visit_type();

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    visit_type = db.Visit_type
                               .Where(a => a.is_removed == false &&
                                   a.id != visit_type_to_check.id &&
                                   a.name == visit_type_to_check.name)
                                   .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }

            if (visit_type == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Check_if_removed(long id)
        {
            bool is_removed = true;

            try
            {
                using (Visit_manager_dbEntities db = new Visit_manager_dbEntities())
                {
                    is_removed = db.Visit_type.Find(id).is_removed;
                }
            }
            catch (Exception ex)
            {
                NLog_logger.Loger.Error(ex);
            }
            return is_removed;
        }


















    }
}