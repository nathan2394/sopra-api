using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "ProductDetails2")]
    public class ProductDetail2
    {
        public string? Type { get; set; }
        public long? OriginID { get; set; }
        public long? RefID { get; set; }
        public string? Name { get; set; }
        public long? NewProd { get; set; }
        public long? FavProd { get; set; }
        public string? Image { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Price { get; set; }
        public long? ClosuresID { get; set; }
        public long? CategoriesID { get; set; }
        public string? PlasticType { get; set; }
        public string? Functions { get; set; }
        public string? Tags { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? DateIn { get; set; }
        public DateTime? DateUp { get; set; }
        public long? UserIn { get; set; }
        public int? UserUp { get; set; }
        public string? RealImage { get; set; }
        public string? WmsCode { get; set; }
        public string? StockIndicator { get; set; }
        public long? NecksID { get; set; }
        public long? ColorsID { get; set; }
        public long? ShapesID { get; set; }
        public decimal? Volume { get; set; }
        public decimal? QtyPack { get; set; }
        public decimal? TotalShared { get; set; }
        public decimal? TotalViews { get; set; }
        public DateTime? NewProdDate { get; set; }
        public decimal? Height { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public int? Diameter { get; set; }
        public int? RimsID { get; set; }
        public long? Status { get; set; }
        public decimal? Stock { get; set; }
        public string? TokpedUrl { get; set; }
        public long? LidsID { get; set; }
        public string? CategoryName { get; set; }
        public int? CountColor { get; set; }
        [NotMapped]
        public int? Percentage { get; set; }
        [NotMapped]
        public long? QtyCart { get; set; }
        public long PackagingsID { get; set; }
        [NotMapped]
        public string? PackagingName { get; set; }
        public string? Note { get; set; }
        public long? Plug { get; set; }
        public int? Ranking { get; set; }
        [NotMapped]
        public string? Code { get; set; }
        [NotMapped]
        public string? Neck { get; set; }
        [NotMapped]
        public string? Rim { get; set; }
        [NotMapped]
        public string? Color { get; set; }
        //public int Flag{ get; set; }

    }

    public class ProductKey
    {
        public string ProductKeys { get; set; }
    }
}
