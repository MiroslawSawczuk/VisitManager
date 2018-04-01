using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Visit_Manager.Models
{
    public class Bottom_charts
    {
        public string Employee_name { get; set; }
        public long Visit_type_id { get; set; }
        public string Visit_type { get; set; }
        public int Visit_count { get; set; }
        public decimal Visit_type_total_price { get; set; }
       
       


    }
}