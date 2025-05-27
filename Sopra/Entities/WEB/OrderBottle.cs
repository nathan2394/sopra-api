using System;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class OrderBottleDto
    {
        public long ID { get; set; }
        public long? RefID { get; set; }
        public string VoucherNo { get; set; }
        public DateTime? TransDate { get; set; }
        public string ReferenceNo { get; set; }
        public long CustomerId { get; set; }
        public long CompanyId { get; set; }
        public string VouchersID { get; set; }
        public decimal? Disc2 { get; set; }
        public decimal? Disc2Value { get; set; }
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
        public decimal? Sfee { get; set; }
        public List<RegulerItem> RegulerItems { get; set; }
        public List<MixItem> MixItems { get; set; }
    }
    public class ClosureItem
    {
        public long Id { get; set; }

        public string? WmsCode { get; set; }
        public long? ProductsId { get; set; }
        public string Name { get; set; }
        public decimal? Qty { get; set; }
        public decimal? QtyBox { get; set; }
        public decimal? Price { get; set; }
        public decimal? Amount { get; set; }
    }
    public class MixItem
    {
        public long Id { get; set; }
        public long PromoId { get; set; }
        public long ParentId { get; set; }
        public string? WmsCode { get; set; }
        public long? ProductsId { get; set; }
        public decimal? Qty { get; set; }
        public decimal? QtyBox { get; set; }
        public decimal? Price { get; set; }
        public decimal? Amount { get; set; }
        public string Notes { get; set; }
        public string? ApprovalStatus { get; set; }
    }
    public class RegulerItem
    {
        public long Id { get; set; }
        public string? WmsCode { get; set; }
        public long? ProductsId { get; set; }
        public string Name { get; set; }
        public decimal? Qty { get; set; }
        public decimal? QtyBox { get; set; }
        public decimal? Price { get; set; }
        public decimal? Amount { get; set; }
        public string Notes { get; set; }
        public List<ClosureItem> ClosureItems { get; set; }
    }
}