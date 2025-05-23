using System;

namespace Sopra.Requests
{
    public class SnapBcaGetInquiry
    {
        public string partnerServiceId { get; set; }
        public string customerNo { get; set; }
        public string virtualAccountNo { get; set; }
        public DateTime trxDateInit { get; set; }
        public long channelCode { get; set; }
        public string language { get; set; }
        public object amount { get; set; }
        public string value { get; set; }
        public string currency { get; set; }
        public string hashedSourceAccountNo { get; set; }
        public string sourceBankCode { get; set; }
        public string inquiryRequestId { get; set; }
        public object additionalInfo { get; set; }
        public string passApp { get; set; }
    }
}
