using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using Sopra.Services;
using Sopra.Helpers;
using Google.Api;
using System;
using Google.Cloud.Storage.V1;
using System.Collections.Generic;
using System.Linq;
using Sopra.Entities;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("image-search")]
    public class ImageSearchController : ControllerBase
    {
        //private readonly string _apiKey;
        private readonly ImageSearchService _service;
        

        public ImageSearchController(ImageSearchService service)
        {
            _service = service;
            //_storageClient = StorageClient.Create();
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest("No image uploaded.");
                }
                //await _service.UploadStorageFromDatabase();
                var imageObject = await _service.UploadToStorage(image);
                if (imageObject.Metadata != null &&
                    imageObject.Metadata.ContainsKey("Labels") &&
                    !string.IsNullOrEmpty(imageObject.Metadata["Labels"]))
                {
                    var (similar, closed,data) = await _service.FindSimilarImage(imageObject.Metadata["Labels"].ToString().Split(",").ToList<string>());
                    var similarProducts = null as List<ProductDetail2>;
                    var rawClosedProducts = null as List<ProductDetail2>;
                    if (similar != "") similarProducts = await _service.FindProduct(similar,data);
                    if (closed != "") rawClosedProducts = await _service.FindProduct(closed,data);
                    if (similarProducts != null && rawClosedProducts != null)
                    {
                        // Remove products from closedProducts that are present in similarProducts
                        rawClosedProducts = rawClosedProducts
                            .Where(closedProd => !similarProducts.Any(similarProd =>
                                similarProd.OriginID == closedProd.OriginID &&
                                similarProd.RefID == closedProd.RefID &&
                                similarProd.Name == closedProd.Name &&
                                similarProd.Type == closedProd.Type &&
                                similarProd.Price == closedProd.Price &&
                                similarProd.Stock == closedProd.Stock &&
                                similarProd.Image == closedProd.Image))
                            .ToList();
                    }
                    var closedProducts = rawClosedProducts.GroupBy(x => x.Name.Split(" ")[0]).ToList();
                    return Ok(new { imageObject.MediaLink, similarProducts, closedProducts });
                }

                List<string> extractedText = await _service.ProcessImage(imageObject.MediaLink);
                if(extractedText != null)
                {
                    var (similar, closed,data) = await _service.FindSimilarImage(extractedText);
                    var similarProducts = null as List<ProductDetail2>;
                    var rawClosedProducts = null as List<ProductDetail2>;
                    if (similar != "") similarProducts = await _service.FindProduct(similar,data);
                    if (closed != "") rawClosedProducts = await _service.FindProduct(closed,data);

                    if (similarProducts != null && rawClosedProducts != null)
                    {
                        // Remove products from closedProducts that are present in similarProducts
                        rawClosedProducts = rawClosedProducts
                            .Where(closedProd => !similarProducts.Any(similarProd =>
                                similarProd.OriginID == closedProd.OriginID &&
                                similarProd.RefID == closedProd.RefID &&
                                similarProd.Name == closedProd.Name &&
                                similarProd.Type == closedProd.Type &&
                                similarProd.Price == closedProd.Price &&
                                similarProd.Stock == closedProd.Stock &&
                                similarProd.Image == closedProd.Image))
                            .ToList();
                    }
                    var closedProducts = rawClosedProducts?
                        .Where(x => !string.IsNullOrEmpty(x.Name))
                        .GroupBy(x => x.Name.Split(" ")[0])
                        .Select(x => x.First())
                        .ToList() ?? new List<ProductDetail2>();
                        
                    return Ok(new { imageObject.MediaLink, similarProducts, closedProducts });
                }
                return Ok(new { imageObject.MediaLink, extractedText });
            }
            catch (Exception ex)
            {
                // Log exception for troubleshooting
                Console.WriteLine($"Error in image search: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("ocr")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> OCRImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest("No image uploaded.");
                }
                // Save the image temporarily for OCR processing
                var tempPath = Path.GetTempFileName();
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Process the image to extract text
                var extractedText = _service.ProcessImageOCR(tempPath);
                System.IO.File.Delete(tempPath); // Clean up the temporary file

                if (string.IsNullOrEmpty(extractedText))
                {
                    return BadRequest("No text could be extracted from the image.");
                }

                // Extract both KTP and NPWP numbers
                var (ktpNumber, npwpNumber) = await _service.ExtractNumbers(extractedText);

                return Ok(new
                {
                    KTPNumber = ktpNumber,
                    NPWPNumber = npwpNumber,
                    FullText = extractedText
                });
            }
            catch (Exception ex)
            {
                // Log exception for troubleshooting
                Console.WriteLine($"Error in image search: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
