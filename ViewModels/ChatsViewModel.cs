using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using arasakagram.Api;
using arasakagram.Services;
using CommunityToolkit.Mvvm.Input;
using System;

namespace arasakagram.ViewModels
{
    public class ChatsViewModel : ObservableObject
    {
        private readonly ChatService _chatService;
        public ObservableCollection<ChatDto> Chats { get; } = new();
        private int _userId;
        private ChatDto _selectedChat;
        public ChatDto SelectedChat
        {
            get => _selectedChat;
            set
            {
                SetProperty(ref _selectedChat, value);
                if (value != null)
                    OpenMessagesCommand.Execute(value);
            }
        }

        public IRelayCommand<ChatDto> OpenMessagesCommand { get; }
        public event Action<ChatDto> OpenMessagesRequested;

        public ChatsViewModel(ChatService chatService, int userId)
        {
            _chatService = chatService;
            _userId = userId;
            OpenMessagesCommand = new RelayCommand<ChatDto>(chat => OpenMessagesRequested?.Invoke(chat));
            LoadChats();
        }
        public async void LoadChats()
        {
            var chats = await _chatService.GetUserChatsAsync(_userId);
            Chats.Clear();
            foreach (var chat in chats)
                Chats.Add(chat);
        }
    }
} 