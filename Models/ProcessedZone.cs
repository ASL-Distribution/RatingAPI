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

        public ProcessedZone(Zone zone, WebRequest webRequest)
        {
            this.Zone = zone;
            this.WebRequest = webRequest;

            GetCharacterOrdinalMatchCounts();
        }

        public void GetCharacterOrdinalMatchCounts()
        {

        }
    }
}