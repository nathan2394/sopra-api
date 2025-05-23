using Google.Cloud.Vision.V1;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Sopra.Helpers;
using System.Net.Http;
using static Google.Apis.Storage.v1.Data.Bucket.LifecycleData;
using Microsoft.EntityFrameworkCore;
using Sopra.Entities;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using Tesseract;

namespace Sopra.Services
{
    public class ImageSearchService : ImageSearchInterface
    {
        private readonly string _projectId = "sopra-poc";
        private readonly string _bucketName = "sopra-web";
        private readonly StorageClient _storageClient;
        private readonly EFContext _context;
        private readonly string _jsonAuthFilePathGoogleCloudAPI;
        private readonly GoogleCredential _credentialGoogleCloudAPI;

        public ImageSearchService(EFContext context)
        {
            _jsonAuthFilePathGoogleCloudAPI = Utility.GoogleApplicationCredential;
            _credentialGoogleCloudAPI = GoogleCredential.FromFile(_jsonAuthFilePathGoogleCloudAPI);
            _storageClient = StorageClient.Create(_credentialGoogleCloudAPI);
            _context = context;
        }

        public string ProcessImageOCR(string imagePath)
        {
            try
            {
                // Path to Tesseract trained data for Indonesian language
                string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                using var ocrEngine = new TesseractEngine(tessDataPath, "ind", Tesseract.EngineMode.Default);
                using var img = Pix.LoadFromFile(imagePath);
                using var page = ocrEngine.Process(img);

                return page.GetText();
            }
            catch (Exception ex)
            {
                throw new Exception("Error during OCR processing", ex);
            }
        }
        public async Task<(string KtpNumber, string NpwpNumber)> ExtractNumbers(string fullText)
        {
            // Simulate an async operation (e.g., database query)
            var ktpMatch = await Task.Run(() => Regex.Match(fullText, @"\b\d{16}\b"));
            string ktpNumber = ktpMatch.Success ? ktpMatch.Value : "KTP number not found";

            var npwpMatch = await Task.Run(() => Regex.Match(fullText, @"\b\d{2}\.\d{3}\.\d{3}\.\d{1}-\d{3}\.\d{3}\b"));
            string npwpNumber = npwpMatch.Success ? npwpMatch.Value : "NPWP number not found";

            return (ktpNumber, npwpNumber);
        }

