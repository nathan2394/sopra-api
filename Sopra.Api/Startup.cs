using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using System;
using System.Diagnostics;

using Sopra.Helpers;
using Sopra.Services;
using Sopra.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Sopra.Services.Api;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Sopra.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Trace.Listeners.Add(new MyTraceListener());
            Trace.WriteLine("Starting API");

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //DB Initializer
            var connectionString = Configuration.GetSection("AppSettings");
            //Utility.ConnectSQL(Configuration["SQL:Server"], Configuration["SQL:Database"], Configuration["SQL:UserID"], Configuration["SQL:Password"]);

            services.AddDbContextPool<EFContext>(opt => opt.UseSqlServer(connectionString["ConnectionString"]));

            //context accesscor
            services.AddHttpContextAccessor();

            //add memory Caching
            services.AddMemoryCache();

            //GCP Config
            // string jsonAuthFilePath = Configuration["GCPStorageAuthFile"];
            // GoogleCredential credential = GoogleCredential.FromFile(jsonAuthFilePath);

            //Google Cloud API Config
            //string jsonAuthFilePathGoogleCloudAPI = Configuration["GoogleApplicationCredential"];
            //GoogleCredential credentialGoogleCloudAPI = GoogleCredential.FromFile(jsonAuthFilePathGoogleCloudAPI);

            // Explicitly specify the credentials when creating StorageClient
            // var storageClient = StorageClient.Create(credential);
            // services.AddSingleton<StorageClient>(storageClient);

            //Authhentication / Authorization
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var jwtKey = Configuration.GetSection("AppSettings:Secret").Value;
            if (jwtKey != null)
            {
                var keyx = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));
                services
                    .AddAuthentication(x =>
                    {
                        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.SaveToken = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = keyx,
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                            // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                            ClockSkew = TimeSpan.Zero
                        };
                    });
            }

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowAnyOrigin();
                });
            });

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SOPRA API",
                    Version = "v1",
                    Description = "SOPRA API Documentation",
                    Contact = new OpenApiContact
                    {
                        Name = "Andi Prasetyo Gunawan",
                        Email = string.Empty,
                        Url = new Uri("https://mixtra.co.id/"),
                    },
                });

                // Enable support for multipart/form-data file uploads
                c.OperationFilter<FileUploadOperationFilter>();

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });

            });

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IServiceAsync<User>, UserService>();
            services.AddScoped<IServiceAsync<Category>, CategoryService>();
            services.AddScoped<IServiceAsync<CategoryDetail>, CategoryDetailService>();
            services.AddScoped<IServiceAsync<Banner>, BannerService>();
            services.AddScoped<IServiceAsync<Calculator>, CalculatorService>();
            services.AddScoped<IServiceAsync<Closure>, ClosureService>();
            services.AddScoped<IServiceAsync<Thermo>, ThermoService>();
            services.AddScoped<IServiceAsync<Lid>, LidService>();
            services.AddScoped<IServiceAsync<Color>, ColorService>();
            services.AddScoped<IServiceAsync<Safety>, SafetyService>();
            services.AddScoped<IServiceAsync<Entities.Image>, ImageService>();
            services.AddScoped<IServiceAsync<Material>, MaterialService>();
            services.AddScoped<IServiceAsync<Rim>, RimService>();
            services.AddScoped<IServiceAsync<StockIndicator>, StockIndicatorService>();
            services.AddScoped<IServiceAsync<Shape>, ShapeService>();
            services.AddScoped<IServiceAsync<Neck>, NeckService>();
            services.AddScoped<IServiceAsync<Packaging>, PackagingService>();
            services.AddScoped<IServiceAsync<AltVolume>, AltVolumeService>();
            services.AddScoped<IServiceAsync<AltVolumeDetail>, AltVolumeDetailService>();
            services.AddScoped<IServiceAsync<AltNeck>, AltNeckService>();
            services.AddScoped<IServiceAsync<AltNeckDetail>, AltNeckDetailService>();
            services.AddScoped<IServiceAsync<AltWeight>, AltWeightService>();
            services.AddScoped<IServiceAsync<AltWeightDetail>, AltWeightDetailService>();
            services.AddScoped<IServiceAsync<Function>, FunctionService>();
            services.AddScoped<IServiceAsync<FunctionDetail>, FunctionDetailService>();
            services.AddScoped<IServiceAsync<Tag>, TagService>();
            services.AddScoped<IServiceAsync<TagDetail>, TagDetailService>();
            services.AddScoped<IServiceAsync<Language>, LanguageService>();
            services.AddScoped<IServiceAsync<Country>, CountryService>();
            services.AddScoped<IServiceAsync<Province>, ProvinceService>();
            services.AddScoped<IServiceAsync<Regency>, RegencyService>();
            services.AddScoped<IServiceAsync<District>, DistrictService>();
            services.AddScoped<IServiceProdAsync<Entities.Product>, ProductService>();
            services.AddScoped<IServiceAsync<TransactionOrderDetail>, TransactionOrderDetailService>();
            services.AddScoped<IServiceAsync<Customer>, CustomerService>();
            services.AddScoped<IServiceResellerAsync<Reseller>, ResellerService>();
            services.AddScoped<IServiceAsync<SearchKeyword>, SearchKeywordService>();
            services.AddScoped<IServiceAsync<SearchQuery>, SearchQueryService>();
            services.AddScoped<SectionCategoryInterface, SectionCategoryService>();
            services.AddScoped<IServiceAsync<ProductCategory>, ProductCategoryService>();
            services.AddScoped<IServiceAsync<SectionCategoryKey>, SectionCategoryKeyService>();
            services.AddScoped<IServiceAsync<Favorite>, FavoriteService>();
            services.AddScoped<IServiceAsync<Company>, CompanyService>();
            services.AddScoped<IServiceAsync<Reason>, ReasonService>();
            services.AddScoped<IServiceAsync<Transport>, TransportService>();
            services.AddScoped<IServiceAsync<Voucher>, VoucherService>();
            services.AddScoped<CartInterface, CartService>();
            services.AddScoped<CartDetailInterface, CartDetailService>();
            //services.AddScoped<IServiceGcpAsync<Gcp>, GcpService>();
            services.AddScoped<SnapBcaService>();
            services.AddScoped<OrderInterface, OrderService>();
            services.AddScoped<OrderBottleInterface, OrderBottleService>();
            services.AddScoped<IServiceAsync<OrderDetail>, OrderDetailService>();
            services.AddScoped<IServiceAsync<Invoice>, InvoiceService>();
            services.AddScoped<IServiceAsync<Payment>, PaymentService>();
            services.AddScoped<IServiceAsync<Deposit>, DepositService>();
            services.AddScoped<IServiceAsync<Subscription>, SubscriptionService>();
            services.AddScoped<IServiceAsync<PromoQuantity>, PromoQuantityService>();
            services.AddScoped<PromoProductInterface, PromoProductService>();
            services.AddScoped<PromoInterface, PromoService>();
            services.AddScoped<IServiceAsync<PromoCartDetail>, PromoCartDetailService>();
            services.AddScoped<IServiceAsync<PromoOrderBottleDetail>, PromoOrderBottleDetailService>();
            services.AddScoped<IServiceAsync<BillingAddress>, BillingAddressService>();
            services.AddScoped<IServiceAsync<DeliveryAddress>, DeliveryAddressService>();
            services.AddScoped<IServiceAsync<WishList>, WishListService>();
            services.AddScoped<IServiceAsync<Discount>, DiscountService>();
            services.AddScoped<SearchOptimizationService>();
            services.AddScoped<ImageSearchService>();
            services.AddScoped<IServiceAsync<UserDealer>, UserDealerService>();
            services.AddScoped<IServiceAsync<Dealer>, DealerService>();
            services.AddScoped<IServiceAsync<Filter>, FilterService>();
            services.AddScoped<UserProductInterface, UserProductService>();
            services.AddScoped<NotificationInterface, NotificationService>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"SOPRA API V1.0201");
                c.RoutePrefix = string.Empty;
            });

            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(x => x.MapControllers());
        }
    }
}
