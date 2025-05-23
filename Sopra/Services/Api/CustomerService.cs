using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Requests;
using System.Collections.Generic;

namespace Sopra.Services
{
    public class CustomerService : IServiceAsync<Customer>
    {
        private readonly EFContext _context;

        public CustomerService(EFContext context)
        {
            _context = context;
        }

        public async Task<Customer> CreateAsync(Customer data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Customers.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Customer", data.ID, "Add");

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

        public async Task<bool> DeleteAsync(long id, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Customers.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Customer", id, "Delete");

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

        public async Task<Customer> EditAsync(Customer data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Customers.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.Name = data.Name;
                obj.CustomerNumber = data.CustomerNumber;
                obj.DeliveryAddress = data.DeliveryAddress;
                obj.BillingAddress = data.BillingAddress;
                obj.DeliveryRegencyID = data.DeliveryRegencyID;
                obj.DeliveryProvinceID = data.DeliveryProvinceID;
                obj.CountriesID = data.CountriesID;
                obj.DeliveryPostalCode = data.DeliveryPostalCode;
                obj.Mobile1 = data.Mobile1;
                obj.PIC = data.PIC;
                obj.Email = data.Email;
                obj.Termin = data.Termin;
                obj.Currency = data.Currency;
                obj.Tax1 = data.Tax1;
                obj.NPWP = data.NPWP;
                obj.NIK = data.NIK;
                obj.TaxType = data.TaxType;
                obj.VirtualAccount = data.VirtualAccount;
                obj.Seller = data.Seller;
                obj.CustomerType = data.CustomerType;
                obj.Mobile2 = data.Mobile2;
                obj.DeliveryDistrictID = data.DeliveryDistrictID;
                obj.DeliveryPhone = data.DeliveryPhone;
                obj.Status = data.Status;
                obj.SalesID = data.SalesID;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Customer", data.ID, "Edit");

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


        public async Task<ListResponse<Customer>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Customers where a.IsDeleted == false select a;

                // // Searching
                // if (!string.IsNullOrEmpty(search))
                //     query = query.Where(x => x.RefID.ToString().Contains(search)
                //         || x.Name.Contains(search)
                //         );

                // // Filtering
                // if (!string.IsNullOrEmpty(filter))
                // {
                //     var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                //     foreach (var f in filterList)
                //     {
                //         var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                //         if (searchList.Length == 2)
                //         {
                //             var fieldName = searchList[0].Trim().ToLower();
                //             var value = searchList[1].Trim();
                //             query = fieldName switch
                //             {
                //                 "refid" => query.Where(x => x.RefID.ToString().Contains(value)),
                //                 "name" => query.Where(x => x.Name.Contains(value)),
                //                 _ => query
                //             };
                //         }
                //     }
                // }

                // // Sorting
                // if (!string.IsNullOrEmpty(sort))
                // {
                //     var temp = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
                //     var orderBy = sort;
                //     if (temp.Length > 1)
                //         orderBy = temp[0];

                //     if (temp.Length > 1)
                //     {
                //         query = orderBy.ToLower() switch
                //         {
                //             "refid" => query.OrderByDescending(x => x.RefID),
                //             "name" => query.OrderByDescending(x => x.Name),
                //             _ => query
                //         };
                //     }
                //     else
                //     {
                //         query = orderBy.ToLower() switch
                //         {
                //             "refid" => query.OrderBy(x => x.RefID),
                //             "name" => query.OrderBy(x => x.Name),
                //             _ => query
                //         };
                //     }
                // }
                // else
                // {
                //     query = query.OrderByDescending(x => x.ID);
                // }

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

                return new ListResponse<Customer>(data, total, page);
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
                return await _context.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.RefID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Customer> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<Customer> GetAccIdAsync(long id, long customerid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }
    }
}

