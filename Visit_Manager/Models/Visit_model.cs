using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Visit_Manager.Models
{
    public class Visit_model
    {
        public long Employee_id { get; set; }//<-- the field needed for proper operation DropDownListFor(a=>a.Employee_id ......
        public long Type_id { get; set; }//<-- the field needed for proper operation DropDownListFor(a=>a.Type_id ......

        public List<SelectListItem> List_of_employees_to_ddl { get; set; }
        public List<SelectListItem> List_of_visit_types_to_ddl { get; set; }

        public List<Visit_in_view> List_of_visits_to_view { get; set; }
        public List<Return_result> List_of_results { get; set; }

        public bool If_action_successed { get; set; }
        public bool If_session_expired { get; set; }

        public string Visit_date { get; set; }
        public Employee Employee { get; set; }
        public Visit_type Visit_type { get; set; }



    }
}