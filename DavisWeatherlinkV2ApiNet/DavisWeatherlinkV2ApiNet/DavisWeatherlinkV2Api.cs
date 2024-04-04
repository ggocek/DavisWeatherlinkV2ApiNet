using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Policy;
using Newtonsoft.Json;
using System.Globalization;
using static System.Collections.Specialized.BitVector32;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json.Linq;

namespace DavisWeatherlinkV2ApiNet
{
    /// <summary>
    /// Umbrella class for representing Davis Vantage Vue data.
    /// </summary>
    public class DavisVantageVue
    {
        /// <summary>
        /// A sensor type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int VantageVueWirelessSensorType = 37;
        /// <summary>
        /// A sensor type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int BarometerSensorType = 242;
        /// <summary>
        /// A sensor type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int ConsoleInsideTempHumSensorType = 365;
        /// <summary>
        /// A sensor type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int ConsoleHealthSensorType = 509;

        /// <summary>
        /// A data structure type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int ISSCUrrentConditionsDST = 23;
        /// <summary>
        /// A data structure type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int BarometerCurrentConditionsDST = 19;
        /// <summary>
        /// A data structure type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int InternalTempHumCurrentConditionsDST = 21;
        /// <summary>
        /// A data structure type value returned from the console for a Vantage Vue station.
        /// </summary>
        public const int ConsoleHealthDST = 27;

        /// <summary>
        /// Metadata JSON string for one weather station. This is public so that a caller can see the returned
        /// information while debugging.
        /// </summary>
        public string curStationData = "";
        /// <summary>
        /// Metadata JSON string for all weather stations. This is public so that a caller can see the returned
        /// information while debugging.
        /// </summary>
        public string stationData = "";
        /// <summary>
        /// Current conditions JSON string. This is public so that a caller can see the returned
        /// information while debugging.
        /// </summary>
        public string conditionsAtStation = "";

        /// <summary>
        /// The key must be inserted into the web request query string. If blank or junk, the response will
        /// be empty or junk.
        /// </summary>
        public string WeatherlinkApiV2Key { get; set; }
        /// <summary>
        /// The secret must be inserted into the header of the web requests. If blank or junk, the response will
        /// be empty or junk.
        /// </summary>
        public string WeatherlinkApiV2Secret { get; set; }
        /// <summary>
        /// StationIdGuid - this GUID is found by logging in to your Weatherlink account. In the browser address bar,
        /// see something like https://www.weatherlink.com/bulletin/1234abcd-5678-ffee-ccbb-009988776655.
        /// The GUID is the string after the last slash.
        /// </summary>
        public string WeatherlinkStationIdGuid { get; set; }
        /// <summary>
        /// StationIdInt - this ID is retrieved via an API call and is then inserted into the query string
        /// of the web request to get current conditions. This value is tied tp the Weatherlink account, so
        /// this presumably does not change unless a change is made to the station or account.
        /// </summary>
        public string WeatherlinkStationIdInt { get; set; }
        /// <summary>
        /// UserAgent for web requests. If blank, "unkown" is sent. As a courtesy to WeatherLink, set this to
        /// something relevant like your name or domain name.
        /// </summary>
        public string WeatherlinkUserAgent { get; set; }
        /// <summary>
        /// Referer for web requests. If blank, "unkown" is sent. As a courtesy to WeatherLink, set this to
        /// something relevant like your web site address.
        /// </summary>
        public string WeatherlinkReferer { get; set; }
        /// <summary>
        /// AcceptLangauge for web request. If blank, "en-US" is sent.
        /// </summary>
        public string WeatherlinkAcceptLanguage { get; set; }

        /// <summary>
        /// Call this with a new instance of DavisVantageVue with properties WeatherlinkApiV2Key, WeatherlinkApiV2Secret,
        /// WeatherlinkStationIdGuid, WeatherlinkUserAgent, WeatherlinkReferer, WeatherlinkAcceptLanguage.
        /// This returns JSON containing the WeatherlinkStationIdInt used by LoadCurrentConditions.
        /// </summary>
        public DavisStations LoadStations
        {
            get
            {
                try
                {
                    string weatherlinkStationDataUrl =
                        "https://api.weatherlink.com/v2/stations" +
                        "?api-key=" + WeatherlinkApiV2Key;
                    conditionsAtStation = string.Empty;

                    HttpWebRequest webRequest = WebRequest.Create(weatherlinkStationDataUrl) as HttpWebRequest;
                    webRequest.Method = "GET";
                    webRequest.KeepAlive = true;
                    webRequest.Headers["X-Api-Secret"] = WeatherlinkApiV2Secret;
                    webRequest.UserAgent = string.IsNullOrEmpty(WeatherlinkUserAgent) ? "unknown" : WeatherlinkUserAgent;
                    webRequest.Referer = string.IsNullOrEmpty(WeatherlinkReferer) ? "unknown" : WeatherlinkReferer;
                    webRequest.Headers.Add(HttpRequestHeader.AcceptLanguage, string.IsNullOrEmpty(WeatherlinkAcceptLanguage) ? "en-US" : WeatherlinkAcceptLanguage);

                    // Get the response
                    DavisStations ds = null;
                    using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
                    {
                        // Always JSON, never XML
                        using (System.IO.StreamReader currentSR = new System.IO.StreamReader(webResponse.GetResponseStream()))
                        {
                            stationData = currentSR.ReadToEnd();
                            // stationData is the full JSON payload.
                            // Deserialize this into the desired class.
                            ds = JsonConvert.DeserializeObject<DavisStations>(stationData);
                            if (ds == null)
                                return new DavisStations()
                                {
                                    stations = null
                                };

                            // One of the API's station properties is "private", but this is a C# keyword
                            // and cannot be used as a property name in the Station class. So, re-parse the
                            // payload to get a searchable string.
                            dynamic dynamicObj = JObject.Parse(stationData); // string with the whole payload
                            if (dynamicObj == null)
                                return new DavisStations()
                                {
                                    stations = null
                                };
                            dynamic dynStations = dynamicObj.stations; // array of strings.
                            if (dynStations == null)
                                return new DavisStations()
                                {
                                    stations = null
                                };
                            for (int iii = 0; iii < dynStations.Count; iii++)
                            {
                                curStationData = (dynStations[iii]).ToString();
                                // curStationData is a string for a station, something like:
                                /*
                                 * "{\r\n  \"station_id\": 123456,\r\n  \"station_id_uuid\": \"11111111-aaaa-bbbb-cccc-888888888888\",
                                 * \r\n  \"station_name\": \"My Station\",\r\n  \"gateway_id\": 12345678,
                                 * \r\n  \"gateway_id_hex\": \"1234567890ab\",\r\n  \"product_number\": \"6313\",
                                 * \r\n  \"username\": \"MyName\",\r\n  \"user_email\": \"my@email.com\",
                                 * \r\n  \"company_name\": \"My Company\",\r\n  \"active\": true,\r\n  144444 false,
                                 * \r\n  \"recording_interval\": 15,\r\n  \"firmware_version\": \"1.4.14\",
                                 * \r\n  \"registered_date\": 1600422513,\r\n  \"time_zone\": \"America/New_York\",
                                 * \r\n  \"city\": \"Mycity\",\r\n  \"region\": \"AK\",
                                 * \r\n  \"country\": \"United States of America\",\r\n  \"latitude\": 40.12345,
                                 * \r\n  \"longitude\": -70.54321,\r\n  \"elevation\": 301.55555\r\n}"
                                */

                                // Parse for the "private" setting.
                                // Remove \r\n:
                                curStationData = curStationData.Replace('\r', ' ');
                                curStationData = curStationData.Replace('\n', ' ');
                                // Remove whitespace
                                curStationData = curStationData.Replace(" ", "");
                                // The relevant substring is
                                // ,\"private\":false, OR ,\"private\":true,
                                // If a user somehow manages to assign this sort of value to another property,
                                // such as company name \"private\":true, results will be unpredictable, but this
                                // should find the private value for anyone not trying to break the code.
                                int privateIndex = curStationData.IndexOf("\"private\":");
                                // Expect at least four characters after the colon.
                                if ((privateIndex < 0) ||
                                    (curStationData.Length < privateIndex + 14))
                                {
                                    // The string is junk
                                    ds.stations[iii].privatej = null;
                                    continue;
                                }
                                // Get the FOUR characters following the colon:
                                string privS = curStationData.Substring(privateIndex + 10, 4).ToLower();
                                if (privS == "true")
                                {
                                    ds.stations[iii].privatej = true;
                                }
                                else if (privS == "fals") /* only looking at four characters */
                                {
                                    ds.stations[iii].privatej = false;
                                }
                                else /* junk */
                                {
                                    ds.stations[iii].privatej = null;
                                }
                            }
                        }
                    }
                    return ds;
                }
                catch
                {
                    return new DavisStations()
                    {
                        stations = null
                    };
                }
            }
        }

