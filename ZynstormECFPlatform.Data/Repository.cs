using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Linq.Expressions;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Data.Extensions;

namespace ZynstormECFPlatform.Data;

public class Repository<TModel> : IRepository<TModel> where TModel : class, IEntityMarker
{
    private readonly StorageContext _context;
    private readonly DbSet<TModel> _dbSet;
    private readonly ISqlGenerator _sqlGenerator;

    private IDbTransaction? ActiveDbTransaction => _context.Database.CurrentTransaction?.GetDbTransaction();

    public Repository(StorageContext context, ISqlGenerator sqlGenerator)
    {
        _context = context;
        _sqlGenerator = sqlGenerator;
        _dbSet = context.Set<TModel>();
    }

    public IDbConnection Connection => _context.Database.GetDbConnection();

    public IDbConnection GetConnection()
    {
        return Connection;
    }

    public IDbTransaction GetActiveTransaction()
    {
        var trx = ActiveDbTransaction;
        if (trx is null)
        {
            throw new InvalidOperationException("There is no active transaction.");
        }

        return trx;
    }

    public IQueryable<TModel> Table => _dbSet;

    public IEnumerable<TModel> Query(string sql, object parameters)
    {
        return Connection.Query<TModel>(sql, parameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TModel>> QueryAsync(string sql, object parameters)
    {
        return Connection.QueryAsync<TModel>(sql, parameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TModel>> QueryAsync(string sql, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Connection.QueryAsync<TModel>(sql, null, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TModel>> QueryAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Connection.QueryAsync<TModel>(sql, parameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TModel>> QueryPagedAsync(string sql, string orderby, int page, int resultsPerPage,
        object parameters = null!)
    {
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync<TModel>(query, dynamicParameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TModel>> QueryPagedAsync(string sql, string orderby, int page, int resultsPerPage,
        object parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync<TModel>(query, dynamicParameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TAny>> QueryPagedAsync<TAny>(string sql, string orderby, int page, int resultsPerPage,
        object parameters = null!)
    {
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync<TAny>(query, dynamicParameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TAny>> QueryPagedAsync<TAny>(string sql, string orderby, int page, int resultsPerPage,
        object parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync<TAny>(query, dynamicParameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TModel>> QueryPagedAsync<T2>(string sql, string orderby, int page, int resultsPerPage,
        Func<TModel, T2, TModel> map, object parameters = null!, string splitOn = "Id")
    {
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public Task<IEnumerable<TModel>> QueryPagedAsync<T2>(string sql, string orderby, int page, int resultsPerPage,
        Func<TModel, T2, TModel> map, object parameters, string splitOn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public Task<IEnumerable<TModel>> QueryPagedAsync<T2, T3>(string sql, string orderby, int page,
        int resultsPerPage,
        Func<TModel, T2, T3, TModel> map, object parameters = null!, string splitOn = "Id")
    {
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public Task<IEnumerable<TModel>> QueryPagedAsync<T2, T3>(string sql, string orderby, int page,
        int resultsPerPage,
        Func<TModel, T2, T3, TModel> map, object parameters, string splitOn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public Task<IEnumerable<TReturn>> QueryPagedAsync<T1, T2, TReturn>(string sql, string orderby, int page,
        int resultsPerPage,
        Func<T1, T2, TReturn> map, object parameters = null!, string splitOn = "Id")
    {
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public Task<IEnumerable<TReturn>> QueryPagedAsync<T1, T2, TReturn>(string sql, string orderby, int page,
        int resultsPerPage, Func<T1, T2, TReturn> map,
        object parameters, string splitOn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public Task<IEnumerable<TReturn>> QueryPagedAsync<T1, T2, TReturn>(string sql, string orderby, int page,
        int resultsPerPage, Func<TModel, T1, T2, TReturn> map,
        object parameters = null!, string splitOn = "Id")
    {
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public Task<IEnumerable<TReturn>> QueryPagedAsync<T1, T2, TReturn>(string sql, string orderby, int page,
        int resultsPerPage, Func<TModel, T1, T2, TReturn> map,
        object parameters, string splitOn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.QueryAsync(query, map, dynamicParameters, transaction: ActiveDbTransaction, splitOn: splitOn);
    }

    public IEnumerable<TAny> QueryPaged<TAny>(string sql, string orderby, int page, int resultsPerPage,
        object parameters = null!)
    {
        var (query, dynamicParameters) = PreparePagedQuery(sql, orderby, page, resultsPerPage, parameters);
        return Connection.Query<TAny>(query, dynamicParameters, transaction: ActiveDbTransaction);
    }

    public IEnumerable<TAny> Query<TAny>(string sql, object? parameters = null!)
    {
        return Connection.Query<TAny>(sql, parameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TAny>> QueryAsync<TAny>(string sql, object parameters = null!)
    {
        return Connection.QueryAsync<TAny>(sql, parameters, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TAny>> QueryAsync<TAny>(string sql, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Connection.QueryAsync<TAny>(sql, null, transaction: ActiveDbTransaction);
    }

    public Task<IEnumerable<TAny>> QueryAsync<TAny>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Connection.QueryAsync<TAny>(sql, parameters, transaction: ActiveDbTransaction);
    }

    public int Execute(string sql, object parameters, IDbTransaction transaction = null!)
    {
        return Connection.Execute(sql, parameters, transaction ?? ActiveDbTransaction);
    }

    public Task<int> ExecuteAsync(string sql, object parameters, IDbTransaction transaction = null!)
    {
        return Connection.ExecuteAsync(sql, parameters, transaction ?? ActiveDbTransaction);
    }

    public Task<int> ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Connection.ExecuteAsync(sql, null, ActiveDbTransaction);
    }

    public Task<int> ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Connection.ExecuteAsync(sql, parameters, ActiveDbTransaction);
    }

    public Task<TAny?> ExecuteScalarAsync<TAny>(string sql, object parameters)
    {
        return Connection.ExecuteScalarAsync<TAny>(sql, parameters);
    }

    public TAny? ExecuteScalar<TAny>(string sql, object parameters)
    {
        return Connection.ExecuteScalar<TAny>(sql, parameters);
    }

    public void ExecuteProcedure(string spName, object parameters)
    {
        Connection.Execute(spName, parameters, transaction: ActiveDbTransaction, commandType: CommandType.StoredProcedure, commandTimeout: 600);
    }

    public IEnumerable<TSpResult> ExecuteProcedure<TSpResult>(string spName, object parameters)
    {
        return Connection.Query<TSpResult>(spName, parameters, transaction: ActiveDbTransaction, commandType: CommandType.StoredProcedure,
            commandTimeout: 600);
    }

    public Task<IEnumerable<TSpResult>> ExecuteProcedureAsync<TSpResult>(string spName, object parameters)
    {
        return Connection.QueryAsync<TSpResult>(spName, parameters, transaction: ActiveDbTransaction, commandType: CommandType.StoredProcedure,
            commandTimeout: 600);
    }

    public void Add(TModel model)
    {
        model.LastUpdateUtc = DateTime.UtcNow;
        _dbSet.Add(model);
    }

    public long Insert(TModel model)
    {
        model.LastUpdateUtc = DateTime.UtcNow;
        _dbSet.Add(model);
        _context.SaveChanges();
        return 0;
    }

    public virtual async Task<TModel?> InsertAsync(TModel model)
    {
        model.LastUpdateUtc = DateTime.UtcNow;
        await _dbSet.AddAsync(model).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return model;
    }

    public void Insert(IEnumerable<TModel> models)
    {
        var entities = models.ToList();
        entities.ForEach(c => c.LastUpdateUtc = DateTime.UtcNow);

        _dbSet.AddRange(entities);
        _context.SaveChanges();
    }

    public async Task InsertAsync(IEnumerable<TModel> models)
    {
        var entities = models.ToList();
        entities.ForEach(c => c.LastUpdateUtc = DateTime.UtcNow);

        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public void InsertNoSave(IEnumerable<TModel> models)
    {
        var entities = models.ToList();
        entities.ForEach(c => c.LastUpdateUtc = DateTime.UtcNow);
        _dbSet.AddRange(entities);
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        TModel entity = await GetAsync(id) ?? null!;

        if (entity is null)
            return false;

        entity.IsDeleted = true;
        entity.DeletedTimeUtc = DateTime.UtcNow;

        return await UpdateAsync(entity);
    }

    public async Task<bool> SoftDeleteAsync(TModel model)
    {
        model.IsDeleted = true;
        model.DeletedTimeUtc = DateTime.UtcNow;

        return await UpdateAsync(model);
    }

    public async Task<bool> SoftDeleteAsync(IEnumerable<TModel> models)
    {
        var entities = models.ToList();

        entities.ForEach(c =>
        {
            c.IsDeleted = true;
            c.DeletedTimeUtc = DateTime.UtcNow;
        });

        _dbSet.UpdateRange(entities);

        return await _context.SaveChangesAsync() >= 1;
    }

    public async Task<bool> HardDeleteAsync(int id)
    {
        var model = await GetAsync(id);

        if (model is not null)
        {
            _dbSet.Remove(model);

            return await _context.SaveChangesAsync() >= 1;
        }
        return false;
    }

    public async Task<bool> HardDeleteAsync(TModel model)
    {
        if (model is not null)
        {
            _dbSet.Remove(model);

            return await _context.SaveChangesAsync() >= 1;
        }
        return false;
    }

    public async Task<bool> HardDeleteAsync(IEnumerable<TModel> models)
    {
        if (models is not null && models.Any())
        {
            _dbSet.RemoveRange(models);

            return await _context.SaveChangesAsync() >= 1;
        }
        return false;
    }

    public void Modify(TModel model)
    {
        model.LastUpdateUtc = DateTime.UtcNow;
        _dbSet.Update(model);
    }

    public Task UpdateAsync(IEnumerable<TModel> models)
    {
        var entities = models.ToList();

        entities.ForEach(c => c.LastUpdateUtc = DateTime.UtcNow);

        _dbSet.UpdateRange(entities);
        return _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(TModel model)
    {
        model.LastUpdateUtc = DateTime.UtcNow;
        _dbSet.Update(model);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public void Update(TModel model)
    {
        model.LastUpdateUtc = DateTime.UtcNow;
        _dbSet.Update(model);
        _context.SaveChanges();
    }

    public void UpdateNoSave(IEnumerable<TModel> models)
    {
        _dbSet.UpdateRange(models);
    }

    public void UpdateNoSave(TModel model)
    {
        model.LastUpdateUtc = DateTime.UtcNow;
        _dbSet.Update(model);
    }

    public async Task<TModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(id, cancellationToken);
    }

    //public async Task<IPagedCollection<TModel>> GetAllPagedAsync(DataTableFilter filter)
    //{
    //    return await _dbSet.AsPagedCollectionAsync(filter.Page, filter.Length);
    //}

    public TModel? Get(int id)
    {
        return _dbSet.Find(id);
    }

    public async Task<IEnumerable<TModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
                           .ToListAsync(cancellationToken)
                           .ConfigureAwait(false);
    }

    public IEnumerable<TModel> GetAll()
    {
        return _dbSet.ToList();
    }

    public async Task<TModel?> GetByAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<TModel?> GetNoTrackingByAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
                           .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<TModel?> GetByAsync<TOrderKey>(Expression<Func<TModel, bool>> predicate,
        Expression<Func<TModel, TOrderKey>> keySelector, bool ascending = true)
    {
        var query = _dbSet.Where(predicate);
        query = ascending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        return await query.FirstOrDefaultAsync();
    }

    public TModel? GetBy(Expression<Func<TModel, bool>> predicate)
    {
        return _dbSet.FirstOrDefault(predicate);
    }

    public IEnumerable<TModel> GetManyBy(Expression<Func<TModel, bool>> predicate)
    {
        return _dbSet.Where(predicate).AsEnumerable();
    }

    public async Task<IEnumerable<TModel>> GetManyByAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate)
                           .ToListAsync(cancellationToken)
                           .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TModel>> GetManyByAsync<TOrderKey>(Expression<Func<TModel, bool>> predicate,
        Expression<Func<TModel, TOrderKey>> keySelector)
    {
        return await _dbSet.Where(predicate)
                           .OrderBy(keySelector)
                           .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<TModel>> GetManyByAsync<TOrderKey>(Expression<Func<TModel, bool>> predicate,
       Expression<Func<TModel, TOrderKey>> keySelector, int take)
    {
        return await _dbSet.Where(predicate)
                           .OrderByDescending(keySelector)
                           .Take(take)
                           .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IPagedCollection<TModel>> GetPagedAsync(int page, int perPage)
    {
        return await _dbSet.AsPagedCollectionAsync(page, perPage);
    }

    public async Task<IPagedCollection<TModel>> GetPagedAsync(int page, int perPage,
        Expression<Func<TModel, bool>> predicate)
    {
        return await _dbSet.Where(predicate).AsPagedCollectionAsync(page, perPage);
    }

    public async Task<IPagedCollection<TModel>> GetPagedAsync<TOrderKey>(int page, int perPage,
        Expression<Func<TModel, bool>> predicate, Expression<Func<TModel, TOrderKey>> keySelector)
    {
        return await _dbSet
            .Where(predicate)
            .OrderBy(keySelector)
            .AsPagedCollectionAsync(page, perPage).ConfigureAwait(false);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    private (string query, DynamicParameters dynamicParameters) PreparePagedQuery(string sql, string orderby,
        int page, int resultsPerPage, object parameters)
    {
        var param = new Dictionary<string, object>();
        var query = _sqlGenerator.SelectPaged(sql, orderby, page, resultsPerPage, param);
        var dynamicParameters = new DynamicParameters(parameters);

        foreach (var parameter in param) dynamicParameters.Add(parameter.Key, parameter.Value);
        return (query, dynamicParameters);
    }
}