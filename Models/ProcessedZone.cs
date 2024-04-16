using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RatingAPI.Business;

namespace RatingAPI.Models
{
    public class ProcessedZone
    {
        public Zone Zone;
        public WebRequest WebRequest;

        public int FromCharacterOrdinalMatchCount, ToCharacterOrdinalMatchCount, TotalCharacterOrdinalMatchCount;
        public bool IsFromAndToEqualMatch = false;
        public bool IsToMatchGreater = false;

        public ProcessedZone(Zone zone, WebRequest webRequest)
        {
            this.Zone = zone;
            this.WebRequest = webRequest;

            GetCharacterOrdinalMatchCounts();
        }

        public void GetCharacterOrdinalMatchCounts()
        {
            var shipmentFromPostal = StringHelper.MakeStringToLength(WebRequest.FromPostal.Trim().Replace(" ", "").ToLower(), 6);
            var shipmentToPostal = StringHelper.MakeStringToLength(WebRequest.ToPostal.Trim().Replace(" ", "").ToLower(), 6);

            var zoneOriginFromPostal = StringHelper.MakeStringToLength(Zone.OriginFromPostal.Trim().Replace(" ", "").ToLower(), 6);
            var zoneOriginToPostal = StringHelper.MakeStringToLength(Zone.OriginToPostal.Trim().Replace(" ", "").ToLower(), 6);

            var zoneDestinationFromPostal = StringHelper.MakeStringToLength(Zone.DestinationFromPostal.Trim().Replace(" ", "").ToLower(), 6);
            var zoneDestinationToPostal = StringHelper.MakeStringToLength(Zone.DestinationToPostal.Trim().Replace(" ", "").ToLower(), 6);

            for (int i = 0; i < 6; i++)
            {
                if (shipmentFromPostal[i] != '~'
                    && shipmentFromPostal[i] == zoneOriginFromPostal[i] 
                    && shipmentFromPostal[i] == zoneOriginToPostal[i])
                {
                    FromCharacterOrdinalMatchCount++;
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                if (shipmentToPostal[i] != '~'
                    && shipmentToPostal[i] == zoneDestinationFromPostal[i]
                    && shipmentToPostal[i] == zoneDestinationToPostal[i])
                {
                    ToCharacterOrdinalMatchCount++;
                }
                else
                {
                    break;
                }
            }

            TotalCharacterOrdinalMatchCount = FromCharacterOrdinalMatchCount + ToCharacterOrdinalMatchCount;
        }
    }
}