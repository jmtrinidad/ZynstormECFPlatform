using System.Collections.Generic;

namespace ZynstormECFPlatform.Dtos;

public class PaginatedResponseDto<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
