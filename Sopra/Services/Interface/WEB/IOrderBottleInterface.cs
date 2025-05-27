using Sopra.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore.Storage;

namespace Sopra.Services
{
    public interface IOrderBottleRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task AddOrderDetailsAsync(List<OrderDetail> details);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task SaveChangesAsync();
        Task<int> GetLastOrderIdAsync();
    }
}