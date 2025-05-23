using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Orders")]
    public class Order : Entity
    {
        public long? RefID { get; set; }
        public string? OrderNo { get; set; }
        public DateTime? TransDate { get; set; }
        public long? CustomersID { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Other { get; set; }
        public decimal? Amount { get; set; }
        public string? Status { get; set; }
        public string? VouchersID { get; set; }
        public decimal? Disc1 { get; set; }
        public decimal? Disc1Value { get; set; }
        public decimal? Disc2 { get; set; }
        public decimal? Disc2Value { get; set; }
        public decimal? Sfee { get; set; }
        public decimal? DPP { get; set; }
        public decimal? TAX { get; set; }
        public decimal? TaxValue { get; set; }
        public decimal? Total { get; set; }
        public string? Departure { get; set; }
        public string? Arrival { get; set; }
        public long? WarehouseID { get; set; }
        public long? CountriesID { get; set; }
        public long? ProvincesID { get; set; }
        public long? RegenciesID { get; set; }
        public long? DistrictsID { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public long? TransportsID { get; set; }
        public decimal? TotalTransport { get; set; }
        public decimal? TotalTransportCapacity { get; set; }
        public decimal? TotalOrderCapacity { get; set; }
        public decimal? TotalOrderWeight { get; set; }
        public decimal? TotalTransportCost { get; set; }
        public decimal? RemainingCapacity { get; set; }
        public long? ReasonsID { get; set; }
        public string? OrderStatus { get; set; }
        public long? ExpeditionsID { get; set; }
        public decimal? BiayaPickup { get; set; }
        public long? CheckInvoice { get; set; }
        public DateTime? InvoicedDate { get; set; }
        public decimal? TotalReguler { get; set; }
        public decimal? TotalJumbo { get; set; }
        public decimal? TotalMix { get; set; }
        public decimal? TotalNewPromo { get; set; }
        public decimal? TotalSupersale { get; set; }
        public string? ValidTime { get; set; }
        public string? DealerTier { get; set; }
        public long? DeliveryStatus { get; set; }
        public long? PartialDeliveryStatus { get; set; }
        public string? PaymentTerm { get; set; }
        public string? Validity { get; set; }
        public long? IsVirtual { get; set; }
        public string? VirtualAccount { get; set; }
        public long? BanksID { get; set; }
        public string? ShipmentNum { get; set; }
        public DateTime? PaidDate { get; set; }
        public long? RecreateOrderStatus { get; set; }
        public long? CompaniesID { get; set; }
        public decimal? AmountTotal { get; set; }
        public long? FutureDateStatus { get; set; }
        public long? ChangeExpeditionStatus { get; set; }
        public long? ChangetruckStatus { get; set; }
        public long? SubscriptionCount { get; set; }
        public long? SubscriptionStatus { get; set; }
        public DateTime? SubscriptionDate { get; set; }
        public string? Username { get; set; }
        public string? UsernameCancel { get; set; }
        public long? SessionID { get; set; }
        public DateTime? SessionDate { get; set; }
        [NotMapped]
        public string? isIntegration { get; set; }
        //[NotMapped]
        public List<OrderDetail> OrderDetail{ get; set; }
        public string? ExternalOrderNo { get; set; }
        public string? DirectPayment { get; set; }
        public long? CartsID { get; set; }
        public string? Type { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Label { get; set; }
        public string? Landmark { get; set; }
        public bool? IsExpress { get; set; }
        [NotMapped]
        public Reason? Reason{ get; set; }
    }
}
