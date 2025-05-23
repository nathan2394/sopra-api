using System;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class OrderBottleDto
    {
        public string VoucherNo { get; set; }
        public DateTime? TransDate { get; set; }
        public string ReferenceNo { get; set; }
        public long CustomerId { get; set; }
        public long CompanyId { get; set; }
        public string Voucher { get; set; }
        public string CreatedBy { get; set; }
        public string OrderStatus { get; set; }
        public string DiscStatus { get; set; }
        public decimal TotalReguler { get; set; }
        public decimal TotalMix { get; set; }
        public decimal DiscPercentage { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal Dpp { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxValue { get; set; }
        public decimal Netto { get; set; }
        public string Dealer { get; set; }
        public List<RegulerItem> RegulerItems { get; set; }
        public List<MixItem> MixItems { get; set; }
    }
    public class ClosureItem
    {
        public int Id { get; set; }
        public int ProductsId { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public int QtyBox { get; set; }
        public int Price { get; set; }
        public int Amount { get; set; }
    }
    public class MixItem
    {
        public int Id { get; set; }
        public int PromoId { get; set; }
        public int ParentId { get; set; }
        public int ProductsId { get; set; }
        public int Qty { get; set; }
        public int QtyBox { get; set; }
        public int Price { get; set; }
        public int Amount { get; set; }
        public string Notes { get; set; }
        public string? ApprovalStatus { get; set; }
    }
    public class RegulerItem
    {
        public int Id { get; set; }
        public int ProductsId { get; set; }
        public string Name { get; set; }
        public int Qty { get; set; }
        public int QtyBox { get; set; }
        public int Price { get; set; }
        public int Amount { get; set; }
        public string Notes { get; set; }
        public List<ClosureItem> ClosureItems { get; set; }
    }
}