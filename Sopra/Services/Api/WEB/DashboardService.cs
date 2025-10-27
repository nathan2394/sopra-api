using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Globalization;

namespace Sopra.Services
{
    public interface DashboardInterface
    {
        Task<ListResponse<dynamic>> LoadTableData(
            string key,
            DateTime startDate,
            DateTime endDate,
            int companyID,
            string search,
            int page,
            int limit
        );
    }

    public class DashboardService : DashboardInterface
    {

        private async Task<ListResponse<object>> FetchDashboard(
            string key,
            DateTime startDate,
            DateTime endDate,
            int companyID,
            string search,
            int page,
            int limit
        )
        {
            try
            {
                var query = $"EXEC spDashboard @Key='{key}', @StartDate='{startDate.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}', @EndDate='{endDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}', @CompanyID={companyID}";
                var result = await Task.Run(() => Utility.SQLGetObjects(query, Utility.SQLDBConnection));

                var dataList = ConvertDataTableToList(result, key);
                var totalCount = dataList.Count;

                // Searching
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLowerInvariant();
                    dataList = dataList.Where((item) =>
                        JsonSerializer.Serialize(item).ToLowerInvariant().Contains(search)).ToList();

                    totalCount = dataList.Count;
                }

                // Set Limit and Page
                if (key != "COUNT_OVERVIEW" && limit != 0)
                    dataList = dataList.Skip(page * limit).Take(limit).ToList();

                return new ListResponse<dynamic>(dataList, totalCount, 0);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        private List<object> ConvertDataTableToList(DataTable dataTable, string key)
        {
            var result = new List<object>();

            switch (key.ToUpper())
            {
                case "COUNT_OVERVIEW":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new CountOverview
                        {
                            Key = row["Key"]?.ToString(),
                            Title = row["Title"]?.ToString(),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0),
                            Count = Convert.ToInt64(row["Count"] ?? 0),
                            Color = row["Color"]?.ToString(),
                            Unit = row["Unit"]?.ToString()
                        });
                    }
                    break;

                case "PENDING_ORDER":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new PendingOrder
                        {
                            OrderID = Convert.ToInt64(row["OrderID"] ?? 0),
                            OrderNo = row["OrderNo"]?.ToString(),
                            OrderDate = Convert.ToDateTime(row["OrderDate"]),
                            CustomerName = row["CustomerName"]?.ToString(),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0),
                            HandleBy = row["HandleBy"]?.ToString()
                        });
                    }
                    break;

                case "ONGOING_INVOICE":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new OngoingInvoice
                        {
                            InvoiceID = Convert.ToInt64(row["InvoiceID"] ?? 0),
                            InvoiceNo = row["InvoiceNo"]?.ToString(),
                            InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
                            CustomerName = row["CustomerName"]?.ToString(),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0),
                            HandleBy = row["HandleBy"]?.ToString(),
                            DueDate = Convert.ToDateTime(row["DueDate"])
                        });
                    }
                    break;

                case "PAID_ORDER":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new PaidOrder
                        {
                            OrderID = Convert.ToInt64(row["OrderID"] ?? 0),
                            OrderNo = row["OrderNo"]?.ToString(),
                            OrderDate = Convert.ToDateTime(row["OrderDate"]),
                            InvoiceID = Convert.ToInt64(row["InvoiceID"] ?? 0),
                            InvoiceNo = row["InvoiceNo"]?.ToString(),
                            InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
                            PaymentID = Convert.ToInt64(row["PaymentID"] ?? 0),
                            PaymentNo = row["PaymentNo"]?.ToString(),
                            PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
                            CustomerName = row["CustomerName"]?.ToString(),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0),
                            HandleBy = row["HandleBy"]?.ToString()
                        });
                    }
                    break;

                case "CANCELED_TRANSACTION":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new CanceledTransaction
                        {
                            OrderID = Convert.ToInt64(row["OrderID"] ?? 0),
                            OrderNo = row["OrderNo"]?.ToString(),
                            OrderDate = Convert.ToDateTime(row["OrderDate"]),
                            CustomerName = row["CustomerName"]?.ToString(),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0),
                            CancelBy = row["CancelBy"]?.ToString(),
                            CancelDate = Convert.ToDateTime(row["CancelDate"]),
                            Reason = row["Reason"]?.ToString()
                        });
                    }
                    break;

                case "TOP_CUSTOMER":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new TopCustomer
                        {
                            CustomerID = Convert.ToInt64(row["CustomerID"] ?? 0),
                            CustomerName = row["CustomerName"]?.ToString(),
                            CountOrder = Convert.ToInt64(row["CountOrder"] ?? 0),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0)
                        });
                    }
                    break;

                case "TOP_PRODUCT":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new TopProduct
                        {
                            ProductID = Convert.ToInt64(row["ProductID"] ?? 0),
                            ProductName = row["ProductName"]?.ToString(),
                            ProductImage = row["ProductImage"]?.ToString(),
                            Quantity = Convert.ToInt64(row["Quantity"] ?? 0)
                        });
                    }
                    break;

                case "TOP_PROMO":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new TopPromo
                        {
                            PromoID = Convert.ToInt64(row["PromoID"] ?? 0),
                            PromoName = row["PromoName"]?.ToString(),
                            PromoImage = row["PromoImage"]?.ToString(),
                            CountOrder = Convert.ToInt64(row["CountOrder"] ?? 0)
                        });
                    }
                    break;

                default:
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var item = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            item[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                        }
                        result.Add(item);
                    }
                    break;
            }

            return result;
        }

        public async Task<ListResponse<object>> LoadTableData(
            string key,
            DateTime startDate,
            DateTime endDate,
            int companyID,
            string search,
            int page,
            int limit    
        )
        {
            try
            {
                return await FetchDashboard(key, startDate, endDate, companyID, search, page, limit);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
    }
}