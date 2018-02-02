using HuderScraper.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuderScraper.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ZipSearchResult
    {
        [JsonProperty]
        public string ZipCode { get; set; }
        [JsonProperty]
        public ZipCodeMetric OneBedStats { get; set; }
        [JsonProperty]
        public ZipCodeMetric TwoBedStats { get; set; }
        [JsonProperty]
        public ZipCodeMetric ThreeBedStats { get; set; }
        [JsonProperty]
        public string ZipDescription { get; set;}
        public string NewZipDescription { get; set; }

        public bool IsItemInView { get; set; }

        public int TotalUnits { get
            {
                if (OneBedStats == null || TwoBedStats == null || ThreeBedStats == null)
                    return 0;

                return OneBedStats.TotalUnits + TwoBedStats.TotalUnits + ThreeBedStats.TotalUnits;
            }
        }

        public double AveragePrice
        {
            get
            {
                if (OneBedStats == null || TwoBedStats == null || ThreeBedStats == null)
                    return 0;

                var totalAvg = ((OneBedStats.UnitAverage * OneBedStats.TotalUnits) +
                                 (TwoBedStats.UnitAverage * TwoBedStats.TotalUnits) +
                                 (ThreeBedStats.UnitAverage * ThreeBedStats.TotalUnits)) /
                                 this.TotalUnits;
                return totalAvg;
            }
        }
    }
}
