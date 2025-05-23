using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sopra.Helpers;
using Sopra.Services.Master;
using SOPRA.Services;
using System;
using System.Configuration;
using System.Diagnostics;
namespace Sopra.Services
{
    public class IntegrationService
    {
        public static void Run(IConfigurationRoot config)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EFContext>();
            optionsBuilder.UseSqlServer($"Data Source={config["SQL:Server"]};Initial Catalog={config["SQL:Database"]};User ID={config["SQL:UserID"]};Password={config["SQL:Password"]};MultipleActiveResultSets=True;Encrypt=False");
            optionsBuilder.EnableSensitiveDataLogging();
            var context = new EFContext(optionsBuilder.Options);

            Trace.WriteLine("========== 123 ==========");
            Trace.WriteLine("Running Sync Services....");
            Trace.WriteLine($"Time: {DateTime.Now:dd MMM yyyy HH:mm:ss}");

            Trace.WriteLine("================");
            Trace.WriteLine("Try connect to DB....");

            Utility.ConnectSQL(config["SQL:Server"], config["SQL:Database"], config["SQL:UserID"], config["SQL:Password"]);
            Trace.WriteLine($"Success connect to SQL server {config["SQL:Server"]}; database : {config["SQL:Database"]}....");

            Utility.ConnectWMSSQL(config["SQLWMS:Server"], config["SQLWMS:Database"], config["SQLWMS:UserID"], config["SQLWMS:Password"]);
            Trace.WriteLine($"Success connect to SQLWMS server {config["SQLWMS:Server"]}; database : {config["SQLWMS:Database"]}....");

            Utility.ConnectMySQL(config["MYSQL:Server"], config["MYSQL:Database"], config["MYSQL:UserID"], config["MYSQL:Password"]);
            Trace.WriteLine($"Success connect to MYSQL server {config["MYSQL:Server"]}; database : {config["MYSQL:Database"]}....");

            Trace.WriteLine("Success connect to DB....");

            Trace.WriteLine("================");
            Trace.WriteLine("Setting up API URL....");
            Utility.APIURL = config["APIURL"];
            Trace.WriteLine($"${Utility.APIURL}");

            Trace.WriteLine("================");
            Trace.WriteLine("Get Last Sync Date....");
            Utility.SyncDate = Utility.getCurrentTimestamps();
            Utility.TransactionSyncDate = Utility.getCurrentTimestamps();

            var obj = Utility.FindObject("[Value]", "Configurations", "Code = 'LastSyncDates'", "", "", "", Utility.SQLDBConnection);
            if (obj != null && obj != DBNull.Value) Utility.SyncDate = Convert.ToDateTime(obj);
            Trace.WriteLine(string.Format("Last Sync: {0:yyyy-MM-dd HH:mm:ss}", Utility.SyncDate));

            var objTransaction = Utility.FindObject("[Value]", "Configurations", "Code = 'TransactionLastSyncDates'", "", "", "", Utility.SQLDBConnection);
            if (objTransaction != null && objTransaction != DBNull.Value) Utility.TransactionSyncDate = Convert.ToDateTime(objTransaction);
            Trace.WriteLine(string.Format("Last Sync Transaction: {0:yyyy-MM-dd HH:mm:ss}", Utility.TransactionSyncDate));

            Trace.WriteLine("================");

            #region Master Data
            //AltVolumeRepository.Sync(); // V
            //AltVolumeDetailRepository.Sync(); // V
            //AltNeckRepository.Sync(); // V
            //AltNeckDetailRepository.Sync(); // V
            //AltWeightRepository.Sync(); // V
            //AltWeightDetailRepository.Sync(); // V
            //FunctionRepository.Sync(); // V
            //FunctionDetailRepository.Sync(); // V
            //TagRepository.Sync(); // V
            //TagDetailRepository.Sync(); // V
            //LanguageRepository.Sync(); // V
            //CountryRepository.Sync(); // V
            //ProvinceRepository.Sync(); // V
            //RegencyRepository.Sync(); // V
            //DistrictRepository.Sync(); // V
            //TransactionOrderDetailRepository.Sync(); // V
            //CustomerRepository.Sync(); // V

            //BillingAddressRepository.Sync(); // V
            //DeliveryAddressRepository.Sync(); // V
            //CategoryRepository.Sync();
            //ColorRepository.Sync();
            //RimRepository.Sync();

            //BannerRepository.Sync();
            //StockIndicatorRepository.Sync();
            //MaterialRepository.Sync();
            //SafetyRepository.Sync();
            //ShapeRepository.Sync();
            //NeckRepository.Sync();
            //ImageRepository.Sync();
            //DiscountRepository.Sync();

            //CalculatorRepository.Sync();
            //ThermoRepository.Sync();
            //LidRepository.Sync();
            //ClosureRepository.Sync();

            //PackagingRepository.Sync(); // V
            //AltSizeRepository.Sync(); // V
            //AltSizeDetailRepository.Sync(); // V
            //SearchQueryRepository.Sync(); // V
            //SearchKeywordRepository.Sync(); // V
            //SectionCategoryRepository.Sync(); // V
            //ProductCategoryRepository.Sync(); // V
            //SectionCategoryKeyRepository.Sync(); // V
            //FavoriteRepository.Sync(); // V
            //CompanyRepository.Sync(); // V
            //ReasonRepository.Sync(); // V
            //TransportRepository.Sync(); // V
            //VoucherRepository.Sync(); // V

            //DepositRepository.Sync(); // V
            //SubscriptionRepository.Sync(); // V

            //PromoRepository.Sync(); // V
            //PromoProductRepository.Sync(); // V
            //PromoQuantityRepository.Sync(); // V
            //PromoCartDetailRepository.Sync(); // V
            //PromoOrderBottleDetailRepository.Sync(); // V

            //TagVideoRepository.Sync(); // V
            //UserDealerRepository.Sync(); // V
            //DealerRepository.Sync(); // V

            //ProductCategoriesKeywordRepository.Sync(); // V
            //ProductKeywordRepository.Sync(); // V

            //CartRepository.Sync(); // V
            //CartDetailRepository.Sync(); // V
            #endregion

            #region TransactionWMS
            // StockWMSRepository.Sync();
            // ShippingRepository.Sync();
            // ProductStatusRepository.Sync();
            #endregion

            #region TransactionEcom
            // NotificationRepository.Sync();
            // UserRepository.Sync();
            //Configuration.GetSection("APIURL").Value;

            Trace.WriteLine($"1 {Utility.APIURL}");
            OrderSyncService.Sync(context);
            Trace.WriteLine("2");
            InvoiceSyncService.Sync(context);
            Trace.WriteLine("3");
            PaymentSyncService.Sync(context);
            #endregion

            Trace.WriteLine("================");
            Trace.WriteLine("Update Last Sync Date....");
            Utility.ExecuteNonQuery(string.Format("UPDATE Configurations SET Value = FORMAT(DATEADD(minute, -2, GETDATE()), 'yyyy-MM-dd HH:mm:ss') WHERE Code = 'TransactionLastSyncDates'"));
        }
    }
}
