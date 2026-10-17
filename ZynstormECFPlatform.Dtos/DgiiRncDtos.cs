namespace ZynstormECFPlatform.Dtos;

public class DgiiRncStatusDto
{
    public bool IsDownloading { get; set; }
    public bool IsIndexing { get; set; }
    public DateTime? LastUpdated { get; set; }
    public long TotalRecords { get; set; }
    public string? StatusMessage { get; set; }
    public string? LastError { get; set; }
}

public class DgiiRncSearchResultDto
{
    public bool Found { get; set; }
    public string Rnc { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CommercialName { get; set; }
    public string? Activity { get; set; }
    public string? Status { get; set; }
    public string? Regime { get; set; }
}
