using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Sopra.Requests;
using System.Text;
using Sopra.Services;
using Google.Apis.Auth.OAuth2;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Serialization;
using Sopra.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Xml.Schema;
using System.Linq;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("search-optimization")]
    public class _SearchOptimizationController : ControllerBase
    {
        private readonly SearchOptimizationService _service;
        public _SearchOptimizationController(SearchOptimizationService service)
        {
            _service = service;
        }

        //[Authorize]
        [HttpPost("{param}")]
        public async Task<IActionResult> GetSearch(string param = "", int limit = 0, int page = 0, int userid = 0, [FromBody] ProductKey productKey = null)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented
                };

                string[] sizes = _service.getSeasonalName();
                if (sizes.Any(s => s.Equals(param, StringComparison.OrdinalIgnoreCase)))
                {
                    var resultSeasonal = await _service.GetDataSeasonal(param, userid);
                    if (resultSeasonal.Count >= 1)
                    {
                        string jsonString = JsonConvert.SerializeObject(resultSeasonal, settings);
                        JObject jsonObject = JObject.Parse("{ \"data\": " + jsonString + " }");
                        jsonObject["total"] = resultSeasonal.Count;
                        return Ok(jsonObject);
                    }
                }

                var (result, tipe, total) = await _service.GetSearch(param, limit, page, productKey);
                if (result != null)
                {
                    List<Dictionary<string, object>> rowsList = new List<Dictionary<string, object>>();
                    foreach (DataRow row in result)
                    {
                        Dictionary<string, object> rowDict = new Dictionary<string, object>();
                        foreach (DataColumn column in row.Table.Columns)
                        {
                            rowDict[column.ColumnName] = row[column];
                            if (column.ColumnName == "CountColor")
                            {
                                //rowDict[column.ColumnName] = await _service.GetRealImage(Convert.ToString(row["Type"]), Convert.ToInt64(row["refID"]));
                                rowDict[column.ColumnName] = await _service.GetCountColor(Convert.ToString(row["Type"]), Convert.ToString(row["Name"]));
                            }
                        }
                        rowDict["QtyCart"] = await _service.GetQtyCart(Convert.ToString(row["Type"]), Convert.ToInt64(row["refID"]), userid);
                        rowDict["color"] = await _service.GetColor(Convert.ToInt64(row["ColorsID"]));
                        rowsList.Add(rowDict);
                    }

                    string jsonString = JsonConvert.SerializeObject(rowsList, settings);
                    JObject jsonObject = JObject.Parse("{ \"data\": " + jsonString + " }");
                    jsonObject["total"] = total;
                    return Ok(jsonObject);
                }
                else
                {
                    var (resultRecommended, totalData) = await _service.GetRecommended(tipe);
                    if (resultRecommended != null)
                    {
                        foreach (var res in resultRecommended)
                        {
                            res.QtyCart = await _service.GetQtyCart(Convert.ToString(res.Type), Convert.ToInt64(res.RefID), userid);
                            res.CountColor = Convert.ToInt32(await _service.GetCountColor(Convert.ToString(res.Type), Convert.ToString(res.Name)));
                        }
                        string jsonString = JsonConvert.SerializeObject(resultRecommended, settings);
                        JObject jsonObject = JObject.Parse("{ \"msg\": \"Not Found\", \"data\": " + jsonString + " }");
                        jsonObject["total"] = totalData;
                        return Ok(jsonObject);
                    }
                    return Ok(new { message = "Not Found" });
                }

            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "_SearchOptimizationController");
                throw;
            }
        }
    }
}
