using System;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class CountOverview
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public long Amount { get; set; }
        public long Count { get; set; }
        public string Color { get; set; }
    }

    public class PendingOrder
    {
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public long Amount { get; set; }
        public string HandleBy { get; set; }
    }

    public class OngoingInvoice
    {
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public long Amount { get; set; }
        public string HandleBy { get; set; }
    }
}