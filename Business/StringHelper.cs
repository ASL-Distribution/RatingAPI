using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RatingAPI.Business
{
    public class StringHelper
    {
        public static string MakeStringToLength(string value, int length)
        {
            var sizedValue = value;

            if (value.Length > 6)
            {
                sizedValue = value.Substring(0, 6);
            }
            else
            {
                var difference = length - value.Length;

                for (int i = 0; i < difference; i++)
                {
                    sizedValue += "~";
                }
            }

            return sizedValue;
        }
    }
}