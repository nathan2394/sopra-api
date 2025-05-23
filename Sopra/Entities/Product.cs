using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Sopra.Entities
{
    //[Table(name: "Banners")]
    public class Product : Entity
    {
        public long? RefID { get; set; }
        public string? Name { get; set; }
        public long? NewProd { get; set; }
        public long? FavProd { get; set; }
        public DateTime? NewProdDate { get; set; }
        public string? Image { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Price { get; set; }
        public long? CategoriesID { get; set; }
        public string? CategoryName { get; set; }
        public string? PlasticType { get; set; }
        public string? TokpedUrl { get; set; }
        public long? MaterialsID { get; set; }
        public decimal? Stock { get; set; }
        public long? ClosuresID { get; set; }
        public long? LidsID { get; set; }
        public string? RealImage { get; set; }
        public string? StockIndicator { get; set; }
        public string? Type { get; set; }
        public decimal? TotalViews { get; set; }
        public decimal? TotalShared { get; set; }
        public string? Color { get; set; }
        public long? ColorsID { get; set; }
		public long? ShapesID { get; set; }
		public long? NecksID { get; set; }
		public decimal? Volume { get; set; }
        public decimal? Height { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Diameter { get; set; }
        public string? Code { get; set; }
        public decimal? QtyPack { get; set; }
        public int? CountColor { get; set; }
        public string? Neck { get; set; }
        public string? Packaging { get; set; }
        public string? Rim { get; set; }
        public long? RimsID { get; set; }
        public string? Note { get; set; }
        public decimal? TotalQty { get; set; }
        public DateTime? Orderdate { get; set; }
        public string? Mobile { get; set; }
        public string? Address { get; set; }
        public string? ProvinceName { get; set; }
        public string? RegencyName { get; set; }
        public string? DistrictName { get; set; }
        public long? Plug { get; set; }
        [NotMapped]
        public Sopra.Entities.Packaging? Packagings { get; set; }

        [NotMapped]
        public List<Sopra.Entities.Image>? Images { get; set; }
        
        [NotMapped]
        public List<Sopra.Entities.Function>? Functions { get; set; }

        [NotMapped]
        public List<Tag>? Tags { get; set; }

        [NotMapped]
        public List<TagVideo>? TagVideos { get; set; }

        [NotMapped]
        public long? Wishlist { get; set; }
        [NotMapped]
        public decimal? LeadTime { get; set; }
        public int? PrintedHet { get; set; }
        public long? QtyCart { get; set; }
        public long? PackagingsID { get; set; }
    }
}
