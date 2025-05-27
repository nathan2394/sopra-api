using System.Data;
using System.Linq;

using Sopra.Helpers;
using System.Threading.Tasks;
using Sopra.Entities;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
namespace Sopra.Services
{
    public class orderBottleRepository
    {
        private readonly EFContext _context;

        public orderBottleRepository(EFContext context)
        {
            _context = context;
        }
        
        public async Task<Order> CreateOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task AddOrderDetailsAsync(List<OrderDetail> details)
        {
            await _context.OrderDetails.AddRangeAsync(details);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<long> GetLastOrderIdAsync()
        {
            var lastId = await _context.Orders
                .OrderByDescending(x => x.ID)
                .Select(x => x.ID)
                .FirstOrDefaultAsync();

            return lastId;
        }
    }
}