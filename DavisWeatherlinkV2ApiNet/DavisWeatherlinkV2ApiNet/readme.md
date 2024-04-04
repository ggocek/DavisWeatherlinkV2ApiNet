# DavisWeatherlinkV2ApiNet - README
(c) Gary Gocek, 2024. See GitHub for licensing:
https://github.com/ggocek/DavisWeatherlinkV2ApiNet

My goal was to be able to display current weather conditions from my
backyard weather station, a Davis Vantage Vue. Davis provides the
Davis WeatherLink API V2. This returns hunks of JSON.

SEE BELOW FOR USAGE SUGGESTIONS.

DavisWeatherlinkV2ApiNet (this package) provides classes representing
the data in the hunks of JSON so that Newtonsoft.Json can deserialize
the data into C# object instances. Then, the caller (such as a web
site), can use the properties of the instances to display selected
values.

The main difficulty for API implementers is that there are hundreds of
properties representing multiple sensors on the weather station. There
are a number of Davis projects on GitHub, but I could not find one that
specifically met my needs. Maybe few customers at the low of of the
Davis product line (Vantage Vue) don't bother to code against the API.

The second difficulty is that there does not seem to be documentation
from Davis that clearly identifies the sensor types and data types
returned from the console for whatever products an implementer happens
to be coding for. DavisWeatherlinkV2ApiNet supports using the APU for
Vantage Vue model 6357 with console model 6313. In other words, I
tracked down the full API specs, retrieved a hunk of data, and spent a
few days trudging through the curly brackets and matching the values to
the properties promised by the documentation. The only discrepancy were
the undocumented timestamp, some kind of ID and the timzone offset of
the console.

Remember that your software will send web requests to weatherlink.com,
which will get data from the console (not directly from the station).
The API can only get what the console has retrieved from the connected
stations, so if you stations are not connected properly,
DavisWeatherlinkV2ApiNet will not be useful..

If you feel you want condition updates more often than you're getting,
you will probably need to purchase premium access to Weatherlink,
instead of the basic acccess I used while developing
DavisWeatherlinkV2ApiNet. Although the console shows conditions in real
time, API calls can get new data every 15 minutes.

## Installation
There is no installer, and no access from NuGet at this time.
Use your development IDE (Visual Studio) to create a reference. Then
your code will have access to:
DavisWeatherlinkV2ApiNet.DavisVantageVue
DavisWeatherlinkV2ApiNet.DavisDisplay
DavisWeatherlinkV2ApiNet.HtmlDisplayType
DavisWeatherlinkV2ApiNet.CurrentConditions
DavisWeatherlinkV2ApiNet.Sensor
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData
DavisWeatherlinkV2ApiNet.DavisStations
DavisWeatherlinkV2ApiNet.Station

Use NuGet to install Newtonsoft.JSON. As of this writiung, v13.0.3.

## Usage suggestions
First get the station integer ID:

DavisWeatherlinkV2ApiNet.DavisVantageVue dvv = new DavisWeatherlinkV2ApiNet.DavisVantageVue()
{
 WeatherlinkApiV2Key = "Api V2 Key",
 WeatherlinkApiV2Secret = "Api V2 Secret",
 WeatherlinkStationIdGuid = "station guid",
 WeatherlinkStationIdInt = "0", /* not needed for getting the station metadata */
 WeatherlinkAcceptLanguage = "en-US",
 WeatherlinkReferer = "something",
 WeatherlinkUserAgent = "whatever"
};
ds = dvv.LoadStations;
foreach (DavisWeatherlinkV2ApiNet.Station myStation in ds.stations)
{
if (myStation.station_name == "desired Station Name")
{
int i = (myStation.station_id.HasValue ? myStation.station_id.Value : -1);
...

Use the station integer ID to get the conditions for a station:

DavisWeatherlinkV2ApiNet.DavisDisplay dd = null;
string weatherlinkApiV2Key = "Api V2 Key",
string weatherlinkApiV2Secret = "Api V2 Secret",
string weatherlinkStationIdGuid = "station guid",
string conditionsAtStation = string.Empty;
DavisWeatherlinkV2ApiNet.DavisVantageVue dvv = new DavisWeatherlinkV2ApiNet.DavisVantageVue()
{
 WeatherlinkApiV2Key = weatherlinkApiV2Key,
 WeatherlinkApiV2Secret = weatherlinkApiV2Secret,
 WeatherlinkStationIdGuid = weatherlinkStationIdGuid,
 WeatherlinkStationIdInt = THE INT ID FROM THE CALL ABOVE,
 WeatherlinkAcceptLanguage = "en-US",
 WeatherlinkReferer = "something",
 WeatherlinkUserAgent = "whatever"
};
DavisWeatherlinkV2ApiNet.CurrentConditions cc = dvv.LoadCurrentConditions;
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleISSData = null;
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleBarometerData = null;
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleInternalTempHumData = null;
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleHealthData = null;
foreach (DavisWeatherlinkV2ApiNet.Sensor mySensor in cc.sensors)
{
 if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.VantageVueWirelessSensorType) &&
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ISSCUrrentConditionsDST))
 {
  consoleISSData = mySensor.data[0];
 }
 else if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.BarometerSensorType) &&
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.BarometerCurrentConditionsDST))
 {
  consoleBarometerData = mySensor.data[0];
 }
 else if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ConsoleInsideTempHumSensorType) &&
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.InternalTempHumCurrentConditionsDST))
 {
  consoleInternalTempHumData = mySensor.data[0];
 }
 else if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ConsoleHealthSensorType) &&
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ConsoleHealthDST))
 {
  consoleHealthData = mySensor.data[0];
 }
}
dd = new DavisWeatherlinkV2ApiNet.DavisDisplay()
{
ConsoleISSData = consoleISSData,
ConsoleBarometerData = consoleBarometerData,
ConsoleInternalTempHumData = consoleInternalTempHumData,
ConsoleHealthData = consoleHealthData
};

Also see the method,
HtmlMarkup(string textColor, string bgColor, string extraConditions, int divwidth, int divheight, HtmlDisplayType hdt)

## Live Examples
https://weather.gocek.org/

https://www.gocek.org/weather/currentweather.aspx
The data dump markup does not include the station metadata since some
of the values contain personally identifiable information.
