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
using FirebaseAdmin.Auth;

namespace Sopra.Services
{
    public interface RolesInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);

        Task<dynamic> GetByIdAsync(long id);
        Task<Role> CreateAsync(Role data, int userId);

        Task<Role> EditAsync(Role data, int userId);
        Task<bool> DeleteAsync(long id, int userId);

    }

    public class RolesService : RolesInterface
    {
        private readonly EFContext _context;

        public RolesService(EFContext context)
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
                var query = from a in _context.Role
                            where a.IsDeleted == false
                            select a;

                var dateBetween = "";

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search));

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
                                "rolesname" => query.Where(x => x.Name.ToString().Equals(value)),
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
                            "id" => query.OrderByDescending(x => x.ID),
                            "rolesname" => query.OrderByDescending(x => x.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "id" => query.OrderByDescending(x => x.ID),
                            "rolesname" => query.OrderByDescending(x => x.Name),
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
                        Name = x.Name
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

        public async Task<dynamic> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.Role
                .Where(x => x.ID == id && x.IsDeleted == false)
                .FirstOrDefaultAsync();

                if (data == null) return null;

                var resData = new
                {
                    ID = data.ID,
                    Name = data.Name
                };

                return resData;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Role> CreateAsync(Role data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload role from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                data.UserIn = userId;
                data.DateIn = Utility.getCurrentTimestamps();

                await _context.Role.AddAsync(data);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data role, payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");
                throw;
            }
        }

        public async Task<Role> EditAsync(Role data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                Trace.WriteLine($"Edit role with request = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                var getRole = _context.Role
                    .Where(i => i.ID == data.ID)
                    .FirstOrDefault();

                if (getRole != null)
                {
                    getRole.Name = data.Name;

                    getRole.DateUp = Utility.getCurrentTimestamps();
                    getRole.UserUp = userId;
                }

                _context.Role.Update(getRole);
                await _context.SaveChangesAsync();

                await Utility.AfterSave(_context, "Role", getRole.ID, "Edit");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"Role edited successfully with ID = {getRole.ID}");

                await dbTrans.CommitAsync();

                return getRole;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"Error editing role, request = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();

                throw;
            }
        }
        
        public async Task<bool> DeleteAsync(long id, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                var obj = await _context.Role.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.DateUp = Utility.getCurrentTimestamps();
                obj.UserUp = userId;

                await _context.SaveChangesAsync();
                await Utility.AfterSave(_context, "Role", id, "Delete");
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
    }
}