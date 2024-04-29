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

            request.Timestamp = DateTime.Now;
            re.WebRequests.Add(request);
            re.SaveChanges();

            foreach (var accessorial in request.Accessorials)
            {
                accessorial.WebRequestID = request.ID;
            }
            re.WebRequestAccessorials.AddRange(request.Accessorials);
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
                    var apiUserGroup = re.APIUserTariffGroups
                                            .FirstOrDefault(m => m.APIUserName == authResult.APIUser.Name);

                    var tariffGroup = re.TariffGroups
                                        .FirstOrDefault(m => m.Name == apiUserGroup.TariffGroupName);

                    var zones = re.Zones
                                    .Where(m => m.TariffGroupID == tariffGroup.ID
                                                &&
                                                    (request.FromPostal.CompareTo(m.OriginFromPostal) == 0
                                                    || request.FromPostal.CompareTo(m.OriginFromPostal) == 1)
                                                &&
                                                    (request.FromPostal.CompareTo(m.OriginToPostal) == 0
                                                    || request.FromPostal.CompareTo(m.OriginToPostal) == -1)
                                                &&
                                                    (request.ToPostal.CompareTo(m.DestinationFromPostal) == 0
                                                    || request.ToPostal.CompareTo(m.DestinationFromPostal) == 1)
                                                &&
                                                    (request.ToPostal.CompareTo(m.DestinationToPostal) == 0
                                                    || request.ToPostal.CompareTo(m.DestinationToPostal) == -1))
                                    .ToList();

                    var processedZones = GetProcessedZones(zones, request);

                    var matchedZone = processedZones
                                        .OrderByDescending(m => m.ToCharacterOrdinalMatchCount)
                                        .ThenByDescending(m => m.FromCharacterOrdinalMatchCount)
                                        .FirstOrDefault();

                    Rate rate = null;

                    decimal cubeWeight = 0;

                    if (request.Dimensions != null
                        && request.Dimensions.Length != 0)
                    {
                        foreach (var dimension in request.Dimensions)
                        {
                            cubeWeight += (dimension.Length.Value * dimension.Width.Value * dimension.Height.Value) / tariffGroup.DimensionFactor.Value;
                        }

                        request.Weight = request.Weight > cubeWeight ? request.Weight : cubeWeight;
                    }

                    if (matchedZone != null)
                    {
                        rate = re.Rates
                                    .FirstOrDefault(m =>    m.TariffGroupID == tariffGroup.ID
                                                            && m.ZoneName == matchedZone.Zone.Name
                                                            && m.Service == request.Service
                                                            && request.Weight >= m.WeightFrom
                                                            && request.Weight < m.WeightTo);
                    }

                    if (rate == null)
                    {
                        webResponse.Timestamp = DateTime.Now;
                        webResponse.Dimensions = request.Dimensions;
                        webResponse.Service = request.Service;
                        webResponse.Weight = request.Weight;
                        webResponse.StatusCode = (int)HttpStatusCode.NoContent;

                        re.WebResponses.Add(webResponse);
                        re.SaveChanges();

                        return StatusCode(HttpStatusCode.NoContent);
                    }
                    else
                    {
                        webResponse.Rate = (rate.Rate1.Value * request.Weight) + GetAccessorialTotals(re, request, tariffGroup);
                        webResponse.Dimensions = request.Dimensions;
                        webResponse.Service = request.Service;
                        webResponse.Zone = rate.ID;
                        webResponse.Weight = request.Weight;
                        webResponse.Timestamp = DateTime.Now;
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

        private List<ProcessedZone> GetProcessedZones(List<Zone> zones, Models.WebRequest webRequest)
        {
            var processedZones = new List<ProcessedZone>();

            foreach (var zone in zones)
            {
                processedZones.Add(new ProcessedZone(zone, webRequest));
            }

            return processedZones;
        }

        private decimal GetAccessorialTotals(RatingAPIEntities re, Models.WebRequest request, TariffGroup tariffGroup)
        {
            decimal total = 0;

            foreach (var accessorial in request.Accessorials)
            {
                var accessorialRate = re.AccessorialRates
                                            .FirstOrDefault(m =>    m.TariffGroupID == tariffGroup.ID
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
