using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;

namespace TasteAtDoor.Hubs
{
    [Authorize]
    public class OrderCallHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderCallHub(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task JoinOrderCall(int orderId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);

            if (user is null || !await CanAccessOrderAsync(orderId, user))
            {
                throw new HubException("You are not allowed to join this call.");
            }

            var groupName = GetGroupName(orderId);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await Clients.OthersInGroup(groupName).SendAsync(
                "PeerJoined",
                new
                {
                    orderId,
                    userName = string.IsNullOrWhiteSpace(user.FullName)
                        ? user.Email
                        : user.FullName
                });
        }

        public async Task LeaveOrderCall(int orderId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);

            if (user is null || !await CanAccessOrderAsync(orderId, user))
            {
                return;
            }

            var groupName = GetGroupName(orderId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.OthersInGroup(groupName).SendAsync("PeerLeft", orderId);
        }

        public async Task SendOffer(int orderId, string offerJson)
        {
            var user = await _userManager.GetUserAsync(Context.User!);

            if (user is null || !await CanAccessOrderAsync(orderId, user))
            {
                throw new HubException("You are not allowed to send an offer for this order.");
            }

            await Clients.OthersInGroup(GetGroupName(orderId)).SendAsync("ReceiveOffer", offerJson);
        }

        public async Task SendAnswer(int orderId, string answerJson)
        {
            var user = await _userManager.GetUserAsync(Context.User!);

            if (user is null || !await CanAccessOrderAsync(orderId, user))
            {
                throw new HubException("You are not allowed to send an answer for this order.");
            }

            await Clients.OthersInGroup(GetGroupName(orderId)).SendAsync("ReceiveAnswer", answerJson);
        }

        public async Task SendIceCandidate(int orderId, string candidateJson)
        {
            var user = await _userManager.GetUserAsync(Context.User!);

            if (user is null || !await CanAccessOrderAsync(orderId, user))
            {
                throw new HubException("You are not allowed to send ICE candidates for this order.");
            }

            await Clients.OthersInGroup(GetGroupName(orderId)).SendAsync("ReceiveIceCandidate", candidateJson);
        }

        public async Task HangUp(int orderId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);

            if (user is null || !await CanAccessOrderAsync(orderId, user))
            {
                return;
            }

            await Clients.OthersInGroup(GetGroupName(orderId)).SendAsync("CallEnded", orderId);
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

        private static string GetGroupName(int orderId)
        {
            return $"order-call-{orderId}";
        }
    }
}