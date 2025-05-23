using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Thermos")]
    public class Thermo : Entity
    {
        public long RefID { get; set; }
        public string Name { get; set; }
        public string? TokpedUrl { get; set; }
        public long NewProd { get; set; }
        public long FavProd { get; set; }
        public DateTime? NewProdDate { get; set; }
        public decimal Stock { get; set; }
        public decimal Price { get; set; }
        public int? PrintedHet { get; set; }
        public decimal Weight { get; set; }
        public string? Image { get; set; }
        public long CategoriesID { get; set; }
        public long Status { get; set; }
        public long MaterialsID { get; set; }
        public long LidsID { get; set; }
        public decimal Volume { get; set; }
        public decimal Qty { get; set; }
        public long RimsID { get; set; }
        public long ColorsID { get; set; }
		public long? ShapesID { get; set; }
		public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public long PackagingsID { get; set; }
        public string? Code { get; set; }
        public decimal TotalViews { get; set; }
        public decimal TotalShared { get; set; }
        public string? Note { get; set; }
		public long? Microwable { get; set; }
		public long? LessThan60 { get; set; }
		public long? LeakProof { get; set; }
		public long? TamperEvident { get; set; }
		public long? AirTight { get; set; }
		public long? BreakResistant { get; set; }
		public long? SpillProof { get; set; }
        public string? WmsCode { get; set; }
    }
}
