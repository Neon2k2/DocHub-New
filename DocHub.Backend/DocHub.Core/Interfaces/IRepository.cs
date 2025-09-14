using System.Linq.Expressions;

namespace DocHub.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task UpdateRangeAsync(IEnumerable<T> entities);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(IEnumerable<T> entities);
    Task DeleteByIdAsync(Guid id);

    Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool ascending = true);
    Task<IEnumerable<T>> GetIncludingAsync(Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includeProperties);
    Task<T?> GetFirstIncludingAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties);
}
