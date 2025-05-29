using System.Collections.Generic;
using System.Threading.Tasks;
using arasakagram.Api;

namespace arasakagram.Services
{
    public class ChatService
    {
        private readonly Client _client;
        public ChatService(Client client) { _client = client; }

        public async Task<ICollection<ChatDto>> GetUserChatsAsync(int userId)
            => await _client.UserAsync(userId);

        public async Task CreateChatAsync(CreateChatDto dto)
            => await _client.CreateAsync(dto);

        public async Task AddMemberAsync(int currentUserId, AddChatMemberDto dto)
            => await _client.AddMemberAsync(currentUserId, dto);
    }
} 