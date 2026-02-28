using System.Text;

namespace LegacyBillingSystem;

public static class SoapEndpoints
{
    public static void MapLegacyBillingSoapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/soap/billing");

        group.MapPost("/GetInvoice", GetInvoice)
            .WithName("GetInvoiceSoap")
            .WithTags("SOAP");

        group.MapPost("/GetCustomerInvoices", GetCustomerInvoices)
            .WithName("GetCustomerInvoicesSoap")
            .WithTags("SOAP");

        group.MapGet("/health", () => Results.Ok(new
        {
            service = "Legacy Billing System",
            status = "Running"
        }))
        .WithName("LegacyBillingHealth")
        .WithTags("Health");
    }

    private static async Task<IResult> GetInvoice(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            var invoiceId = ExtractInvoiceIdFromSoapRequest(requestBody);

            if (string.IsNullOrEmpty(invoiceId))
            {
                return CreateSoapFault("Client.InvalidRequest", "Missing INV_ID in request");
            }

            var delay = Random.Shared.Next(1000, 3000);
            await Task.Delay(delay);

            if (Random.Shared.Next(100) < 15)
            {
                return CreateSoapFault(
                    "Server.DatabaseError",
                    $"Database connection failed (Invoice: {invoiceId})"
                );
            }

            var invoice = GenerateMockInvoice(invoiceId);
            var soapResponse = SerializeToSoap(invoice);

            return Results.Content(soapResponse, "text/xml; charset=utf-8");
        }
        catch (Exception ex)
        {
            return CreateSoapFault("Server.InternalError", $"Unexpected error: {ex.Message}");
        }
    }

    private static async Task<IResult> GetCustomerInvoices(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            var domain = ExtractDomainFromSoapRequest(requestBody);

            if (string.IsNullOrEmpty(domain))
            {
                return CreateSoapFault("Client.InvalidRequest", "Missing DOMAIN in request");
            }

            var delay = Random.Shared.Next(1000, 2000);
            await Task.Delay(delay);

            if (Random.Shared.Next(100) < 10)
            {
                return CreateSoapFault(
                    "Server.DatabaseError",
                    $"Database connection failed (Domain: {domain})"
                );
            }

            var invoices = GenerateMockInvoicesForDomain(domain);
            var soapResponse = SerializeCustomerInvoicesToSoap(domain, invoices);

            return Results.Content(soapResponse, "text/xml; charset=utf-8");
        }
        catch (Exception ex)
        {
            return CreateSoapFault("Server.InternalError", $"Unexpected error: {ex.Message}");
        }
    }

    private static string ExtractDomainFromSoapRequest(string soapXml)
    {
        var startTag = "<DOMAIN>";
        var endTag = "</DOMAIN>";

        var startIndex = soapXml.IndexOf(startTag);
        if (startIndex == -1) return string.Empty;

        startIndex += startTag.Length;
        var endIndex = soapXml.IndexOf(endTag, startIndex);
        if (endIndex == -1) return string.Empty;

        return soapXml.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static List<LegacySoapInvoice> GenerateMockInvoicesForDomain(string domain)
    {
        if (domain.Equals("devmentors.io", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateDevMentorsInvoices();
        }

        if (domain.Equals("example.com", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateExampleIncInvoices();
        }

        if (domain.Equals("ticketflow.com", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateTicketFlowInvoices();
        }

        return new List<LegacySoapInvoice>();
    }

    private static List<LegacySoapInvoice> GenerateDevMentorsInvoices()
    {
        var now = DateTime.Now;

        return new List<LegacySoapInvoice>
        {
            new LegacySoapInvoice
            {
                INV_ID = "DM-2024-001",
                CUST_REF = "DEVMENTORS",
                INV_NUM = "INV-2024-DM-001",
                AMT_TOT = "2500.00",
                AMT_CURR = "USD",
                STS_CD = "P",
                DT_ISS = now.AddDays(-60).ToString("yyyyMMdd"),
                DT_DUE = now.AddDays(-30).ToString("yyyyMMdd"),
                DT_PD = now.AddDays(-35).ToString("yyyyMMdd"),
                LINE_ITEMS = new LegacyLineItems
                {
                    Items = new List<LegacyLineItem>
                    {
                        new() { ITM_DESC = "Premium Support Package", QTY = "1", PRC_UNIT = "2500.00", AMT_LN = "2500.00" }
                    }
                }
            },
            new LegacySoapInvoice
            {
                INV_ID = "DM-2024-002",
                CUST_REF = "DEVMENTORS",
                INV_NUM = "INV-2024-DM-002",
                AMT_TOT = "1200.00",
                AMT_CURR = "USD",
                STS_CD = "P",
                DT_ISS = now.AddDays(-90).ToString("yyyyMMdd"),
                DT_DUE = now.AddDays(-60).ToString("yyyyMMdd"),
                DT_PD = now.AddDays(-65).ToString("yyyyMMdd"),
                LINE_ITEMS = new LegacyLineItems
                {
                    Items = new List<LegacyLineItem>
                    {
                        new() { ITM_DESC = "API Access License Q3", QTY = "1", PRC_UNIT = "1200.00", AMT_LN = "1200.00" }
                    }
                }
            }
        };
    }

    private static List<LegacySoapInvoice> GenerateExampleIncInvoices()
    {
        var now = DateTime.Now;

        return new List<LegacySoapInvoice>
        {
            new LegacySoapInvoice
            {
                INV_ID = "EX-2024-001",
                CUST_REF = "EXAMPLE-INC",
                INV_NUM = "INV-2024-12345",
                AMT_TOT = "1250.50",
                AMT_CURR = "USD",
                STS_CD = "O",
                DT_ISS = now.AddDays(-75).ToString("yyyyMMdd"),
                DT_DUE = now.AddDays(-45).ToString("yyyyMMdd"),
                DT_PD = "",
                LINE_ITEMS = new LegacyLineItems
                {
                    Items = new List<LegacyLineItem>
                    {
                        new() { ITM_DESC = "Enterprise Support Q4", QTY = "1", PRC_UNIT = "1000.00", AMT_LN = "1000.00" },
                        new() { ITM_DESC = "Additional API Calls", QTY = "5000", PRC_UNIT = "0.05", AMT_LN = "250.50" }
                    }
                }
            },
            new LegacySoapInvoice
            {
                INV_ID = "EX-2024-002",
                CUST_REF = "EXAMPLE-INC",
                INV_NUM = "INV-2024-10001",
                AMT_TOT = "999.00",
                AMT_CURR = "USD",
                STS_CD = "P",
                DT_ISS = now.AddDays(-120).ToString("yyyyMMdd"),
                DT_DUE = now.AddDays(-90).ToString("yyyyMMdd"),
                DT_PD = now.AddDays(-95).ToString("yyyyMMdd"),
                LINE_ITEMS = new LegacyLineItems
                {
                    Items = new List<LegacyLineItem>
                    {
                        new() { ITM_DESC = "Enterprise Support Q3", QTY = "1", PRC_UNIT = "999.00", AMT_LN = "999.00" }
                    }
                }
            }
        };
    }

    private static List<LegacySoapInvoice> GenerateTicketFlowInvoices()
    {
        var now = DateTime.Now;

        return new List<LegacySoapInvoice>
        {
            new LegacySoapInvoice
            {
                INV_ID = "TF-2024-001",
                CUST_REF = "TICKETFLOW",
                INV_NUM = "INV-2024-TF-001",
                AMT_TOT = "500.00",
                AMT_CURR = "USD",
                STS_CD = "P",
                DT_ISS = now.AddDays(-45).ToString("yyyyMMdd"),
                DT_DUE = now.AddDays(-15).ToString("yyyyMMdd"),
                DT_PD = now.AddDays(-20).ToString("yyyyMMdd"),
                LINE_ITEMS = new LegacyLineItems
                {
                    Items = new List<LegacyLineItem>
                    {
                        new() { ITM_DESC = "Standard Support Plan", QTY = "1", PRC_UNIT = "500.00", AMT_LN = "500.00" }
                    }
                }
            },
            new LegacySoapInvoice
            {
                INV_ID = "TF-2024-002",
                CUST_REF = "TICKETFLOW",
                INV_NUM = "INV-2024-TF-002",
                AMT_TOT = "150.00",
                AMT_CURR = "USD",
                STS_CD = "U",
                DT_ISS = now.AddDays(-10).ToString("yyyyMMdd"),
                DT_DUE = now.AddDays(20).ToString("yyyyMMdd"),
                DT_PD = "",
                LINE_ITEMS = new LegacyLineItems
                {
                    Items = new List<LegacyLineItem>
                    {
                        new() { ITM_DESC = "Additional Users (3)", QTY = "3", PRC_UNIT = "50.00", AMT_LN = "150.00" }
                    }
                }
            }
        };
    }

    private static string SerializeCustomerInvoicesToSoap(string domain, List<LegacySoapInvoice> invoices)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
        sb.AppendLine("  <soap:Body>");
        sb.AppendLine("    <GetCustomerInvoicesResponse xmlns=\"http://legacy.billing.corp/2005\">");
        sb.AppendLine($"      <DOMAIN>{domain}</DOMAIN>");
        sb.AppendLine("      <INVOICES>");

        foreach (var invoice in invoices)
        {
            sb.AppendLine("        <INVOICE>");
            sb.AppendLine($"          <INV_ID>{invoice.INV_ID}</INV_ID>");
            sb.AppendLine($"          <CUST_REF>{invoice.CUST_REF}</CUST_REF>");
            sb.AppendLine($"          <INV_NUM>{invoice.INV_NUM}</INV_NUM>");
            sb.AppendLine($"          <AMT_TOT>{invoice.AMT_TOT}</AMT_TOT>");
            sb.AppendLine($"          <AMT_CURR>{invoice.AMT_CURR}</AMT_CURR>");
            sb.AppendLine($"          <STS_CD>{invoice.STS_CD}</STS_CD>");
            sb.AppendLine($"          <DT_ISS>{invoice.DT_ISS}</DT_ISS>");
            sb.AppendLine($"          <DT_DUE>{invoice.DT_DUE}</DT_DUE>");
            sb.AppendLine($"          <DT_PD>{invoice.DT_PD}</DT_PD>");
            sb.AppendLine("        </INVOICE>");
        }

        sb.AppendLine("      </INVOICES>");
        sb.AppendLine("    </GetCustomerInvoicesResponse>");
        sb.AppendLine("  </soap:Body>");
        sb.AppendLine("</soap:Envelope>");

        return sb.ToString();
    }

    private static string ExtractInvoiceIdFromSoapRequest(string soapXml)
    {
        var startTag = "<INV_ID>";
        var endTag = "</INV_ID>";

        var startIndex = soapXml.IndexOf(startTag);
        if (startIndex == -1) return string.Empty;

        startIndex += startTag.Length;
        var endIndex = soapXml.IndexOf(endTag, startIndex);
        if (endIndex == -1) return string.Empty;

        return soapXml.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static LegacySoapInvoice GenerateMockInvoice(string invoiceId)
    {
        var random = Random.Shared;
        var statusCodes = new[] { "P", "U", "O", "C" };
        var statusCode = statusCodes[random.Next(statusCodes.Length)];

        var issueDate = DateTime.Now.AddDays(-random.Next(1, 90));
        var dueDate = issueDate.AddDays(30);
        var paidDate = statusCode == "P" ? issueDate.AddDays(random.Next(1, 25)) : (DateTime?)null;

        return new LegacySoapInvoice
        {
            INV_ID = invoiceId,
            CUST_REF = $"CUST{random.Next(1000, 9999)}",
            INV_NUM = $"INV-2024-{random.Next(10000, 99999)}",
            AMT_TOT = (random.Next(100, 10000) + random.NextDouble()).ToString("F2"),
            AMT_CURR = "USD",
            STS_CD = statusCode,
            DT_ISS = issueDate.ToString("yyyyMMdd"),
            DT_DUE = dueDate.ToString("yyyyMMdd"),
            DT_PD = paidDate?.ToString("yyyyMMdd") ?? "",
            LINE_ITEMS = new LegacyLineItems
            {
                Items = new List<LegacyLineItem>
                {
                    new()
                    {
                        ITM_DESC = "Monthly Subscription Fee",
                        QTY = "1",
                        PRC_UNIT = "99.99",
                        AMT_LN = "99.99"
                    },
                    new()
                    {
                        ITM_DESC = "Additional Support Hours",
                        QTY = random.Next(1, 10).ToString(),
                        PRC_UNIT = "150.00",
                        AMT_LN = (random.Next(1, 10) * 150.0).ToString("F2")
                    }
                }
            }
        };
    }

    private static string SerializeToSoap(LegacySoapInvoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
        sb.AppendLine("  <soap:Body>");
        sb.AppendLine("    <GetInvoiceResponse xmlns=\"http://legacy.billing.corp/2005\">");
        sb.AppendLine("      <Invoice>");
        sb.AppendLine($"        <INV_ID>{invoice.INV_ID}</INV_ID>");
        sb.AppendLine($"        <CUST_REF>{invoice.CUST_REF}</CUST_REF>");
        sb.AppendLine($"        <INV_NUM>{invoice.INV_NUM}</INV_NUM>");
        sb.AppendLine($"        <AMT_TOT>{invoice.AMT_TOT}</AMT_TOT>");
        sb.AppendLine($"        <AMT_CURR>{invoice.AMT_CURR}</AMT_CURR>");
        sb.AppendLine($"        <STS_CD>{invoice.STS_CD}</STS_CD>");
        sb.AppendLine($"        <DT_ISS>{invoice.DT_ISS}</DT_ISS>");
        sb.AppendLine($"        <DT_DUE>{invoice.DT_DUE}</DT_DUE>");
        sb.AppendLine($"        <DT_PD>{invoice.DT_PD}</DT_PD>");
        sb.AppendLine("        <LINE_ITEMS>");

        if (invoice.LINE_ITEMS?.Items != null)
        {
            foreach (var item in invoice.LINE_ITEMS.Items)
            {
                sb.AppendLine("          <ITEM>");
                sb.AppendLine($"            <ITM_DESC>{item.ITM_DESC}</ITM_DESC>");
                sb.AppendLine($"            <QTY>{item.QTY}</QTY>");
                sb.AppendLine($"            <PRC_UNIT>{item.PRC_UNIT}</PRC_UNIT>");
                sb.AppendLine($"            <AMT_LN>{item.AMT_LN}</AMT_LN>");
                sb.AppendLine("          </ITEM>");
            }
        }

        sb.AppendLine("        </LINE_ITEMS>");
        sb.AppendLine("      </Invoice>");
        sb.AppendLine("    </GetInvoiceResponse>");
        sb.AppendLine("  </soap:Body>");
        sb.AppendLine("</soap:Envelope>");

        return sb.ToString();
    }

    private static IResult CreateSoapFault(string faultCode, string faultString)
    {
        var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <soap:Fault>
      <faultcode>{faultCode}</faultcode>
      <faultstring>{faultString}</faultstring>
      <detail>
        <BillingError xmlns=""http://legacy.billing.corp/2005"">
          <ErrorCode>SYS_ERR</ErrorCode>
          <ErrorMessage>{faultString}</ErrorMessage>
          <Timestamp>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</Timestamp>
        </BillingError>
      </detail>
    </soap:Fault>
  </soap:Body>
</soap:Envelope>";

        return Results.Content(xml, "text/xml; charset=utf-8", statusCode: 500);
    }
}
