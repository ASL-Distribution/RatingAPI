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

            request.Timestamp = DateTime.Now;
            re.WebRequests.Add(request);
            re.SaveChanges();

            foreach (var accessorial in request.Accessorials)
            {
                accessorial.WebRequestID = request.ID;
            }
            re.WebRequestAccessorials.AddRange(request.);
            re.SaveChanges();

            var webResponse = new Models.WebRespons();
            webResponse.WebRequestID = request.ID;

            try
            {
                //var start = DateTime.Now;

                var authResult = Authentication.Validate(we, HttpContext.Current.Request.Headers);
                Logging.Log("authentication fine.");

                request.APIUserID = authResult.APIUser.id;

                if (authResult.Passed)
                {
                    var apiUserGroup = re.APIUserGroups
                                            .FirstOrDefault(m => m.APIUserID == authResult.APIUser.id);

                var rate = re.Rates
                                .FirstOrDefault(m =>    m.RateGroupID == apiUserGroup.GroupID
                                                        && request.Service == m.Service
                                                        &&
                                                            (request.FromPostal.CompareTo(m.) == 0
                                                            || request.FromPostal.CompareTo(m.OriginPostalFrom) == 1)
                                                        &&
                                                            (request.FromPostal.CompareTo(m.OriginPostalTo) == 0
                                                            || request.FromPostal.CompareTo(m.OriginPostalTo) == -1)
                                                        &&
                                                            (request.ToPostal.CompareTo(m.DestinationPostalFrom) == 0
                                                            || request.ToPostal.CompareTo(m.DestinationPostalFrom) == 1)
                                                        &&
                                                            (request.ToPostal.CompareTo(m.DestinationPostalTo) == 0
                                                            || request.ToPostal.CompareTo(m.DestinationPostalTo) == -1)
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
                    webResponse.Rate = (rate.Rate1.Value * request.Weight) + GetAccessorialTotals(re, request, apiUserGroup);
                    webResponse.Height = request.Height;
                    webResponse.Width = request.Width;
                    webResponse.Length = request.Length;
                    webResponse.Service = request.Service;
                    webResponse.Zone = rate.Zone;
                    webResponse.Weight = request.Weight;
                    webResponse.timestamp = DateTime.Now;
                    webResponse.Milliseconds = (int)(DateTime.Now - request.Timestamp.Value).TotalMilliseconds;
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

        private decimal GetAccessorialTotals(RatingAPIEntities re, Models.WebRequest request, APIUserGroup apiUserGroup)
        {
            decimal total = 0;

            foreach (var accessorial in request.Accessorials)
            {
                var accessorialRate = re.AccessorialRates
                                            .FirstOrDefault(m =>    m.RateGroupID == apiUserGroup.GroupID
                                                                    && m.Name == accessorial.AccessorialName);

                if (accessorialRate != null)
                {
                    total += accessorialRate.Rate.Value * accessorial.Amount.Value;
                }
            }

            return total;
        }
    }
}
