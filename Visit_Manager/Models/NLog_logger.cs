using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Visit_Manager.Models
{
    public static class NLog_logger
    {
        public static Logger Loger = LogManager.GetCurrentClassLogger();
    }
}