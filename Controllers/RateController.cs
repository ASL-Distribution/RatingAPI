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

            request.FromPostal = request.FromPostal.Trim().Replace(" ", "");
            request.ToPostal = request.ToPostal.Trim().Replace(" ", "");

            try
            {
                //var start = DateTime.Now;

                var authResult = Authentication.Validate(we, HttpContext.Current.Request.Headers);
                Logging.Log("authentication fine.");

                if (authResult.Passed)
                {
                    request.APIUserID = authResult.APIUser.id;

                    var tariffGroup = re.TariffGroups
                                            .FirstOrDefault(m => m.ID == request.TariffGroupID);

                    if (tariffGroup == null)
                    {
                        webResponse.Timestamp = DateTime.Now;
                        webResponse.Dimensions = request.Dimensions;
                        webResponse.Service = request.Service;
                        webResponse.StatusCode = (int)HttpStatusCode.NoContent;
                        webResponse.Pieces = request.Dimensions.Length;
                        webResponse.ErrorMessages = "Tariff Group ID incorret.";

                        re.WebResponses.Add(webResponse);
                        re.SaveChanges();

                        IHttpActionResult response;

                        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
                        responseMessage.Content = new StringContent(JsonConvert.SerializeObject(webResponse), Encoding.UTF8, "application/json");
                        response = ResponseMessage(responseMessage);

                        return response;
                    }

                    var zones = re.Zones
                                    .Where(m => m.TariffGroupID == tariffGroup.ID
                                                &&
                                                (request.FromPostal.CompareTo(m.OriginFromPostal.Trim().Replace(" ", "")) == 0
                                                || request.FromPostal.CompareTo(m.OriginFromPostal.Trim().Replace(" ", "")) == 1)
                                            &&
                                                (request.FromPostal.CompareTo(m.OriginToPostal.Trim().Replace(" ", "")) == 0
                                                || request.FromPostal.CompareTo(m.OriginToPostal.Trim().Replace(" ", "")) == -1)
                                            &&
                                                (request.ToPostal.CompareTo(m.DestinationFromPostal.Trim().Replace(" ", "")) == 0
                                                || request.ToPostal.CompareTo(m.DestinationFromPostal.Trim().Replace(" ", "")) == 1)
                                            &&
                                                (request.ToPostal.CompareTo(m.DestinationToPostal.Trim().Replace(" ", "")) == 0
                                                || request.ToPostal.CompareTo(m.DestinationToPostal.Trim().Replace(" ", "")) == -1))
                                    .ToList();

                    var processedZones = GetProcessedZones(zones, request);

                    var matchedZone = processedZones
                                        .OrderByDescending(m => m.ToCharacterOrdinalMatchCount)
                                        .ThenByDescending(m => m.FromCharacterOrdinalMatchCount)
                                        .FirstOrDefault();

                    Rate rate = null;

                    decimal weight = 0;
                    decimal actualWeight = 0;

                    if (request.Dimensions != null
                        && request.Dimensions.Length != 0)
                    {
                        foreach (var dimension in request.Dimensions)
                        {
                            var cubeWeight = (dimension.Length.Value * dimension.Width.Value * dimension.Height.Value) / tariffGroup.DimensionFactor.Value;
                            weight += cubeWeight > dimension.Weight ? cubeWeight : dimension.Weight.Value;
                            actualWeight += dimension.Weight.Value;
                        }
                    }

                    var service = re.Services
                                        .FirstOrDefault(m => m.Name.ToLower().Trim() == request.Service.ToLower().Trim());

                    if (service == null)
                    {
                        webResponse.Timestamp = DateTime.Now;
                        webResponse.Dimensions = request.Dimensions;
                        webResponse.Service = request.Service;
                        webResponse.RatedWeight = weight;
                        webResponse.ActualWeight = actualWeight;
                        webResponse.StatusCode = (int)HttpStatusCode.NoContent;
                        webResponse.Pieces = request.Dimensions.Length;
                        webResponse.ErrorMessages = "Service is not set correctly.";

                        re.WebResponses.Add(webResponse);
                        re.SaveChanges();

                        IHttpActionResult response;

                        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
                        responseMessage.Content = new StringContent(JsonConvert.SerializeObject(webResponse), Encoding.UTF8, "application/json");
                        response = ResponseMessage(responseMessage);

                        return response;
                    }

                    if (matchedZone != null
                        && service.ChargeByWeight.HasValue
                        && service.ChargeByWeight.Value)
                    {
                        rate = re.Rates
                                    .FirstOrDefault(m =>    m.TariffGroupID == tariffGroup.ID
                                                            && m.ZoneName == matchedZone.Zone.Name
                                                            && m.Service.ToLower() == request.Service.ToLower()
                                                            && ((weight >= m.WeightFrom
                                                                && weight < m.WeightTo))
                                                            );
                    }

                    QuantityRate quantityRate = null;

                    if (matchedZone != null
                        && service.ChargeByQuantity.HasValue
                        && service.ChargeByQuantity.Value)
                    {
                        quantityRate = re.QuantityRates
                                            .FirstOrDefault(m =>    m.TariffGroupID == tariffGroup.ID
                                                                    && m.QuantityFrom <= request.Pieces
                                                                    && m.QuantityTo >= request.Pieces
                                                                    && m.ZoneName == matchedZone.Zone.Name
                                                                    && m.Service.ToLower() == request.Service.ToLower());
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
                        webResponse.RatedWeight = weight;
                        webResponse.ActualWeight = actualWeight;
                        webResponse.StatusCode = (int)HttpStatusCode.NoContent;
                        webResponse.Pieces = request.Dimensions.Length;
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
                        var sizeAndGirthCharges = GetSizeOverageCharge(re, tariffGroup, request.Dimensions);
                        webResponse.Rate = Math.Round(shippingRate + accessorialRate + fuelRateCharge + sizeAndGirthCharges.SizeCharges + sizeAndGirthCharges.GirthCharges, 2);
                        webResponse.ShippingRate = Math.Round(shippingRate, 2);
                        webResponse.Dimensions = request.Dimensions;
                        webResponse.ActualWeight = actualWeight;
                        webResponse.RatedWeight = weight;
                        webResponse.Service = request.Service;
                        webResponse.Zone = matchedZone.Zone.ID;
                        webResponse.Timestamp = DateTime.Now;
                        webResponse.Pieces = request.Dimensions == null ? (request.Pieces.HasValue ? request.Pieces.Value : 0) : request.Dimensions.Length;
                        webResponse.Milliseconds = (int)(DateTime.Now - request.Timestamp.Value).TotalMilliseconds;
                        webResponse.StatusCode = (int)HttpStatusCode.NoContent;
                        webResponse.FuelRate = Math.Round(fuelRateCharge, 2);
                        webResponse.SizeSurchargeRate = Math.Round(sizeAndGirthCharges.SizeCharges, 2);
                        webResponse.GirthRate = Math.Round(sizeAndGirthCharges.GirthCharges, 2);
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

        private SizeAndGirthCharges GetSizeOverageCharge(RatingAPIEntities re, TariffGroup tariffGroup, WebRequestDimension[] dimensions)
        {
            var sizeAndGirthCharges = new SizeAndGirthCharges();

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

                        if (currentSizeCharge > currentGirthCharge)
                        {
                            sizeAndGirthCharges.SizeCharges += currentSizeCharge;
                        }
                        else
                        {
                            sizeAndGirthCharges.GirthCharges += currentGirthCharge;
                        }
                    }
                }
            }

            return sizeAndGirthCharges;
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
