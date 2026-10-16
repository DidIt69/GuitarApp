using SQLite;
using GuitarApp.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GuitarApp.Infrastructure.Data;

public class SQLiteRepository<T> : IRepository<T> where T : class, new()
{
    private readonly SQLiteAsyncConnection _db;

    public SQLiteRepository() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "guitar_app.db3"))
    {
    }

    public SQLiteRepository(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
        _db.CreateTableAsync<T>().Wait();
    }

    public Task<List<T>> GetAllAsync() => _db.Table<T>().ToListAsync();
    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate) => _db.Table<T>().Where(predicate).ToListAsync();
    public Task<int> SaveAsync(T entity) => _db.InsertOrReplaceAsync(entity);
    public Task<int> DeleteAsync(T entity) => _db.DeleteAsync(entity);
}
