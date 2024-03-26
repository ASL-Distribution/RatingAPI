using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using RatingAPI.Models;

namespace ShiptrackPullAPI.Business
{
    public class ReportingLogging
    {
        public static void Log(string text, bool getCallerInfo = false)
        {
            string logEntry = "";

            if (getCallerInfo)
            {
                var method = new StackTrace().GetFrame(1).GetMethod();
                var classO = method.ReflectedType.Name;
                string callingClassAndMethod = classO + "." + method.Name;

                logEntry += callingClassAndMethod + " | ";
            }

            logEntry += text;

            DateTime timestamp = DateTime.Now;

            ReportingEntities we = new ReportingEntities();

            Log log = new Log();
            log.timestamp = timestamp;
            log.logentry = logEntry;

            we.Logs.Add(log);
            we.SaveChanges();
        }
    }
}