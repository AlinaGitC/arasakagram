using arasakagram.Api;
using arasakagram.Models;
using arasakagram.Services;
using arasakagram.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace arasakagram.ViewModels
{
    public class MessengerViewModel : ObservableObject
    {
        private readonly Client _apiClient;
        private readonly ChatService _chatService;
        private readonly GroupService _groupService;
        private readonly UserService _userService;
        private readonly SignalRService _signalRService;
        private const string SignalRUrl = "http://localhost:5047";
        private int? _currentSignalRChatId;
        private readonly Dictionary<int, string> _userNameCache = new();
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<ChatModel> Chats { get; } = new();
        public ObservableCollection<MessageModel> Messages { get; } = new();

        private ChatModel selectedChat;
        public ChatModel SelectedChat
        {
            get => selectedChat;
            set
            {
                if (SetProperty(ref selectedChat, value) && value != null)
                {
                    ConnectSignalR(value.Id);
                    LoadMessagesForChatAsync(value.Id);
                    SelectedChatName = value.Name;
                    UpdateChatMembersInfoAsync(value.Id);
                }
                OnPropertyChanged(nameof(IsAddMemberVisible));
            }
        }

        private string messageText;
        public string MessageText
        {
            get => messageText;
            set => SetProperty(ref messageText, value);
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }

        public ICommand SendMessageCommand { get; }
        public ICommand OpenCreateGroupCommand { get; }
        public ICommand OpenSearchUsersCommand { get; }
        public ICommand OpenAddMemberCommand { get; }
        public ICommand CreateChatInGroupCommand { get; }
        public ICommand AttachFileCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand EditProfileCommand { get; }

        public enum ChatSection { Personal, Groups }
        private ChatSection selectedSection = ChatSection.Personal;
        public ChatSection SelectedSection
        {
            get => selectedSection;
            set => SetProperty(ref selectedSection, value);
        }

        public ObservableCollection<ChatModel> PersonalChats { get; } = new();
        public ObservableCollection<GroupWithChatsModel> GroupsWithChats { get; } = new();

        public ICommand SelectPersonalCommand { get; }
        public ICommand SelectGroupsCommand { get; }

        private string selectedChatName;
        public string SelectedChatName
        {
            get => selectedChatName;
            set => SetProperty(ref selectedChatName, value);
        }
        private int selectedChatMembersCount;
        public int SelectedChatMembersCount
        {
            get => selectedChatMembersCount;
            set => SetProperty(ref selectedChatMembersCount, value);
        }
        private int selectedChatOnlineCount;
        public int SelectedChatOnlineCount
        {
            get => selectedChatOnlineCount;
            set => SetProperty(ref selectedChatOnlineCount, value);
        }

        public ObservableCollection<FileModel> SelectedFiles { get; } = new();
        public IRelayCommand<FileModel> RemoveSelectedFileCommand { get; }

        public class GroupWithChatsModel
        {
            public int GroupId { get; set; }
            public string GroupName { get; set; }
            public ObservableCollection<ChatModel> Chats { get; set; } = new();
        }

        private UserInfo currentUser;
        public UserInfo CurrentUser
        {
            get => currentUser;
            set => SetProperty(ref currentUser, value);
        }
        public ICommand ShowAboutCommand { get; }

        public Action OpenEditProfileAction { get; set; }

        private string searchText;
        public string SearchText
        {
            get => searchText;
            set
            {
                SetProperty(ref searchText, value);
                if (IsSearchMode)
                    UpdateSearchResults();
            }
        }

        private bool isSearchMode;
        public bool IsSearchMode
        {
            get => isSearchMode;
            set => SetProperty(ref isSearchMode, value);
        }

        public ObservableCollection<UserInfo> SearchUsers { get; } = new();

        public IRelayCommand<MessageModel> DeleteMessageCommand { get; }
        public IRelayCommand DeleteChatCommand { get; }
        public IAsyncRelayCommand RefreshGroupsCommand { get; }

        public bool IsAddMemberVisible
        {
            get
            {
                // Кнопка видна только если выбран чат с типом 0 (групповой)
                return SelectedChat != null && SelectedChat.ID_ChatType == 0;
            }
        }

        public MessengerViewModel(Client apiClient, ChatService chatService, GroupService groupService, UserService userService, SignalRService signalRService)
        {
            _apiClient = apiClient;
            _chatService = chatService;
            _groupService = groupService;
            _userService = userService;
            _signalRService = signalRService;
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
            OpenCreateGroupCommand = new RelayCommand(OpenCreateGroup);
            OpenSearchUsersCommand = new RelayCommand(OpenSearchUsers);
            OpenAddMemberCommand = new RelayCommand(OpenAddMember);
            CreateChatInGroupCommand = new AsyncRelayCommand<int>(CreateChatInGroupAsync);
            SelectPersonalCommand = new RelayCommand(() => SelectedSection = ChatSection.Personal);
            SelectGroupsCommand = new RelayCommand(() => SelectedSection = ChatSection.Groups);
            AttachFileCommand = new AsyncRelayCommand(AttachFileAsync);
            RemoveSelectedFileCommand = new RelayCommand<FileModel>(file =>
            {
                if (file != null)
                    SelectedFiles.Remove(file);
            });
            LogoutCommand = new RelayCommand(Logout);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            EditProfileCommand = new RelayCommand(OpenEditProfile);
            _signalRService.MessageReceived += OnSignalRMessageReceived;
            _signalRService.GroupMembersChanged += OnGroupMembersChanged;
            _signalRService.MessageDeleted += OnSignalRMessageDeleted;
            _signalRService.ChatDeleted += OnSignalRChatDeleted;
            LoadChatsAsync();
            DeleteMessageCommand = new AsyncRelayCommand<MessageModel>(DeleteMessageAsync);
            DeleteChatCommand = new AsyncRelayCommand(DeleteChatAsync);
            RefreshGroupsCommand = new AsyncRelayCommand(RefreshGroupsAsync);
        }

        public async Task LoadChatsAsync()
        {
            try
            {
                PersonalChats.Clear();
                GroupsWithChats.Clear();
                if (AuthService.CurrentUserId == null)
                {
                    ErrorMessage = "Пользователь не авторизован.";
                    return;
                }

                if (AuthService.CurrentUserId.HasValue)
                {
                    // Личные чаты
                    var personalChats = await _chatService.GetUserChatsAsync(AuthService.CurrentUserId.Value);
                    foreach (var dto in personalChats)
                    {
                        var members = await _apiClient.MembersAsync(dto.Id);
                        var otherUser = members.FirstOrDefault(u => u.Id != AuthService.CurrentUserId);
                        string chatName = otherUser?.Login ?? "Неизвестный пользователь";
                        string lastMessage = string.Empty;
                        var messages = (await _apiClient.ChatAsync(dto.Id))?.ToList();
                        if (messages != null && messages.Count > 0)
                            lastMessage = messages[^1].Text;
                        byte[] avatar = otherUser?.Avatar; // аватар собеседника
                        var chatModel = new ChatModel
                        {
                            Id = dto.Id,
                            Name = chatName,
                            LastMessage = lastMessage,
                            Avatar = avatar,
                            ID_ChatType = dto.ID_ChatType,
                            CreatedByUserId = dto.CreatedByUserId
                        };
                        PersonalChats.Add(chatModel);
                    }
                    // Группы и их чаты
                    await LoadGroupsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке чатов: {ex.Message}");
                MessageBox.Show("Не удалось загрузить чаты. Пожалуйста, проверьте подключение к интернету и попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadGroupsAsync()
        {
            if (!AuthService.CurrentUserId.HasValue) return;

            try
            {
                var groupDtos = await _groupService.GetUserGroupsAsync(AuthService.CurrentUserId.Value);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Пользователь состоит в {groupDtos.Count} группах");
                
                // Создаем временный список для новых групп
                var newGroups = new List<GroupWithChatsModel>();
                
                foreach (var group in groupDtos)
                {
                    var groupModel = new GroupWithChatsModel
                    {
                        GroupId = group.Id,
                        GroupName = group.Name
                    };
                    
                    var groupChats = await _apiClient.ChatsAsync(group.Id);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Группа '{group.Name}' (ID={group.Id}) содержит {groupChats.Count} чатов");
                    
                    foreach (var dto in groupChats)
                    {
                        var members = await _apiClient.MembersAsync(dto.Id);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Чат '{dto.Name}' (ID={dto.Id}) участников: {members.Count}, содержит пользователя: {members.Any(u => u.Id == AuthService.CurrentUserId.Value)}");
                        if (!members.Any(u => u.Id == AuthService.CurrentUserId.Value))
                            continue; // Пропустить чат, если пользователь не участник
                            
                        string lastMessage = string.Empty;
                        var messages = (await _apiClient.ChatAsync(dto.Id))?.ToList();
                        if (messages != null && messages.Count > 0)
                            lastMessage = messages[^1].Text;
                            
                        var chatModel = new ChatModel
                        {
                            Id = dto.Id,
                            Name = dto.Name,
                            LastMessage = lastMessage,
                            Avatar = null, // групповые чаты не имеют аватарок
                            ID_ChatType = dto.ID_ChatType,
                            CreatedByUserId = dto.CreatedByUserId
                        };
                        groupModel.Chats.Add(chatModel);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] В группу '{group.Name}' добавлено {groupModel.Chats.Count} чатов для пользователя");
                    newGroups.Add(groupModel);
                }
                
                // Обновляем коллекцию групп в основном потоке
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    GroupsWithChats.Clear();
                    foreach (var group in newGroups)
                    {
                        GroupsWithChats.Add(group);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке групп: {ex}");
                throw; // Пробрасываем исключение, чтобы обработать его в вызывающем коде
            }
        }

        private async Task RefreshGroupsAsync()
        {
            try
            {
                IsLoading = true;
                await LoadGroupsAsync();
                System.Diagnostics.Debug.WriteLine("[DEBUG] Список групп успешно обновлен");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Ошибка при обновлении групп: {ex.Message}");
                MessageBox.Show("Не удалось обновить список групп. Пожалуйста, проверьте подключение к интернету и попробуйте снова.", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadMessagesForChatAsync(int chatId)
        {
            try
            {
                Messages.Clear();
                var messageDtos = await _apiClient.ChatAsync(chatId);
                foreach (var dto in messageDtos)
                {
                    byte[] avatar = null;
                    if (dto.UserId != 100) // если не бот
                    {
                        var user = await _userService.GetUserByIdAsync(dto.UserId);
                        avatar = user?.Avatar;
                    }
                    Messages.Add(new MessageModel
                    {
                        Id = dto.Id,
                        ChatId = dto.ChatId,
                        Sender = dto.UserLogin,
                        Text = dto.Text,
                        Time = dto.Timestamp.DateTime,
                        IsOwn = AuthService.CurrentUserId.HasValue && dto.UserId == AuthService.CurrentUserId.Value,
                        Files = dto.Files?.Select(f => new FileModel
                        {
                            Id = f.Id,
                            FileName = f.FileName,
                            FileTypeName = f.FileTypeName,
                            URL = f.Url
                        }).ToList() ?? new List<FileModel>(),
                        Avatar = avatar
                    });
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is arasakagram.Views.MessengerWindow win)
                        win.ScrollMessagesToEnd();
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private async Task SendMessageAsync()
        {
            try
            {
                // Запрет на отправку файлов без текста
                if (string.IsNullOrWhiteSpace(MessageText))
                {
                    MessageBox.Show("Нельзя отправлять файлы без текстового сообщения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var fileIds = SelectedFiles.Select(f => f.Id).ToList();
                var dto = new SendMessageDto
                {
                    ID_Chat = SelectedChat?.Id ?? 0,
                    ID_User = AuthService.CurrentUserId ?? 0,
                    ContentMessages = MessageText,
                    Files = fileIds
                };
                await _apiClient.SendAsync(dto);
                MessageText = string.Empty;
                SelectedFiles.Clear();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is arasakagram.Views.MessengerWindow win)
                        win.ScrollMessagesToEnd();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCreateGroup()
        {
            var vm = new CreateGroupViewModel(_groupService);
            var win = new Window
            {
                Title = "Создать группу",
                Content = new CreateGroupView { DataContext = vm },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };
            vm.GroupCreated += () => { win.Close(); LoadChatsAsync(); };
            win.ShowDialog();
        }

        private void OpenSearchUsers()
        {
            var vm = new SearchUsersViewModel(_userService, _chatService);
            var win = new Window
            {
                Title = "Поиск пользователей",
                Content = new SearchUsersView { DataContext = vm },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };
            vm.ChatCreated += () => { win.Close(); LoadChatsAsync(); };
            win.ShowDialog();
        }

        private async void OpenAddMember()
        {
            if (SelectedChat == null) return;
            
            try
            {
                // Получаем текущих участников чата
                var members = await _apiClient.MembersAsync(SelectedChat.Id);
                var memberIds = members.Select(m => m.Id).ToList();
                
                var vm = new AddMemberViewModel(_chatService, _userService, SelectedChat.Id, AuthService.CurrentUserId ?? 0, memberIds);
                var win = new Window
                {
                    Title = "Добавить участника",
                    Content = new AddMemberView { DataContext = vm },
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow
                };
                vm.MemberAdded += () => { win.Close(); LoadChatsAsync(); };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке участников чата: {ex.Message}");
                MessageBox.Show("Не удалось загрузить список участников чата", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ConnectSignalR(int chatId)
        {
            if (_currentSignalRChatId.HasValue)
            {
                await _signalRService.DisconnectAsync(_currentSignalRChatId.Value);
            }
            await _signalRService.ConnectAsync(SignalRUrl, chatId);
            _currentSignalRChatId = chatId;
        }

        private async void OnSignalRMessageReceived(int userId, string text, DateTime timestamp, List<FileDto> files)
        {
            string senderName;
            if (userId == 100)
            {
                senderName = "Бот";
            }
            else if (!_userNameCache.TryGetValue(userId, out senderName))
            {
                var user = await _userService.GetUserByIdAsync(userId);
                senderName = user?.Login ?? userId.ToString();
                _userNameCache[userId] = senderName;
            }
            var fileModels = files?.Select(f => new FileModel
            {
                Id = f.Id,
                FileName = f.FileName,
                FileTypeName = f.FileTypeName,
                URL = f.Url
            }).ToList() ?? new List<FileModel>();

            byte[] avatar = null;
            if (userId == 100)
            {
                avatar = null; // бот
            }
            else if (AuthService.CurrentUserId.HasValue && userId == AuthService.CurrentUserId.Value && CurrentUser != null)
            {
                avatar = CurrentUser.Avatar;
            }
            else
            {
                var user = await _userService.GetUserByIdAsync(userId);
                avatar = user?.Avatar;
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new MessageModel
                {
                    Sender = senderName,
                    Text = text,
                    Time = timestamp,
                    IsOwn = AuthService.CurrentUserId.HasValue && userId == AuthService.CurrentUserId.Value,
                    Files = fileModels,
                    Avatar = avatar
                });
                if (Application.Current.MainWindow is arasakagram.Views.MessengerWindow win)
                    win.ScrollMessagesToEnd();
            });
        }

        private async Task CreateChatInGroupAsync(int groupId)
        {
            var inputDialog = new InputDialog("Введите имя и тему чата:");
            if (inputDialog.ShowDialog() == true)
            {
                var chatName = inputDialog.ChatName;
                var chatTopic = inputDialog.ChatTopic;
                var dto = new CreateChatDto
                {
                    Name = chatName,
                    ChatTopic = chatTopic,
                    ID_User_CreatedBy = AuthService.CurrentUserId.Value,
                    ID_ChatType = 2 // 2 — групповой чат
                };
                await _apiClient.CreateAsync(dto);
                // Получаем id только что созданного чата
                var allChats = await _apiClient.UserAsync(AuthService.CurrentUserId.Value);
                var createdChat = allChats.LastOrDefault(c => c.Name == chatName);
                if (createdChat != null)
                {
                    await _apiClient.AddChatAsync(groupId, createdChat.Id);
                }
                LoadChatsAsync();
            }
        }

        private async void UpdateChatMembersInfoAsync(int chatId)
        {
            try
            {
                var members = await _apiClient.MembersAsync(chatId);
                SelectedChatMembersCount = members.Count;
                SelectedChatOnlineCount = members.Count(m => m.IsActive); // предполагается, что IsActive = онлайн
            }
            catch
            {
                SelectedChatMembersCount = 0;
                SelectedChatOnlineCount = 0;
            }
        }

        private void OnGroupMembersChanged(int chatId, int membersCount)
        {
            if (SelectedChat != null && SelectedChat.Id == chatId)
                SelectedChatMembersCount = membersCount;
        }

        private async Task AttachFileAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Все поддерживаемые файлы|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.pdf;*.doc;*.docx;*.txt|Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Документы|*.pdf;*.doc;*.docx;*.txt",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                var fileName = System.IO.Path.GetFileName(filePath);
                try
                {
                    using var stream = System.IO.File.OpenRead(filePath);
                    using var content = new System.Net.Http.MultipartFormDataContent();
                    content.Add(new System.Net.Http.StreamContent(stream), "File", fileName);

                    using var httpClient = new System.Net.Http.HttpClient();
                    var response = await httpClient.PostAsync("http://localhost:5047/api/File/upload", content);

                    var json = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                        int? fileId = result.id;
                        if (fileId == null)
                        {
                            MessageBox.Show("Ошибка: сервер не вернул id файла!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        string fileNameResp = result.fileName != null ? (string)result.fileName : fileName;
                        string url = result.url != null ? (string)result.url : null;

                        SelectedFiles.Add(new FileModel
                        {
                            Id = fileId.Value,
                            FileName = fileNameResp,
                            URL = url
                        });
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка загрузки файла: {response.StatusCode}\n{json}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Исключение при загрузке файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Logout()
        {
            AuthService.Logout();
            var loginWindow = new LoginView();
            loginWindow.Show();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is arasakagram.Views.MessengerWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }

        private void ShowAbout()
        {
            MessageBox.Show("Workflow Messenger\nВерсия 1.0.0\n© 2025", "О программе");
        }

        public async Task InitCurrentUserAsync()
        {
            if (AuthService.CurrentUserId != null)
            {
                var user = await _userService.GetUserByIdAsync(AuthService.CurrentUserId.Value);
                if (user != null)
                {
                    string fio = user.Firstname;
                    if (!string.IsNullOrWhiteSpace(user.MiddleName)) fio += $" {user.MiddleName[0]}.";
                    if (!string.IsNullOrWhiteSpace(user.LastName)) fio += $" {user.LastName[0]}.";
                    CurrentUser = new UserInfo
                    {
                        Id = user.Id,
                        Avatar = user.Avatar,
                        FullNameShort = fio,
                        Phone = user.Phone,
                        FirstName = user.Firstname,
                        LastName = user.LastName,
                        MiddleName = user.MiddleName,
                        Email = user.Email,
                        Login = user.Login
                    };
                }
            }
        }

        public void OpenEditProfile()
        {
            OpenEditProfileAction?.Invoke();
        }

        private async void UpdateSearchResults()
        {
            if (!IsSearchMode)
                return;
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                var all = (await _userService.GetAllUsersAsync()).Where(u => u.Id != CurrentUser.Id);
                SearchUsers.Clear();
                foreach (var u in all)
                    SearchUsers.Add(new UserInfo
                    {
                        Id = u.Id,
                        Avatar = u.Avatar,
                        FullNameShort = u.Firstname + (string.IsNullOrWhiteSpace(u.LastName) ? "" : $" {u.LastName[0]}.") + (string.IsNullOrWhiteSpace(u.MiddleName) ? "" : $" {u.MiddleName[0]}."),
                        Phone = u.Phone,
                        FirstName = u.Firstname,
                        LastName = u.LastName,
                        MiddleName = u.MiddleName,
                        Email = u.Email,
                        Login = u.Login
                    });
            }
            else
            {
                var all = (await _userService.GetAllUsersAsync()).Where(u => u.Id != CurrentUser.Id);
                var filtered = all.Where(u =>
                    (u.Firstname?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.LastName?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Login?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Phone?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false));
                SearchUsers.Clear();
                foreach (var u in filtered)
                    SearchUsers.Add(new UserInfo
                    {
                        Id = u.Id,
                        Avatar = u.Avatar,
                        FullNameShort = u.Firstname + (string.IsNullOrWhiteSpace(u.LastName) ? "" : $" {u.LastName[0]}.") + (string.IsNullOrWhiteSpace(u.MiddleName) ? "" : $" {u.MiddleName[0]}."),
                        Phone = u.Phone,
                        FirstName = u.Firstname,
                        LastName = u.LastName,
                        MiddleName = u.MiddleName,
                        Email = u.Email,
                        Login = u.Login
                    });
            }
        }

        public async Task CreateChatWithUserAsync(UserInfo user)
        {
            if (user == null || CurrentUser == null) return;
            var dto = new CreateChatDto
            {
                Name = user.FullNameShort,
                ID_User_CreatedBy = CurrentUser.Id,
                ID_ChatType = 1,
                MemberUserId = user.Id,
                ChatTopic = "Личный чат"
            };
            try
            {
                var response = await _apiClient.CreateAsync(dto);
                var obj = Newtonsoft.Json.Linq.JObject.Parse(response);
                int chatId = (int)obj["id"];
                await LoadChatsAsync();
                var chat = PersonalChats.FirstOrDefault(c => c.Id == chatId);
                if (chat != null)
                    SelectedChat = chat;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании чата: {ex.Message}\n{ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            IsSearchMode = false;
            SearchText = string.Empty;
        }

        private void OnSignalRMessageDeleted(int messageId)
        {
            var msg = Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
                Application.Current.Dispatcher.Invoke(() => Messages.Remove(msg));
        }

        private void OnSignalRChatDeleted(int chatId)
        {
            if (SelectedChat != null && SelectedChat.Id == chatId)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedChat = null;
                    Messages.Clear();
                    MessageBox.Show("Чат был удалён.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            // Также можно обновить список чатов
            Application.Current.Dispatcher.Invoke(async () => await LoadChatsAsync());
        }

        public async Task DeleteMessageAsync(MessageModel message)
        {
            if (message == null) return;
            if (MessageBox.Show("Удалить сообщение у всех?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _apiClient.Delete2Async(message.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления сообщения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async Task DeleteChatAsync()
        {
            if (SelectedChat == null) return;
            // Запрет для не-админа на удаление группового чата
            if (SelectedChat.ID_ChatType == 2 && CurrentUser != null && CurrentUser.Id != SelectedChat.CreatedByUserId)
            {
                MessageBox.Show("Только администратор (создатель) группы может удалить групповой чат!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show("Удалить чат?", "Удаление чата", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _apiClient.DeleteAsync(SelectedChat.Id, CurrentUser.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления чата: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


    }

    public class UserInfo : ObservableObject
    {
        public int Id { get; set; }
        private byte[] avatar;
        public byte[] Avatar
        {
            get => avatar;
            set
            {
                if (SetProperty(ref avatar, value))
                {
                    OnPropertyChanged(nameof(AvatarImage));
                }
            }
        }
        public string FullNameShort { get; set; } // "Имя Ф. О."
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public string Login { get; set; } // Логин пользователя
        public BitmapImage AvatarImage
        {
            get
            {
                if (Avatar == null || Avatar.Length == 0) return null;
                using (var ms = new MemoryStream(Avatar))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
        }
    }
}