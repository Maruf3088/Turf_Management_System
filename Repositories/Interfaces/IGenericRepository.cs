using System.Linq.Expressions;
using turf_management_system.Models.Pagination;

namespace turf_management_system.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(string? includeProperties = "");
        Task<T?> GetByIdAsync(object id);
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate, string? includeProperties = "");
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string? includeProperties = "");
        Task<int> GetCountAsync(Expression<Func<T, bool>>? predicate = null);
    }
}
