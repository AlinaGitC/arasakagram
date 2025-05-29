using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using arasakagram.Api;
using arasakagram.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace arasakagram.ViewModels
{
    public class SearchUsersViewModel : ObservableObject
    {
        private readonly UserService _userService;
        private readonly ChatService _chatService;
        public SearchUsersViewModel(UserService userService, ChatService chatService)
        {
            _userService = userService;
            _chatService = chatService;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            CreateChatWithUserCommand = new AsyncRelayCommand<UserDto>(CreateChatWithUserAsync);
            Users = new ObservableCollection<UserDto>();
        }

        private string _query;
        public string Query { get => _query; set => SetProperty(ref _query, value); }

        public ObservableCollection<UserDto> Users { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand<UserDto> CreateChatWithUserCommand { get; }

        private UserDto _selectedUser;
        public UserDto SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public event System.Action ChatCreated;

        private async Task SearchAsync()
        {
            Users.Clear();
            var allUsers = await _userService.GetAllUsersAsync();
            foreach (var user in allUsers)
            {
                if (string.IsNullOrWhiteSpace(Query) ||
                    user.Login.Contains(Query, System.StringComparison.OrdinalIgnoreCase) ||
                    user.Firstname.Contains(Query, System.StringComparison.OrdinalIgnoreCase) ||
                    user.LastName.Contains(Query, System.StringComparison.OrdinalIgnoreCase))
                {
                    Users.Add(user);
                }
            }
        }

        private async Task CreateChatWithUserAsync(UserDto user)
        {
            if (user == null || AuthService.CurrentUserId == null) return;
            var dto = new CreateChatDto
            {
                Name = user.Login, // или другое отображаемое имя
                ID_User_CreatedBy = AuthService.CurrentUserId.Value,
                ID_ChatType = 1, // 1 — личный чат
                MemberUserId = user.Id,
                ChatTopic = "Личный чат"
            };
            await _chatService.CreateChatAsync(dto);
            ChatCreated?.Invoke(); // Событие для закрытия окна и обновления чатов
        }
    }
} 