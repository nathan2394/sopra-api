using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "ProductDetails")]
    public class ProductDetail : Entity
    {
        public long RefID { get; set; }
        public string? Name { get; set; }
        public long? NewProd { get; set; }
        public long? FavProd { get; set; }
        public DateTime? NewProdDate { get; set; }
        public decimal? Stock { get; set; }
        public decimal? Price { get; set; }
        public decimal? Weight { get; set; }
        public string? Image { get; set; }
        public string? Color { get; set; }
        public decimal? Height { get; set; }
        public decimal? Length { get; set; }
        public decimal? Volume { get; set; }
        public string? Code { get; set; }
        public decimal? QtyPack { get; set; }
        public string? Closure { get; set; }
        public string? PlasticType { get; set; }
        public string? Type { get; set; }
        public string? Neck { get; set; }
        public decimal? Width { get; set; }
        public string? Note { get; set; }
        public string? Rim { get; set; }
        public decimal? Diameter { get; set; }
        public DateTime? ExpDate{ get; set; }
        public long? Category { get; set; }
        public string? Function { get; set; }
        public string? Tag { get; set; }
        public long? OriginID { get; set; }
    }
}
