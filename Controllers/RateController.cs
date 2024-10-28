using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using RatingAPI.Models;
using RatingAPI.Business;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

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

            if (request.Accessorials != null)
            {
                foreach (var accessorial in request.Accessorials)
                {
                    accessorial.WebRequestID = request.ID;
                }
                re.WebRequestAccessorials.AddRange(request.Accessorials);
                re.SaveChanges();
            }

            var webResponse = new Models.WebRespons();
            webResponse.WebRequestID = request.ID;

            try
            {
                //var start = DateTime.Now;

                var authResult = Authentication.Validate(we, HttpContext.Current.Request.Headers);
                Logging.Log("authentication fine.");

                if (authResult.Passed)
                {
                    request.APIUserID = authResult.APIUser.id;

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
                    var originalWeight = request.Weight;

                    if (request.Dimensions != null
                        && request.Dimensions.Length != 0)
                    {
                        foreach (var dimension in request.Dimensions)
                        {
                            cubeWeight += (dimension.Length.Value * dimension.Width.Value * dimension.Height.Value) / tariffGroup.DimensionFactor.Value;
                        }

                        request.Weight = request.Weight > cubeWeight ? request.Weight : cubeWeight;
                    }

                    if (matchedZone != null
                        && tariffGroup.ChargeByWeight.HasValue
                        && tariffGroup.ChargeByWeight.Value)
                    {
                        rate = re.Rates
                                    .FirstOrDefault(m =>    m.TariffGroupID == tariffGroup.ID
                                                            && m.ZoneName == matchedZone.Zone.Name
                                                            && m.Service.ToLower() == request.Service.ToLower()
                                                            && ((request.Weight >= m.WeightFrom
                                                                && request.Weight < m.WeightTo)
                                                                ||
                                                                (request.Weight == m.WeightFrom
                                                                || request.Weight == m.WeightTo))
                                                            );
                    }

                    QuantityRate quantityRate = null;

                    if (matchedZone != null
                        && tariffGroup.ChargeByQuantity.HasValue
                        && tariffGroup.ChargeByQuantity.Value)
                    {
                        quantityRate = re.QuantityRates
                                            .FirstOrDefault(m =>    m.TariffGroupID == tariffGroup.ID
                                                                    && m.QuantityFrom <= request.Pieces
                                                                    && m.QuantityTo >= request.Pieces
                                                                    && m.ZoneName == matchedZone.Zone.Name);
                    }

                    FuelRate fuelRate = null;

                    if (matchedZone != null
                        && tariffGroup.FuelRate.HasValue
                        && tariffGroup.FuelRate.Value != 0)
                    {
                        fuelRate = re.FuelRates
                                        .FirstOrDefault(m => m.TariffGroupID == tariffGroup.ID
                                                                && m.FuelRateFrom <= tariffGroup.FuelRate
                                                                && m.FuelRateTo > tariffGroup.FuelRate);
                    }

                    if (rate == null && quantityRate == null)
                    {
                        webResponse.Timestamp = DateTime.Now;
                        webResponse.Dimensions = request.Dimensions;
                        webResponse.Service = request.Service;
                        webResponse.Weight = originalWeight;
                        webResponse.CubeWeight = cubeWeight;
                        webResponse.StatusCode = (int)HttpStatusCode.NoContent;

                        webResponse.ErrorMessages = "Either the origin postal code, destination postal code, or weight is out of range.";

                        re.WebResponses.Add(webResponse);
                        re.SaveChanges();

                        IHttpActionResult response;

                        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
                        responseMessage.Content = new StringContent(JsonConvert.SerializeObject(webResponse), Encoding.UTF8, "application/json");
                        response = ResponseMessage(responseMessage);

                        return response;

                    }
                    else
                    {
                        var shippingRate = (rate.Rate1.Value) + GetQuantityRate(quantityRate);
                        var accessorialRate = GetAccessorialTotals(re, request, tariffGroup);
                        var fuelRateCharge = GetFuelRate(shippingRate, fuelRate);
                        var sizeOverageCharge = GetSizeOverageCharge(re, tariffGroup, request.Dimensions);
                        webResponse.Rate = Math.Round(shippingRate + accessorialRate + fuelRateCharge + sizeOverageCharge, 2);
                        webResponse.Dimensions = request.Dimensions;    
                        webResponse.Service = request.Service;
                        webResponse.Zone = rate.ID;
                        webResponse.Weight = originalWeight;
                        webResponse.CubeWeight = Math.Round(cubeWeight, 2);
                        webResponse.Timestamp = DateTime.Now;
                        webResponse.Pieces = request.Dimensions == null ? (request.Pieces.HasValue ? request.Pieces.Value : 0) : request.Dimensions.Length;
                        webResponse.Milliseconds = (int)(DateTime.Now - request.Timestamp.Value).TotalMilliseconds;
                        webResponse.StatusCode = (int)HttpStatusCode.NoContent;
                        webResponse.FuelRate = Math.Round(fuelRateCharge, 2);
                        webResponse.SizeSurchargeRate = Math.Round(sizeOverageCharge, 2);
                        webResponse.AccessorialsRate = Math.Round(accessorialRate, 2);
                        webResponse.AccessorialCharges = GetAccesorialCharges(re, request, tariffGroup);

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

        private AccessorialCharge[] GetAccesorialCharges(RatingAPIEntities re, Models.WebRequest request, TariffGroup tariffGroup)
        {
            decimal total = 0;

            var accessorialCharges = new List<AccessorialCharge>();

            if (request.Accessorials != null)
            {
                foreach (var accessorial in request.Accessorials)
                {
                    var accessorialRate = re.AccessorialRates
                                                .FirstOrDefault(m => m.TariffGroupID == tariffGroup.ID
                                                                        && m.Name == accessorial.AccessorialName);

                    if (accessorialRate != null)
                    {
                        AccessorialCharge ac = new AccessorialCharge();
                        ac.Name = accessorialRate.Name;
                        ac.Charge = accessorialRate.Rate.Value * accessorial.Amount.Value;

                        accessorialCharges.Add(ac);
                    }
                }
            }

            if (accessorialCharges.Count != 0)
            {
                return accessorialCharges.ToArray();
            }

            return null;
        }

        private decimal GetSizeOverageCharge(RatingAPIEntities re, TariffGroup tariffGroup, WebRequestDimension[] dimensions)
        {
            decimal charge = 0;

            if (dimensions != null)
            {
                var sizeOverages = re.SizeSideOverages
                                        .Where(m => m.TariffGroupID == tariffGroup.ID)
                                        .OrderBy(m => m.AnySideOverInches)
                                        .ToList();

                var girthOverages = re.GirthOverages
                                        .Where(m => m.TariffGroupID == tariffGroup.ID)
                                        .OrderBy(m => m.GirthOverInches)
                                        .ToList();

                foreach (var dimension in dimensions)
                {
                    if (dimension != null)
                    {
                        decimal currentSizeCharge = 0;

                        var dims = new List<decimal>();
                        dims.Add(dimension.Width.HasValue ? dimension.Width.Value : 0);
                        dims.Add(dimension.Length.HasValue ? dimension.Length.Value : 0);
                        dims.Add(dimension.Height.HasValue ? dimension.Height.Value : 0);

                        foreach (var sizeOverage in sizeOverages)
                        {
                            if (dims.Any(m => m > sizeOverage.AnySideOverInches)
                                && sizeOverage.Charge.HasValue
                                && sizeOverage.Charge.Value > currentSizeCharge)
                            {
                                currentSizeCharge = sizeOverage.Charge.Value;
                            }
                        }

                        var orderedDims = dims
                                            .OrderBy(m => m)
                                            .ToList();

                        var lowestTwoDoubleSum = (orderedDims[0] * 2) + (orderedDims[1] * 2);

                        decimal currentGirthCharge = 0;

                        foreach (var girthOverage in girthOverages)
                        {
                            if (lowestTwoDoubleSum > girthOverage.GirthOverInches
                                && girthOverage.Charge.HasValue
                                && girthOverage.Charge.Value > currentGirthCharge)
                            {
                                currentGirthCharge = girthOverage.Charge.Value;
                            }
                        }

                        charge += currentGirthCharge > currentSizeCharge ? currentGirthCharge : currentSizeCharge;
                    }
                }
            }

            return charge;
        }

        private decimal GetFuelRate(decimal? shippingRate, FuelRate fuelRate)
        {
            if (fuelRate == null)
            {
                return 0;
            }

            return shippingRate.HasValue && shippingRate.Value != 0 ? fuelRate.PercentageRate.Value * shippingRate.Value : 0;
        }

        private decimal GetQuantityRate(QuantityRate quantityRate)
        {
            if (quantityRate == null)
            {
                return 0;
            }

            return quantityRate.Rate.HasValue ? quantityRate.Rate.Value : 0;
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

            if (request.Accessorials != null)
            {
                foreach (var accessorial in request.Accessorials)
                {
                    var accessorialRate = re.AccessorialRates
                                                .FirstOrDefault(m => m.TariffGroupID == tariffGroup.ID
                                                                        && m.Name == accessorial.AccessorialName);

                    if (accessorialRate != null)
                    {
                        total += accessorialRate.Rate.Value * accessorial.Amount.Value;
                    }
                }
            }

            return total;
        }
    }
}
