using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;

using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;
using System.Data;
using Newtonsoft.Json;
using System.Configuration;
using Google.Protobuf.WellKnownTypes;

namespace Sopra.Services
{
    public interface DepositsInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<Deposit> CreateAsync(Deposit data, int userId);
    }

    public class DepositsService : DepositsInterface
    {
        private readonly EFContext _context;

        public DepositsService(EFContext context)
        {
            _context = context;
        }

        public async Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                //Get data from context Orders join to Users
                //Users join to Customers 
                var query = from a in _context.Deposit
                            join c in _context.Users on a.CustomersID equals c.RefID
                            join d in _context.Customers on c.CustomersID equals d.RefID into customerJoin
                            from d in customerJoin.DefaultIfEmpty()
                            where a.IsDeleted == false
                            select new { Deposit = a, Customer = d };

                var dateBetween = "";

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Deposit.CustomersID.ToString().Equals(search));

                // Filtering
                if (!string.IsNullOrEmpty(filter))
                {
                    var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in filterList)
                    {
                        var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        if (searchList.Length == 2)
                        {
                            var fieldName = searchList[0].Trim().ToLower();
                            var value = searchList[1].Trim();
                            if (fieldName == "transdate") dateBetween = Convert.ToString(value);
                            query = fieldName switch
                            {
                                "customersid" => query.Where(x => x.Deposit.RefID.ToString().Equals(value)),
                                "customersname" => query.Where(x => x.Customer.Name.ToString().Equals(value)),
                                _ => query
                            };
                        }
                    }
                }

                // Sorting
                if (!string.IsNullOrEmpty(sort))
                {
                    var temp = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var orderBy = sort;
                    if (temp.Length > 1)
                        orderBy = temp[0];

                    if (temp.Length > 1)
                    {
                        query = orderBy.ToLower() switch
                        {
                            "customersid" => query.OrderByDescending(x => x.Deposit.CustomersID),
                            "customersname" => query.OrderByDescending(x => x.Customer.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "transdate" => query.OrderBy(x => x.Deposit.TransDate),
                            "customersid" => query.OrderBy(x => x.Deposit.CustomersID),
                            "customersname" => query.OrderBy(x => x.Customer.Name),
                            "totalamount" => query.OrderBy(x => x.Deposit.DateUp),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.Deposit.TransDate);
                }

                if (dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    query = query.Where(x => x.Deposit.TransDate >= start && x.Deposit.TransDate <= end);

                }

                // Get Total Before Limit and Page
                total = await query.CountAsync();

                // Set Limit and Page
                if (limit != 0)
                    query = query.Skip(page * limit).Take(limit);

                // Get Data
                var data = await query.ToListAsync();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsync(limit, page, total, search, sort, filter, date);
                }

                // Map to DTO
                var resData = data.Select(x =>
                {
                    return new
                    {
                        ID = x.Deposit.ID,
                        RefID = x.Deposit.RefID,
                        TransDate = x.Deposit.TransDate,
                        CustomerID = x.Deposit.CustomersID,
                        CustomerName = x.Customer?.Name ?? "",
                        TotalAmount = x.Deposit.TotalAmount
                    };
                })
                .Distinct()
                .ToList();

                return new ListResponse<dynamic>(resData, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Deposit> CreateAsync(Deposit data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await _context.Deposit.AddAsync(data);
                await _context.SaveChangesAsync();

                var logItem = new UserLog
                {
                    ObjectID = data.ObjectID ?? 0,
                    ModuleID = 1,
                    UserID = userId,
                    Description = "Deposit was Created.",
                    TransDate = Utility.getCurrentTimestamps(),
                    DateIn = Utility.getCurrentTimestamps(),
                    UserIn = userId,
                    UserUp = 0,
                    IsDeleted = false
                };

                await _context.UserLogs.AddAsync(logItem);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data order, payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");
                throw;
            }
        }
    }
}