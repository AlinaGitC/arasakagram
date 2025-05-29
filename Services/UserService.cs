using System.Collections.Generic;
using System.Threading.Tasks;
using arasakagram.Api;
using System.Linq;

namespace arasakagram.Services
{
    public class UserService
    {
        private readonly Client _client;
        public UserService(Client client) { _client = client; }

        public async Task<ICollection<UserDto>> GetAllUsersAsync()
            => await _client.UserAllAsync();

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var users = await _client.UserAllAsync();
            return users.FirstOrDefault(u => u.Id == userId);
        }
    }
} 