using System;
using System.Collections.Generic;

namespace Sidekick_E_Invoicing.Models
{
    public class InvoiceItem
    {
        public InvoiceItem()
        {
            fixedNotifiedValueOrRetailPrice = 0;
            salesTaxWithheldAtSource = 0;
            extraTax = "";
            fedPayable = 0;
            discount = 0;
            sroItemSerialNo = "";
            sroScheduleNo = "ICTO TABLE I";
        }

        public string hsCode { get; set; }
        public string productDescription { get; set; }
        public string rate { get; set; }
        public string uoM { get; set; }
        public decimal quantity { get; set; }
        public decimal totalValues { get; set; }
        public decimal valueSalesExcludingST { get; set; }
        public decimal fixedNotifiedValueOrRetailPrice { get; set; }
        public decimal salesTaxApplicable { get; set; }
        public decimal salesTaxWithheldAtSource { get; set; }
        public string extraTax { get; set; }
        public decimal furtherTax { get; set; }
        public decimal fedPayable { get; set; }
        public decimal discount { get; set; }
        public string saleType { get; set; }
        public string sroItemSerialNo { get; set; }
        public string sroScheduleNo { get; set; }
    }

    public class InvoiceData
    {
        public InvoiceData()
        {
            invoiceType = "Sale Invoice";
            invoiceRefNo = "";
        }

        public string invoiceType { get; set; }
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
        public string invoiceRefNo { get; set; }
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