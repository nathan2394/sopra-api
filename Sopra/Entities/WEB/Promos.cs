using System;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class Promos
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<Products> Products { get; set; }
        public List<Quantities> Quantities { get; set; }
    }
    public class Products
    {
        public long ProductID { get; set; }
        public List<Details> Details { get; set; }
    }

    public class Details
    {
        public long? Accs1ID { get; set; }
        public long? Accs2ID { get; set; }
        public long? Price1 { get; set; }
        public long? Price2 { get; set; }
        public long? Price3 { get; set; }
    }

    public class Quantities
    {
        public long? Qty1 { get; set; }
        public long? Qty2 { get; set; }
        public long? Qty3 { get; set; }
    }
}