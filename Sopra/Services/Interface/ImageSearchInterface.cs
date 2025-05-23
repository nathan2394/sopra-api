using Microsoft.AspNetCore.Http;
using Sopra.Entities;
using Sopra.Responses;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Sopra.Services
{
    public interface ImageSearchInterface
    {

        Task<Google.Apis.Storage.v1.Data.Object> UploadToStorage(IFormFile image);
        Task<List<string>> ProcessImage(string imageUrl);
        Task UploadStorageFromDatabase();
        Task<(string similar, string closed, List<Dictionary<string, object>> data)> FindSimilarImage(List<string> labels);
        Task<List<ProductDetail2>> FindProduct(string keyword, List<Dictionary<string, object>> data);
        string ProcessImageOCR(string imagePath);
        Task<(string KtpNumber, string NpwpNumber)> ExtractNumbers(string fullText);
    }
}
