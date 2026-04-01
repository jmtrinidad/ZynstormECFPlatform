using System.Xml.Serialization;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfDocument : BaseEntity
{
    [XmlIgnore]
    public int EcfDocumentId { get; set; }

    [XmlIgnore]
    public int ClientId { get; set; }

    [XmlIgnore]
    public int ClientBrancheId { get; set; }

    [XmlIgnore]
    public int ApiKeyId { get; set; }

    [XmlElement("TipoeCF")]
    public int EcfTypeId { get; set; }

    [XmlIgnore]
    public string ExternalReference { get; set; } = null!;

    [XmlElement("eNCF")]
    public string Ncf { get; set; } = null!;

    [XmlElement("RNCComprador")]
    public string CustomerRnc { get; set; } = null!;

    [XmlElement("RazonSocialComprador")]
    public string CustomerName { get; set; } = null!;

    [XmlElement("CorreoComprador")]
    public string? CustomerEmail { get; set; }

    [XmlElement("DireccionComprador")]
    public string? CustomerAddress { get; set; }

    [XmlElement("FechaEmision")]
    public DateTime IssueDateUtc { get; set; }

    [XmlIgnore]
    public int CurrencyId { get; set; }

    [XmlIgnore]
    public decimal SubTotal { get; set; }

    [XmlIgnore]
    public decimal Itbistotal { get; set; }

    [XmlElement("MontoTotal")]
    public decimal Total { get; set; }

    [XmlIgnore]
    public int EcfStatusId { get; set; }

    [XmlIgnore]
    public string? HangfireJobId { get; set; }

    [XmlIgnore]
    public DateTime CreatedAtUtc { get; set; }

    [XmlIgnore]
    public virtual ApiKey ApiKey { get; set; } = null!;

    [XmlIgnore]
    public virtual Client Client { get; set; } = null!;

    [XmlIgnore]
    public virtual ClientBranche ClientBranche { get; set; } = null!;

    [XmlIgnore]
    public virtual Currency Currency { get; set; } = null!;

    [XmlIgnore]
    public virtual EcfStatus EcfStatus { get; set; } = null!;

    [XmlIgnore]
    public virtual EcfType EcfType { get; set; } = null!;

    [XmlArray("DetallesItems"), XmlArrayItem("Item")]
    public virtual ICollection<EcfDocumentDetail> EcfDocumentDetails { get; set; } = [];

    [XmlIgnore]
    public virtual ICollection<EcfDocumentTotal> EcfDocumentTotals { get; set; } = [];

    [XmlIgnore]
    public virtual ICollection<EcfStatusHistory> EcfStatusHistories { get; set; } = [];

    [XmlIgnore]
    public virtual ICollection<SystemLog> SystemLogs { get; set; } = [];

    [XmlIgnore]
    public virtual ICollection<EcfTransmission> EcfTransmissions { get; set; } = [];

    [XmlIgnore]
    public virtual ICollection<EcfXmlDocument> EcfXmlDocuments { get; set; } = [];

    #region e-CF Extended Properties

    [XmlElement("Version")]
    public string Version { get; set; } = "1.0";

    [XmlElement("FechaVencimientoSecuencia")]
    public DateTime SequenceExpirationDate { get; set; }

    [XmlElement("IndicadorEnvioDiferido")]
    public int? DeferredSendIndicator { get; set; }

    [XmlElement("IndicadorMontoGravado")]
    public int? TaxIncludedIndicator { get; set; }

    [XmlElement("IndicadorServicioTodoIncluido")]
    public int? AllInclusiveServiceIndicator { get; set; }

    [XmlElement("NombreComercial")]
    public string? IssuerCommercialName { get; set; }

    [XmlElement("Sucursal")]
    public string? IssuerBranchCode { get; set; }

    [XmlElement("MunicipioEmisor")]
    public string? IssuerMunicipality { get; set; }

    [XmlElement("ProvinciaEmisor")]
    public string? IssuerProvince { get; set; }

    [XmlElement("ActividadEconomica")]
    public string? IssuerActivityCode { get; set; }

    [XmlElement("CodigoVendedor")]
    public string? IssuerSellerCode { get; set; }

    [XmlElement("WebEmisor")]
    public string? IssuerWebSite { get; set; }

    [XmlElement("InformacionAdicionalEmisor")]
    public string? IssuerAdditionalInfo { get; set; }

    [XmlElement("TipoIngresos")]
    public string? IncomeType { get; set; }

    [XmlElement("TipoPago")]
    public int PaymentType { get; set; }

    [XmlElement("FechaLimitePago")]
    public DateTime? PaymentDeadline { get; set; }

    [XmlElement("TerminoPago")]
    public string? PaymentTerms { get; set; }

    [XmlElement("ContactoComprador")]
    public string? CustomerContact { get; set; }

    [XmlElement("MunicipioComprador")]
    public string? CustomerMunicipality { get; set; }

    [XmlElement("ProvinciaComprador")]
    public string? CustomerProvince { get; set; }

    [XmlElement("FechaEntrega")]
    public DateTime? DeliveryDate { get; set; }

    [XmlElement("ContactoEntrega")]
    public string? DeliveryContact { get; set; }

    [XmlElement("DireccionEntrega")]
    public string? DeliveryAddress { get; set; }

    [XmlElement("TelefonoAdicional")]
    public string? AdditionalPhone { get; set; }

    [XmlElement("FechaOrdenCompra")]
    public DateTime? PurchaseOrderDate { get; set; }

    [XmlElement("NumeroOrdenCompra")]
    public string? PurchaseOrderNumber { get; set; }

    [XmlElement("NCFModificado")]
    public string? ModifiedNcf { get; set; }

    [XmlElement("FechaNCFModificado")]
    public DateTime? ModifiedNcfDate { get; set; }

    [XmlElement("CodigoModificacion")]
    public int? ModificationCode { get; set; }

    [XmlElement("RazonModificacion")]
    public string? ModificationReason { get; set; }

    [XmlElement("FechaHoraFirma")]
    public DateTime? SignatureDateTime { get; set; }

    #endregion e-CF Extended Properties
}