using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// Class for the items queried from Amazon.
namespace Amazon_Product_Surfer.Models
{
    public class ItemFromAmazon
    {
        public string ID { get; set; }
        public string name { get; set; }
        public int priceAmazon { get; set; }
        public int priceNew { get; set; }
        public int priceUsed { get; set; }
        public float priceAmazonF { get; set; }
        public float priceNewF { get; set; }
        public float priceUsedF { get; set; }
        public string website { get; set; }

        public ItemFromAmazon(string ID, string Name, int PriceAmazon,
                               int PriceNew, int PriceUsed, float PriceAmazonF,
                               float PriceNewF, float PriceUsedF, string Website)
        {
            this.ID = ID;
            this.name = Name;
            this.priceAmazon = PriceAmazon;
            this.priceNew = PriceNew;
            this.priceUsed = PriceUsed;
            this.website = Website;
            this.priceAmazonF = PriceAmazonF;
            this.priceNewF = PriceNewF;
            this.priceUsedF = PriceUsedF;
        }
    }
}