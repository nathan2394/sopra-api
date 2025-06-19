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
        Task<ListResponse<dynamic>> GetAllAsync(long objectId, long moduleId);
    }

    public class UserLogService : UserLogInterface
    {
        private readonly EFContext _context;

        public UserLogService(EFContext context)
        {
            _context = context;
        }
        
        public async Task<ListResponse<dynamic>> GetAllAsync(long objectId, long moduleId)
        {
            try
            {
                var data = await _context.UserLogs
                .Where(x => x.ObjectID == objectId &&
                    x.ModuleID == moduleId &&
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
    }
}