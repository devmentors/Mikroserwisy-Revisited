using System.Xml.Serialization;

namespace LegacyBillingSystem;

[XmlRoot("soap:Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class SoapEnvelope<T>
{
    [XmlElement("soap:Body")]
    public SoapBody<T>? Body { get; set; }
}

public class SoapBody<T>
{
    [XmlElement]
    public T? Response { get; set; }
}

[XmlRoot("GetInvoiceResponse")]
public class LegacySoapInvoice
{
    [XmlElement("INV_ID")]
    public string? INV_ID { get; set; }

    [XmlElement("CUST_REF")]
    public string? CUST_REF { get; set; }

    [XmlElement("INV_NUM")]
    public string? INV_NUM { get; set; }

    [XmlElement("AMT_TOT")]
    public string? AMT_TOT { get; set; }

    [XmlElement("AMT_CURR")]
    public string? AMT_CURR { get; set; }

    [XmlElement("STS_CD")]
    public string? STS_CD { get; set; }

    [XmlElement("DT_ISS")]
    public string? DT_ISS { get; set; }

    [XmlElement("DT_DUE")]
    public string? DT_DUE { get; set; }

    [XmlElement("DT_PD")]
    public string? DT_PD { get; set; }

    [XmlElement("LINE_ITEMS")]
    public LegacyLineItems? LINE_ITEMS { get; set; }
}

public class LegacyLineItems
{
    [XmlElement("ITEM")]
    public List<LegacyLineItem>? Items { get; set; }
}

public class LegacyLineItem
{
    [XmlElement("ITM_DESC")]
    public string? ITM_DESC { get; set; }

    [XmlElement("QTY")]
    public string? QTY { get; set; }

    [XmlElement("PRC_UNIT")]
    public string? PRC_UNIT { get; set; }

    [XmlElement("AMT_LN")]
    public string? AMT_LN { get; set; }
}

[XmlRoot("GetInvoiceResponse")]
public class GetInvoiceResponse
{
    [XmlElement("Invoice")]
    public LegacySoapInvoice? Invoice { get; set; }
}

[XmlRoot("GetCustomerInvoicesResponse")]
public class LegacyCustomerInvoicesResponse
{
    [XmlElement("DOMAIN")]
    public string? DOMAIN { get; set; }

    [XmlElement("INVOICES")]
    public LegacyInvoicesList? INVOICES { get; set; }
}

public class LegacyInvoicesList
{
    [XmlElement("INVOICE")]
    public List<LegacySoapInvoice>? Items { get; set; }
}
