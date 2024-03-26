using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RatingAPI.Business
{
    public class PostalCodeHelper
    {
        private bool CheckIfPostalCodeIsWithinRange(string postalFrom, string postsalTo, string postalCode)
        {
            if ((postalCode.CompareTo(postalFrom) == 0
                || postalCode.CompareTo(postalFrom) == 1)

                &&

                (postalCode.CompareTo(postsalTo) == 0
                || postalCode.CompareTo(postsalTo) == -1))
            {
                return true;
            }

            return false;
        }
    }
}