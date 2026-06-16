using System;
using System.Collections.Generic;

namespace SDK_E_INVOICING_SYSTEM.Models
{
    public class InvoiceItem
    {
        public string hsCode { get; set; }
        public string productDescription { get; set; }
        public string rate { get; set; }
        public string uoM { get; set; }
        public decimal quantity { get; set; }
        public decimal totalValues { get; set; }
        public decimal valueSalesExcludingST { get; set; }
        public decimal fixedNotifiedValueOrRetailPrice { get; set; } = 0;
        public decimal salesTaxApplicable { get; set; }
        public decimal salesTaxWithheldAtSource { get; set; } = 0;
        public string extraTax { get; set; } = "";
        public decimal furtherTax { get; set; }
        public decimal fedPayable { get; set; } = 0;
        public decimal discount { get; set; } = 0;
        public string saleType { get; set; }
        public string sroItemSerialNo { get; set; } = "";

        public string sroScheduleNo { get; set; } = "ICTO TABLE I";
    }

    public class InvoiceData
    {
        public string invoiceType { get; set; } = "Sale Invoice";
        public string invoiceDate { get; set; }
        public string sellerNTNCNIC { get; set; }
        public string sellerBusinessName { get; set; }
        public string sellerProvince { get; set; }
        public string sellerAddress { get; set; }
        public string buyerNTNCNIC { get; set; }
        public string buyerBusinessName { get; set; }
        public string buyerProvince { get; set; }
        public string buyerAddress { get; set; }
        public string buyerRegistrationType { get; set; }
        public string invoiceRefNo { get; set; } = "";
        public string scenarioId { get; set; }
        public List<InvoiceItem> items { get; set; }
    }

    public class InvoiceStatus
    {
        public string itemSNo { get; set; }
        public string statusCode { get; set; }
        public string status { get; set; }
        public string errorCode { get; set; }
        public string error { get; set; }
    }

    public class ValidationResponse
    {
        public string statusCode { get; set; }
        public string status { get; set; }
        public string errorCode { get; set; }
        public string error { get; set; }
        public List<InvoiceStatus> invoiceStatuses { get; set; }
    }

    public class ApiResponse
    {
        public string dated { get; set; }
        public ValidationResponse validationResponse { get; set; }  // nullable if API returns empty
        public string exception { get; set; }
    }
}