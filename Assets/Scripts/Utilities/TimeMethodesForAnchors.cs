using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThesisHololens.utilities
{
    static class TimeMethodesForAnchors
    {
        public const string timeFormat = "HH:mm:ss:dd:MM:yyyy";


        /// <summary>
        /// takes the end of the topic (anchor name and time) 
        /// </summary>
        /// <param name="anchor"></param>
        /// <returns>returns a keyvaluepar with the name and ttime</returns>
        public static KeyValuePair<string, DateTime> parseAnchor(string anchor)
        {
            try
            {
                DateTime anchor_time = parseTime(getTimeFromAnchor(anchor));
                string anchor_name = getNameFromAnchor(anchor);

                return new KeyValuePair<string, DateTime>(anchor_name, anchor_time);
            }catch(Exception e)
            {
                
                return default(KeyValuePair<string, DateTime>);
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="anchor"></param>
        /// <returns>anchor.Substring(0, anchor.LastIndexOf("__"));</returns>
        public static string getNameFromAnchor(string anchor)
        { 
            return anchor.Substring(0, anchor.LastIndexOf("__"));

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="anchor"></param>
        /// <returns>referenceAnchor.Substring(0, referenceAnchor.LastIndexOf("__"));</returns>
        public static string getTimeFromAnchor(string anchor)
        {
            return anchor.Substring(anchor.LastIndexOf("__") + 2);
        }

        /// <summary>
        /// takes the name of an object ant puts the time behind it
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string generateNameWithTimeForAnchor(string name)
        {

            return name + "__" + getCurrentTimeforAnchorAsString();

        }


        public static string getCurrentTimeforAnchorAsString()
        {
            string time_str = System.DateTime.Now.ToUniversalTime().ToString(timeFormat);

            return time_str;
        }

        public static DateTime getCurrentTimeforAnchors()
        {


            return DateTime.Now.ToUniversalTime();
        }

        public static string addCurrentTimeToAnchorName(string anchorName)
        {
            anchorName += "__";
            anchorName += getCurrentTimeforAnchorAsString();

            return anchorName;
        }

        public static string getAnchorNameWithTimeFromKeyValuePair(KeyValuePair<string, DateTime> anchor)
        {

            return anchor.Key + "__" + anchor.Value.ToString(timeFormat);
        }


        /// <summary>
        /// throws exeption if fails
        /// </summary>
        /// <param name="time_str">need format from System.Globalization.CultureInfo.InvariantCulture</param>
        /// <returns></returns>
        public static System.DateTime parseTime(string time_str)
        {

            return System.DateTime.ParseExact(time_str, timeFormat, System.Globalization.CultureInfo.InvariantCulture);

        }




    }
}
