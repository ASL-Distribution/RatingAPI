//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RatingAPI.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class APIUser
    {
        public int id { get; set; }
        public Nullable<int> UserId { get; set; }
        public string Name { get; set; }
        public string EDIClientAccountId { get; set; }
        public string clientid { get; set; }
        public string client_secret { get; set; }
        public Nullable<System.DateTime> TokenDateTime { get; set; }
        public string token { get; set; }
        public string callbackurl { get; set; }
        public Nullable<decimal> maxTransId { get; set; }
        public string CustomerAuthenticationKey { get; set; }
        public Nullable<System.DateTime> CallbackURLRegistrationDate { get; set; }
    }
}
