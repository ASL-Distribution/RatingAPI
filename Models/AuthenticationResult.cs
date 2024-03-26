using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RatingAPI.Models
{
    public class AuthenticationResult
    {
        public string Message;
        public bool Passed;
        public APIUser APIUser;

        public AuthenticationResult(string message, bool passed, APIUser apiUser)
        {
            this.Message = message;
            this.Passed = passed;
            this.APIUser = apiUser;
        }
    }
}