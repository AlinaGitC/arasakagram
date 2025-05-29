using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using arasakagram.Api;
using arasakagram.Services;
using System.Threading.Tasks;

namespace arasakagram.ViewModels
{
    public class CreateChatViewModel : ObservableObject
    {
        private readonly ChatService _chatService;
        private readonly int _currentUserId;
        public CreateChatViewModel(ChatService chatService, int currentUserId)
        {
            _chatService = chatService;
            _currentUserId = currentUserId;
            CreateChatCommand = new AsyncRelayCommand(CreateChatAsync);
        }

        private string _name;
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        private string _chatTopic;
        public string ChatTopic { get => _chatTopic; set => SetProperty(ref _chatTopic, value); }

        private int _chatType = 1; // 1 — групповой, 2 — личный (пример)
        public int ChatType { get => _chatType; set => SetProperty(ref _chatType, value); }

        private int? _memberUserId;
        public int? MemberUserId { get => _memberUserId; set => SetProperty(ref _memberUserId, value); }

        public IAsyncRelayCommand CreateChatCommand { get; }
        public event System.Action ChatCreated;

        private async Task CreateChatAsync()
        {
            var dto = new CreateChatDto
            {
                Name = Name,
                ChatTopic = ChatTopic,
                ID_ChatType = ChatType,
                ID_User_CreatedBy = _currentUserId,
                MemberUserId = MemberUserId ?? 0 // Передаём id второго пользователя для личного чата
            };
            await _chatService.CreateChatAsync(dto);
            ChatCreated?.Invoke();
        }
    }
} 