using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RatingAPI.Models
{
    public class ProcessedZone
    {
        public Zone Zone;
        public WebRequest WebRequest;

        public int FromCharacterOrdinalMatch, ToCharacterOrdinalMatch, TotalCharacterOrdinalMatch;
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
            var shipmentFromPostal = WebRequest.FromPostal.Trim().Replace(" ", "").ToLower();
            var shipmentToPostal = WebRequest.ToPostal.Trim().Replace(" ", "").ToLower();

            var zoneOriginFromPostal = Zone.OriginFromPostal.Trim().Replace(" ", "").ToLower();
            var zoneOriginToPostal = Zone.OriginToPostal.Trim().Replace(" ", "").ToLower();

            var zoneDestinationFromPostal = Zone.DestinationFromPostal.Trim().Replace(" ", "").ToLower();
            var zoneDestinationToPostal = Zone.DestinationToPostal.Trim().Replace(" ", "").ToLower();

            if (shipmentFromPostal[0] == )
        }
    }
}