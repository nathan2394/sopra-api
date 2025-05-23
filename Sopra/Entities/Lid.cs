using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Lids")]
    public class Lid : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public long NewProd { get; set; }
        public long FavProd { get; set; }
        public long RimsID { get; set; }
        public long MaterialsID { get; set; }
        public long ColorsID { get; set; }
        public decimal Price { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal Qty { get; set; }
        public string? Code { get; set; }
        public string? Image { get; set; }
        public long Status { get; set; }
        public string? Note { get; set; }
        public string? WmsCode { get; set; }
        public decimal? TotalViews { get; set; }
        public decimal? TotalShared { get; set; }
    }
}
