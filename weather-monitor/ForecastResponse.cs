using System;
using System.Collections.Generic;
using System.Text;

namespace weather.monitor
{
    public class ForecastResponse
    {
        public object[] context { get; set; }
        public string type { get; set; }
        public Geometry geometry { get; set; }
        public Properties properties { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; }
        public Geometry1[] geometries { get; set; }
    }

    public class Geometry1
    {
        public string type { get; set; }
        public object[] coordinates { get; set; }
    }

    public class Properties
    {
        public DateTime updated { get; set; }
        public string units { get; set; }
        public string forecastGenerator { get; set; }
        public DateTime generatedAt { get; set; }
        public DateTime updateTime { get; set; }
        public string validTimes { get; set; }
        public Elevation elevation { get; set; }
        public Period[] periods { get; set; }
    }

    public class Elevation
    {
        public float value { get; set; }
        public string unitCode { get; set; }
    }

    public class Period
    {
        public int number { get; set; }
        public string name { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public bool isDaytime { get; set; }
        public int temperature { get; set; }
        public string temperatureUnit { get; set; }
        public object temperatureTrend { get; set; }
        public string windSpeed { get; set; }
        public string windDirection { get; set; }
        public string icon { get; set; }
        public string shortForecast { get; set; }
        public string detailedForecast { get; set; }
    }

    public class DisplayPeriod
    {
        public DisplayPeriod() { }
        public DisplayPeriod(Period period)
        {
            number = period.number;
            name = period.name;
            startTime = ParseTime(period.startTime);
            endTime = ParseTime(period.endTime);
            timeSpan = $"{startTime.ToString("MM/dd H:m")}-{endTime.ToString("H:m")}";
            isDaytime = period.isDaytime;
            temperature = period.temperature;
            belowFreezing = string.Equals(period.temperatureUnit, "f", StringComparison.OrdinalIgnoreCase) ? period.temperature < 32 : period.temperature < 0;
            temperatureUnit = period.temperatureUnit;
            temperatureTrend = period.temperatureTrend;
            windSpeed = period.windSpeed;
            windDirection = period.windDirection;
            icon = period.icon;
            shortForecast = period.shortForecast;
            detailedForecast = period.detailedForecast;
        }

        public DateTime ParseTime(string dateTime)
        {
            var timezone = dateTime.Substring(dateTime.Length - 6);
            var minus = timezone[0] == '-'? true : false;
            var hourMinSplit = timezone.Replace(minus ? "-" : "+", "").Split(':'); ;
            var hours = (minus ? -1 : 1 ) * int.Parse(hourMinSplit[0]);
            var mins = (minus ? -1 : 1) * int.Parse(hourMinSplit[1]);
            var result = DateTime.Parse(dateTime).ToUniversalTime();

            result = result.AddHours(hours);
            result = result.AddMinutes(mins);
            return result;
        }

        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public int number { get; set; }
        public string name { get; set; }
        public string timeSpan { get; set; }
        public bool belowFreezing { get; set; }
        public bool isDaytime { get; set; }
        public int temperature { get; set; }
        public string temperatureUnit { get; set; }
        public object temperatureTrend { get; set; }
        public string windSpeed { get; set; }
        public string windDirection { get; set; }
        public string icon { get; set; }
        public string shortForecast { get; set; }
        public string detailedForecast { get; set; }
    }
}
