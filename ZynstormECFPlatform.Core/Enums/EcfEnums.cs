namespace ZynstormECFPlatform.Core.Enums;

public enum EcfType
{
    StandardElectronicInvoice = 31,
    ConsumerElectronicInvoice = 32,
    DebitNoteElectronic = 33,
    CreditNoteElectronic = 34,
    ElectronicPurchase = 41,
    MinorElectronicExpenses = 43,
    SpecialRegimesElectronic = 44,
    GovernmentElectronic = 45,
    ExportElectronicInvoice = 46,
    ForeignPaymentsElectronic = 47
}

public enum PaymentType
{
    Cash = 1,
    Credit = 2,
    Free = 3
}

public enum PaymentForm
{
    Cash = 1,
    CheckTransferDeposit = 2,
    DebitCreditCard = 3,
    CreditSale = 4,
    BondsGiftCertificates = 5,
    Swap = 6,
    CreditNote = 7,
    Other = 8
}

public enum IncomeType
{
    NonFinancialOperations = 1,
    FinancialIncome = 2,
    ExtraordinaryIncome = 3,
    RentalIncome = 4,
    DepreciableAssetSale = 5,
    OtherIncome = 6
}

public enum BillingIndicator
{
    NonTaxable = 0,
    TaxRate18 = 1,
    TaxRate16 = 2,
    TaxRate0 = 3,
    Exempt = 4
}

public enum ItemType
{
    Good = 1,
    Service = 2
}

public enum ModificationCode
{
    AnullNCF = 1,
    CorrectText = 2,
    CorrectAmount = 3,
    ContingencyReplacement = 4,
    ConsumerReference = 5
}
