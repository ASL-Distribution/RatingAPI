using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RatingAPI.Models;

namespace ShiptrackPullAPI.Business
{
    public class Tracking
    {
        public static void Monitor(DateTime start, DateTime end, string methodName)
        {
            ReportingEntities re = new ReportingEntities();
            var timedNotification = re.TimedNotifications
                                        .FirstOrDefault(m => m.TimedNotificationType == "API Method"
                                                             && m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

            int elapsedTime = Convert.ToInt32((end - start).TotalMilliseconds);

            if (timedNotification == null)
            {
                timedNotification = new TimedNotification();
                timedNotification.MaxAllowableTime = 180000;
                timedNotification.Name = methodName;
                timedNotification.Description = methodName;
                timedNotification.Subject = "API Method " + methodName + " Too Slow";
                timedNotification.Message = "API method " + methodName + " running too slowly.";
                timedNotification.EmailInterval = 10800000;

                re.TimedNotifications.Add(timedNotification);
            }
            timedNotification.Timestamp = start;
            timedNotification.ElapsedTime = elapsedTime;
            timedNotification.Name = methodName;

            re.SaveChanges();
        }
    }
}