        /// <summary>
        /// Call this with a new instance of DavisVantageVue with properties WeatherlinkApiV2Key, WeatherlinkApiV2Secret,
        /// WeatherlinkStationIdGuid, WeatherlinkUserAgent, WeatherlinkReferer, WeatherlinkAcceptLanguage,
        /// WeatherlinkStationIdInt.
        /// This returns JSON containing the station current condiitions.
        /// </summary>
        public CurrentConditions LoadCurrentConditions
        {
            get
            {
                try
                {
                    string weatherlinkCurrentDataUrl =
                        "https://api.weatherlink.com/v2/current/" + WeatherlinkStationIdInt +
                        "?api-key=" + WeatherlinkApiV2Key;
                    conditionsAtStation = string.Empty;

                    HttpWebRequest webRequest = WebRequest.Create(weatherlinkCurrentDataUrl) as HttpWebRequest;
                    webRequest.Method = "GET";
                    webRequest.KeepAlive = true;
                    webRequest.Headers["X-Api-Secret"] = WeatherlinkApiV2Secret;
                    webRequest.UserAgent = string.IsNullOrEmpty(WeatherlinkUserAgent) ? "unknown" : WeatherlinkUserAgent;
                    webRequest.Referer = string.IsNullOrEmpty(WeatherlinkReferer) ? "unknown" : WeatherlinkReferer;
                    webRequest.Headers.Add(HttpRequestHeader.AcceptLanguage, string.IsNullOrEmpty(WeatherlinkAcceptLanguage) ? "en-US" : WeatherlinkAcceptLanguage);

                    // Get the response
                    using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
                    {
                        // Always JSON, never XML
                        using (System.IO.StreamReader currentSR = new System.IO.StreamReader(webResponse.GetResponseStream()))
                        {
                            conditionsAtStation = currentSR.ReadToEnd();
                            // conditionsAtStation is the full JSON payload.
                            // Deserialize this into the desired class.
                            return JsonConvert.DeserializeObject<CurrentConditions>(conditionsAtStation);
                        }
                    }
                }
                catch
                {
                    return new CurrentConditions()
                    {
                        station_id = -999
                    };
                }
            }
        }
    }

    /// <summary>
    /// A class for generating HTML markup from current conditions
    /// </summary>
    public class DavisDisplay
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="setValues">If false, the following properties are left blank: TempSuffix, HumSuffix
        /// WindSpeedSuffix, BarSuffix, RainSuffix. Otherwise, they are set to American values,
        /// "°F", "%", "mph", "inHg", " (inches)</param>
        public DavisDisplay(bool setValues)
        {
            if (setValues)
            {
                TempSuffix = "°F";
                HumSuffix = "%";
                WindSpeedSuffix = "mph";
                BarSuffix = "inHg";
                RainSuffix = "\"";
            }
        }

        /// <summary>
        /// Used with the HtmlMarkup method
        /// </summary>
        public enum HtmlDisplayType
        {
            /// <summary>
            ///  Use Pretty for a friendly human version
            /// </summary>
            Pretty,
            /// <summary>
            ///  Use Dump for a raw list of properties and values
            /// </summary>
            Dump
        }

        /// <summary>
        /// A helper property for your code, instead of using a literal string
        /// at display time. This is not set from any data returned by the Davis API.
        /// </summary>
        public string TempSuffix { get; set; }
        /// <summary>
        /// A helper property for your code, instead of using a literal string
        /// at display time. This is not set from any data returned by the Davis API.
        /// </summary>
        public string HumSuffix { get; set; }
        /// <summary>
        /// A helper property for your code, instead of using a literal string
        /// at display time. This is not set from any data returned by the Davis API.
        /// </summary>
        public string WindSpeedSuffix { get; set; }
        /// <summary>
        /// A helper property for your code, instead of using a literal string
        /// at display time. This is not set from any data returned by the Davis API.
        /// </summary>
        public string BarSuffix { get; set; }
        /// <summary>
        /// A helper property for your code, instead of using a literal string
        /// at display time. This is not set from any data returned by the Davis API.
        /// </summary>
        public string RainSuffix { get; set; }

        /// <summary>
        /// When the API returns the JSON for a Vantage Vue station, there is one big chunk of JSON
        /// containing four sets of sensor data: basic current conditions, barometer current conditions,
        /// ambient current conditions (where the console resides), and console health properties.
        /// This Davis package uses Newtonsoft to make one deserialization call against the JSON,
        /// returning an object with an array of four sets of sensor data. In order to
        /// determine which set of data goes with each sensor, the caller needs to get the whole object
        /// and then, for each array element, check the sensor_type and data_structure_type.
        /// See the code examples in the readme file.
        /// The four pairs of sensor_type / data_structure_type supported by this package are
        /// 37/23, 242/19, 365/21, 509/27. Any other combinations are not known to this package.
        /// That doesn't mean you can't use this package, but you'll need to figure out what is in your JSON.
        /// Note that after deserializtion, every sensor will have every property, but only the properties
        /// for that sensor will be valid. For example, if you want to know a barometer value, you need to
        /// find the sensor data for the barometer sensor; the barometer values in the console health sensor data
        /// will be null. That's not a bug, it just means the developer of this package was too lazy to
        /// get the JSON response, parse it into four sets, and deserialize it four times. And that's also
        /// because the Davis API documentation is hard to use. My station has four sensors, so that's what I coded.
        /// ConsoleISSData should be set to the sensor data for sensor_type 37 and data_structure_type 23.
        /// </summary>
        public WeatherLinkConsoleISSData ConsoleISSData { get; set; }
        /// <summary>
        /// See the description of ConsoleISSData.
        /// ConsoleBarometerData should be set to the sensor data for sensor_type 242 and data_structure_type 19.
        /// </summary>
        public WeatherLinkConsoleISSData ConsoleBarometerData { get; set; }
        /// <summary>
        /// See the description of ConsoleISSData.
        /// ConsoleInternalTempHumData should be set to the sensor data for sensor_type 365 and data_structure_type 21.
        /// </summary>
        public WeatherLinkConsoleISSData ConsoleInternalTempHumData { get; set; }
        /// <summary>
        /// See the description of ConsoleISSData.
        /// ConsoleHealthData should be set to the sensor data for sensor_type 509 and data_structure_type 27.
        /// </summary>
        public WeatherLinkConsoleISSData ConsoleHealthData { get; set; }

        /// <summary>
        /// Calculate the .NET DateTime from a JSON long date (epoch) value.
        /// </summary>
        /// <param name="jsonDate">The long date (epoch) value</param>
        /// <returns>.NET DateTime</returns>
        public DateTime JsonDate2DateTime(long jsonDate)
        {
            try
            {
                string sa = @"""" + "/Date(" + jsonDate + ")/" + @"""";
                DateTime dt = JsonConvert.DeserializeObject<DateTime>(sa);
                return dt;
            }
            catch
            {
                return new DateTime(1900, 1, 1);
            }
        }

        /// <summary>
        /// Calculate the English language compass direction from the degrees.
        /// </summary>
        /// <param name="degrees">0-359</param>
        /// <returns>N, SSW, etc.</returns>
        public string EnglishDir(int degrees)
        {
            if (degrees < 12)
                return "N";
            else if (degrees < 33)
                return "NNE";
            else if (degrees < 56)
                return "NE";
            else if (degrees < 78)
                return "ENE";
            else if (degrees < 101)
                return "E";
            else if (degrees < 123)
                return "ESE";
            else if (degrees < 146)
                return "SE";
            else if (degrees < 168)
                return "SSE";
            else if (degrees < 191)
                return "S";
            else if (degrees < 213)
                return "SSW";
            else if (degrees < 236)
                return "SW";
            else if (degrees < 258)
                return "WSW";
            else if (degrees < 281)
                return "W";
            else if (degrees < 303)
                return "WNW";
            else if (degrees < 326)
                return "NW";
            else if (degrees < 348)
                return "NNW";

            return "N";
        }

