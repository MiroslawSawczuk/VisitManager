using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Visit_Manager.Models
{
    public class Visit_from_view
    {
        public TimeSpan Start_time { get; set; }
        public TimeSpan End_time { get; set; }
        public long Visit_id { get; set; }
        public long Employee_id { get; set; }
        public long Type_id { get; set; }
        public string Visit_date { get; set; }
        public string Client_name { get; set; }
        public string Client_surname { get; set; }
        public string Client_tel_number { get; set; }
        public string Client_email { get; set; }
        public decimal Price { get; set; }
        public string Describe { get; set; }
        public string Type_unclassified { get; set; }
    }
}