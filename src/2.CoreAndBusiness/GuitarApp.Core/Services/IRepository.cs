using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GuitarApp.Core.Services;

public interface IRepository<T> where T : class, new()
{
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<int> SaveAsync(T entity);
    Task<int> DeleteAsync(T entity);
}
