using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class TurfImageRepository : GenericRepository<TurfImage>, ITurfImageRepository
    {
        public TurfImageRepository(AppDbContext context) : base(context)
        {
        }
    }
}
