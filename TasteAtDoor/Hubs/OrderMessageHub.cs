using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;

namespace TasteAtDoor.Hubs
{
    [Authorize]
    public class OrderMessageHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderMessageHub(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task JoinOrderChat(int orderId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);

            if (user is null || !await CanAccessOrderAsync(orderId, user))
            {
                throw new HubException("You are not allowed to join this chat.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(orderId));
        }

        public async Task LeaveOrderChat(int orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(orderId));
        }

        public static string GetGroupName(int orderId)
        {
            return $"order-chat-{orderId}";
        }

        private async Task<bool> CanAccessOrderAsync(int orderId, ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return true;
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null)
            {
                return false;
            }

            if (order.ApplicationUserId == user.Id)
            {
                return true;
            }

            var isCatererForThisOrder = order.OrderItems.Any(oi =>
                oi.MenuItem != null &&
                oi.MenuItem.CaretakerId == user.Id);

            return isCatererForThisOrder;
        }
    }
}
