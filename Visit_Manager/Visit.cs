//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Visit_Manager
{
    using System;
    using System.Collections.Generic;
    
    public partial class Visit
    {
        public long id { get; set; }
        public string client_name { get; set; }
        public string client_surname { get; set; }
        public string client_tel_number { get; set; }
        public string client_email { get; set; }
        public long employee_id { get; set; }
        public System.DateTime start_time { get; set; }
        public System.DateTime end_time { get; set; }
        public string describe { get; set; }
        public bool is_removed { get; set; }
        public long type_id { get; set; }
        public string type_unclassified { get; set; }
        public decimal price { get; set; }
    
        public virtual Employee Employee { get; set; }
        public virtual Visit_type Visit_type { get; set; }
    }
}
