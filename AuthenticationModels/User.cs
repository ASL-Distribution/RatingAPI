//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RatingAPI.AuthenticationModels
{
    using System;
    using System.Collections.Generic;
    
    public partial class User
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public Nullable<int> CompanyID { get; set; }
        public string VerificationCode { get; set; }
        public Nullable<bool> Verified { get; set; }
        public string PasswordResetVerificationCode { get; set; }
        public string SignInToken { get; set; }
        public Nullable<System.DateTime> TokenTimeStamp { get; set; }
        public Nullable<int> role { get; set; }
        public Nullable<bool> Authorized { get; set; }
    }
}
