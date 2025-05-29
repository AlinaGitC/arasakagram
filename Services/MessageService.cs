using System.Collections.Generic;
using System.Threading.Tasks;
using arasakagram.Api;

namespace arasakagram.Services
{
    public class MessageService
    {
        private readonly Client _client;
        public MessageService(Client client) { _client = client; }

        public async Task<ICollection<MessageDto>> GetChatMessagesAsync(int chatId)
            => await _client.ChatAsync(chatId);

        public async Task SendMessageAsync(SendMessageDto dto)
            => await _client.SendAsync(dto);
    }
} 