using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Closures")]
    public class Closure : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Diameter { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public long NecksID { get; set; }
        public long Status { get; set; }
        public long ColorsID { get; set; }
        public decimal QtyPack { get; set; }
        public string? Code { get; set; }
        public decimal Stock { get; set; }
        public string? Image { get; set; }
        public int? ClosureType { get; set; }
        public int? Ranking { get; set; }
        public string? WmsCode { get; set; }
        public decimal? TotalViews { get; set; }
        public decimal? TotalShared { get; set; }
    }
}
