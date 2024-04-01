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
            WebservicesEntities we = new WebservicesEntities();
            RatingAPIEntities re = new RatingAPIEntities();

            request.timestamp = DateTime.Now;
            re.WebRequests.Add(request);
            re.SaveChanges();

            foreach (var accessorial in request.Accessorials)
            {
                accessorial.WebRequestID = request.ID;
            }
            re.WebRequestAccessorials.AddRange(request.Accessorials);
            re.SaveChanges();

            var webResponse = new Models.WebResponse();
            webResponse.WebRequestID = request.ID;

            try
            {
                //var start = DateTime.Now;

                var authResult = Authentication.Validate(we, HttpContext.Current.Request.Headers);
                Logging.Log("authentication fine.");

                if (authResult.Passed)
                {
                var rate = re.Rates
                                .FirstOrDefault(m =>    request.ClientID == m.ClientID
                                                        && request.Service == m.Service
                                                        &&
                                                            (request.ToPostal.CompareTo(m.PostalFrom) == 0
                                                            || request.ToPostal.CompareTo(m.PostalFrom) == 1)
                                                        &&
                                                            (request.ToPostal.CompareTo(m.PostalTo) == 0
                                                            || request.ToPostal.CompareTo(m.PostalTo) == -1)
                                                        && request.Weight >= m.WeightFrom
                                                        && request.Weight <= m.WeightTo);

                if (rate == null)
                {
                    webResponse.timestamp = DateTime.Now;
                    webResponse.Height = request.Height;
                    webResponse.Width = request.Width;
                    webResponse.Length = request.Length;
                    webResponse.Service = request.Service;
                    webResponse.Weight = request.Weight;
                    webResponse.StatusCode = (int)HttpStatusCode.NoContent;

                    re.WebResponses.Add(webResponse);
                    re.SaveChanges();

                    return StatusCode(HttpStatusCode.NoContent);
                }
                else
                {
                    webResponse.Rate = (rate.Rate1.Value * request.Weight) + GetAccessorialTotals(re, request);
                    webResponse.Height = request.Height;
                    webResponse.Width = request.Width;
                    webResponse.Length = request.Length;
                    webResponse.Service = request.Service;
                    webResponse.Zone = rate.Zone;
                    webResponse.Weight = request.Weight;
                    webResponse.timestamp = DateTime.Now;
                    webResponse.Milliseconds = (int)(DateTime.Now - request.timestamp.Value).TotalMilliseconds;
                    webResponse.StatusCode = (int)HttpStatusCode.NoContent;

                    re.WebResponses.Add(webResponse);
                    re.SaveChanges();

                    return Ok(webResponse);
                }
                }
                else
                {
                    return StatusCode(HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception ex)
            {
                webResponse.ErrorMessages = ex.Message;

                re.WebResponses.Add(webResponse);
                re.SaveChanges();
                return Ok(webResponse);
            }
        }

        private decimal GetAccessorialTotals(RatingAPIEntities re, Models.WebRequest request)
        {
            decimal total = 0;

            foreach (var accessorial in request.Accessorials)
            {
                var accessorialRate = re.AccessorialRates
                                            .FirstOrDefault(m =>    m.ClientID == request.ClientID
                                                                    && m.Accessorial == accessorial.AccessorialName);

                if (accessorialRate != null)
                {
                    total += accessorialRate.Rate.Value * accessorial.Amount.Value;
                }
            }

            return total;
        }
    }
}
