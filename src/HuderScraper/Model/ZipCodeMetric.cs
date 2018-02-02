using Scrapers.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace HuderScraper.Models
{
    public class ZipCodeMetric
    {
        public ZipCodeMetric()
        {
            UnitCounts = new List<UnitsAtCost>();
            UnitCounts.Add(new UnitsAtCost(0, 99));
            UnitCounts.Add(new UnitsAtCost(100, 149));
            UnitCounts.Add(new UnitsAtCost(150, 199));
            UnitCounts.Add(new UnitsAtCost(200, 249));
            UnitCounts.Add(new UnitsAtCost(250, 299));
            UnitCounts.Add(new UnitsAtCost(300, 349));
            UnitCounts.Add(new UnitsAtCost(350, 399));
            UnitCounts.Add(new UnitsAtCost(400, 449));
            UnitCounts.Add(new UnitsAtCost(450, 499));
            UnitCounts.Add(new UnitsAtCost(500, 549));
            UnitCounts.Add(new UnitsAtCost(550, 599));
            UnitCounts.Add(new UnitsAtCost(600, 649));
            UnitCounts.Add(new UnitsAtCost(650, 699));
            UnitCounts.Add(new UnitsAtCost(700, 749));
            UnitCounts.Add(new UnitsAtCost(750, 799));
            UnitCounts.Add(new UnitsAtCost(800, 899));
            UnitCounts.Add(new UnitsAtCost(900, 999));
            UnitCounts.Add(new UnitsAtCost(1000, 1249));
            UnitCounts.Add(new UnitsAtCost(1250, 1499));
            UnitCounts.Add(new UnitsAtCost(1500, 1999));
            UnitCounts.Add(new UnitsAtCost(2000, 2499));
            UnitCounts.Add(new UnitsAtCost(2500, 2999));
            UnitCounts.Add(new UnitsAtCost(3000, 3499));
            UnitCounts.Add(new UnitsAtCost(3500, int.MaxValue));

            ActualUnits = new List<UnitsAtCost>();

        }

        public string ZipCode { get; set; }
        public Rental UnitType { get; set; }
        public int TotalUnits { get; set; }
        public List<UnitsAtCost> UnitCounts { get; set; }

        public List<UnitsAtCost> ActualUnits { get; set; }

        public double UnitAverage {
            get
            {
                if(TotalUnits == 0) return 0.0;

                int totalCosts = 0;

                foreach(UnitsAtCost uac in UnitCounts)
                {
                    int midpoint = uac.MinPrice + (uac.MaxPrice - uac.MinPrice);
                    totalCosts += midpoint * uac.Count;
                }

                return totalCosts / TotalUnits;
            }
        }

        public string UnitAverageString
        {
            get
            {
                return String.Format("{0:C2}", this.UnitAverage);
            }
        }

        public int GetPriceRange(int minPrice, int maxPrice)
        {
            if (UnitCounts == null || UnitCounts.Count != 24)
                return 0;

            int unitCount = 0;
            foreach(UnitsAtCost uac in UnitCounts)
            {
                if (uac.MinPrice >= minPrice && uac.MaxPrice <= maxPrice)
                    unitCount += uac.Count;
            }

            return unitCount;
        }

        public static ZipCodeMetric ProcessZipChunk(List<string[]> zipChunk)
        {
            if (zipChunk.Count < 2)
                return null;

            ZipCodeMetric thisMetric = new ZipCodeMetric();

            for(int i = 0; i < zipChunk.Count; i++)
            {
                if(i == 0)
                {
                    thisMetric.ZipCode = zipChunk[i][0];

                    if (zipChunk[i][4].Contains("1 bedroom"))
                        thisMetric.UnitType = Rental.OneBed;
                    else if (zipChunk[i][4].Contains("2 bedrooms"))
                        thisMetric.UnitType = Rental.TwoBed;
                    else if (zipChunk[i][4].Contains("3 bedrooms"))
                        thisMetric.UnitType = Rental.ThreeBed;

                    thisMetric.TotalUnits = Convert.ToInt32(zipChunk[i][5]);
                }
                else if (i > 1 && zipChunk[i][4].Contains("$") && i < thisMetric.UnitCounts.Count + 1)
                {
                    thisMetric.UnitCounts[i - 2].Count = Convert.ToInt32(zipChunk[i][5]);
                    if (thisMetric.UnitCounts[i - 2].Count != 0)
                        thisMetric.ActualUnits.Add(thisMetric.UnitCounts[i - 2]);
                }
            }

            return thisMetric;
        }

    }

    public class UnitsAtCost
    {
        public UnitsAtCost(int minPrice, int maxPrice)
        {
            MinPrice = minPrice;
            MaxPrice = maxPrice;
        }

        public int MinPrice { get; private set; }
        public int MaxPrice { get; private set; }
        public int Count { get; set; }
    }
    
    public enum Rental
    {
        OneBed, 
        TwoBed, 
        ThreeBed, 
    }
}
