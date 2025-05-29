using System.Threading.Tasks;
using arasakagram.Api;

namespace arasakagram.Services
{
    public class GroupService
    {
        private readonly Client _client;
        public GroupService(Client client) { _client = client; }

        public async Task CreateGroupAsync(CreateGroupDto dto)
            => await _client.GroupPOSTAsync(dto);

        public async Task<ICollection<GroupDto>> GetUserGroupsAsync(int userId)
            => await _client.GroupUserAsync(userId);
    }
} 