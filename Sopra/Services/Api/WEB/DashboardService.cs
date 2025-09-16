using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace Sopra.Services
{
    public interface DashboardInterface
    {
        Task<ListResponse<dynamic>> LoadOverview(
            DateTime startDate, 
            DateTime endDate, 
            int companyID,
            string search
        );
        Task<ListResponse<dynamic>> LoadTableData(
            string key, 
            DateTime startDate, 
            DateTime endDate, 
            int companyID,
            string search
        );
    }

    public class DashboardService : DashboardInterface
    {

        private async Task<ListResponse<object>> FetchDashboard(
            string key,
            DateTime startDate,
            DateTime endDate,
            int companyID,
            string search)
        {
            try
            {
                var query = $"EXEC spDashboard @Key='{key}', @StartDate='{startDate:yyyy-MM-dd HH:mm:ss}', @EndDate='{endDate:yyyy-MM-dd HH:mm:ss}', @CompanyID={companyID}";
                var result = await Task.Run(() => Utility.SQLGetObjects(query, Utility.SQLDBConnection));

                var dataList = ConvertDataTableToList(result, key);

                // Searching
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLowerInvariant();
                    dataList = dataList.Where((item) => 
                        JsonSerializer.Serialize(item).ToLowerInvariant().Contains(search)).ToList();
                }

                return new ListResponse<dynamic>(dataList, dataList.Count, 0);
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

        public async Task<ListResponse<object>> LoadOverview(
            DateTime startDate, 
            DateTime endDate, 
            int companyID,
            string search)
        {
            try
            {
                return await FetchDashboard("COUNT_OVERVIEW", startDate, endDate, companyID, search);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        
        public async Task<ListResponse<object>> LoadTableData(
            string key,
            DateTime startDate,
            DateTime endDate,
            int companyID,
            string search)
        {
            try
            {
                return await FetchDashboard(key, startDate, endDate, companyID, search);
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