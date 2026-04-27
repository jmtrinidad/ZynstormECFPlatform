namespace ZynstormECFPlatform.Core.Enums;

/// <summary>
/// Tipo de Documento Electrónico (e-CF).
/// </summary>
public enum EcfType
{
    /// <summary> Factura de Crédito Fiscal (E31) </summary>
    StandardElectronicInvoice = 31,
    
    /// <summary> Factura de Consumo (E32) </summary>
    ConsumerElectronicInvoice = 32,
    
    /// <summary> Nota de Crédito (E33) </summary>
    DebitNoteElectronic = 33,
    
    /// <summary> Nota de Débito (E34) </summary>
    CreditNoteElectronic = 34,
    
    /// <summary> Registro de Compras (E41) </summary>
    ElectronicPurchase = 41,
    
    /// <summary> Gastos Menores (E43) </summary>
    MinorElectronicExpenses = 43,
    
    /// <summary> Regímenes Especiales de Tributación (E44) </summary>
    SpecialRegimesElectronic = 44,
    
    /// <summary> Gubernamental (E45) </summary>
    GovernmentElectronic = 45,
    
    /// <summary> Comprobante de Exportación (E46) </summary>
    ExportElectronicInvoice = 46,
    
    /// <summary> Pagos al Exterior (E47) </summary>
    ForeignPaymentsElectronic = 47
}

/// <summary>
/// Tipo de Pago de la Factura.
/// </summary>
public enum PaymentType
{
    /// <summary> Pago al contado </summary>
    Cash = 1,
    
    /// <summary> Venta a crédito </summary>
    Credit = 2,
    
    /// <summary> Entrega gratuita </summary>
    Free = 3
}

/// <summary>
/// Forma de Pago.
/// </summary>
public enum PaymentForm
{
    /// <summary> Efectivo </summary>
    Cash = 1,
    /// <summary> Cheque/Transferencia/Depósito </summary>
    CheckTransferDeposit = 2,
    /// <summary> Tarjeta de Débito/Crédito </summary>
    DebitCreditCard = 3,
    /// <summary> Venta a Crédito </summary>
    CreditSale = 4,
    /// <summary> Bonos o Certificados de regalo </summary>
    BondsGiftCertificates = 5,
    /// <summary> Permuta </summary>
    Swap = 6,
    /// <summary> Nota de crédito </summary>
    CreditNote = 7,
    /// <summary> Otras Formas de pago </summary>
    Other = 8
}

/// <summary>
/// Tipo de Ingresos.
/// </summary>
public enum IncomeType
{
    /// <summary> Ingresos por operaciones (No financieros) </summary>
    NonFinancialOperations = 1,
    /// <summary> Ingresos Financieros </summary>
    FinancialIncome = 2,
    /// <summary> Ingresos Extraordinarios </summary>
    ExtraordinaryIncome = 3,
    /// <summary> Ingresos por Arrendamientos </summary>
    RentalIncome = 4,
    /// <summary> Ingresos por Venta de Activo Depreciable </summary>
    DepreciableAssetSale = 5,
    /// <summary> Otros Ingresos </summary>
    OtherIncome = 6
}

/// <summary>
/// Indicador de Facturación (ITBIS).
/// </summary>
public enum BillingIndicator
{
    /// <summary> No Facturable (18%) </summary>
    NonTaxable = 0,
    /// <summary> ITBIS 1 (18%) </summary>
    TaxRate18 = 1,
    /// <summary> ITBIS 2 (16%) </summary>
    TaxRate16 = 2,
    /// <summary> ITBIS 3 (0%) </summary>
    TaxRate0 = 3,
    /// <summary> Exento (E) </summary>
    Exempt = 4
}

/// <summary>
/// Tipo de Ítem.
/// </summary>
public enum ItemType
{
    /// <summary> Producto físico o mercancía </summary>
    Good = 1,
    /// <summary> Prestación de servicios </summary>
    Service = 2
}

/// <summary>
/// Código de Motivo de Modificación.
/// </summary>
public enum ModificationCode
{
    /// <summary> Anula el NCF modificado </summary>
    AnullNCF = 1,
    /// <summary> Corrige Texto del Comprobante Fiscal modificado </summary>
    CorrectText = 2,
    /// <summary> Corrige montos del NCF modificado </summary>
    CorrectAmount = 3,
    /// <summary> Reemplazo NCF emitido en contingencia </summary>
    ContingencyReplacement = 4,
    /// <summary> Referencia Factura Consumo Electrónica </summary>
    ConsumerReference = 5
}

/// <summary>
/// Códigos de Unidades de Medida oficiales de la DGII (1 al 62).
/// </summary>
public enum UnitOfMeasure
{
    Barril = 1,
    Bolsa = 2,
    Bote = 3,
    Bultos = 4,
    Botella = 5,
    CajaCajon = 6,
    Cajetilla = 7,
    Centimetro = 8,
    Cilindro = 9,
    Conjunto = 10,
    Contenedor = 11,
    Dia = 12,
    Docena = 13,
    Fardo = 14,
    Galones = 15,
    Grado = 16,
    Gramo = 17,
    Granel = 18,
    Hora = 19,
    Huacal = 20,
    Kilogramo = 21,
    KilovatioHora = 22,
    Libra = 23,
    Litro = 24,
    Lote = 25,
    Metro = 26,
    MetroCuadrado = 27,
    MetroCubico = 28,
    MillonesUnidadesTermicas = 29,
    Minuto = 30,
    Paquete = 31,
    Par = 32,
    Pie = 33,
    Pieza = 34,
    Rollo = 35,
    Sobre = 36,
    Segundo = 37,
    Tanque = 38,
    Tonelada = 39,
    Tubo = 40,
    Yarda = 41,
    YardaCuadrada = 42,
    Unidad = 43,
    Elemento = 44,
    Millar = 45,
    Saco = 46,
    Lata = 47,
    Display = 48,
    Bidon = 49,
    Racion = 50,
    Quintal = 51,
    ToneladasRegistroBruto = 52,
    PieCuadrado = 53,
    Pasajero = 54,
    Pulgadas = 55,
    ParqueoBarcosMuelle = 56,
    Bandeja = 57,
    Hectarea = 58,
    Mililitro = 59,
    Miligramo = 60,
    Onzas = 61,
    OnzasTroy = 62
}

/// <summary>
/// Códigos de Actividad Económica (CIIU). Ejemplos comunes.
/// </summary>
public enum IssuerActivityCode
{
    VentaAlPorMenorSupermercados = 471100,
    TransporteDeCarga = 492310,
    ConsultoriaInformatica = 620200,
    ServiciosJuridicos = 691000,
    ServiciosDeContabilidad = 692020,
    OtrasActividadesProfesionales = 749000
}

