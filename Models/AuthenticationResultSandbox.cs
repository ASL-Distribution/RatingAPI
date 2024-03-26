using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RatingAPI.Models
{
    public class AuthenticationResultSandbox
    {
        public string Message;
        public bool Passed;
        public string BillToAccounts;
        public APIUsersSandbox APIUserSandbox;

        public AuthenticationResultSandbox(string message, bool passed, APIUsersSandbox apiUserSandbox)
        {
            this.Message = message;
            this.Passed = passed;
            this.APIUserSandbox = apiUserSandbox;
        }

        internal APIUser GetProductionCustomer()
        {
            WebservicesEntities we = new WebservicesEntities();

            return we.APIUsers
                        .FirstOrDefault(m => m.id == APIUserSandbox.id);
        }
    }
}