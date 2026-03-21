using X.PagedList;
using ZynstormECFPlatform.Abstractions.Data;

namespace ZynstormECFPlatform.Data.Extensions;

public static class PagedCollectionExtensions
{
    public static IPagedCollection<T> AsPagedCollection<T>(this IPagedList<T> pagedList)
    {
        return new PagedCollection<T>(pagedList);
    }

    public static IPagedCollection<T> AsPagedCollection<T>(this IQueryable<T> queryable, int pageNumber,
        int pageSize)
    {
        return new PagedCollection<T>(queryable.ToPagedList(pageNumber, pageSize));
    }

    public static async Task<IPagedCollection<T>> AsPagedCollectionAsync<T>(this IQueryable<T> queryable,
        int pageNumber, int pageSize)
    {
        var pagedList = await queryable.ToPagedListAsync(pageNumber, pageSize).ConfigureAwait(false);
        return new PagedCollection<T>(pagedList);
    }

    public static IPagedCollection<T> AsPagedCollection<T>(this IEnumerable<T> enumerable, int pageNumber,
        int pageSize, int totalItems)
    {
        return new PagedCollection<T>(enumerable, pageNumber, pageSize, totalItems);
    }
}