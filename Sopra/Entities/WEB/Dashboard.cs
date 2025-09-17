using System;
using System.Collections.Generic;

namespace Sopra.Entities
{
    public class CountOverview
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public long Amount { get; set; }
        public long Count { get; set; }
        public string Color { get; set; }
        public string Unit { get; set; }
    }

    public class PendingOrder
    {
        public long OrderID { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public long Amount { get; set; }
        public string HandleBy { get; set; }
    }

    public class OngoingInvoice
    {
        public long InvoiceID { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; }
        public long Amount { get; set; }
        public string HandleBy { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class PaidOrder
    {
        public long OrderID { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public long InvoiceID { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public long PaymentID { get; set; }
        public string PaymentNo { get; set; }
        public DateTime PaymentDate { get; set; }
        public string CustomerName { get; set; }
        public long Amount { get; set; }
        public string HandleBy { get; set; }
    }

    public class CanceledTransaction
    {
        public long OrderID { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public long Amount { get; set; }
        public string CancelBy { get; set; }
        public DateTime CancelDate { get; set; }
        public string Reason { get; set; }
    }

    public class TopCustomer
    {
        public string CustomerName { get; set; }
        public long Amount { get; set; }
    }

    public class TopProduct
    {
        public string ProductName { get; set; }
        public long Quantity { get; set; }
    }
}