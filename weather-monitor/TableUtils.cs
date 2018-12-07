using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using weather.monitor;

namespace Weather.Monitor
{
    public class TableUtils
    {
        public static MemoryStream GetHtmlForecastResults(List<DisplayPeriod> periods)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write("<title>I Am The bauss</title><style>table{border:1px;width:100%}td{border-style:solid}tr{border-color:red;border-style:solid}.weather-table{border-style:solid}.belowFreezing{background-color:red}.aboveFreezing{background-color:green}</style><table class=weather-table><tr><th>Date and Time<th>Forecast<th>Temperature<th>Wind</th></tr>");
            foreach (var period in periods)
            {
                sw.Write($@"<tr>
                            <td>{period.timeSpan}</td>
                            <td><img src=""{period.icon}"" />{period.shortForecast}</td> 
                            <td class=""{(period.belowFreezing ? "belowFreezing" : "aboveFreezing")}"">{period.temperature} {period.temperatureUnit}</td>
                            <td>{period.windSpeed} {period.windDirection}</td>
                            </tr>");
            }
            sw.Write("</table></body></html>");
            sw.Flush();
            //sw.Close();

            return ms;
        }
    }
}
