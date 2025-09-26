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
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Sopra.Services
{
    public interface CustomersInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<Customer> GetByIdAsync(long id);
    }

    public class CustomersService : CustomersInterface
    {
        private readonly EFContext _context;

        public CustomersService(EFContext context)
        {
            _context = context;
        }

        public async Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var now = Utility.getCurrentTimestamps();

                var query = from a in _context.Customers
                            join b in _context.Users on a.RefID equals b.CustomersID
                            from c in _context.UserDealers.Where(x => x.UserId == b.RefID && now >= x.StartDate && now <= x.EndDate).DefaultIfEmpty()
                            from d in _context.Dealers.Where(x => c != null && x.RefID == c.DealerId).DefaultIfEmpty()
                            where a.IsDeleted == false
                            select new
                            {
                                ID = a.ID,
                                RefID = a.RefID,
                                DateIn = a.DateIn,
                                UserID = b.RefID,
                                CustomerNumber = a.CustomerNumber,
                                Seller = a.Seller,
                                Termin = a.Termin,
                                Name = a.Name,
                                Mobile1 = a.Mobile1,
                                Email = a.Email,
                                DealerId = d != null ? d.RefID : 0,
                                DealerName = d != null ? d.Tier : "Regular",
                                DepositBalance = _context.Deposit
                                    .Where(x => x.CustomersID == b.RefID)
                                    .Sum(x => x.TotalAmount) ?? 0
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search)
                        || x.Mobile1.Equals(search)
                    );

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

                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.Name.ToString().Equals(value)),
                                "phonenum" => query.Where(x => x.Mobile1.ToString().Equals(value)),
                                "email" => query.Where(x => x.Email.ToString().Equals(value)),
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
                            "customerName" => query.OrderByDescending(x => x.Name),
                            "email" => query.OrderByDescending(x => x.Email),
                            "depositBalance" => query.OrderByDescending(x => x.DepositBalance),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "customerName" => query.OrderBy(x => x.Name),
                            "email" => query.OrderBy(x => x.Email),
                            "depositBalance" => query.OrderBy(x => x.DepositBalance),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.ID);
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
                        ID = x.ID,
                        RefID = x.RefID,
                        UserID = x.UserID,

                        CustomerNumber = x.CustomerNumber,
                        CustomerName = x.Name,
                        Seller = x.Seller,
                        Termin = x.Termin,

                        Email = x.Email,
                        Mobile1 = x.Mobile1,

                        DealerID = x.DealerId,
                        DealerName = x.DealerName,

                        DepositBalance = x.DepositBalance
                    };
                })
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

        public async Task<Customer> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Customers
                .AsNoTracking()
                .Join(_context.Users,
                    customer => customer.RefID,
                    user => user.CustomersID,
                    (customer, user) => new { Customer = customer, User = user })
                .Where(x => x.User.RefID == id && x.Customer.IsDeleted == false)
                .Select(x => x.Customer)
                .FirstOrDefaultAsync();
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