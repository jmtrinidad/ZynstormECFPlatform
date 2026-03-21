using System.Data;
using System.Linq.Expressions;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Abstractions.Data;

public interface IRepository<TModel>
    where TModel : IEntityMarker
{
    IDbConnection Connection { get; }

    IQueryable<TModel> Table { get; }

    IDbConnection GetConnection();

    IDbTransaction GetActiveTransaction();

    //Task<IPagedCollection<TModel>> GetAllPagedAsync(DataTableFilter filter);

    IEnumerable<TModel> Query(string sql, object parameters);

    Task<IEnumerable<TModel>> QueryAsync(string sql, object parameters);

    Task<IEnumerable<TModel>> QueryAsync(string sql, CancellationToken cancellationToken);

    Task<IEnumerable<TModel>> QueryAsync(string sql, object parameters, CancellationToken cancellationToken);

    Task<IEnumerable<TModel>> QueryPagedAsync(string sql, string orderby, int page, int resultsPerPage,
        object parameters = null!);

    Task<IEnumerable<TAny>> QueryPagedAsync<TAny>(string sql, string orderby, int page, int resultsPerPage,
        object parameters = null!);

    Task<IEnumerable<TModel>> QueryPagedAsync<T2>(string sql, string orderby, int page, int resultsPerPage,
        Func<TModel, T2, TModel> map, object parameters = null!, string splitOn = "Id");

    Task<IEnumerable<TModel>> QueryPagedAsync<T2, T3>(string sql, string orderby, int page, int resultsPerPage,
        Func<TModel, T2, T3, TModel> map, object parameters = null!, string splitOn = "Id");

    Task<IEnumerable<TReturn>> QueryPagedAsync<T1, T2, TReturn>(string sql, string orderby, int page,
        int resultsPerPage,
        Func<T1, T2, TReturn> map, object parameters = null!, string splitOn = "Id");

    Task<IEnumerable<TReturn>> QueryPagedAsync<T1, T2, TReturn>(string sql, string orderby, int page,
        int resultsPerPage,
        Func<TModel, T1, T2, TReturn> map, object parameters = null!, string splitOn = "Id");

    IEnumerable<TAny> QueryPaged<TAny>(string sql, string orderby, int page, int resultsPerPage,
        object parameters = null!);

    IEnumerable<TAny> Query<TAny>(string sql, object parameters = null!);

    Task<IEnumerable<TAny>> QueryAsync<TAny>(string sql, object parameters = null!);

    Task<IEnumerable<TAny>> QueryAsync<TAny>(string sql, CancellationToken cancellationToken);

    Task<IEnumerable<TAny>> QueryAsync<TAny>(string sql, object? parameters, CancellationToken cancellationToken);

    int Execute(string sql, object parameters, IDbTransaction transaction = null!);

    Task<int> ExecuteAsync(string sql, object parameters, IDbTransaction transaction = null!);

    Task<int> ExecuteAsync(string sql, CancellationToken cancellationToken);

    Task<int> ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken);

    Task<TAny?> ExecuteScalarAsync<TAny>(string sql, object parameters);

    TAny? ExecuteScalar<TAny>(string sql, object parameters);

    void ExecuteProcedure(string spName, object parameters);

    IEnumerable<TSpResult> ExecuteProcedure<TSpResult>(string spName, object parameters);

    Task<IEnumerable<TSpResult>> ExecuteProcedureAsync<TSpResult>(string spName, object parameters);

    void Add(TModel model);

    long Insert(TModel model);

    Task<TModel?> InsertAsync(TModel model);

    void Insert(IEnumerable<TModel> models);

    Task InsertAsync(IEnumerable<TModel> models);

    void InsertNoSave(IEnumerable<TModel> models);

    //void Remove(TModel model);

    //Task<bool> DeleteAsync(TModel model);

    //Task DeleteAllAsync();

    //void Delete(TModel model);

    Task<bool> SoftDeleteAsync(TModel model);

    Task<bool> SoftDeleteAsync(IEnumerable<TModel> models);

    Task<bool> SoftDeleteAsync(int id);

    Task<bool> HardDeleteAsync(int id);

    Task<bool> HardDeleteAsync(TModel model);

    Task<bool> HardDeleteAsync(IEnumerable<TModel> models);

    void Modify(TModel model);

    Task UpdateAsync(IEnumerable<TModel> models);

    void UpdateNoSave(IEnumerable<TModel> models);

    Task<bool> UpdateAsync(TModel model);

    void Update(TModel model);

    Task<TModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    TModel? Get(int id);

    Task<IEnumerable<TModel>> GetAllAsync(CancellationToken cancellationToken = default);

    IEnumerable<TModel> GetAll();

    Task<TModel?> GetByAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default);

    Task<TModel?> GetNoTrackingByAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default);

    Task<TModel?> GetByAsync<TOrderKey>(Expression<Func<TModel, bool>> predicate, Expression<Func<TModel, TOrderKey>> keySelector, bool ascending = true);

    TModel? GetBy(Expression<Func<TModel, bool>> predicate);

    IEnumerable<TModel> GetManyBy(Expression<Func<TModel, bool>> predicate);

    Task<IEnumerable<TModel>> GetManyByAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default);

    Task<IEnumerable<TModel>> GetManyByAsync<TOrderKey>(Expression<Func<TModel, bool>> predicate,
        Expression<Func<TModel, TOrderKey>> keySelector);

    Task<IPagedCollection<TModel>> GetPagedAsync(int page, int perPage);

    Task<IPagedCollection<TModel>> GetPagedAsync(int page, int perPage, Expression<Func<TModel, bool>> predicate);

    Task<IPagedCollection<TModel>> GetPagedAsync<TOrderKey>(int page, int perPage,
        Expression<Func<TModel, bool>> predicate, Expression<Func<TModel, TOrderKey>> keySelector);

    Task<int> SaveChangesAsync();

    int SaveChanges();
}