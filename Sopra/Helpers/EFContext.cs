using Microsoft.EntityFrameworkCore;

using Sopra.Entities;

namespace Sopra.Helpers
{
    public class EFContext : DbContext
    {
        
        public EFContext(DbContextOptions<EFContext> opts) : base(opts) { }
        public DbSet<Filter> Filters { get; set; }
        public DbSet<ProductStatus> ProductStatuses { get; set; }
        public DbSet<ProductKeyword> ProductKeywords { get; set; }
        public DbSet<ProductCategoriesKeyword> ProductCategoriesKeywords { get; set; }
        public DbSet<WishList> WishLists { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AltSize> AltSizes { get; set; }
        public DbSet<AltSizeDetail> AltSizeDetails { get; set; }
        public DbSet<Category> Categorys { get; set; }
        public DbSet<Credential> Credentials { get; set; }
        public DbSet<CredentialToken> CredentialTokens { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<BillingAddress> BillingAddresses { get; set; }
        public DbSet<DeliveryAddress> DeliveryAddresses { get; set; }
        public DbSet<CategoryDetail> CategoryDetails { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Calculator> Calculators { get; set; }
        public DbSet<Closure> Closures { get; set; }
        public DbSet<Thermo> Thermos { get; set; }
        public DbSet<Lid> Lids { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Safety> Safetys { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Rim> Rims { get; set; }
        public DbSet<StockIndicator> StockIndicators { get; set; }
        public DbSet<Shape> Shapes { get; set; }
        public DbSet<Neck> Necks { get; set; }
        public DbSet<Dealer> Dealers { get; set; }
        public DbSet<UserDealer> UserDealers { get; set; }
        public DbSet<Packaging> Packagings { get; set; }
        public DbSet<AltVolume> AltVolumes { get; set; }
        public DbSet<AltVolumeDetail> AltVolumeDetails { get; set; }
        public DbSet<AltNeck> AltNecks { get; set; }
        public DbSet<AltNeckDetail> AltNeckDetails { get; set; }
        public DbSet<AltWeight> AltWeights { get; set; }
        public DbSet<AltWeightDetail> AltWeightDetails { get; set; }
        public DbSet<Function> Functions { get; set; }
        public DbSet<FunctionDetail> FunctionDetails { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagDetail> TagDetails { get; set; }
        public DbSet<TagVideo> TagVideos { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Country> Countrys { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<Regency> Regencys { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<PromoDetail> PromoDetails { get; set; }
        public DbSet<TransactionOrderDetail> TransactionOrderDetails { get; set; }
        public DbSet<VTransactionOrderDetail> VTransactionOrderDetails { get; set; }
        public DbSet<VProductStatus> VProductStatuses { get; set; }
        public DbSet<Reseller> Resellers { get; set; }
        public DbSet<SearchKeyword> SearchKeywords { get; set; }
        public DbSet<SearchQuery> SearchQuerys { get; set; }
        public DbSet<SectionCategory> SectionCategorys { get; set; }
        public DbSet<ProductCategory> ProductCategorys { get; set; }
        public DbSet<SectionCategoryKey> SectionCategoryKeys { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Company> Companys { get; set; }
        public DbSet<Reason> Reasons { get; set; }
        public DbSet<Transport> Transports { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<Shipping> Shippings { get; set; }
        public DbSet<Gcp> Gcps { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<Deposit> Deposit { get; set; }
        public DbSet<DepositDetail> DepositDetails { get; set; }
        public DbSet<Promo> Promos { get; set; }
        public DbSet<PromoCartDetail> PromoCartDetails { get; set; }
        public DbSet<PromoOrderBottleDetail> PromoOrderBottleDetails { get; set; }
        public DbSet<PromoProduct> PromoProducts { get; set; }
        public DbSet<PromoQuantity> PromoQuantities { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ProductDetail2> ProductDetails2 { get; set; }
        public DbSet<PopularityIndicator> PopularityIndicators { get; set; }
        public DbSet<BestSeller> BestSellers { get; set; }

        public DbSet<spAfterSave> AfterSave { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VTransactionOrderDetail>().HasNoKey().ToView("VTransactionOrderDetail");
            modelBuilder.Entity<VProductStatus>().HasNoKey().ToView("VProductStatus");
            modelBuilder.Entity<ProductStatus>().HasNoKey().ToView("ProductStatus");
            modelBuilder.Entity<ProductDetail2>().HasNoKey().ToView("ProductDetails2");
            modelBuilder.Entity<VATransaction>().HasNoKey().ToView("VTransactionVA");
        }
    }
}