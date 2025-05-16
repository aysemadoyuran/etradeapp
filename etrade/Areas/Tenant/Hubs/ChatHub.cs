using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using etrade.Entity;
using etrade.Data.Concrete;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    private static readonly Dictionary<string, string> _userConnections = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> _adminConnections = new Dictionary<string, string>();
    private readonly TenantContext _context;

    public ChatHub(TenantContext context)
    {
        _context = context;
    }

    // Kullanıcı bağlandığında
    public async Task RegisterUser(string userId, string tenant)
    {
        _userConnections[userId] = Context.ConnectionId;

        // Adminlere yeni kullanıcının bağlandığını bildir
        await Clients.All.SendAsync("UserConnected", userId, tenant);
    }

    // Admin bağlandığında
    [Authorize(Roles = "TenantAdmin")]
    public async Task RegisterAdmin()
    {
        var userId = Context.UserIdentifier;
        _adminConnections[userId] = Context.ConnectionId;

        // Tüm adminlere yeni adminin bağlandığını bildir
        await Clients.All.SendAsync("AdminConnected", userId);
    }

    // Kullanıcıdan mesaj gönderme
    public async Task SendMessageToAdmin(string content, string tenant)
    {
        try
        {
            var fromUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(fromUserId))
            {
                throw new HubException("Kullanıcı kimliği bulunamadı");
            }

            var message = new Message
            {
                FromUserId = fromUserId,
                Tenant = "Aysoft.com",
                Content = content,
                Timestamp = DateTime.UtcNow,
                FromConnectionId = Context.ConnectionId
            };
            Console.WriteLine(">>> Gelen Message Bilgileri <<<");
            Console.WriteLine($"FromUserId: {fromUserId}");
            Console.WriteLine($"Tenant: {tenant}");
            Console.WriteLine($"Content: {content}");
            Console.WriteLine($"ConnectionId: {Context.ConnectionId}");
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessageFromUser", fromUserId, content, message.Timestamp, tenant);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata oluştu: " + ex.Message);

            if (ex.InnerException != null)
            {
                Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
            }

            Console.WriteLine("StackTrace: " + ex.StackTrace);
        }
    }

    // Admin'den kullanıcıya mesaj gönderme
    [Authorize(Roles = "TenantAdmin")]
    public async Task SendMessageToUser(string toUserId, string content)
    {
        var fromUserId = Context.UserIdentifier;
        var message = new Message
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Content = content,
            Timestamp = DateTime.UtcNow,
            FromConnectionId = Context.ConnectionId
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Belirli kullanıcıya mesajı gönder
        if (_userConnections.TryGetValue(toUserId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessageFromAdmin", fromUserId, content, message.Timestamp);
        }
    }

    // Bağlantı kesildiğinde
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.UserIdentifier;

        if (_userConnections.ContainsKey(userId))
        {
            _userConnections.Remove(userId);
            await Clients.All.SendAsync("UserDisconnected", userId);
        }

        if (_adminConnections.ContainsKey(userId))
        {
            _adminConnections.Remove(userId);
            await Clients.All.SendAsync("AdminDisconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Önceki mesajları getirme
    public async Task<List<Message>> GetMessageHistory(string otherUserId)
    {
        var userId = Context.UserIdentifier;
        return await _context.Messages
            .Where(m => (m.FromUserId == userId && m.ToUserId == otherUserId) ||
                         (m.FromUserId == otherUserId && m.ToUserId == userId))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }
}