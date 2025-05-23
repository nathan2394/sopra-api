using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class OrderGroup
    {
        public long? CartsID { get; set; }
        public int? Total { get; set; }
        public List<Order>? Orders { get; set; }
    }
}
