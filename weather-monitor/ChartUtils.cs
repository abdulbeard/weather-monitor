using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using weather.monitor;

namespace Weather.Monitor
{
    public class ChartUtils
    {
        private static int value = -1;
        public static string GetGoogleChartUri(List<DisplayPeriod> periods, int numFirstPeriodsToUse = 48, TraceWriter log = null)
        {
            var linechart = new GoogleLineChart();

            periods = periods.Take(numFirstPeriodsToUse).ToList();

            var maxTemp = periods.Max(x => x.temperature);
            var minTemp = periods.Min(x => x.temperature);
            var xAxisData = new List<string>();
            var xAxisPeriodStartTimes = new List<DateTime>();
            foreach(var period in periods)
            {
                if(GetValue() == 0)
                {
                    xAxisData.Add(period.startTime.ToString("MM/dd H:mm"));
                    xAxisPeriodStartTimes.Add(period.startTime);
                }
            }
            var lastPeriod = periods.Last();
            xAxisData.Add(lastPeriod.endTime.ToString("MM/dd H:mm"));
            xAxisPeriodStartTimes.Add(lastPeriod.endTime);
            int yAxisStepSize = 5;

            log?.Info($"xaxisdata: {JsonConvert.SerializeObject(xAxisData)}");
            log?.Info($"xaxisperiodstarttimes: {JsonConvert.SerializeObject(xAxisPeriodStartTimes)}");

            linechart.SetXAxisData(xAxisData);
            linechart.SetYAxisData(periods.Select(x => x.temperature).ToList(), 32);
            linechart.SetChartColor(new List<string> { "FFC6A5", "000000" });
            linechart.SetChartGridLines(100/numFirstPeriodsToUse, yAxisStepSize, 5, 2, 0, 0);
            linechart.SetChartSize(1000, 300);
            linechart.SetLineFills();
            linechart.SetChartLegends(new List<string> { "FORECAST_TEMP", "FREEZING_POINT" });
            linechart.SetChartTitle("Weather Forecast");
            linechart.SetHorizontalStripes("BBBBBB", "EEEEEE", yAxisStepSize / 100.0, yAxisStepSize / 100.0);
                //        chf=c,ls,90,BBBBBB,0.05,EEEEEE,0.05

            //linechart.SetAxisRange(1, minTemp - 5, maxTemp + 5, 5);
            linechart.SetChartLineStyles(2.0, 0.0, 0.0);
            linechart.cht = "lc";
            linechart.chxt = "x,y";
            linechart.chdlp = "b";


            return linechart.GetUrl();
        }

        public static string GetChartImageData(string uri)
        {
            return GetBase64ImageDataFromUri(uri);
        }

        public static string GetChartImageData(List<DisplayPeriod> periods, int numFirstPeriodsToUse = 48)
        {
            var uri = GetGoogleChartUri(periods, numFirstPeriodsToUse);
            return GetBase64ImageDataFromUri(uri);
        }

        private static string GetBase64ImageDataFromUri(string uri)
        {
            var rawImageResponse = new HttpClient().GetAsync(uri).Result;
            var base64String = Convert.ToBase64String(rawImageResponse.Content.ReadAsByteArrayAsync().Result);
            var imageString = $"data:{rawImageResponse.Content.Headers.ContentType};base64,{base64String}";
            return imageString;
        }

        private static int GetValue()
        {
            if (value == 5) { value = 0; }
            else { value++; }
            return value;
        }
    }

    public class GoogleLineChart
    {
        public string cht { get; set; }
        public string chd { get; set; }
        public string chls { get; set; }
        public string chg { get; set; }
        public string chxt { get; set; }
        public string chxl { get; set; }
        public string chs { get; set; }
        public string chm { get; set; }
        public string chco { get; set; }
        public string chdl { get; set; }
        public string chxr { get; set; }
        public string chdlp { get; set; }
        public string chtt { get; set; }
        public string chf { get; set; }
        public string GetUrl()
        {
            return $"https://chart.googleapis.com/chart?cht={cht}" +
                $"&chd={chd}&chls={chls}&chg={chg}&chxt={chxt}&chxl={chxl}&chs={chs}&chm={chm}&chco={chco}" +
                $"&chdl={chdl}{(string.IsNullOrWhiteSpace(chxr) ? "" : $"&chxr={chxr}")}&chdlp={chdlp}" +
                $"&chtt={chtt}&chf={chf}";
        }

        public void SetChartTitle(string title)
        {
            chtt = title.Replace(" ", "+");
        }

        public void SetHorizontalStripes(string color1, string color2, double color1WidthPercent, double color2WidthPercent)
        {
            chf = $"c,ls,90,{color1},{color1WidthPercent},{color2},{color2WidthPercent}";
        }

        public void SetXAxisData(List<string> values)
        {
            chxl = $"0:|{string.Join("|", values)}";
        }
        public void SetYAxisData(List<int> values, int baseLine)
        {
            chd = $"t:{string.Join(",", values)}|{baseLine},{baseLine}";
        }

        public void SetChartLineStyles(double lineThickness, double dashLength = 1.0, double spaceLength = 0.0)
        {
            chls = $"{lineThickness},{dashLength},{spaceLength}";
        }

        public void SetChartSize(int width, int height)
        {
            chs = $"{width}x{height}";
        }

        public void SetChartColor(List<string> colors)
        {
            chco = string.Join(",", colors);
        }

        public void SetChartGridLines(double xAxisStepSize, double yAxisStepSize, int dashLength = 4, int spaceLength = 1, int xOffset = 0, int yOffset = 0)
        {
            chg = $"{xAxisStepSize},{yAxisStepSize},{dashLength},{spaceLength},{xOffset},{yOffset}";
        }

        public void SetLineFills(bool fillFromBottom = true, string color = "ff9900", string opacity = "cc", int startLineIndex = 0, int endLineIndex = 0)
        {
            chm = $"{(fillFromBottom ? "B" : "b")},{color}{opacity},{startLineIndex},{endLineIndex},0";
        }

        public void SetChartLegends(List<string> legends)
        {
            chdl = string.Join("|", legends);
        }

        public void SetAxisRange(int axisIndex, int startValue, int endValue, int lineGraduation = 5)
        {
            chxr = $"{axisIndex},{startValue},{endValue},{lineGraduation}";
        }
    }
}
