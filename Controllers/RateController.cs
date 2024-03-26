﻿using System;
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
            WebservicesEntities we = new WebservicesEntities();
            RatingAPIEntities re = new RatingAPIEntities();

            request.timestamp = DateTime.Now;
            re.WebRequests.Add(request);
            re.SaveChanges();

            /*var start = DateTime.Now;

            var authResult = Authentication.Validate(we, HttpContext.Current.Request.Headers);
            Logging.Log("authentication fine.");

            if (authResult.Passed)
            {*/
                var rate = re.Rates
                                .FirstOrDefault(m => request.ClientID == m.ClientID
                                                        && request.Service == m.Service
                                                        &&
                                                            request.ToPostal.CompareTo(m.PostalFrom) == 0
                                                            || request.ToPostal.CompareTo(m.PostalFrom) == 1
                                                        &&
                                                            request.ToPostal.CompareTo(m.PostalTo) == 0
                                                            || request.ToPostal.CompareTo(m.PostalTo) == -1
                                                        &&
                                                            request.Weight >= m.WeightFrom
                                                            && request.Weight <= m.WeightTo);

                if (rate == null)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }
                else
                {
                    var webResponse = new Models.WebResponse();
                    webResponse.Rate = rate.Rate1.Value * request.Weight;
                    webResponse.Height = request.Height;
                    webResponse.Width = request.Width;
                    webResponse.Length = request.Length;
                    webResponse.Service = request.Service;
                    webResponse.StatusCode = 200;
                    webResponse.Zone = rate.Zone;
                    webResponse.Weight = request.Weight;
                    webResponse.WebRequestID = request.ID;
                    webResponse.timestamp = DateTime.Now;
                    webResponse.Milliseconds = (int)(DateTime.Now - request.timestamp.Value).TotalMilliseconds;

                    re.WebResponses.Add(webResponse);
                    re.SaveChanges();

                    return Ok(webResponse);
                }
            /*}
            else
            {
                return StatusCode(HttpStatusCode.Unauthorized);
            }*/
        }
    }
}
