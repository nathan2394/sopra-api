using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using Microsoft.AspNetCore.Mvc;
using Sopra.Requests;
using Azure.Core;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Sopra.Services
{
    public class DeliveryAddressService : IServiceAsync<DeliveryAddress>
    {
        private readonly EFContext _context;

        public DeliveryAddressService(EFContext context)
        {
            _context = context;
        }

        public async Task<DeliveryAddress> CreateAsync(DeliveryAddress data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.RefID = 0;
                await _context.DeliveryAddresses.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "DeliveryAddress", data.ID, "Add");

                await dbTrans.CommitAsync();

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.DeliveryAddresses.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "DeliveryAddress", id, "Delete");

                await dbTrans.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<DeliveryAddress> EditAsync(DeliveryAddress data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.DeliveryAddresses.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                var updateIsUse = await _context.DeliveryAddresses.Where(x => x.UserId == obj.UserId ).ToListAsync();
                if(updateIsUse != null)
                {
                    foreach (var item in updateIsUse)
                    {
                        item.IsUse = false;
                        //_context.DeliveryAddresses.Update(item);
                    }
                    _context.DeliveryAddresses.UpdateRange(updateIsUse);
                }

                obj.Address = data.Address;
                obj.ZipCode = data.ZipCode;

                obj.DistrictId = data.DistrictId;
                obj.CityId = data.CityId;
                obj.CountryId= data.CountryId;
                obj.ProvinceId = data.ProvinceId;
                obj.IsUse = data.IsUse;
                obj.UserId= data.UserId;
                obj.Label = data.Label;
                obj.Landmark = data.Landmark;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "DeliveryAddress", data.ID, "Edit");

                await dbTrans.CommitAsync();

                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public Task<DeliveryAddress> GetAccIdAsync(long id, long customerid)
        {
            throw new NotImplementedException();
        }

        public async Task<ListResponse<DeliveryAddress>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.DeliveryAddresses where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.DistrictId.ToString().Equals(search)
                        || x.CountryId.ToString().Equals(search)
                        || x.CityId.ToString().Equals(search)
                        || x.ProvinceId.ToString().Equals(search)
                        || x.Address.ToString().Contains(search)
                        || x.ZipCode.ToString().Contains(search)
                        || x.IsUse.ToString().Contains(search)
                        || x.UserId.ToString().Equals(search));

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
                                "districtid" => query.Where(x => x.DistrictId.ToString().Equals(value)),
                                "countryid" => query.Where(x => x.CountryId.ToString().Equals(value)),
                                "cityid" => query.Where(x => x.CityId.ToString().Equals(value)),
                                "provinceid" => query.Where(x => x.ProvinceId.ToString().Equals(value)),
                                "userid" => query.Where(x => x.UserId.ToString().Equals(value)),
                                "address" => query.Where(x => x.Address.ToString().Contains(value)),
                                "zipcode" => query.Where(x => x.ZipCode.Contains(value)),
                                "isuse" => query.Where(x => x.IsUse.ToString().Contains(value)),
                                //"role" => query.Where(x => x.RoleID.Contains(value)),
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
                            "districtid" => query.OrderByDescending(x => x.DistrictId),
                            "countryid" => query.OrderByDescending(x => x.CountryId),
                            "cityid" => query.OrderByDescending(x => x.CityId),
                            "provinceid" => query.OrderByDescending(x => x.ProvinceId),
                            "address" => query.OrderByDescending(x => x.Address),
                            "zipcode" => query.OrderByDescending(x => x.ZipCode),
                            "isuse" => query.OrderByDescending(x => x.IsUse),
                            "userid" => query.OrderByDescending(x => x.UserId),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "districtid" => query.OrderBy(x => x.DistrictId),
                            "countryid" => query.OrderBy(x => x.CountryId),
                            "cityid" => query.OrderBy(x => x.CityId),
                            "provinceid" => query.OrderBy(x => x.ProvinceId),
                            "address" => query.OrderBy(x => x.Address),
                            "zipcode" => query.OrderBy(x => x.ZipCode),
                            "isuse" => query.OrderBy(x => x.IsUse),
                            "userid" => query.OrderBy(x => x.UserId),
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

                return new ListResponse<DeliveryAddress>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<DeliveryAddress> GetByIdAsync(long id)
        {
            try
            {
                return await _context.DeliveryAddresses.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        Task<DeliveryAddress> IServiceAsync<DeliveryAddress>.ChangePassword(ChangePassword obj, long id)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }
    }
}
