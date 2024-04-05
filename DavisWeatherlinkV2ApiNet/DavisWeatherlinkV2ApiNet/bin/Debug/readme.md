# DavisWeatherlinkV2ApiNet - README
.NET classes for coding against the Davis WeatherLink API V2, for the Vantage Vue station and console
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

## Traditional .NET references (this project is not uploaded to nuget.org):
There is no installer, and no access from NuGet at this time.
Use your development IDE (Visual Studio) to create a reference. The
.DLL, .config and .pdb are in the bin/Debug and bin/Release folders. Then
your code will have access to:<br />
DavisWeatherlinkV2ApiNet.DavisVantageVue<br />
DavisWeatherlinkV2ApiNet.DavisDisplay<br />
DavisWeatherlinkV2ApiNet.HtmlDisplayType<br />
DavisWeatherlinkV2ApiNet.CurrentConditions<br />
DavisWeatherlinkV2ApiNet.Sensor<br />
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData<br />
DavisWeatherlinkV2ApiNet.DavisStations<br />
DavisWeatherlinkV2ApiNet.Station<br />

## Local .nupkg packaging (this project is not uploaded to nuget.org):
Find DavisWeatherlinkV2ApiNet.2024.4.4.nupkg in the bin/Debug and
bin/Release folders (or whatever the current version is, in case I
forget to update this readme).

If you build this package yourself, build the Debug and Release
configurations as usual. The with Visual Studio:<br />
Tools -> Command Line -> Developer Command Prompt<br />
msbuild -t:pack /p:Configuration=Release /p:Platform="Any CPU"<br />
msbuild -t:pack /p:Configuration=Debug /p:Platform="Any CPU"<br />
These msbuild commands generate the .nupkg file. This project was
originally a .NET Framework project with a packages.config file,
then that was migrated to a PackageReference style project. This does
not give a Packages command in the project properties, but it allows a
NuGet package to be created.

In your client  project (that is to utilize DavisWeatherlinkV2ApiNet),
add a folder called "nugetlocal" next to your .sln file. The folder
name can be anything, but my example below assumes nugetlocal). Copy
the desired .nupkg file to this folder (from either Debug or Release).<br />
Create a file called NuGet.config next to your solution file with the
following contents:<br />
&lt;?xml version="1.0" encoding="utf-8"?&gt;<br />
  &lt;configuration&gt;<br />
    &lt;packageSources&gt;<br />
      &lt;add key="nugetlocal" value="./nugetlocal" /&gt;<br />
    &lt;/packageSources&gt;<br />
    &lt;activePackageSource&gt;<br />
      &lt;!-- this tells that all of them are active --&gt;<br />
      &lt;add key="All" value="(Aggregate source)" /&gt;<br />
    &lt;/activePackageSource&gt;<br />
 &lt;/configuration&gt;<br />

When you restart your client solution, packages should appear in
the NuGet browser, or be installable using Install-Package.

## Newtonsoft.JSON
Use NuGet to install Newtonsoft.JSON. As of this writiung, v13.0.3.

## Usage suggestions
First get the station integer ID:

DavisWeatherlinkV2ApiNet.DavisVantageVue dvv = new DavisWeatherlinkV2ApiNet.DavisVantageVue()<br />
{<br />
 WeatherlinkApiV2Key = "Api V2 Key",<br />
 WeatherlinkApiV2Secret = "Api V2 Secret",<br />
 WeatherlinkStationIdGuid = "station guid",<br />
 WeatherlinkStationIdInt = "0", /* not needed for getting the station metadata */<br />
 WeatherlinkAcceptLanguage = "en-US",<br />
 WeatherlinkReferer = "something",<br />
 WeatherlinkUserAgent = "whatever"<br />
};<br />
ds = dvv.LoadStations;<br />
foreach (DavisWeatherlinkV2ApiNet.Station myStation in ds.stations)<br />
{<br />
if (myStation.station_name == "desired Station Name")<br />
{<br />
int i = (myStation.station_id.HasValue ? myStation.station_id.Value : -1);<br />
...<br />

Use the station integer ID to get the conditions for a station:

DavisWeatherlinkV2ApiNet.DavisDisplay dd = null;<br />
string weatherlinkApiV2Key = "Api V2 Key",<br />
string weatherlinkApiV2Secret = "Api V2 Secret",<br />
string weatherlinkStationIdGuid = "station guid",<br />
string conditionsAtStation = string.Empty;<br />
DavisWeatherlinkV2ApiNet.DavisVantageVue dvv = new DavisWeatherlinkV2ApiNet.DavisVantageVue()<br />
{<br />
 WeatherlinkApiV2Key = weatherlinkApiV2Key,<br />
 WeatherlinkApiV2Secret = weatherlinkApiV2Secret,<br />
 WeatherlinkStationIdGuid = weatherlinkStationIdGuid,<br />
 WeatherlinkStationIdInt = THE INT ID FROM THE CALL ABOVE,<br />
 WeatherlinkAcceptLanguage = "en-US",<br />
 WeatherlinkReferer = "something",<br />
 WeatherlinkUserAgent = "whatever"<br />
};<br />
DavisWeatherlinkV2ApiNet.CurrentConditions cc = dvv.LoadCurrentConditions;<br />
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleISSData = null;<br />
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleBarometerData = null;<br />
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleInternalTempHumData = null;<br />
DavisWeatherlinkV2ApiNet.WeatherLinkConsoleISSData consoleHealthData = null;<br />
foreach (DavisWeatherlinkV2ApiNet.Sensor mySensor in cc.sensors)<br />
{<br />
 if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.VantageVueWirelessSensorType) &&<br />
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ISSCUrrentConditionsDST))<br />
 {<br />
  consoleISSData = mySensor.data[0];<br />
 }<br />
 else if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.BarometerSensorType) &&<br />
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.BarometerCurrentConditionsDST))<br />
 {<br />
  consoleBarometerData = mySensor.data[0];<br />
 }<br />
 else if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ConsoleInsideTempHumSensorType) &&<br />
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.InternalTempHumCurrentConditionsDST))<br />
 {<br />
  consoleInternalTempHumData = mySensor.data[0];<br />
 }<br />
 else if ((mySensor.sensor_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ConsoleHealthSensorType) &&<br />
 (mySensor.data_structure_type == DavisWeatherlinkV2ApiNet.DavisVantageVue.ConsoleHealthDST))<br />
 {<br />
  consoleHealthData = mySensor.data[0];<br />
 }<br />
}<br />
dd = new DavisWeatherlinkV2ApiNet.DavisDisplay()<br />
{<br />
ConsoleISSData = consoleISSData,<br />
ConsoleBarometerData = consoleBarometerData,<br />
ConsoleInternalTempHumData = consoleInternalTempHumData,<br />
ConsoleHealthData = consoleHealthData<br />
};<br />

Also see the method,
HtmlMarkup(string textColor, string bgColor, string extraConditions, int divwidth, int divheight, HtmlDisplayType hdt)

## Live Examples
https://weather.gocek.org/

https://www.gocek.org/weather/currentweather.aspx<br />
The data dump markup does excludes the station metadata since some
of the values contain personally identifiable information.
