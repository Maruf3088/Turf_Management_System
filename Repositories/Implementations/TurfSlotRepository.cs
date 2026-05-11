using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class TurfSlotRepository : GenericRepository<TurfSlot>, ITurfSlotRepository
    {
        public TurfSlotRepository(AppDbContext context) : base(context)
        {
        }
    }
}
