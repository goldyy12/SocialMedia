using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    // Only authenticated users may connect. Each connection is placed in a group
    // named after the user id taken from the validated JWT — never from client
    // input — so a user can only ever receive their own messages.
    [Authorize]
    public class ChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.GetCurrentUserId();
            if (userId == null)
            {
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());
            await base.OnConnectedAsync();
        }
    }
}
