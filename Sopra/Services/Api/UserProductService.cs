using Microsoft.EntityFrameworkCore;
using Sopra.Entities;
using Sopra.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Services
{
    public interface UserProductInterface
    {
        Task<List<object>> GetQtyCart(long customerId);
        Task<List<object>> GetWishlist(long customerId);
    }
    public class UserProductService : UserProductInterface
    {
        private readonly EFContext _context;

        public UserProductService(EFContext context)
        {
            _context = context;
        }
        public async Task<List<object>> GetQtyCart(long customerId)
        {
            var data = await (
                        from product_detail in _context.ProductDetails2
                        let qtyCart = (from c in _context.Carts
                                       join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
                                       from carts_detail in carts.DefaultIfEmpty()
                                       where c.CustomersID == customerId
                                           && carts_detail.ObjectID == product_detail.RefID
                                       && carts_detail.Type == (product_detail.Type == "closure" ? "closures" : product_detail.Type)
                                       && carts_detail.IsDeleted == false
                                       select carts_detail.Qty
                                                            ).Sum()
                        where qtyCart != 0
                        select new
                        {
                            Product = new Product
                            {
                                RefID = product_detail.RefID,
                                Name = product_detail.Name,
                                QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart)
                            }
                        }).ToListAsync<object>();
            if (data != null) return data;
            else return null;
        }

        public async Task<List<object>> GetWishlist(long customerId)
        {
            var data = await (
                    from  wishlist in _context.WishLists
                    where wishlist.UserId == customerId
                    select new
                    {
                        WishList = new WishList
                        {
                            ProductId = wishlist.ProductId,
                        }
                    }).ToListAsync<object>();
            if (data != null) return data;
            else return null;
        }
    }
}
