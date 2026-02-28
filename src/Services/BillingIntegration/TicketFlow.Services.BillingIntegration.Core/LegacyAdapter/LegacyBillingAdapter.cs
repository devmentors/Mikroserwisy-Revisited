using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TicketFlow.Services.BillingIntegration.Core.Models;
using TicketFlow.Services.BillingIntegration.Core.Repositories;

namespace TicketFlow.Services.BillingIntegration.Core.LegacyAdapter;

public sealed class LegacyBillingAdapter : IInvoiceRepository, IPaymentStandingRepository
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LegacyBillingAdapter> _logger;

    public LegacyBillingAdapter(
        HttpClient httpClient,
        ILogger<LegacyBillingAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Invoice?> GetByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching invoice {InvoiceId} from legacy system", invoiceId);

        try
        {
            var soapRequest = CreateSoapRequest(invoiceId);

            var response = await _httpClient.PostAsync(
                "/soap/billing/GetInvoice",
                new StringContent(soapRequest, Encoding.UTF8, "text/xml"),
                cancellationToken);

            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

            if (responseXml.Contains("<soap:Fault>"))
            {
                var faultMessage = ExtractSoapFaultMessage(responseXml);
                _logger.LogError("SOAP Fault: {FaultMessage}", faultMessage);
                throw new InvalidOperationException($"Legacy billing system error: {faultMessage}");
            }

            var legacyInvoice = ParseSoapResponse(responseXml);
            var cleanInvoice = TranslateFromLegacy(legacyInvoice);

            _logger.LogInformation("Successfully fetched invoice {InvoiceId}", invoiceId);

            return cleanInvoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<CustomerPaymentStanding> GetByDomainAsync(
        string domain, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking payment standing for {Domain}", domain);

        try
        {
            var soapRequest = CreateCustomerInvoicesSoapRequest(domain);

            var response = await _httpClient.PostAsync(
                "/soap/billing/GetCustomerInvoices",
                new StringContent(soapRequest, Encoding.UTF8, "text/xml"),
                cancellationToken);

            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

            if (responseXml.Contains("<soap:Fault>"))
            {
                var faultMessage = ExtractSoapFaultMessage(responseXml);
                _logger.LogError("SOAP Fault: {FaultMessage}", faultMessage);
                throw new InvalidOperationException($"Legacy billing system error: {faultMessage}");
            }

            var legacyInvoices = ParseCustomerInvoicesSoapResponse(responseXml);
            var paymentStanding = TranslateToPaymentStanding(domain, legacyInvoices);

            _logger.LogInformation(
                "Payment standing for {Domain}: InGoodStanding={Good}, DaysOverdue={Days}, OverdueAmount={Amount}",
                domain,
                paymentStanding.InGoodStanding,
                paymentStanding.DaysOverdue,
                paymentStanding.OverdueAmount);

            return paymentStanding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check payment standing for {Domain}", domain);
            throw;
        }
    }

    private CustomerPaymentStanding TranslateToPaymentStanding(
        string domain, List<LegacySoapInvoiceDto> legacyInvoices)
    {
        var overdueInvoices = legacyInvoices
            .Where(inv => inv.STS_CD == "O")
            .ToList();

        if (overdueInvoices.Count == 0)
        {
            return new CustomerPaymentStanding(
                Domain: domain,
                InGoodStanding: true,
                DaysOverdue: null,
                OverdueAmount: null,
                OverdueInvoiceCount: 0);
        }

        var maxDaysOverdue = overdueInvoices
            .Select(inv => CalculateDaysOverdue(inv.DT_DUE))
            .Max();

        var totalOverdue = overdueInvoices
            .Sum(inv => ParseDecimal(inv.AMT_TOT));

        var inGoodStanding = maxDaysOverdue < 30;

        return new CustomerPaymentStanding(
            Domain: domain,
            InGoodStanding: inGoodStanding,
            DaysOverdue: maxDaysOverdue,
            OverdueAmount: totalOverdue,
            OverdueInvoiceCount: overdueInvoices.Count);
    }

    private int CalculateDaysOverdue(string legacyDueDate)
    {
        var dueDate = ParseLegacyDate(legacyDueDate);
        var daysOverdue = (int)(DateTimeOffset.UtcNow - dueDate).TotalDays;
        return Math.Max(0, daysOverdue);
    }

    private string CreateCustomerInvoicesSoapRequest(string domain)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetCustomerInvoicesRequest xmlns=""http://legacy.billing.corp/2005"">
      <DOMAIN>{domain}</DOMAIN>
    </GetCustomerInvoicesRequest>
  </soap:Body>
</soap:Envelope>";
    }

    private List<LegacySoapInvoiceDto> ParseCustomerInvoicesSoapResponse(string soapXml)
    {
        var doc = XDocument.Parse(soapXml);
        var ns = XNamespace.Get("http://legacy.billing.corp/2005");

        var invoicesElement = doc.Descendants(ns + "INVOICES").FirstOrDefault();
        if (invoicesElement is null)
        {
            return new List<LegacySoapInvoiceDto>();
        }

        return invoicesElement.Elements("INVOICE")
            .Select(inv => new LegacySoapInvoiceDto(
                INV_ID: inv.Element("INV_ID")?.Value ?? "",
                CUST_REF: inv.Element("CUST_REF")?.Value ?? "",
                INV_NUM: inv.Element("INV_NUM")?.Value ?? "",
                AMT_TOT: inv.Element("AMT_TOT")?.Value ?? "0",
                AMT_CURR: inv.Element("AMT_CURR")?.Value ?? "USD",
                STS_CD: inv.Element("STS_CD")?.Value ?? "",
                DT_ISS: inv.Element("DT_ISS")?.Value ?? "",
                DT_DUE: inv.Element("DT_DUE")?.Value ?? "",
                DT_PD: inv.Element("DT_PD")?.Value ?? "",
                LINE_ITEMS: new List<LegacyLineItemDto>()
            ))
            .ToList();
    }

    private string CreateSoapRequest(string invoiceId)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetInvoiceRequest xmlns=""http://legacy.billing.corp/2005"">
      <INV_ID>{invoiceId}</INV_ID>
    </GetInvoiceRequest>
  </soap:Body>
</soap:Envelope>";
    }

    private LegacySoapInvoiceDto ParseSoapResponse(string soapXml)
    {
        var doc = XDocument.Parse(soapXml);
        var ns = XNamespace.Get("http://legacy.billing.corp/2005");

        var invoiceElement = doc.Descendants(ns + "Invoice").FirstOrDefault()
            ?? throw new InvalidOperationException("Invoice element not found in SOAP response");

        var lineItemsElement = invoiceElement.Element("LINE_ITEMS");
        var lineItems = lineItemsElement?.Elements("ITEM")
            .Select(item => new LegacyLineItemDto(
                ITM_DESC: item.Element("ITM_DESC")?.Value ?? "",
                QTY: item.Element("QTY")?.Value ?? "0",
                PRC_UNIT: item.Element("PRC_UNIT")?.Value ?? "0",
                AMT_LN: item.Element("AMT_LN")?.Value ?? "0"
            )).ToList() ?? new List<LegacyLineItemDto>();

        return new LegacySoapInvoiceDto(
            INV_ID: invoiceElement.Element("INV_ID")?.Value ?? "",
            CUST_REF: invoiceElement.Element("CUST_REF")?.Value ?? "",
            INV_NUM: invoiceElement.Element("INV_NUM")?.Value ?? "",
            AMT_TOT: invoiceElement.Element("AMT_TOT")?.Value ?? "0",
            AMT_CURR: invoiceElement.Element("AMT_CURR")?.Value ?? "USD",
            STS_CD: invoiceElement.Element("STS_CD")?.Value ?? "",
            DT_ISS: invoiceElement.Element("DT_ISS")?.Value ?? "",
            DT_DUE: invoiceElement.Element("DT_DUE")?.Value ?? "",
            DT_PD: invoiceElement.Element("DT_PD")?.Value ?? "",
            LINE_ITEMS: lineItems
        );
    }

    private Invoice TranslateFromLegacy(LegacySoapInvoiceDto legacy)
    {
        return new Invoice(
            Id: legacy.INV_ID,
            CustomerReference: legacy.CUST_REF,
            InvoiceNumber: legacy.INV_NUM,
            TotalAmount: ParseDecimal(legacy.AMT_TOT),
            Currency: legacy.AMT_CURR,
            Status: TranslateStatus(legacy.STS_CD),
            IssueDate: ParseLegacyDate(legacy.DT_ISS),
            DueDate: ParseLegacyDate(legacy.DT_DUE),
            PaidDate: string.IsNullOrEmpty(legacy.DT_PD)
                ? null
                : ParseLegacyDate(legacy.DT_PD),
            LineItems: legacy.LINE_ITEMS.Select(TranslateLineItem).ToList()
        );
    }

    private InvoiceLineItem TranslateLineItem(LegacyLineItemDto legacy)
    {
        return new InvoiceLineItem(
            Description: legacy.ITM_DESC,
            Quantity: int.Parse(legacy.QTY),
            UnitPrice: ParseDecimal(legacy.PRC_UNIT),
            LineAmount: ParseDecimal(legacy.AMT_LN)
        );
    }

    private InvoiceStatus TranslateStatus(string statusCode) => statusCode switch
    {
        "P" => InvoiceStatus.Paid,
        "U" => InvoiceStatus.Unpaid,
        "O" => InvoiceStatus.Overdue,
        "C" => InvoiceStatus.Cancelled,
        _ => throw new InvalidOperationException($"Unknown status code: {statusCode}")
    };

    private DateTimeOffset ParseLegacyDate(string legacyDate)
    {
        if (string.IsNullOrEmpty(legacyDate) || legacyDate.Length != 8)
        {
            throw new FormatException($"Invalid date format: {legacyDate}. Expected YYYYMMDD.");
        }

        return DateTimeOffset.ParseExact(
            legacyDate,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);
    }

    private decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    private string ExtractSoapFaultMessage(string soapXml)
    {
        try
        {
            var doc = XDocument.Parse(soapXml);
            var faultString = doc.Descendants(XName.Get("faultstring", "http://schemas.xmlsoap.org/soap/envelope/"))
                .FirstOrDefault()?.Value;

            return faultString ?? "Unknown SOAP Fault";
        }
        catch
        {
            return "Failed to parse SOAP Fault";
        }
    }
}

internal record LegacySoapInvoiceDto(
    string INV_ID,
    string CUST_REF,
    string INV_NUM,
    string AMT_TOT,
    string AMT_CURR,
    string STS_CD,
    string DT_ISS,
    string DT_DUE,
    string DT_PD,
    List<LegacyLineItemDto> LINE_ITEMS);

internal record LegacyLineItemDto(
    string ITM_DESC,
    string QTY,
    string PRC_UNIT,
    string AMT_LN);