        /// <summary>
        /// Write some markup to show current conditions, see readme for coding examples.
        /// </summary>
        /// <param name="textColor">Text color - do not imbed quotes in the arg</param>
        /// <param name="bgColor">Background color - do not imbed quotes in the arg</param>
        /// <param name="placeTitle">A plain name of the station location, such as "Niagara Falls, NY USA",
        /// only used for HtmlDisplayType Pretty.</param>
        /// <param name="extraConditions">Conditions in addition to the weather station conditions, such
        /// as cloudiness retrieved from NOAA, only used for HtmlDisplayType Pretty.</param>
        /// <param name="credits">Credit for the data, such as "Davis Vantage Vue and NOAA",
        /// only used for HtmlDisplayType Pretty.</param>
        /// <param name="divwidth">Container width in pixels.</param>
        /// <param name="divheight">Container height in pixels.</param>
        /// <param name="hdt">The display type.</param>
        /// <returns>HTML markup for the desired display type</returns>
        public string HtmlMarkup(string textColor, string bgColor, string placeTitle, string extraConditions,
            string credits, int divwidth, int divheight, HtmlDisplayType hdt)
        {
            string myS = string.Empty;
            string myMarkup = string.Empty;

            switch (hdt)
            {
                case HtmlDisplayType.Pretty:
                    // text color, bg color, width, height
                    myS += "<div style=\"color:" + textColor + "; background-color:" + bgColor + "; font-weight:bold; " +
                        "width:" + divwidth.ToString() + "px; height:" + divheight.ToString() + "px; " +
                        "min-width:" + divwidth.ToString() + "px; min-height:" + divheight.ToString() + "px; " +
                        "max-width:" + divwidth.ToString() + "px; max-height:" + divheight.ToString() + "px; " +
                        "float:left; overflow-wrap:anywhere;\"" +
                        ">" + Environment.NewLine;
                    myMarkup = PrettyMarkup(myS, placeTitle, extraConditions, credits);
                    myS = myMarkup + "</div>" + Environment.NewLine;
                    break;
                case HtmlDisplayType.Dump:
                    // text color, bg color, width (height unspecified)
                    myS += "<div style=\"color:" + textColor + "; background-color:" + bgColor + "; font-weight:bold; " +
                        "width:" + divwidth.ToString() + "px; " +
                        "min-width:" + divwidth.ToString() + "px; " +
                        "max-width:" + divwidth.ToString() + "px; " +
                        "float:left; overflow-wrap:anywhere;\"" +
                        ">";
                    myMarkup = DumpMarkup(myS);
                    myS = myMarkup + "</div>";
                    break;
                default:
                    break;
            }

            return myS;
        }

