using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "StockIndicators")]
    public class StockIndicator : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public decimal MinQty { get; set; }
        public decimal MaxQty { get; set; }
    }
}
