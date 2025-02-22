using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TidyTapBit.Core.Models
{
    public class TradeOrder
    {
        public string Symbol { get; set; }
        public string Side { get; set; } // "Buy" or "Sell"
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string OrderType { get; set; } // "Limit", "Market"
        public string TimeInForce { get; set; } // "GTC", "IOC"
    }
}
