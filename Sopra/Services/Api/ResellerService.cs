using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using System.Reflection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Sopra.Services
{
	public class ResellerService : IServiceResellerAsync<Reseller>
	{
		private readonly EFContext _context;

		public ResellerService(EFContext context)
		{
			_context = context;
		}

		public async Task<ListResponse<Reseller>> GetResellerAsync(int limit, int page, int total, string search, int provinceid)
		{
			IQueryable<Reseller> query = null;
			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				query = from a in _context.Customers
						join b in _context.Provinces on a.DeliveryProvinceID equals b.RefID
						join c in _context.Regencys on b.RefID equals c.ProvincesID
						join d in _context.Districts on c.RefID equals d.RegenciesID
						where a.Status == 1
						&& a.CustomerType == 1
						&& (provinceid == 0 || a.DeliveryProvinceID == provinceid)
						group new { a, b, c, d } by a.Name into grouped
						select new Reseller
						{
							ID = grouped.First().a.ID,
							RefID = grouped.First().a.RefID,
							Name = grouped.Key,
							Address = grouped.First().a.DeliveryAddress,
							Mobile = grouped.First().a.Mobile1,
							ProvinceID = grouped.First().a.DeliveryProvinceID,
							Province = grouped.First().b.Name,
							Regency = grouped.First().c.Name,
							District = grouped.First().d.Name,
						};

				// Searching
				if (!string.IsNullOrEmpty(search))
					query = query.Where(x => x.RefID.ToString().Contains(search)
						|| x.Name.Contains(search)
						|| x.Address.Contains(search)
						|| x.Mobile.Contains(search)
						|| x.Province.Contains(search)
						|| x.Regency.Contains(search)
						|| x.District.Contains(search)
						);

				// Get Total Before Limit and Page
				total = await query.CountAsync();

				// Set Limit and Page
				if (limit != 0)
					query = query.Skip(page * limit).Take(limit);

				// Get Data
				var data = await query.ToListAsync();

				if (provinceid != 0)
				{
					data = data.OrderBy(p => p.Name)
						.ToList();
				}
				else
				{
					data = data.OrderBy(p => p.Province).ToList();
				}

				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetResellerAsync(limit, page, total, search, provinceid);
				}

				return new ListResponse<Reseller>(data, total, page);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<Reseller> GetByIdAsync(long id)
		{
			try
			{
				//return await _context.Resellers.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id);
			var	reseller = await (from a in _context.Customers
						join b in _context.Provinces on a.DeliveryProvinceID equals b.RefID
						join c in _context.Regencys on b.RefID equals c.ProvincesID
						join d in _context.Districts on c.RefID equals d.RegenciesID
						where a.Status == 1
						&& a.CustomerType == 1
						&& a.ID == id
						group new { a, b, c, d } by a.Name into grouped
						select new Reseller
						{
							ID = grouped.First().a.ID,
							RefID = grouped.First().a.RefID,
							Name = grouped.Key,
							Address = grouped.First().a.DeliveryAddress,
							Mobile = grouped.First().a.Mobile1,
							ProvinceID = grouped.First().a.DeliveryProvinceID,
							Province = grouped.First().b.Name,
							Regency = grouped.First().c.Name,
							District = grouped.First().d.Name,
						}).FirstOrDefaultAsync();

				return reseller;
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

