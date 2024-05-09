using RatingAPI.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using RatingAPI.AuthenticationModels;

namespace RatingAPI.Business
{
    public class Authentication
    {
        public static AuthenticationResult Validate(WebservicesEntities db, NameValueCollection headers)
        {
            string message = "";
            HttpContext httpContext = HttpContext.Current;
            NameValueCollection headerList = httpContext.Request.Headers;
            var token = headerList.Get("token");

            var apiUsers = db.APIUsers.Where(m => m.token == token);
            APIUser apiUser = apiUsers.FirstOrDefault();

            if (apiUser == null)
            {
                message = "FAILED AUTHORIZATION.  INVALID TOKEN.";
                return new AuthenticationResult(message, false, null);
            }
            else
            {
                if (apiUser.TokenDateTime >= DateTime.Now.AddHours(-24))
                {
                    AuthenticationEntities ae = new AuthenticationEntities();

                    var user = ae.Users
                                    .FirstOrDefault(m =>    m.ID == apiUser.UserId
                                                            && m.Authorized.HasValue
                                                            && m.Authorized.Value
                                                            && m.Verified.HasValue
                                                            && m.Verified.Value);

                    if (user == null)
                    {
                        message = "FAILED AUTHORIZATION.  USER IS NOT AUTHORIZED/VERIFIED.";
                        return new AuthenticationResult(message, false, null);
                    }

                    message = "VALID TOKEN";
                    return new AuthenticationResult(message, true, apiUser);
                }
                else
                {
                    message = "TOKEN EXPIRED.";
                    return new AuthenticationResult(message, false, null);
                }
            }
        }

        public static AuthenticationResultSandbox ValidateSandbox(WebservicesEntities db, NameValueCollection headers)
        {
            string message = "";
            HttpContext httpContext = HttpContext.Current;
            NameValueCollection headerList = httpContext.Request.Headers;
            var token = headerList.Get("token");

            var apiSandboxUser = db.APIUsersSandboxes
                            .FirstOrDefault(m => m.token == token);

            if (apiSandboxUser == null)
            {
                message = "FAILED AUTHORIZATION.  INVALID TOKEN.";
                return new AuthenticationResultSandbox(message, false, null);
            }
            else
            {
                if (apiSandboxUser.TokenDateTime >= DateTime.Now.AddHours(-24))
                {
                    AuthenticationEntities ae = new AuthenticationEntities();

                    var user = ae.Users
                                    .FirstOrDefault(m =>    m.ID == apiSandboxUser.UserId
                                                            && m.Authorized.HasValue
                                                            && m.Authorized.Value
                                                            && m.Verified.HasValue
                                                            && m.Verified.Value);

                    if (user == null)
                    {
                        message = "FAILED AUTHORIZATION.  USER IS NOT AUTHORIZED/VERIFIED.";
                        return new AuthenticationResultSandbox(message, false, null);
                    }

                    message = "VALID TOKEN";
                    return new AuthenticationResultSandbox(message, true, apiSandboxUser);
                }
                else
                {
                    message = "TOKEN EXPIRED.";
                    return new AuthenticationResultSandbox(message, false, null);
                }
            }
        }
    }
}