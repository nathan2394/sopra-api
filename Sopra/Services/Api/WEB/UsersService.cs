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
using Microsoft.SqlServer.Server;

namespace Sopra.Services
{
    public interface UsersInterface
    {
        Task<ListResponse<Users>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);

        Task<dynamic> GetByIdAsync(long id);
        Task<Users> CreateAsync(Users data, int userId);

        Task<User> EditAsync(Users data, int userId);
        Task<bool> DeleteAsync(long id, int userId);

    }

    public class UsersService : UsersInterface
    {
        private readonly EFContext _context;

        public UsersService(EFContext context)
        {
            _context = context;
        }

        private async Task ValidateSave(Users data, Boolean isCreate)
        {
            // SAME EMAIL?
            var existing = await _context.Users.FirstOrDefaultAsync(x => x.Email == data.Email && x.ID != data.ID);
            if (existing != null)
            {
                throw new ArgumentException("Email is already in use.");
            }
            
            // EMAIL
            if (string.IsNullOrEmpty(data.Email))
            {
                throw new ArgumentException("Email must not be empty.");
            }

            // FIRST NAME
            if (string.IsNullOrEmpty(data.FirstName))
            {
                throw new ArgumentException("First name must not be empty.");
            }

            // ROLE
            if (data.RoleID <= 0)
            {
                throw new ArgumentException("Role must not be empty.");
            }

            if (isCreate)
            {
                // PASSWORD
                if (string.IsNullOrEmpty(data.Password))
                {
                    throw new ArgumentException("Password must not be empty");
                }
            }

             // PASSWORD
            if (!string.IsNullOrEmpty(data.Password) && data.Password != data.ConfirmPassword)
            {
                throw new ArgumentException("Please confirm your password correctly.");
            }
        }

        public async Task<ListResponse<Users>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                //Get data from context Orders join to Users
                //Users join to Customers 
                var query = from a in _context.Users
                            join b in _context.Role on a.RoleID equals b.ID into RoleJoin
                            from b in RoleJoin.DefaultIfEmpty()
                            where a.IsDeleted == false && a.RoleID != 9 // Reseller
                            select new { User = a, Role = b };

                var dateBetween = "";

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.User.Name.Contains(search)
                        || x.User.Email.Contains(search)
                        || x.User.FirstName.Contains(search)
                        || x.User.LastName.Contains(search)
                        || x.Role.Name.Contains(search)
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
                            if (fieldName == "transdate") dateBetween = Convert.ToString(value);
                            query = fieldName switch
                            {
                                "username" => query.Where(x => x.User.Name.ToString().Equals(value)),
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
                            "id" => query.OrderByDescending(x => x.User.ID),
                            "username" => query.OrderByDescending(x => x.User.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "id" => query.OrderByDescending(x => x.User.ID),
                            "username" => query.OrderByDescending(x => x.User.Name),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.User.ID);
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
                    return new Users
                    {
                        ID = x.User.ID,
                        RefID = x.User.RefID,

                        FirstName = x.User.FirstName,
                        LastName = x.User.LastName,

                        Email = x.User.Email,

                        RoleID = x.User.RoleID,
                        RoleName = x.Role?.Name ?? ""
                    };
                })
                .ToList();

                return new ListResponse<Users>(resData, total, page);
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
                var data = await _context.Users
                .Where(x => x.ID == id && x.IsDeleted == false)
                .Join(_context.Role,
                    us => us.RoleID,
                    r => r.ID,
                    (us, r) => new
                    {
                        ID = us.ID,
                        FirstName = us.FirstName,
                        LastName = us.LastName,
                        RoleID = us.RoleID,
                        Email = us.Email,

                        CompanyID = us.CompanyID,
                        RoleName = r.Name
                    })
                .FirstOrDefaultAsync();

                if (data == null) return null;

                var resData = new
                {
                    ID = data.ID,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    Email = data.Email,

                    CompanyID = string.IsNullOrEmpty(data.CompanyID)
                        ? new int[] { }
                        : data.CompanyID.Split(',').Select(int.Parse).ToArray(),

                    RoleID = data.RoleID,
                    RoleName = data.RoleName
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

        public async Task<Users> CreateAsync(Users data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload user from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await ValidateSave(data, true);

                var tempData = new User
                {
                    ID = 0,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    Name = $"{data.FirstName} {data.LastName}",

                    CompanyID = string.Join(",", data.CompanyID),
                    RoleID = data.RoleID,

                    Email = data.Email,
                    Password = Utility.HashPassword(data.Password),

                    UserIn = userId,
                    DateIn = Utility.getCurrentTimestamps()
                };

                await _context.Users.AddAsync(tempData);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data user, payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");
                throw;
            }
        }

        public async Task<User> EditAsync(Users data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"Edit user with request = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await ValidateSave(data, false);

                var obj = _context.Users
                    .Where(i => i.ID == data.ID)
                    .FirstOrDefault();

                if (obj != null)
                {
                    obj.FirstName = data.FirstName;
                    obj.LastName = data.LastName;
                    obj.Name = $"{data.FirstName} {data.LastName}";

                    obj.CompanyID = string.Join(",", data.CompanyID);
                    obj.RoleID = data.RoleID;
                    obj.Email = data.Email;

                    if (!string.IsNullOrEmpty(data.Password))
                    {
                        obj.Password = Utility.HashPassword(data.Password);
                    }

                    obj.DateUp = Utility.getCurrentTimestamps();
                    obj.UserUp = userId;
                }

                _context.Users.Update(obj);
                await _context.SaveChangesAsync();

                await Utility.AfterSave(_context, "User", obj.ID, "Edit");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"User edited successfully with ID = {obj.ID}");

                await dbTrans.CommitAsync();

                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"Error editing user, request = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();

                throw;
            }
        }
        
        public async Task<bool> DeleteAsync(long id, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                var obj = await _context.Users.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.DateUp = Utility.getCurrentTimestamps();
                obj.UserUp = userId;

                await _context.SaveChangesAsync();
                await Utility.AfterSave(_context, "User", id, "Delete");
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