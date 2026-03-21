using System.Collections;
using X.PagedList;
using ZynstormECFPlatform.Abstractions.Data;

namespace ZynstormECFPlatform.Data;

public class PagedCollection<T> : IPagedCollection<T>
{
    private readonly IEnumerable<T> _pagedList;

    public PagedCollection(IPagedList<T> pagedList) : this(pagedList, pagedList.PageNumber, pagedList.PageSize,
        pagedList.TotalItemCount)
    {
    }

    public PagedCollection(IEnumerable<T> enumerable, int pageNumber, int pageSize, int totalItems)
    {
        _pagedList = enumerable;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItemCount = totalItems;
        PageCount = TotalItemCount > 0
            ? (int)Math.Ceiling(TotalItemCount / (double)PageSize)
            : 0;

        HasPreviousPage = PageNumber > 1;
        HasNextPage = PageNumber < PageCount;
        IsFirstPage = PageNumber == 1;
        IsLastPage = PageNumber >= PageCount;
        FirstItemOnPage = (PageNumber - 1) * PageSize + 1;

        var numberOfLastItemOnPage = FirstItemOnPage + PageSize - 1;

        LastItemOnPage = numberOfLastItemOnPage > TotalItemCount
            ? TotalItemCount
            : numberOfLastItemOnPage;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _pagedList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static implicit operator PagedCollection<T>(PagedList<T> pagedList)
    {
        return new PagedCollection<T>(pagedList);
    }

    public int PageCount { get; }
    public int TotalItemCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
    public bool IsFirstPage { get; }
    public bool IsLastPage { get; }
    public int FirstItemOnPage { get; }
    public int LastItemOnPage { get; }
}