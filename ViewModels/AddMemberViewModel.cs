using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using arasakagram.Api;
using arasakagram.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Windows;

namespace arasakagram.ViewModels
{
    public class AddMemberViewModel : ObservableObject
    {
        private readonly ChatService _chatService;
        private readonly UserService _userService;
        private readonly int _chatId;
        private readonly int _currentUserId;
        private List<UserDto> _allUsers = new List<UserDto>();
        private readonly List<int> _existingMemberIds;

        public AddMemberViewModel(ChatService chatService, UserService userService, int chatId, int currentUserId, IEnumerable<int> existingMemberIds = null)
        {
            _chatService = chatService;
            _userService = userService;
            _chatId = chatId;
            _currentUserId = currentUserId;
            _existingMemberIds = existingMemberIds?.ToList() ?? new List<int>();
            
            AddMemberCommand = new AsyncRelayCommand(AddMemberAsync);
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            
            Users = new ObservableCollection<UserDto>();
            FilteredUsers = new ObservableCollection<UserDto>();
            
            // Загружаем пользователей
            LoadUsers().ConfigureAwait(false);
        }

        public ObservableCollection<UserDto> Users { get; }
        public ObservableCollection<UserDto> FilteredUsers { get; }
        
        private UserDto _selectedUser;
        public UserDto SelectedUser 
        { 
            get => _selectedUser; 
            set => SetProperty(ref _selectedUser, value); 
        }

        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    // При изменении поискового запроса сразу обновляем отфильтрованный список
                    FilterUsers();
                }
            }
        }

        private int _roleId = 2; // 1 — админ, 2 — обычный участник
        public int RoleId { get => _roleId; set => SetProperty(ref _roleId, value); }

        public IAsyncRelayCommand AddMemberCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public event System.Action MemberAdded;

        private async Task LoadUsers()
        {
            try
            {
                // После загрузки участников загружаем всех пользователей
                var allUsers = await _userService.GetAllUsersAsync();
                
                // Фильтруем пользователей: исключаем текущего пользователя и уже добавленных участников
                _allUsers = allUsers
                    .Where(u => u.Id != _currentUserId && !_existingMemberIds.Contains(u.Id))
                    .ToList();
                
                Users.Clear();
                FilteredUsers.Clear();
                
                foreach (var user in _allUsers)
                {
                    Users.Add(user);
                    FilteredUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке пользователей: {ex.Message}");
            }
        }
        
        private async Task SearchAsync()
        {
            await Task.CompletedTask; // Асинхронная заглушка
            FilterUsers();
        }
        
        private void FilterUsers()
        {
            try
            {
                FilteredUsers.Clear();
                
                if (string.IsNullOrWhiteSpace(SearchQuery))
                {
                    // Если поисковый запрос пуст, показываем всех пользователей, кроме текущего и уже добавленных
                    foreach (var user in _allUsers)
                    {
                        if (user.Id != _currentUserId && !_existingMemberIds.Contains(user.Id))
                        {
                            FilteredUsers.Add(user);
                        }
                    }
                }
                else
                {
                    // Фильтруем пользователей по поисковому запросу
                    var query = SearchQuery.Trim().ToLower();
                    var filtered = _allUsers
                        .Where(u => 
                            (u.Id != _currentUserId && !_existingMemberIds.Contains(u.Id)) &&
                            ((u.Login != null && u.Login.ToLower().Contains(query)) ||
                             (u.Firstname != null && u.Firstname.ToLower().Contains(query)) ||
                             (u.LastName != null && u.LastName.ToLower().Contains(query)) ||
                             (u.MiddleName != null && u.MiddleName.ToLower().Contains(query)) ||
                             ($"{u.Firstname} {u.LastName} {u.MiddleName}".ToLower().Contains(query))))
                        .ToList();
                    
                    foreach (var user in filtered)
                    {
                        FilteredUsers.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при фильтрации пользователей: {ex.Message}");
            }
        }

        private async Task AddMemberAsync()
        {
            if (SelectedUser == null) return;
            
            try
            {
                var dto = new AddChatMemberDto
                {
                    ID_Chat = _chatId,
                    ID_Userr = SelectedUser.Id,
                    ID_ChatRole = RoleId
                };
                
                await _chatService.AddMemberAsync(_currentUserId, dto);
                
                // Обновляем список участников и пользователей
                _existingMemberIds.Add(SelectedUser.Id);
                _allUsers.Remove(SelectedUser);
                FilteredUsers.Remove(SelectedUser);
                
                MemberAdded?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении участника: {ex.Message}");
                // Можно показать сообщение об ошибке пользователю
                MessageBox.Show($"Не удалось добавить участника: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}