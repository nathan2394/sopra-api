using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class ProductGroup
    {
        public string? Name { get; set; }
        public int? Total { get; set; }
        public List<Product>? Products { get; set; }
        
    }
}
