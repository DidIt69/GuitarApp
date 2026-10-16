#region Imports
using SQLite;
using GuitarApp.Core.Services;
using System.Linq.Expressions;
#endregion

namespace GuitarApp.Infrastructure.Data;

public class SQLiteRepository<T> : IRepository<T> where T : class, new()
{
    #region Constants
    private readonly SQLiteAsyncConnection _db;
    #endregion

    #region COnstructors
    /// <summary>
    /// 
    /// </summary>
    public SQLiteRepository() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "guitar_app.db3")) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbPath"></param>
    public SQLiteRepository(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
        _db.CreateTableAsync<T>().Wait();
    }
    #endregion

    #region Implement IRepository<>
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task<List<T>> GetAllAsync() => _db.Table<T>().ToListAsync();

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate) => _db.Table<T>().Where(predicate).ToListAsync();

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task<int> SaveAsync(T entity) => _db.InsertOrReplaceAsync(entity);

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task<int> DeleteAsync(T entity) => _db.DeleteAsync(entity);
    #endregion
}
