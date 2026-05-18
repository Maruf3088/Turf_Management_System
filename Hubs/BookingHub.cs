using Microsoft.AspNetCore.SignalR;

namespace turf_management_system.Hubs
{
    /// <summary>
    /// SignalR hub for real-time slot availability updates.
    /// Clients join a group per turf+date: "turf-{turfId}-{date}"
    /// </summary>
    public class BookingHub : Hub
    {
        // Client joins the group for a specific turf+date to receive live updates
        public async Task JoinTurfDateGroup(string turfId, string date)
        {
            var groupName = GetGroupName(turfId, date);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveTurfDateGroup(string turfId, string date)
        {
            var groupName = GetGroupName(turfId, date);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        private static string GetGroupName(string turfId, string date)
            => $"turf-{turfId}-{date}";
    }

    /// <summary>
    /// Helper service injected into BookingService/Controllers to push real-time events.
    /// </summary>
    public class BookingHubNotifier
    {
        private readonly IHubContext<BookingHub> _hub;

        public BookingHubNotifier(IHubContext<BookingHub> hub)
        {
            _hub = hub;
        }

        public async Task NotifySlotLocked(string turfId, string date, string slotId, DateTime lockedUntil, int lockedByUserId)
        {
            var group = $"turf-{turfId}-{date}";
            await _hub.Clients.Group(group).SendAsync("SlotLocked", slotId, lockedUntil, lockedByUserId);
        }


        public async Task NotifySlotReleased(string turfId, string date, string slotId)
        {
            var group = $"turf-{turfId}-{date}";
            await _hub.Clients.Group(group).SendAsync("SlotReleased", slotId);
        }

        public async Task NotifySlotBooked(string turfId, string date, string slotId)
        {
            var group = $"turf-{turfId}-{date}";
            await _hub.Clients.Group(group).SendAsync("SlotBooked", slotId);
        }
    }
}
