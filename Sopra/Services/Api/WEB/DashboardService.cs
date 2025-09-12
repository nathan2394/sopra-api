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

namespace Sopra.Services
{
    public interface DashboardInterface
    {
        Task<ListResponse<dynamic>> LoadOverview(
            DateTime startDate, 
            DateTime endDate, 
            int companyID
        );
        Task<ListResponse<dynamic>> LoadTableData(
            string key, 
            DateTime startDate, 
            DateTime endDate, 
            int companyID
        );
    }

    public class DashboardService : DashboardInterface
    {
        private async Task<ListResponse<object>> FetchDashboard(
            string key,
            DateTime startDate,
            DateTime endDate,
            int companyID)
        {
            try
            {
                var query = $"EXEC spDashboard @Key='{key}', @StartDate='{startDate:yyyy-MM-dd HH:mm:ss}', @EndDate='{endDate:yyyy-MM-dd HH:mm:ss}', @CompanyID={companyID}";
                var result = await Task.Run(() => Utility.SQLGetObjects(query, Utility.SQLDBConnection));

                var dataList = ConvertDataTableToList(result, key);

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
                        });
                    }
                    break;

                case "PENDING_ORDER":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new PendingOrder
                        {
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
                            OrderNo = row["InvoiceNo"]?.ToString(),
                            OrderDate = Convert.ToDateTime(row["InvoiceDate"]),
                            CustomerName = row["CustomerName"]?.ToString(),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0),
                            HandleBy = row["HandleBy"]?.ToString()
                        });
                    }
                    break;

                case "PAID_ORDER":
                    foreach (DataRow row in dataTable.Rows)
                    {
                        result.Add(new
                        {
                            OrderNo = row["OrderNo"]?.ToString(),
                            OrderDate = Convert.ToDateTime(row["OrderDate"]),
                            InvoiceNo = row["InvoiceNo"]?.ToString(),
                            InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
                            CustomerName = row["CustomerName"]?.ToString(),
                            Amount = Convert.ToInt64(row["Amount"] ?? 0),
                            HandleBy = row["HandleBy"]?.ToString()
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
            int companyID)
        {
            try
            {
                return await FetchDashboard("COUNT_OVERVIEW", startDate, endDate, companyID);
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
            int companyID)
        {
            try
            {
                return await FetchDashboard(key, startDate, endDate, companyID);
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