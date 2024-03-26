using RatingAPI.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;

namespace RatingAPI.Business
{
    public class ReportingAuthentication
    {
        public static ReportingAuthenticationResult Validate(ReportingEntities db, string token)
        {
            string message = "";

            var user = db.users
                                .FirstOrDefault(m => m.token == token);

            if (user == null)
            {
                message = "FAILED AUTHORIZATION.  INVALID TOKEN.";
                return new ReportingAuthenticationResult(message, false, null);
            }
            else
            {
                if (user.tokendatetime >= DateTime.Now.AddHours(-24))
                {
                    message = "VALID TOKEN";
                    return new ReportingAuthenticationResult(message, true, user);
                }
                else
                {
                    message = "TOKEN EXPIRED.";
                    return new ReportingAuthenticationResult(message, false, null);
                }
            }
        }
    }
}