        /// <summary>
        /// Render the data as a raw list of properties and values.
        /// </summary>
        /// <param name="containerMarkup">The Div container markup into which the property markup will be inserted</param>
        /// <returns>HTML markup</returns>
        private string DumpMarkup(string containerMarkup)
        {
            string myS = containerMarkup;

            myS += "Sensor type " + DavisVantageVue.VantageVueWirelessSensorType.ToString() +
                ", data structure type " + DavisVantageVue.ISSCUrrentConditionsDST.ToString() +
                ":<br />" + Environment.NewLine;
            myS += "ConsoleISSData.ts=" +
                (ConsoleISSData.ts.HasValue ? ConsoleISSData.ts.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.tx_id=" +
                (ConsoleISSData.tx_id.HasValue ? ConsoleISSData.tx_id.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.tz_offset=" +
                (ConsoleISSData.tz_offset.HasValue ? ConsoleISSData.tz_offset.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.temp=" +
                (ConsoleISSData.temp.HasValue ? ConsoleISSData.temp.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.hum=" +
                (ConsoleISSData.hum.HasValue ? ConsoleISSData.hum.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.dew_point=" +
                (ConsoleISSData.dew_point.HasValue ? ConsoleISSData.dew_point.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wet_bulb=" +
                (ConsoleISSData.wet_bulb.HasValue ? ConsoleISSData.wet_bulb.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.heat_index=" +
                (ConsoleISSData.heat_index.HasValue ? ConsoleISSData.heat_index.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_chill=" +
                (ConsoleISSData.wind_chill.HasValue ? ConsoleISSData.wind_chill.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.thw_index=" +
                (ConsoleISSData.thw_index.HasValue ? ConsoleISSData.thw_index.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.thsw_index=" +
                (ConsoleISSData.thsw_index.HasValue ? ConsoleISSData.thsw_index.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wbgt=" +
                (ConsoleISSData.wbgt.HasValue ? ConsoleISSData.wbgt.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_speed_last=" +
                (ConsoleISSData.wind_speed_last.HasValue ? ConsoleISSData.wind_speed_last.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_dir_last=" +
                (ConsoleISSData.wind_dir_last.HasValue ? ConsoleISSData.wind_dir_last.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_speed_avg_last_1_min=" +
                (ConsoleISSData.wind_speed_avg_last_1_min.HasValue ? ConsoleISSData.wind_speed_avg_last_1_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_speed_avg_last_2_min=" +
                (ConsoleISSData.wind_speed_avg_last_2_min.HasValue ? ConsoleISSData.wind_speed_avg_last_2_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_dir_scalar_avg_last_1_min=" +
                (ConsoleISSData.wind_dir_scalar_avg_last_1_min.HasValue ? ConsoleISSData.wind_dir_scalar_avg_last_1_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_dir_scalar_avg_last_2_min=" +
                (ConsoleISSData.wind_dir_scalar_avg_last_2_min.HasValue ? ConsoleISSData.wind_dir_scalar_avg_last_2_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_speed_hi_last_2_min=" +
                (ConsoleISSData.wind_speed_hi_last_2_min.HasValue ? ConsoleISSData.wind_speed_hi_last_2_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_dir_at_hi_speed_last_2_min=" +
                (ConsoleISSData.wind_dir_at_hi_speed_last_2_min.HasValue ? ConsoleISSData.wind_dir_at_hi_speed_last_2_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_speed_avg_last_10_min=" +
                (ConsoleISSData.wind_speed_avg_last_10_min.HasValue ? ConsoleISSData.wind_speed_avg_last_10_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_dir_scalar_avg_last_10_min=" +
                (ConsoleISSData.wind_dir_scalar_avg_last_10_min.HasValue ? ConsoleISSData.wind_dir_scalar_avg_last_10_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_speed_hi_last_10_min=" +
                (ConsoleISSData.wind_speed_hi_last_10_min.HasValue ? ConsoleISSData.wind_speed_hi_last_10_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_dir_at_hi_speed_last_10_min=" +
                (ConsoleISSData.wind_dir_at_hi_speed_last_10_min.HasValue ? ConsoleISSData.wind_dir_at_hi_speed_last_10_min.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.wind_run_day=" +
                (ConsoleISSData.wind_run_day.HasValue ? ConsoleISSData.wind_run_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_size=" +
                (ConsoleISSData.rain_size.HasValue ? ConsoleISSData.rain_size.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_last_clicks=" +
                (ConsoleISSData.rain_rate_last_clicks.HasValue ? ConsoleISSData.rain_rate_last_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_last_in=" +
                (ConsoleISSData.rain_rate_last_in.HasValue ? ConsoleISSData.rain_rate_last_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_last_mm=" +
                (ConsoleISSData.rain_rate_last_mm.HasValue ? ConsoleISSData.rain_rate_last_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_hi_clicks=" +
                (ConsoleISSData.rain_rate_hi_clicks.HasValue ? ConsoleISSData.rain_rate_hi_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_hi_in=" +
                (ConsoleISSData.rain_rate_hi_in.HasValue ? ConsoleISSData.rain_rate_hi_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_hi_mm=" +
                (ConsoleISSData.rain_rate_hi_mm.HasValue ? ConsoleISSData.rain_rate_hi_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_15_min_clicks=" +
                (ConsoleISSData.rainfall_last_15_min_clicks.HasValue ? ConsoleISSData.rainfall_last_15_min_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_15_min_in=" +
                (ConsoleISSData.rainfall_last_15_min_in.HasValue ? ConsoleISSData.rainfall_last_15_min_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_15_min_mm=" +
                (ConsoleISSData.rainfall_last_15_min_mm.HasValue ? ConsoleISSData.rainfall_last_15_min_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_hi_last_15_min_clicks=" +
                (ConsoleISSData.rain_rate_hi_last_15_min_clicks.HasValue ? ConsoleISSData.rain_rate_hi_last_15_min_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_hi_last_15_min_in=" +
                (ConsoleISSData.rain_rate_hi_last_15_min_in.HasValue ? ConsoleISSData.rain_rate_hi_last_15_min_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_rate_hi_last_15_min_mm=" +
                (ConsoleISSData.rain_rate_hi_last_15_min_mm.HasValue ? ConsoleISSData.rain_rate_hi_last_15_min_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_60_min_clicks=" +
                (ConsoleISSData.rainfall_last_60_min_clicks.HasValue ? ConsoleISSData.rainfall_last_60_min_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_60_min_in=" +
                (ConsoleISSData.rainfall_last_60_min_in.HasValue ? ConsoleISSData.rainfall_last_60_min_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_60_min_mm=" +
                (ConsoleISSData.rainfall_last_60_min_mm.HasValue ? ConsoleISSData.rainfall_last_60_min_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_24_hr_clicks=" +
                (ConsoleISSData.rainfall_last_24_hr_clicks.HasValue ? ConsoleISSData.rainfall_last_24_hr_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_24_hr_in=" +
                (ConsoleISSData.rainfall_last_24_hr_in.HasValue ? ConsoleISSData.rainfall_last_24_hr_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_last_24_hr_mm=" +
                (ConsoleISSData.rainfall_last_24_hr_mm.HasValue ? ConsoleISSData.rainfall_last_24_hr_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_current_clicks=" +
                (ConsoleISSData.rain_storm_current_clicks.HasValue ? ConsoleISSData.rain_storm_current_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_current_in=" +
                (ConsoleISSData.rain_storm_current_in.HasValue ? ConsoleISSData.rain_storm_current_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_current_mm=" +
                (ConsoleISSData.rain_storm_current_mm.HasValue ? ConsoleISSData.rain_storm_current_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_current_start_at=" +
                (ConsoleISSData.rain_storm_current_start_at.HasValue ? ConsoleISSData.rain_storm_current_start_at.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_last_clicks=" +
                (ConsoleISSData.rain_storm_last_clicks.HasValue ? ConsoleISSData.rain_storm_last_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_last_in=" +
                (ConsoleISSData.rain_storm_last_in.HasValue ? ConsoleISSData.rain_storm_last_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_last_mm=" +
                (ConsoleISSData.rain_storm_last_mm.HasValue ? ConsoleISSData.rain_storm_last_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_last_start_at=" +
                (ConsoleISSData.rain_storm_last_start_at.HasValue ? ConsoleISSData.rain_storm_last_start_at.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rain_storm_last_end_at=" +
                (ConsoleISSData.rain_storm_last_end_at.HasValue ? ConsoleISSData.rain_storm_last_end_at.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_day_clicks=" +
                (ConsoleISSData.rainfall_day_clicks.HasValue ? ConsoleISSData.rainfall_day_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_day_in=" +
                (ConsoleISSData.rainfall_day_in.HasValue ? ConsoleISSData.rainfall_day_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_day_mm=" +
                (ConsoleISSData.rainfall_day_mm.HasValue ? ConsoleISSData.rainfall_day_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_month_clicks=" +
                (ConsoleISSData.rainfall_month_clicks.HasValue ? ConsoleISSData.rainfall_month_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_month_in=" +
                (ConsoleISSData.rainfall_month_in.HasValue ? ConsoleISSData.rainfall_month_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_month_mm=" +
                (ConsoleISSData.rainfall_month_mm.HasValue ? ConsoleISSData.rainfall_month_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_year_clicks=" +
                (ConsoleISSData.rainfall_year_clicks.HasValue ? ConsoleISSData.rainfall_year_clicks.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_year_in=" +
                (ConsoleISSData.rainfall_year_in.HasValue ? ConsoleISSData.rainfall_year_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rainfall_year_mm=" +
                (ConsoleISSData.rainfall_year_mm.HasValue ? ConsoleISSData.rainfall_year_mm.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.solar_rad=" +
                (ConsoleISSData.solar_rad.HasValue ? ConsoleISSData.solar_rad.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.solar_energy_day=" +
                (ConsoleISSData.solar_energy_day.HasValue ? ConsoleISSData.solar_energy_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.et_day=" +
                (ConsoleISSData.et_day.HasValue ? ConsoleISSData.et_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.et_month=" +
                (ConsoleISSData.et_month.HasValue ? ConsoleISSData.et_month.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.et_year=" +
                (ConsoleISSData.et_year.HasValue ? ConsoleISSData.et_year.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.uv_index=" +
                (ConsoleISSData.uv_index.HasValue ? ConsoleISSData.uv_index.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.uv_dose_day=" +
                (ConsoleISSData.uv_dose_day.HasValue ? ConsoleISSData.uv_dose_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.hdd_day=" +
                (ConsoleISSData.hdd_day.HasValue ? ConsoleISSData.hdd_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.cdd_day=" +
                (ConsoleISSData.cdd_day.HasValue ? ConsoleISSData.cdd_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.reception_day=" +
                (ConsoleISSData.reception_day.HasValue ? ConsoleISSData.reception_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rssi_last=" +
                (ConsoleISSData.rssi_last.HasValue ? ConsoleISSData.rssi_last.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.crc_errors_day=" +
                (ConsoleISSData.crc_errors_day.HasValue ? ConsoleISSData.crc_errors_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.resyncs_day=" +
                (ConsoleISSData.resyncs_day.HasValue ? ConsoleISSData.resyncs_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.packets_received_day=" +
                (ConsoleISSData.packets_received_day.HasValue ? ConsoleISSData.packets_received_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.packets_received_streak=" +
                (ConsoleISSData.packets_received_streak.HasValue ? ConsoleISSData.packets_received_streak.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.packets_missed_day=" +
                (ConsoleISSData.packets_missed_day.HasValue ? ConsoleISSData.packets_missed_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.packets_missed_streak=" +
                (ConsoleISSData.packets_missed_streak.HasValue ? ConsoleISSData.packets_missed_streak.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.packets_received_streak_hi_day=" +
                (ConsoleISSData.packets_received_streak_hi_day.HasValue ? ConsoleISSData.packets_received_streak_hi_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.packets_missed_streak_hi_day=" +
                (ConsoleISSData.packets_missed_streak_hi_day.HasValue ? ConsoleISSData.packets_missed_streak_hi_day.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.rx_state=" +
                (ConsoleISSData.rx_state.HasValue ? ConsoleISSData.rx_state.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.freq_error_current=" +
                (ConsoleISSData.freq_error_current.HasValue ? ConsoleISSData.freq_error_current.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.freq_error_total=" +
                (ConsoleISSData.freq_error_total.HasValue ? ConsoleISSData.freq_error_total.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.freq_index=" +
                (ConsoleISSData.freq_index.HasValue ? ConsoleISSData.freq_index.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.last_packet_received_timestamp=" +
                (ConsoleISSData.last_packet_received_timestamp.HasValue ? ConsoleISSData.last_packet_received_timestamp.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.trans_battery_flag=" +
                (ConsoleISSData.trans_battery_flag.HasValue ? ConsoleISSData.trans_battery_flag.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.trans_battery_volt=" +
                (ConsoleISSData.trans_battery_volt.HasValue ? ConsoleISSData.trans_battery_volt.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.solar_panel_volt=" +
                (ConsoleISSData.solar_panel_volt.HasValue ? ConsoleISSData.solar_panel_volt.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.supercap_volt=" +
                (ConsoleISSData.supercap_volt.HasValue ? ConsoleISSData.supercap_volt.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.spars_volt=" +
                (ConsoleISSData.spars_volt.HasValue ? ConsoleISSData.spars_volt.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleISSData.spars_rpm=" +
                (ConsoleISSData.spars_rpm.HasValue ? ConsoleISSData.spars_rpm.ToString() : "null") +
                "<br />" + Environment.NewLine;

            myS += "<br/>Sensor type " + DavisVantageVue.BarometerSensorType.ToString() +
                ", data structure type " + DavisVantageVue.BarometerCurrentConditionsDST.ToString() +
                ":<br />" + Environment.NewLine;
            myS += "ConsoleBarometerData.bar_sea_level=" +
                (ConsoleBarometerData.bar_sea_level.HasValue ? ConsoleBarometerData.bar_sea_level.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleBarometerData.bar_trend=" +
                (ConsoleBarometerData.bar_trend.HasValue ? ConsoleBarometerData.bar_trend.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleBarometerData.bar_absolute=" +
                (ConsoleBarometerData.bar_absolute.HasValue ? ConsoleBarometerData.bar_absolute.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleBarometerData.bar_offset=" +
                (ConsoleBarometerData.bar_offset.HasValue ? ConsoleBarometerData.bar_offset.ToString() : "null") +
                "<br />" + Environment.NewLine;

            myS += "<br/>Sensor type " + DavisVantageVue.ConsoleInsideTempHumSensorType.ToString() +
                ", data structure type " + DavisVantageVue.InternalTempHumCurrentConditionsDST.ToString() +
                ":<br />" + Environment.NewLine;
            myS += "ConsoleInternalTempHumData.temp_in=" +
                (ConsoleInternalTempHumData.temp_in.HasValue ? ConsoleInternalTempHumData.temp_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleInternalTempHumData.hum_in=" +
                (ConsoleInternalTempHumData.hum_in.HasValue ? ConsoleInternalTempHumData.hum_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleInternalTempHumData.dew_point_in=" +
                (ConsoleInternalTempHumData.dew_point_in.HasValue ? ConsoleInternalTempHumData.dew_point_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleInternalTempHumData.wet_bulb_in=" +
                (ConsoleInternalTempHumData.wet_bulb_in.HasValue ? ConsoleInternalTempHumData.wet_bulb_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleInternalTempHumData.heat_index_in=" +
                (ConsoleInternalTempHumData.heat_index_in.HasValue ? ConsoleInternalTempHumData.heat_index_in.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleInternalTempHumData.wbgt_in=" +
                (ConsoleInternalTempHumData.wbgt_in.HasValue ? ConsoleInternalTempHumData.wbgt_in.ToString() : "null") +
                "<br />" + Environment.NewLine;

            myS += "<br/>Sensor type " + DavisVantageVue.ConsoleHealthSensorType.ToString() +
                ", data structure type " + DavisVantageVue.ConsoleHealthDST.ToString() +
                ":<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.health_version=" +
                (ConsoleHealthData.health_version.HasValue ? ConsoleHealthData.health_version.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.console_sw_version=" +
                (ConsoleHealthData.console_sw_version != null ? ConsoleHealthData.console_sw_version : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.console_radio_version=" +
                (ConsoleHealthData.console_radio_version != null ? ConsoleHealthData.console_radio_version : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.console_api_level=" +
                (ConsoleHealthData.console_api_level.HasValue ? ConsoleHealthData.console_api_level.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.battery_voltage=" +
                (ConsoleHealthData.battery_voltage.HasValue ? ConsoleHealthData.battery_voltage.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.battery_percent=" +
                (ConsoleHealthData.battery_percent.HasValue ? ConsoleHealthData.battery_percent.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.battery_condition=" +
                (ConsoleHealthData.battery_condition.HasValue ? ConsoleHealthData.battery_condition.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.battery_current=" +
                (ConsoleHealthData.battery_current.HasValue ? ConsoleHealthData.battery_current.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.battery_temp=" +
                (ConsoleHealthData.battery_temp.HasValue ? ConsoleHealthData.battery_temp.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.charger_plugged=" +
                (ConsoleHealthData.charger_plugged.HasValue ? ConsoleHealthData.charger_plugged.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.battery_status=" +
                (ConsoleHealthData.battery_status.HasValue ? ConsoleHealthData.battery_status.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.os_uptime=" +
                (ConsoleHealthData.os_uptime.HasValue ? ConsoleHealthData.os_uptime.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.app_uptime=" +
                (ConsoleHealthData.app_uptime.HasValue ? ConsoleHealthData.app_uptime.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.bgn=" +
                (ConsoleHealthData.bgn.HasValue ? ConsoleHealthData.bgn.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.ip_address_type=" +
                (ConsoleHealthData.ip_address_type.HasValue ? ConsoleHealthData.ip_address_type.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.ip_v4_address=" +
                (ConsoleHealthData.ip_v4_address != null ? ConsoleHealthData.ip_v4_address : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.ip_v4_gateway=" +
                (ConsoleHealthData.ip_v4_gateway != null ? ConsoleHealthData.ip_v4_gateway : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.ip_v4_netmask=" +
                (ConsoleHealthData.ip_v4_netmask != null ? ConsoleHealthData.ip_v4_netmask : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.dns_type_used=" +
                (ConsoleHealthData.dns_type_used.HasValue ? ConsoleHealthData.dns_type_used.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.rx_kilobytes=" +
                (ConsoleHealthData.rx_kilobytes.HasValue ? ConsoleHealthData.rx_kilobytes.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.tx_kilobytes=" +
                (ConsoleHealthData.tx_kilobytes.HasValue ? ConsoleHealthData.tx_kilobytes.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.local_api_queries=" +
                (ConsoleHealthData.local_api_queries.HasValue ? ConsoleHealthData.local_api_queries.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.wifi_rssi=" +
                (ConsoleHealthData.wifi_rssi.HasValue ? ConsoleHealthData.wifi_rssi.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.link_uptime=" +
                (ConsoleHealthData.link_uptime.HasValue ? ConsoleHealthData.link_uptime.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.connection_uptime=" +
                (ConsoleHealthData.connection_uptime.HasValue ? ConsoleHealthData.connection_uptime.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.bootloader_version=" +
                (ConsoleHealthData.bootloader_version.HasValue ? ConsoleHealthData.bootloader_version.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.clock_source=" +
                (ConsoleHealthData.clock_source.HasValue ? ConsoleHealthData.clock_source.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.gnss_sip_tx_id=" +
                (ConsoleHealthData.gnss_sip_tx_id.HasValue ? ConsoleHealthData.gnss_sip_tx_id.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.free_mem=" +
                (ConsoleHealthData.free_mem.HasValue ? ConsoleHealthData.free_mem.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.internal_free_space=" +
                (ConsoleHealthData.internal_free_space.HasValue ? ConsoleHealthData.internal_free_space.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.system_free_space=" +
                (ConsoleHealthData.system_free_space.HasValue ? ConsoleHealthData.system_free_space.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.queue_kilobytes=" +
                (ConsoleHealthData.queue_kilobytes.HasValue ? ConsoleHealthData.queue_kilobytes.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.database_kilobytes=" +
                (ConsoleHealthData.database_kilobytes.HasValue ? ConsoleHealthData.database_kilobytes.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.battery_cycle_count=" +
                (ConsoleHealthData.battery_cycle_count.HasValue ? ConsoleHealthData.battery_cycle_count.ToString() : "null") +
                "<br />" + Environment.NewLine;
            myS += "ConsoleHealthData.console_os_version=" +
                (ConsoleHealthData.console_os_version != null ? ConsoleHealthData.console_os_version : "null") +
                "<br />" + Environment.NewLine;

            return myS;
        }

        /// <summary>
        /// Render the data in a friendly format for humans
        /// </summary>
        /// <param name="containerMarkup">The Div container markup into which the property markup will be inserted</param>
        /// <param name="placeTitle">A plain name of the station location, such as "Niagara Falls, NY USA"</param>
        /// <param name="extraConditions">Conditions in addition to the weather station conditions, such
        /// as cloudiness retrieved from NOAA.</param>
        /// <param name="credits">Credit for the data, such as "Davis Vantage Vue and NOAA"</param>
        /// <returns>HTML markup</returns>
        private string PrettyMarkup(string containerMarkup, string placeTitle, string extraConditions, string credits)
        {
            string myS = containerMarkup;

            // Outdoor Temperature
            string ot = (ConsoleISSData.temp.HasValue ? ConsoleISSData.temp.Value.ToString("N1", new CultureInfo("en-US")) : "-40.0");
            myS += "<big>" + ot + TempSuffix + ", " + extraConditions + "</big>";
            myS += "<br />" + Environment.NewLine;

            myS += placeTitle;
            myS += "<br />" + Environment.NewLine;

            // Current Wind speed
            string cs = (ConsoleISSData.wind_speed_last.HasValue ? ConsoleISSData.wind_speed_last.Value.ToString("N0", new CultureInfo("en-US")) : "0");
            // Make sure there is no decimal point in the displayed string
            myS += "Wind " + Convert.ToInt32(cs).ToString() + " " + WindSpeedSuffix;
            // Compass Dir in degrees
            string cdir = EnglishDir((ConsoleISSData.wind_dir_last.HasValue ? ConsoleISSData.wind_dir_last.Value : 0));
            myS += " " + cdir;
            myS += "&nbsp;&nbsp;&nbsp;" + Environment.NewLine;
            // Daily Rain inches
            double rai = (ConsoleISSData.rainfall_day_in.HasValue ? ConsoleISSData.rainfall_day_in.Value : 0);
            string re = rai.ToString("N2", new CultureInfo("en-US"));
            re = "Rain " + re + "\"";
            myS += re;
            myS += "<br />" + Environment.NewLine;

            // Outdoor Humidity
            string rh = (ConsoleISSData.hum.HasValue ? ConsoleISSData.hum.Value.ToString("N0", new CultureInfo("en-US")) : "0");
            // Make sure there is no decimal point in the displayed string
            myS += "Humidity " + Convert.ToInt32(rh).ToString() + HumSuffix;
            myS += "<br />" + Environment.NewLine;

            // Barometer
            // For some reason, I can't get barometer to work unless I get the number on a separate line.
            double bar = (ConsoleBarometerData.bar_sea_level.HasValue ? ConsoleBarometerData.bar_sea_level.Value : 0);
            string ba = bar.ToString("N2", new CultureInfo("en-US"));
            myS += "Barometer " + ba + " " + BarSuffix;
            myS += "<br />" + Environment.NewLine;

            // last update
            DateTime updt = JsonDate2DateTime(
                ((ConsoleISSData.ts.HasValue ? ConsoleISSData.ts.Value * 1000 : 0) +
                (ConsoleISSData.tz_offset.HasValue ? (ConsoleISSData.tz_offset.Value * 1000) : 0)));
            myS += updt.ToShortDateString() + " " + updt.ToShortTimeString();
            bool isDaylight = TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now);
            myS += " " + (isDaylight ? "EDT" : "EST");
            myS += "<br />" + Environment.NewLine;

            myS += "<small>" + credits + "</small>" + Environment.NewLine;

            return myS;
        }
    }

    /// <summary>
    /// Response parent structure from a call to get the current conditions at a Vantage Vuew station.
    /// </summary>
    public class CurrentConditions
    {
        public string station_id_uuid { get; set; }
        public int station_id { get; set; }
        public Sensor[] sensors { get; set; }
        public long? generated_at { get; set; } // JSONDATE

        public override string ToString()
        {
            return station_id.ToString();
        }
    }

    /// <summary>
    /// Response parent structure for the current conditions of a particular Vantage Vue station.
    /// </summary>
    public class Sensor
    {
        public int lsid { get; set; }
        public int sensor_type { get; set; }
        public int data_structure_type { get; set; }
        /// <summary>
        /// This contains the data array for a sensor.
        /// </summary>
        public WeatherLinkConsoleISSData[] data { get; set; } // One for each sensor
    }

    /// <summary>
    /// Response structure for the data portion of the current conditions at a WeatherLinkConsoleISS.
    /// </summary>
    public class WeatherLinkConsoleISSData
    {
        // ConsoleISSData - Sensor type 37, data structure type 23

        /// <summary>
        /// timestamp JSONDATE - undocumented
        /// </summary>
        public long? ts { get; set; }
        /// <summary>
        /// some sort of ID - some sensors have it, some do not - undocumented
        /// </summary>
        public int? tx_id { get; set; }
        /// <summary>
        /// The number of seconds from UTC (possibly negative). US Eastern Time is -14400 during EDT, -18000 during EST. - undocumented
        /// </summary>
        public int? tz_offset { get; set; }

        /// <summary>
        /// Most recent temperature reading - degrees Fahrenheit
        /// </summary>
        public double? temp { get; set; }
        /// <summary>
        /// Most recent humidity reading - percent relative humidity
        /// </summary>
        public double? hum { get; set; }
        /// <summary>
        /// Most recent derived dew point - degrees Fahrenheit
        /// </summary>
        public double? dew_point { get; set; }
        /// <summary>
        /// Most recent derived wet bulb - degrees Fahrenheit
        /// </summary>
        public double? wet_bulb { get; set; }
        /// <summary>
        /// Most recent derived heat index - degrees Fahrenheit
        /// </summary>
        public double? heat_index { get; set; }
        /// <summary>
        /// Most recent derived wind chill - degrees Fahrenheit
        /// </summary>
        public double? wind_chill { get; set; }
        /// <summary>
        /// Most recent derived temperature, humidity, wind index - degrees Fahrenheit
        /// </summary>
        public double? thw_index { get; set; }
        /// <summary>
        /// Most recent derived temperature, humidity, solar, wind index - degrees Fahrenheit
        /// </summary>
        public double? thsw_index { get; set; }
        /// <summary>
        /// Most recent derived wet bulb globe temperature - degrees Fahrenheit
        /// </summary>
        public double? wbgt { get; set; }
        /// <summary>
        /// Most recent wind speed value - miles per hour
        /// </summary>
        public double? wind_speed_last { get; set; }
        /// <summary>
        /// Most recent wind direction value - degrees
        /// </summary>
        public int? wind_dir_last { get; set; }
        /// <summary>
        /// Average wind speed over last 1 minute - miles per hour
        /// </summary>
        public double? wind_speed_avg_last_1_min { get; set; }
        /// <summary>
        /// Average wind speed over last 2 minutes - miles per hour
        /// </summary>
        public double? wind_speed_avg_last_2_min { get; set; }
        /// <summary>
        /// Scalar average wind direction over last 1 minute - degrees
        /// </summary>
        public int? wind_dir_scalar_avg_last_1_min { get; set; }
        /// <summary>
        /// Scalar average wind direction over last 2 minutes - degrees
        /// </summary>
        public int? wind_dir_scalar_avg_last_2_min { get; set; }
        /// <summary>
        /// Maximum wind speed over last 2 minutes - miles per hour
        /// </summary>
        public double? wind_speed_hi_last_2_min { get; set; }
        /// <summary>
        /// Direction of maximum wind speed over last 2 minutes - degrees
        /// </summary>
        public int? wind_dir_at_hi_speed_last_2_min { get; set; }
        /// <summary>
        /// Average wind speed over last 10 minutes - miles per hour
        /// </summary>
        public double? wind_speed_avg_last_10_min { get; set; }
        /// <summary>
        /// Scalar average wind direction over last 10 minutes - degrees
        /// </summary>
        public int? wind_dir_scalar_avg_last_10_min { get; set; }
        /// <summary>
        /// Maximum wind speed over last 10 minutes - miles per hour
        /// </summary>
        public double? wind_speed_hi_last_10_min { get; set; }
        /// <summary>
        /// Direction of maximum wind speed over last 10 minutes - degrees
        /// </summary>
        public int? wind_dir_at_hi_speed_last_10_min { get; set; }
        /// <summary>
        /// Daily accumulation of wind run since local midnight - miles
        /// </summary>
        public double? wind_run_day { get; set; }
        /// <summary>
        /// Rain collector size - 1 = 0.01 inch; 2 = 0.2 mm; 3 = 0.1 mm; 4 = 0.001 inch
        /// </summary>
        public int? rain_size { get; set; }
        /// <summary>
        /// Most recent rain rate - clicks
        /// </summary>
        public int? rain_rate_last_clicks { get; set; }
        /// <summary>
        /// Most recent rain rate - inches
        /// </summary>
        public double? rain_rate_last_in { get; set; }
        /// <summary>
        /// Most recent rain rate - millimeters
        /// </summary>
        public double? rain_rate_last_mm { get; set; }
        /// <summary>
        /// Highest rain rate over the last 1 minute - clicks
        /// </summary>
        public int? rain_rate_hi_clicks { get; set; }
        /// <summary>
        /// Highest rain rate over the last 1 minute - inches
        /// </summary>
        public double? rain_rate_hi_in { get; set; }
        /// <summary>
        /// Highest rain rate over the last 1 minute - millimeters
        /// </summary>
        public double? rain_rate_hi_mm { get; set; }
        /// <summary>
        /// Total rain over the last 15 minutes - clicks
        /// </summary>
        public int? rainfall_last_15_min_clicks { get; set; }
        /// <summary>
        /// Total rain over the last 15 minutes - inches
        /// </summary>
        public double? rainfall_last_15_min_in { get; set; }
        /// <summary>
        /// Total rain over the last 15 minutes - millimeters
        /// </summary>
        public double? rainfall_last_15_min_mm { get; set; }
        /// <summary>
        /// Highest rain rate over the last 15 minutes - clicks
        /// </summary>
        public int? rain_rate_hi_last_15_min_clicks { get; set; }
        /// <summary>
        /// Highest rain rate over the last 15 minutes - inches
        /// </summary>
        public double? rain_rate_hi_last_15_min_in { get; set; }
        /// <summary>
        /// Highest rain rate over the last 15 minutes - millimeters
        /// </summary>
        public double? rain_rate_hi_last_15_min_mm { get; set; }
        /// <summary>
        /// Total rain over the last 60 minutes - clicks
        /// </summary>
        public int? rainfall_last_60_min_clicks { get; set; }
        /// <summary>
        /// Total rain over the last 60 minutes - inches
        /// </summary>
        public double? rainfall_last_60_min_in { get; set; }
        /// <summary>
        /// Total rain over the last 60 minutes - millimeters
        /// </summary>
        public double? rainfall_last_60_min_mm { get; set; }
        /// <summary>
        /// Total rain over the last 24 hours - clicks
        /// </summary>
        public int? rainfall_last_24_hr_clicks { get; set; }
        /// <summary>
        /// Total rain over the last 24 hours - inches
        /// </summary>
        public double? rainfall_last_24_hr_in { get; set; }
        /// <summary>
        /// Total rain over the last 24 hours - millimeters
        /// </summary>
        public double? rainfall_last_24_hr_mm { get; set; }
        /// <summary>
        /// Total rain in the current storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm) - clicks
        /// </summary>
        public int? rain_storm_current_clicks { get; set; }
        /// <summary>
        /// Total rain in the current storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm) - inches
        /// </summary>
        public double? rain_storm_current_in { get; set; }
        /// <summary>
        /// Total rain in the current storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm) - millimeters
        /// </summary>
        public double? rain_storm_current_mm { get; set; }
        /// <summary>
        /// Unix timestamp of the start of the current storm (the first rain recorded after a period of 24 hours or more without
        /// rain is considered a new storm) - seconds
        /// </summary>
        public long? rain_storm_current_start_at { get; set; }
        /// <summary>
        /// Total rain in the previous storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm) - clicks
        /// </summary>
        public int? rain_storm_last_clicks { get; set; }
        /// <summary>
        /// Total rain in the previous storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm) - inches
        /// </summary>
        public double? rain_storm_last_in { get; set; }
        /// <summary>
        /// Total rain in the previous storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm)
        /// </summary>
        public double? rain_storm_last_mm { get; set; }
        /// <summary>
        /// Unix timestamp of the start of the previous storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm) - seconds
        /// </summary>
        public long? rain_storm_last_start_at { get; set; }
        /// <summary>
        /// Unix timestamp of the end of the previous storm (the first rain recorded after a period of 24 hours or more without rain is considered
        /// a new storm) - seconds
        /// </summary>
        public long? rain_storm_last_end_at { get; set; }
        /// <summary>
        /// Total rain since local midnight - clicks
        /// </summary>
        public int? rainfall_day_clicks { get; set; }
        /// <summary>
        /// Total rain since local midnight - inches
        /// </summary>
        public double? rainfall_day_in { get; set; }
        /// <summary>
        /// Total rain since local midnight - millimeters
        /// </summary>
        public double? rainfall_day_mm { get; set; }
        /// <summary>
        /// Total rain since local midnight on the first of the month - clicks
        /// </summary>
        public int? rainfall_month_clicks { get; set; }
        /// <summary>
        /// Total rain since local midnight on the first of the month - inches
        /// </summary>
        public double? rainfall_month_in { get; set; }
        /// <summary>
        /// Total rain since local midnight on the first of the month - millimeters
        /// </summary>
        public double? rainfall_month_mm { get; set; }
        /// <summary>
        /// Total rain since local midnight on the first of the month where the month is the user's selected month for the
        /// start of their rain year - clicks
        /// </summary>
        public int? rainfall_year_clicks { get; set; }
        /// <summary>
        /// Total rain since local midnight on the first of the month where the month is the user's selected month for the
        /// start of their rain year - inches
        /// </summary>
        public double? rainfall_year_in { get; set; }
        /// <summary>
        /// Total rain since local midnight on the first of the month where the month is the user's selected month for the
        /// start of their rain year - millimeters
        /// </summary>
        public double? rainfall_year_mm { get; set; }
        /// <summary>
        /// Most recent solar radiation reading - watts per square meter
        /// </summary>
        public double? solar_rad { get; set; }
        /// <summary>
        /// Daily accumulation of solar energy since local midnight - langleys
        /// </summary>
        public double? solar_energy_day { get; set; }
        /// <summary>
        /// Sum of evapotranspiration since local midnight, only calculated hourly - inches
        /// </summary>
        public double? et_day { get; set; }
        /// <summary>
        /// Sum of evapotranspiration since local midnight on the first of the month, only calculated hourly - inches
        /// </summary>
        public double? et_month { get; set; }
        /// <summary>
        /// Sum of evapotranspiration since local midnight on the first of the month where the month is the user's selected month
        /// for the start of their rain year, only calculated hourly - inches
        /// </summary>
        public double? et_year { get; set; }
        /// <summary>
        /// Most recent UV index - ultraviolet index
        /// </summary>
        public double? uv_index { get; set; }
        /// <summary>
        /// Total UV Dose since local midnight or since last user reset - minimum erythemal dose
        /// </summary>
        public double? uv_dose_day { get; set; }
        /// <summary>
        /// units - heating degree days in degrees Fahrenheit, to convert to an equivalent Celsius value use C = F x 5 / 9
        /// </summary>
        public double? hdd_day { get; set; }
        /// <summary>
        /// units - cooling degree days in degrees Fahrenheit, to convert to an equivalent Celsius value use C = F x 5 / 9
        /// </summary>
        public double? cdd_day { get; set; }
        /// <summary>
        /// Percentage of packets received versus total possible over the local day once synced unless manually reset by user,
        /// updates every packet (null when not yet synced) - percent
        /// </summary>
        public int? reception_day { get; set; }
        /// <summary>
        /// DavisTalk Received Signal Strength Indication (RSSI) of last packet received, updates every packet - decibel-milliwatts
        /// </summary>
        public int? rssi_last { get; set; }
        /// <summary>
        /// Number of data packets containing CRC errors over the local day unless manually reset by user, updates every minute
        /// </summary>
        public int? crc_errors_day { get; set; }
        /// <summary>
        /// Total number of resyncs since local midnight unless manually reset by user. The console will attempt to resynchronize
        /// with the station after 20 consecutive bad packets, updates every minute
        /// </summary>
        public int? resyncs_day { get; set; }
        /// <summary>
        /// Total number of received packets over the local day unless manually reset by user, updates every packet
        /// </summary>
        public int? packets_received_day { get; set; }
        /// <summary>
        /// Current number of packets received in a row, updates every minute
        /// </summary>
        public int? packets_received_streak { get; set; }
        /// <summary>
        /// Total number of packets that are missed for any reason over the local day unless manually reset by user, updates every minute
        /// </summary>
        public int? packets_missed_day { get; set; }
        /// <summary>
        /// Current number of missed packets in a row that are missed for any reason, can be reset by user at any time, updates every minute
        /// </summary>
        public int? packets_missed_streak { get; set; }
        /// <summary>
        /// Longest Streak of Packets Received in a row over the local day unless manually reset by user, updates every minute
        /// </summary>
        public int? packets_received_streak_hi_day { get; set; }
        /// <summary>
        /// Longest streak of consecutive packets received over the archive interval
        /// </summary>
        public int? packets_missed_streak_hi_day { get; set; }
        /// <summary>
        /// Configured Receiver State, updates every minute:0 = Synced & Tracking OK.
        /// 1 = Synced (missing less than 20 DavisTalk packets in a row).
        /// 2 = Scanning (after missing 20 DavisTalk packets in a row). State at end of interval.
        /// </summary>
        public int? rx_state { get; set; }
        /// <summary>
        /// Current radio frequency error of the radio packets received, updates every packet
        /// </summary>
        public int? freq_error_current { get; set; }
        /// <summary>
        /// Cumulative radio frequency error of the last packet received, updates every packet
        /// </summary>
        public int? freq_error_total { get; set; }
        /// <summary>
        /// Frequency Index of Last Packet Received, updates every packet
        /// </summary>
        public int? freq_index { get; set; }
        /// <summary>
        /// Unix timestamp of last DavisTalk packet that was received. Null if no packets have been received since this transmitter was
        /// configured or set
        /// </summary>
        public long? last_packet_received_timestamp { get; set; }
        /// <summary>
        /// 0 = battery OK. 1 = battery low.
        /// </summary>
        public int? trans_battery_flag { get; set; }
        /// <summary>
        /// Current transmitter battery voltage - volts
        /// </summary>
        public double? trans_battery_volt { get; set; }
        /// <summary>
        /// Current transmitter solar panel voltage - volts
        /// </summary>
        public double? solar_panel_volt { get; set; }
        /// <summary>
        /// Current transmitter super capacitor voltage - volts
        /// </summary>
        public double? supercap_volt { get; set; }
        /// <summary>
        /// Current SPARS battery voltage - volts
        /// </summary>
        public double? spars_volt { get; set; }
        /// <summary>
        /// Current SPARS RPM - RPM
        /// </summary>
        public int? spars_rpm { get; set; }

        // ********** WeatherLinkConsoleBarometerData -  Sensor type 242, data structure type 19
        /// <summary>
        /// Most recent barometer sensor reading + bar_offset; reduced to sea level using altimeter reduction method - inches of mercury
        /// </summary>
        public double? bar_sea_level { get; set; }
        /// <summary>
        /// Current 3 hour barometer trend with following definitions: Rising Rapidly: bar_trend > =0.060
        /// Rising Slowly: bar_trend >= 0.020\nSteady: bar_trend < 0.020 and bar_trend > -0.020
        /// Falling Slowly: bar_trend <= -0.020, Falling Rapidly: bar_trend <= -0.060, Not Available: bar_trend = null
        /// inches of mercury
        /// </summary>
        public double? bar_trend { get; set; }
        /// <summary>
        /// Raw barometer sensor reading + bar_offset - inches of mercury
        /// </summary>
        public double? bar_absolute { get; set; }
        /// <summary>
        /// User configured offset - inches of mercury
        /// </summary>
        public double? bar_offset { get; set; }

        // ********** WeatherLinkConsoleInternalTempHumData -  Sensor type 365, data structure type 21

        /// <summary>
        /// Most recent inside temperature reading - degrees Fahrenheit
        /// </summary>
        public double? temp_in { get; set; }
        /// <summary>
        /// Most recent inside humidity reading - percent relative humidity
        /// </summary>
        public double? hum_in { get; set; }
        /// <summary>
        /// Most recent inside dew point reading - degrees Fahrenheit
        /// </summary>
        public double? dew_point_in { get; set; }
        /// <summary>
        /// Most recent inside wet buld reading - degrees Fahrenheit
        /// </summary>
        public double? wet_bulb_in { get; set; }
        /// <summary>
        /// Most recent inside heat index reading - degrees Fahrenheit
        /// </summary>
        public double? heat_index_in { get; set; }
        /// <summary>
        /// Most recent inside wet bulb globe temperature reading - degrees Fahrenheit
        /// </summary>
        public double? wbgt_in { get; set; }

        // ********** WeatherLinkConsoleHealthData -  Sensor type 509, data structure type 27
        /// <summary>
        /// Health data structure version number
        /// </summary>
        public int? health_version { get; set; }
        /// <summary>
        /// Console application version number
        /// </summary>
        public string console_sw_version { get; set; }
        /// <summary>
        /// Radio firmware version number
        /// </summary>
        public string console_radio_version { get; set; }
        /// <summary>
        /// API level version number
        /// </summary>
        public long? console_api_level { get; set; }
        /// <summary>
        /// Battery voltage - millivolts
        /// </summary>
        public int? battery_voltage { get; set; }
        /// <summary>
        /// Battery charge - percentage
        /// </summary>
        public int? battery_percent { get; set; }
        /// <summary>
        /// Battery health condition: 1:Unknown 2:Good 3:Overheat 4:Dead 5:Overvoltage 6:Unspecified Failure 7:Cold
        /// </summary>
        public int? battery_condition { get; set; }
        /// <summary>
        /// Positive means the battery is charging while negative means the battery is discharging - milliamps
        /// </summary>
        public double? battery_current { get; set; }
        /// <summary>
        /// Battery temperature - degrees Celsius
        /// </summary>
        public double? battery_temp { get; set; }
        /// <summary>
        /// Power charger plugged in status: 0:Unplugged 1:Plugged in
        /// </summary>
        public int? charger_plugged { get; set; }
        /// <summary>
        /// Battery charge status: 1:Unknown 2:Charging 3:Discharging 4:Not charging 5:Full
        /// </summary>
        public int? battery_status { get; set; }
        /// <summary>
        /// Uptime since last reboot - seconds
        /// </summary>
        public long? os_uptime { get; set; }
        /// <summary>
        /// Uptime since last app restart - seconds
        /// </summary>
        public long? app_uptime { get; set; }
        /// <summary>
        /// Background Noise level of DavisTalk radio receiver
        /// </summary>
        public int? bgn { get; set; }
        /// <summary>
        /// IP address type: 1:Dynamic 2:Dyn DNS Override 3:Static
        /// </summary>
        public int? ip_address_type { get; set; }
        /// <summary>
        /// IP Address of the Console
        /// </summary>
        public string ip_v4_address { get; set; }
        /// <summary>
        /// IP Address of the LAN’s Router or Gateway
        /// </summary>
        public string ip_v4_gateway { get; set; }
        /// <summary>
        /// IP Address of the Subnet Mask
        /// </summary>
        public string ip_v4_netmask { get; set; }
        /// <summary>
        /// DNS used (1=Primary, 2=Secondary, 3=public) if available
        /// </summary>
        public int? dns_type_used { get; set; }
        /// <summary>
        /// Total Bytes received on network interface - 1000 bytes
        /// </summary>
        public long? rx_kilobytes { get; set; }
        /// <summary>
        /// Total Bytes sent on network interface - 1000 bytes
        /// </summary>
        public long? tx_kilobytes { get; set; }
        /// <summary>
        /// Local API queries served
        /// </summary>
        public long? local_api_queries { get; set; }
        /// <summary>
        /// WiFi Signal Strength - decibel-milliwatts
        /// </summary>
        public int? wifi_rssi { get; set; }
        /// <summary>
        /// Uptime since last WiFi network loss - seconds
        /// </summary>
        public long? link_uptime { get; set; }
        /// <summary>
        /// Uptime since last upload connection loss - seconds
        /// </summary>
        public long? connection_uptime { get; set; }
        /// <summary>
        /// Bootloader version
        /// </summary>
        public long? bootloader_version { get; set; }
        /// <summary>
        /// 0: Unset, 1: User, 2: Network, 3: GNSS
        /// </summary>
        public int? clock_source { get; set; }
        /// <summary>
        /// some sort of ID
        /// </summary>
        public int? gnss_sip_tx_id { get; set; }
        /// <summary>
        /// Available internal RAM in KB - kilobytes
        /// </summary>
        public long? free_mem { get; set; }
        /// <summary>
        /// Available space in the storage partition in KB - kilobytes
        /// </summary>
        public long? internal_free_space { get; set; }
        /// <summary>
        /// Available space in the system partition in KB - kilobytes
        /// </summary>
        public long? system_free_space { get; set; }
        /// <summary>
        /// Backend upload record queue size in KB - kilobytes
        /// </summary>
        public long? queue_kilobytes { get; set; }
        /// <summary>
        /// Onboard database size in KB - kilobytes
        /// </summary>
        public long? database_kilobytes { get; set; }
        /// <summary>
        /// Number of times the battery has used the equivalent of a full battery charge since the last counter reset
        /// </summary>
        public long? battery_cycle_count { get; set; }
        /// <summary>
        /// Console operating system version number
        /// </summary>
        public string console_os_version { get; set; }
    }

    /// <summary>
    /// Response parent structure from a call to get the stations.
    /// </summary>
    public class DavisStations
    {
        /// <summary>
        /// The array of stations associated with the sensor identified by the sensor guid.
        /// </summary>
        public Station[] stations { get; set; }
        /// <summary>
        /// Generated at epoch
        /// </summary>
        public long? generated_at { get; set; } // JSONDATE
        public override string ToString()
        {
            return generated_at.ToString();
        }
    }

    /// <summary>
    /// Response parent structure from a call to get the stations.
    /// </summary>
    public class Station
    {
        /// <summary>
        /// Integer ID for use in calls to get current conditions
        /// </summary>
        public int? station_id { get; set; }
        /// <summary>
        /// Station GUID (not to be confused with the console GUID).
        /// </summary>
        public string station_id_uuid { get; set; }
        /// <summary>
        /// Station name that appears on the console.
        /// </summary>
        public string station_name { get; set; }
        /// <summary>
        /// gateway_id for the station.
        /// </summary>
        public long? gateway_id { get; set; }
        /// <summary>
        /// gateway_id_hex for the station.
        /// </summary>
        public string gateway_id_hex { get; set; }
        /// <summary>
        /// product_number for the station.
        /// </summary>
        public string product_number { get; set; }
        /// <summary>
        /// username for the station.
        /// </summary>
        public string username { get; set; }
        /// <summary>
        /// user_email for the station.
        /// </summary>
        public string user_email { get; set; }
        /// <summary>
        /// company_name for the station.
        /// </summary>
        public string company_name { get; set; }
        /// <summary>
        /// active for the station.
        /// </summary>
        public bool? active { get; set; }
        /// <summary>
        /// The JSON contains a property "private" for the station, but that's a C# keyword and cannot
        /// be used as an object property name. So, when deserialization occurs, privatej will be null for
        /// each station (because the JSON does not contain privatej). After deserialization,
        /// DavisWeatherlinkV2ApiNet manually re-parses the JSON to set privatej correctly for each station.
        /// </summary>
        public bool? privatej { get; set; }
        /// <summary>
        /// recording_interval for the station.
        /// </summary>
        public int? recording_interval { get; set; }
        /// <summary>
        /// firmware_version for the station.
        /// </summary>
        public string firmware_version { get; set; }
        /// <summary>
        /// registered_date for the station.
        /// </summary>
        public long? registered_date { get; set; }
        /// <summary>
        /// time_zone for the station.
        /// </summary>
        public string time_zone { get; set; }
        /// <summary>
        /// city for the station.
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// region for the station.
        /// </summary>
        public string region { get; set; }
        /// <summary>
        /// country for the station.
        /// </summary>
        public string country { get; set; }
        /// <summary>
        /// latitude for the station.
        /// </summary>
        public double? latitude { get; set; }
        /// <summary>
        /// longitude for the station.
        /// </summary>
        public double? longitude { get; set; }
        /// <summary>
        /// elevation for the station.
        /// </summary>
        public double? elevation { get; set; }
    }
}