        private  long GetCountColor(string type, string name)
        {
            if (type.ToLower() == "bottle")
            {
                // return await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                var Countcolor =  (from calculator in _context.Calculators
                                        join color in _context.Colors on calculator.ColorsID equals color.RefID
                                        where calculator.Name.Substring(0, calculator.Name.Length) == name
                                              && calculator.Status == 3
                                        select color.Name).Distinct().ToList();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            else if (type.ToLower() == "closure")
            {
                var Countcolor = (from closures in _context.Closures
                                       join color in _context.Colors on closures.ColorsID equals color.RefID
                                       where closures.Name.Substring(0, closures.Name.Length) == name
                                             && closures.Status == 3
                                       select color.Name).Distinct().ToList();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            else if (type.ToLower() == "thermo")
            {
                var Countcolor = (from thermo in _context.Thermos
                                       join color in _context.Colors on thermo.ColorsID equals color.RefID
                                       where thermo.Name.Substring(0, thermo.Name.Length) == name
                                             && thermo.Status == 3
                                       select color.Name).Distinct().ToList();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            else if (type.ToLower() == "lid")
            {
                var Countcolor = (from lid in _context.Lids
                                       join color in _context.Colors on lid.ColorsID equals color.RefID
                                       where lid.Name.Substring(0, lid.Name.Length) == name
                                             && lid.Status == 3
                                       select color.Name).Distinct().ToList();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            return 0;
        }

        public async Task UploadStorageFromDatabase()
        {
            string csvFilePath = "E:\\Karvin\\Kerja\\MIT\\Work\\SOPRA\\sopra-api\\Sopra.Api\\product.csv";
            var httpClient = new HttpClient();

            using (var reader = new StreamReader(csvFilePath))
            {
                var headerLine = reader.ReadLine(); // Read the header line
                var dataLines = new List<string[]>();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    dataLines.Add(values);
                }

                foreach (var data in dataLines)
                {
                    string productImage = data[0];
                    string productName = data[1];
                    bool isUpdate = false;

                    // Generate a local file path
                    string localFilePath = Path.GetTempFileName();
                    string fileName = "productImage/" + Path.GetFileName(new Uri(productImage).AbsolutePath);

                    // Download the image from the URL
                    using (var response = await httpClient.GetAsync(productImage))
                    {
                        response.EnsureSuccessStatusCode();
                        await using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }

                    await using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {

                        try
                        {
                            var existingObject = await _storageClient.GetObjectAsync(_bucketName, fileName);
                            if (existingObject != null)
                            {
                                //continue;
                                // Retrieve existing metadata
                                var existingMetadata = existingObject.Metadata ?? new Dictionary<string, string>();

                                // Update metadata with new labels
                                existingMetadata["Labels"] = string.Join(",", await ProcessImage(existingObject.MediaLink));

                                var updatedObject = new Google.Apis.Storage.v1.Data.Object
                                {
                                    Bucket = _bucketName,
                                    Name = fileName,
                                    ContentType = existingObject.ContentType,
                                    Metadata = existingMetadata
                                };

                                await _storageClient.UpdateObjectAsync(updatedObject);
                                isUpdate = true;
                            }
                        }
                        catch (Google.GoogleApiException e)
                        {
                            if (e.Error.Code != 404)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }

                        if (!isUpdate)
                        {
                            var storageObject = new Google.Apis.Storage.v1.Data.Object
                            {
                                Bucket = _bucketName,
                                Name = fileName,
                                ContentType = null,
                                Metadata = new Dictionary<string, string>
                            {
                                { "Name",  productName },
                            }
                            };
                            var uploadedObject = await _storageClient.UploadObjectAsync(storageObject, fileStream);
                            var existingUploadMetadata = uploadedObject.Metadata ?? new Dictionary<string, string>();
                            existingUploadMetadata["Labels"] = string.Join(",", await ProcessImage(uploadedObject.MediaLink));

                            var updatedUploadObject = new Google.Apis.Storage.v1.Data.Object
                            {
                                Bucket = _bucketName,
                                Name = fileName,
                                ContentType = uploadedObject.ContentType,
                                Metadata = existingUploadMetadata
                            };

                            await _storageClient.UpdateObjectAsync(updatedUploadObject);
                        }
                    }
                    File.Delete(localFilePath);
                }
            }
        }


        public async Task<Google.Apis.Storage.v1.Data.Object> UploadToStorage(IFormFile image)
        {
            // Check if the object already exists
            try
            {
                var existingObject = await _storageClient.GetObjectAsync(_bucketName, "productImage/" + image.FileName);
                if (existingObject != null)
                {
                    // Object already exists, return the media link
                    return existingObject;
                }

                var existingUploadObject = await _storageClient.GetObjectAsync(_bucketName, "userUploadImage/" + image.FileName);
                if (existingUploadObject != null)
                {
                    // Object already exists, return the media link
                    return existingUploadObject;
                }
            }
            catch (Google.GoogleApiException e)
            {
                if (e.Error.Code != 404)
                {
                    Console.WriteLine(e.Message);
                }
                else
                {
                    Console.WriteLine(e.Message);
                }
            }

            using (var stream = new MemoryStream())
            {
                await image.CopyToAsync(stream);
                stream.Position = 0;
                var storageObject = await _storageClient.UploadObjectAsync(_bucketName, "userUploadImage/" + image.FileName, image.ContentType, stream);
                var existingUploadMetadata = storageObject.Metadata ?? new Dictionary<string, string>();
                existingUploadMetadata["Labels"] = string.Join(",", await ProcessImage(storageObject.MediaLink));

                var updatedUploadObject = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = _bucketName,
                    Name = "userUploadImage/" + image.FileName,
                    ContentType = storageObject.ContentType,
                    Metadata = existingUploadMetadata
                };

                await _storageClient.UpdateObjectAsync(updatedUploadObject);

                return storageObject;
            }
        }

        public async Task<List<string>> ProcessImage(string imageUrl)
        {
            var client = new ImageAnnotatorClientBuilder
            {
                CredentialsPath = Utility.GoogleApplicationCredential
            }.Build();

            var image = Google.Cloud.Vision.V1.Image.FromUri(imageUrl);
            var response = await client.DetectLabelsAsync(image);
            return response.Select(annotation => annotation.Description).ToList();
        }

        public async Task<List<ProductDetail2>> FindProduct(string keyword, List<Dictionary<string, object>> data)
        {
            try
            {
                var entries = keyword.Split('|', StringSplitOptions.RemoveEmptyEntries);
                var uniqueEntries = new HashSet<string>(entries);

                // Extract the second word from each unique entry
                var keywords = uniqueEntries
                    .Select(entry => entry.Split(' '))
                    .Where(parts => parts.Length > 1)
                    .Select(parts => parts[1])
                    .Distinct()
                    .ToList();

                var dat = await _context.ProductDetails2
                    .AsNoTracking()
                    .Select(x => new ProductDetail2
                    {
                        Type = x.Type,
                        OriginID = x.OriginID,
                        RefID = x.RefID,
                        Name = x.Name,
                        TokpedUrl = x.TokpedUrl,
                        NewProd = x.NewProd,
                        FavProd = x.FavProd,
                        Image = x.Image,
                        Weight = x.Weight,
                        Price = x.Price,
                        Stock = x.Stock,
                        ClosuresID = x.ClosuresID,
                        CategoriesID = x.CategoriesID,
                        CategoryName = x.CategoryName,
                        PlasticType = x.PlasticType,
                        Functions = x.Functions,
                        Tags = x.Tags,
                        StockIndicator = x.StockIndicator,
                        NecksID = x.NecksID,
                        ColorsID = x.ColorsID,
                        ShapesID = x.ShapesID,
                        Volume = x.Volume,
                        QtyPack = x.QtyPack,
                        TotalShared = x.TotalShared,
                        TotalViews = x.TotalViews,
                        NewProdDate = x.NewProdDate,
                        Height = x.Height,
                        Length = x.Length,
                        Width = x.Width,
                        //Whistlist = x.Whistlist,
                        Diameter = x.Diameter,
                        RimsID = x.RimsID,
                        LidsID = x.LidsID,
                        Status = x.Status,
                        CountColor = x.CountColor
                    })
                    .ToListAsync();

                var result = dat.Where(x => keywords.Any(key => x.Name.ToLower().Contains(key.ToLower()))).Select(x =>
                {
                    var matchingPercentage = data
                        .Where(p => x.Name.ToLower().Contains(p["name"].ToString().ToLower()))
                        .Select(p => Convert.ToInt32(p["percentage"]))
                        .FirstOrDefault();

                    // Assign the matching percentage to the Percentage field
                    x.Percentage = matchingPercentage;
                    return x;
                }).ToList();

                if (result != null && result.Any())
                {
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log the exception 
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

        public async Task<(string similar, string closed, List<Dictionary<string, object>> data)> FindSimilarImage(List<string> labels)
        {
            var existingObjects = _storageClient.ListObjectsAsync(_bucketName, "productImage/");
            string similarProduct= "";
            string closedProduct = "";
            Random rnd = new Random();
            List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
            await foreach (var obj in existingObjects)
            {
                if (obj.Metadata != null && obj.Metadata.ContainsKey("Labels"))
                {
                    var storedLabels = obj.Metadata["Labels"].Split(',').ToList();
                    // Define a similarity threshold, e.g., 95% common labels
                    if (AreLabelsSimilar(storedLabels, labels,0.95))
                    {
                        Dictionary<string, object> d = new Dictionary<string, object>();
                        similarProduct = similarProduct + obj.Metadata["Name"].ToString() + "|";
                        var pctg = (95  * Math.Min(storedLabels.Count, labels.Count)) / 10;
                        d["name"] = obj.Metadata["Name"].ToString().Split(" ")[1];
                        d["percentage"] = pctg;
                        data.Add(d);
                        break;
                    }
                }
            }

            await foreach (var obj in existingObjects)
            {
                if (obj.Metadata != null && obj.Metadata.ContainsKey("Labels"))
                {
                    var storedLabels = obj.Metadata["Labels"].Split(',').ToList();
                    // Define a similarity threshold, e.g., 70% common labels
                    if (AreLabelsSimilar(storedLabels, labels, 0.8))
                    {

                        if (closedProduct != "")
                        {
                            Dictionary<string, object> d = new Dictionary<string, object>();
                            closedProduct = closedProduct + obj.Metadata["Name"].ToString() + "|";
                            var pctg = (rnd.Next(80, 85) * Math.Min(storedLabels.Count, labels.Count)) / 10;
                            d["name"] = obj.Metadata["Name"].ToString().Split(" ")[1];
                            d["percentage"] = pctg;
                            data.Add(d);
                        }
                        else
                        {
                            Dictionary<string, object> d = new Dictionary<string, object>();
                            closedProduct = obj.Metadata["Name"].ToString() + "|";
                            var commonLabels = storedLabels.Intersect(labels).ToList();
                            var pctg = (0.95 * Math.Min(storedLabels.Count, labels.Count)) * 10;
                            d["name"] = obj.Metadata["Name"].ToString().Split(" ")[1];
                            d["percentage"] = pctg;
                            data.Add(d);
                        }
                    }
                }
            }
            if (similarProduct != "" && closedProduct != "") return (similarProduct, closedProduct,data);
            return (similarProduct, closedProduct,data);
        }

        private bool AreLabelsSimilar(List<string> storedLabels, List<string> newLabels, double similarityThreshold)
        {
            var commonLabels = storedLabels.Intersect(newLabels).ToList();
            return commonLabels.Count >= (similarityThreshold * Math.Min(storedLabels.Count, newLabels.Count));
        }
    }
}
