using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using RatingAPI.Models;
using RatingAPI.Business;
using System.Net;

namespace RatingAPI.Controllers
{
    public class RateController : ApiController
    {
        public IHttpActionResult Get([FromBody] RatingAPI.Models.WebRequest request)
        {
            var start = DateTime.Now;

            WebservicesEntities we = new WebservicesEntities();
            var authResult = Authentication.Validate(we, HttpContext.Current.Request.Headers);
            Logging.Log("authentication fine.");

            if (authResult.Passed)
            {
                ///return Ok(webResponse);
            }
            else
            {
                return StatusCode(HttpStatusCode.Unauthorized);
            }

            var response = new RatingAPI.Models.WebResponse();
            response.Rate = (decimal)5.99;

            return Ok(response);
        }
    }
}
