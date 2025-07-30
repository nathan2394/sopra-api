using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Sopra.Helpers;
using Sopra.Responses;
using System.Data;
using Sopra.Entities;
using System.Runtime.CompilerServices;

namespace Sopra.Services
{
    public interface UserLogInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync();
        Task<ListResponse<dynamic>> GetByOrderIdAsync(long objectId, long moduleId);
    }

    public class UserLogService : UserLogInterface
    {
        private readonly EFContext _context;

        public UserLogService(EFContext context)
        {
            _context = context;
        }

        public async Task<ListResponse<dynamic>> GetAllAsync()
        {
            try
            {
                var data = await _context.UserLogs
                .Where(x => x.IsDeleted == false)
                .Join(_context.Users,
                    x => x.UserID,
                    y => y.ID,
                    (x, y) => new
                    {
                        x.ID,
                        x.ObjectID,
                        x.ModuleID,
                        x.UserID,
                        x.Description,
                        x.TransDate,
                        User = new
                        {
                            y.FirstName,
                            y.LastName,
                            FullName = y.FirstName + " " + y.LastName
                        }
                    })
                .OrderByDescending(x => x.TransDate)
                .ToListAsync();

                var dynamicData = data.Cast<dynamic>();

                return new ListResponse<dynamic>(dynamicData, data.Count, 0);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        
        public async Task<ListResponse<dynamic>> GetByOrderIdAsync(long objectId, long moduleId)
        {
            try
            {
                var orderLogs = await _context.UserLogs
                .Where(x => x.ObjectID == objectId &&
                    x.ModuleID == 1 &&
                    x.IsDeleted == false
                )
                .Join(_context.Users,
                    x => x.UserID,
                    y => y.ID,
                    (x, y) => new
                    {
                        x.ID,
                        x.ObjectID,
                        x.ModuleID,
                        x.UserID,
                        x.Description,
                        x.TransDate,
                        User = new
                        {
                            y.FirstName,
                            y.LastName,
                            FullName = y.FirstName + " " + y.LastName
                        }
                    })
                .OrderByDescending(x => x.TransDate)
                .ToListAsync();

                var invoiceLogs = await (
                    from o in _context.Orders
                    join i in _context.Invoices on o.ID equals i.OrdersID
                    join ul in _context.UserLogs on i.ID equals ul.ObjectID
                    join u in _context.Users on ul.UserID equals u.ID
                    where o.ID == objectId && ul.ModuleID == 2 && !ul.IsDeleted
                    select new
                    {
                        ul.ID,
                        ul.ObjectID,
                        ul.ModuleID,
                        ul.UserID,
                        ul.Description,
                        ul.TransDate,
                        User = new { u.FirstName, u.LastName, FullName = u.FirstName + " " + u.LastName }
                    })
                    .OrderByDescending(x => x.TransDate)
                    .ToListAsync();

                var result = new
                {
                    order = orderLogs,
                    invoice = invoiceLogs
                };
                
                return new ListResponse<dynamic>(new[] { result }, orderLogs.Count + invoiceLogs.Count, 0);
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