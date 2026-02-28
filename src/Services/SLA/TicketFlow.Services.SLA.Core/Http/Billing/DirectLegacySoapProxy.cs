using System.Text;

namespace TicketFlow.Services.SLA.Core.Http.Billing;

public class DirectLegacySoapProxy
{
    private readonly HttpClient _httpClient;

    public DirectLegacySoapProxy(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string> GetCustomerInvoicesXmlAsync(
        string domain, CancellationToken cancellationToken)
    {
        var soapRequest = $@"<?xml version=""1.0""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetCustomerInvoicesRequest xmlns=""http://legacy.billing.corp/2005"">
      <DOMAIN>{domain}</DOMAIN>
    </GetCustomerInvoicesRequest>
  </soap:Body>
</soap:Envelope>";

        var response = await _httpClient.PostAsync(
            "/soap/billing/GetCustomerInvoices",
            new StringContent(soapRequest, Encoding.UTF8, "text/xml"),
            cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
