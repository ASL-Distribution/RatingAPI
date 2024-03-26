using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RatingAPI.Models
{
    public class ReportingAuthenticationResult
    {
        public string Message;
        public bool Passed;
        public user user;

        public ReportingAuthenticationResult(string message, bool passed, user user)
        {
            this.Message = message;
            this.Passed = passed;
            this.user = user;
        }
    }
}