using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;

using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;
using System.Data;

namespace Sopra.Services
{
    public interface AgentAiInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(string param, int limit, int page, int total, string search, string sort,
        string filter, string date, ProductKeyAisah productKeyAisah = null);
    }

    public class AgentAiService : AgentAiInterface
    {
        public DataTable execProductDetail(string param = "", ProductKeyAisah productKeyAisah = null)
        {
            var query = string.Format(@"
                    EXEC spCreateProductDetails '{0}','{1}','{2}'
                ", param, productKeyAisah.ProductFunctions, productKeyAisah.ProductKeys);

            var result = Utility.SQLGetObjects(query, Utility.SQLDBConnection);

            // Check if result has rows to sort
            if (result != null && result.Rows.Count > 0)
            {
                // Use DataView to apply sorting on the DataTable
                DataView dataView = result.DefaultView;
                dataView.Sort = "Type ASC"; // Specify your column sorting here

                // Return the sorted DataTable
                return dataView.ToTable();
            }

            return result;
        }

        public Task<ListResponse<dynamic>> GetAllAsync(string param, int limit, int page, int total, string search, string sort, string filter, string date, ProductKeyAisah productKeyAisah = null)
        {
            Utility.ConnectSQL(Utility.SQL_Server, Utility.SQL_Database, Utility.SQL_UserID, Utility.SQL_Password);
            var resData = execProductDetail(param, productKeyAisah);

            // Convert DataTable to IEnumerable<dynamic>
            var list = new List<dynamic>();
            if (resData != null)
            {
                foreach (DataRow row in resData.Rows)
                {
                    var obj = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
                    foreach (DataColumn col in resData.Columns)
                    {
                        obj[col.ColumnName] = row[col];
                    }
                    list.Add(obj);
                }
            }

            return Task.FromResult(new ListResponse<dynamic>(list, total, page));
        }
    }
}