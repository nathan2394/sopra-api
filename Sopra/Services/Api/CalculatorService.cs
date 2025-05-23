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
    public class CalculatorService : IServiceAsync<Calculator>
    {
        private readonly EFContext _context;

        public CalculatorService(EFContext context)
        {
            _context = context;
        }

        public async Task<Calculator> CreateAsync(Calculator data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Calculators.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Calculator", data.ID, "Add");

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
                var obj = await _context.Calculators.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Calculator", id, "Delete");

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

        public async Task<Calculator> EditAsync(Calculator data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Calculators.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.Name = data.Name;
                obj.NewProd = data.NewProd;
                obj.FavProd = data.FavProd;
                obj.NewProdDate = data.NewProdDate;
                obj.Stock = data.Stock;
                obj.Price = data.Price;
                obj.Weight = data.Weight;
                obj.Image = data.Image;
                obj.CategoriesID = data.CategoriesID;
                obj.Status = data.Status;
                obj.ColorsID = data.ColorsID;
                obj.MaterialsID = data.MaterialsID;
                obj.Height = data.Height;
                obj.Length = data.Length;
                obj.Volume = data.Volume;
                obj.DailyOutput = data.DailyOutput;
                obj.Code = data.Code;
                obj.QtyPack = data.QtyPack;
                obj.TotalViews = data.TotalViews;
                obj.TotalShared = data.TotalShared;
                obj.ClosuresID = data.ClosuresID;
                obj.ShapesID = data.ShapesID;
                obj.NecksID = data.NecksID;
                obj.Width = data.Width;
                obj.PackagingsID = data.PackagingsID;
                obj.Note = data.Note;
                obj.Microwable = data.Microwable;
                obj.LessThan60 = data.LessThan60;
                obj.LeakProof = data.LeakProof;
                obj.TamperEvident = data.TamperEvident;
                obj.AirTight = data.AirTight;
                obj.BreakResistant = data.BreakResistant;
                obj.SpillProof = data.SpillProof;
                obj.Plug = data.Plug;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Calculator", data.ID, "Edit");

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


        public async Task<ListResponse<Calculator>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Calculators where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.Name.Contains(search)
                        || x.NewProd.ToString().Contains(search)
                        || x.FavProd.ToString().Contains(search)
                        || x.NewProdDate.ToString().Contains(search)
                        || x.Stock.ToString().Contains(search)
                        || x.Price.ToString().Contains(search)
                        || x.Weight.ToString().Contains(search)
                        || x.Image.Contains(search)
                        || x.CategoriesID.ToString().Contains(search)
                        || x.Status.ToString().Contains(search)
                        || x.ColorsID.ToString().Contains(search)
                        || x.MaterialsID.ToString().Contains(search)
                        || x.Height.ToString().Contains(search)
                        || x.Length.ToString().Contains(search)
                        || x.Volume.ToString().Contains(search)
                        || x.Code.Contains(search)
                        || x.QtyPack.ToString().Contains(search)
                        || x.TotalViews.ToString().Contains(search)
                        || x.TotalShared.ToString().Contains(search)
                        || x.ClosuresID.ToString().Contains(search)
                        || x.ShapesID.ToString().Contains(search)
                        || x.NecksID.ToString().Contains(search)
                        || x.Width.ToString().Contains(search)
                        || x.PackagingsID.ToString().Contains(search)
                        || x.Note.Contains(search)
                        || x.Microwable.ToString().Contains(search)
                        || x.LessThan60.ToString().Contains(search)
                        || x.LeakProof.ToString().Contains(search)
                        || x.TamperEvident.ToString().Contains(search)
                        || x.AirTight.ToString().Contains(search)
                        || x.BreakResistant.ToString().Contains(search)
                        || x.SpillProof.ToString().Contains(search)
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
                            query = fieldName switch
                            {
                                "refid" => query.Where(x => x.RefID.ToString().Contains(value)),
                                "name" => query.Where(x => x.Name.Contains(value)),
                                "newprod" => query = query.Where(x => x.NewProd.ToString().Contains(value)),
                                "favprod" => query = query.Where(x => x.FavProd.ToString().Contains(value)),
                                "newproddate" => query = query.Where(x => x.NewProdDate.ToString().Contains(value)),
                                "stock" => query = query.Where(x => x.Stock.ToString().Contains(value)),
                                "price" => query = query.Where(x => x.Price.ToString().Contains(value)),
                                "weight" => query = query.Where(x => x.Weight.ToString().Contains(value)),
                                "image" => query = query.Where(x => x.Image.Contains(value)),
                                "categoriesid" => query = query.Where(x => x.CategoriesID.ToString().Contains(value)),
                                "status" => query = query.Where(x => x.Status.ToString().Contains(value)),
                                "colorsid" => query = query.Where(x => x.ColorsID.ToString().Contains(value)),
                                "materialsid" => query = query.Where(x => x.MaterialsID.ToString().Contains(value)),
                                "height" => query = query.Where(x => x.Height.ToString().Contains(value)),
                                "length" => query = query.Where(x => x.Length.ToString().Contains(value)),
                                "volume" => query = query.Where(x => x.Volume.ToString().Contains(value)),
                                "code" => query = query.Where(x => x.Code.Contains(value)),
                                "qtypack" => query = query.Where(x => x.QtyPack.ToString().Contains(value)),
                                "totalviews" => query = query.Where(x => x.TotalViews.ToString().Contains(value)),
                                "totalshared" => query = query.Where(x => x.TotalShared.ToString().Contains(value)),
                                "closureid" => query = query.Where(x => x.ClosuresID.ToString().Contains(value)),
                                "shapesid" => query = query.Where(x => x.ShapesID.ToString().Contains(value)),
                                "necksid" => query = query.Where(x => x.NecksID.ToString().Contains(value)),
                                "width" => query = query.Where(x => x.Width.ToString().Contains(value)),
                                "packagingsid" => query = query.Where(x => x.PackagingsID.ToString().Contains(value)),
                                "note" => query = query.Where(x => x.Note.Contains(value)),
								"microwable" => query = query.Where(x => x.Microwable.ToString().Contains(value)),
								"lessthan60" => query = query.Where(x => x.LessThan60.ToString().Contains(value)),
								"leakproof" => query = query.Where(x => x.LeakProof.ToString().Contains(value)),
								"tamperevident" => query = query.Where(x => x.TamperEvident.ToString().Contains(value)),
								"airtight" => query = query.Where(x => x.AirTight.ToString().Contains(value)),
								"breakresistant" => query = query.Where(x => x.BreakResistant.ToString().Contains(value)),
								"spillproof" => query = query.Where(x => x.SpillProof.ToString().Contains(value)),
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
                            "refid" => query.OrderByDescending(x => x.RefID),
                            "name" => query.OrderByDescending(x => x.Name),
                            "newprod" => query = query.OrderByDescending(x => x.NewProd),
                            "favprod" => query = query.OrderByDescending(x => x.FavProd),
                            "newprodDate" => query = query.OrderByDescending(x => x.NewProdDate),
                            "stock" => query = query.OrderByDescending(x => x.Stock),
                            "price" => query = query.OrderByDescending(x => x.Price),
                            "weight" => query = query.OrderByDescending(x => x.Weight),
                            "image" => query = query.OrderByDescending(x => x.Image),
                            "categoriesid" => query = query.OrderByDescending(x => x.CategoriesID),
                            "status" => query = query.OrderByDescending(x => x.Status),
                            "colorsid" => query = query.OrderByDescending(x => x.ColorsID),
                            "materialsid" => query = query.OrderByDescending(x => x.MaterialsID),
                            "height" => query = query.OrderByDescending(x => x.Height),
                            "length" => query = query.OrderByDescending(x => x.Length),
                            "volume" => query = query.OrderByDescending(x => x.Volume),
                            "code" => query = query.OrderByDescending(x => x.Code),
                            "qtypack" => query = query.OrderByDescending(x => x.QtyPack),
                            "totalviews" => query = query.OrderByDescending(x => x.TotalViews),
                            "totalshared" => query = query.OrderByDescending(x => x.TotalShared),
                            "closuresid" => query = query.OrderByDescending(x => x.ClosuresID),
                            "shapesid" => query = query.OrderByDescending(x => x.ShapesID),
                            "necksid" => query = query.OrderByDescending(x => x.NecksID),
                            "width" => query = query.OrderByDescending(x => x.Width),
                            "packagingsid" => query = query.OrderByDescending(x => x.PackagingsID),
                            "note" => query = query.OrderByDescending(x => x.Note),
							"microwable" => query = query.OrderByDescending(x => x.Microwable),
							"lessthan60" => query = query.OrderByDescending(x => x.LessThan60),
							"leakproof" => query = query.OrderByDescending(x => x.LeakProof),
							"tamperevident" => query = query.OrderByDescending(x => x.TamperEvident),
							"airtight" => query = query.OrderByDescending(x => x.AirTight),
							"breakresistant" => query = query.OrderByDescending(x => x.BreakResistant),
							"spillproof" => query = query.OrderByDescending(x => x.SpillProof),
							_ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "name" => query.OrderBy(x => x.Name),
                            "newprod" => query = query.OrderBy(x => x.NewProd),
                            "favprod" => query = query.OrderBy(x => x.FavProd),
                            "newprodDate" => query = query.OrderBy(x => x.NewProdDate),
                            "stock" => query = query.OrderBy(x => x.Stock),
                            "price" => query = query.OrderBy(x => x.Price),
                            "weight" => query = query.OrderBy(x => x.Weight),
                            "image" => query = query.OrderBy(x => x.Image),
                            "categoriesid" => query = query.OrderBy(x => x.CategoriesID),
                            "status" => query = query.OrderBy(x => x.Status),
                            "colorsid" => query = query.OrderBy(x => x.ColorsID),
                            "materialsid" => query = query.OrderBy(x => x.MaterialsID),
                            "height" => query = query.OrderBy(x => x.Height),
                            "length" => query = query.OrderBy(x => x.Length),
                            "volume" => query = query.OrderBy(x => x.Volume),
                            "code" => query = query.OrderBy(x => x.Code),
                            "qtypack" => query = query.OrderBy(x => x.QtyPack),
                            "totalviews" => query = query.OrderBy(x => x.TotalViews),
                            "totalshared" => query = query.OrderBy(x => x.TotalShared),
                            "closuresid" => query = query.OrderBy(x => x.ClosuresID),
                            "shapesid" => query = query.OrderBy(x => x.ShapesID),
                            "necksid" => query = query.OrderBy(x => x.NecksID),
                            "width" => query = query.OrderBy(x => x.Width),
                            "packagingsid" => query = query.OrderBy(x => x.PackagingsID),
                            "note" => query = query.OrderBy(x => x.Note),
							"microwable" => query = query.OrderBy(x => x.Microwable),
							"lessthan60" => query = query.OrderBy(x => x.LessThan60),
							"leakproof" => query = query.OrderBy(x => x.LeakProof),
							"tamperevident" => query = query.OrderBy(x => x.TamperEvident),
							"airtight" => query = query.OrderBy (x => x.AirTight),
							"breakresistant" => query = query.OrderBy(x => x.BreakResistant),
							"spillproof" => query = query.OrderBy(x => x.SpillProof),
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

                return new ListResponse<Calculator>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Calculator> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Calculators.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Calculator> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<Calculator> GetAccIdAsync(long id, long customerid)
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

