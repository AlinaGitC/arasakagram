using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using arasakagram.Api;
using arasakagram.Services;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Windows;
using arasakagram.Models;

namespace arasakagram.ViewModels
{
    public class MessagesViewModel : ObservableObject
    {
        private readonly MessageService _messageService;
        private readonly SignalRService _signalRService;
        private readonly string _signalRUrl = "http://localhost:5047";
        public ObservableCollection<MessageDto> Messages { get; } = new();
        private int _chatId;
        private string _newMessage;
        public string NewMessage
        {
            get => _newMessage;
            set => SetProperty(ref _newMessage, value);
        }

        public IRelayCommand SendMessageCommand { get; }
        private int _userId;
        public IRelayCommand AddMemberCommand { get; }
        public event Action AddMemberRequested;

        public ObservableCollection<FileModel> SelectedFiles { get; } = new();
        public ICommand AttachFileCommand { get; }
        public IRelayCommand<FileModel> RemoveSelectedFileCommand { get; }

        public MessagesViewModel(MessageService messageService, int chatId, int userId, SignalRService signalRService)
        {
            _messageService = messageService;
            _chatId = chatId;
            _userId = userId;
            _signalRService = signalRService;
            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(NewMessage));
            AddMemberCommand = new RelayCommand(() => AddMemberRequested?.Invoke());
            AttachFileCommand = new RelayCommand(async () => await AttachFileAsync());
            RemoveSelectedFileCommand = new RelayCommand<FileModel>(file =>
            {
                if (file != null)
                    SelectedFiles.Remove(file);
            });
            _signalRService.MessageReceived += OnMessageReceived;
            ConnectSignalR();
            LoadMessages();
        }

        private async void ConnectSignalR()
        {
            await _signalRService.ConnectAsync(_signalRUrl, _chatId);
        }

        private void OnMessageReceived(int userId, string text, DateTime timestamp, List<FileDto> files)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new MessageDto
                {
                    UserId = userId,
                    Text = text,
                    Timestamp = timestamp,
                    Files = files
                });
            });
        }

        public async void LoadMessages()
        {
            var messages = await _messageService.GetChatMessagesAsync(_chatId);
            Messages.Clear();
            foreach (var msg in messages)
                Messages.Add(msg);
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
                        int fileId = result.ID != null ? (int)result.ID : (int)result.Id;
                        string fileNameResp = result.FileName != null ? (string)result.FileName : fileName;
                        string url = result.Url != null ? (string)result.Url : null;

                        SelectedFiles.Add(new FileModel
                        {
                            Id = fileId,
                            FileName = fileNameResp,
                            URL = url
                        });                    }
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

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMessage)) return;
            try
            {
                var fileIds = SelectedFiles.Select(f => f.Id).ToList();
                var dto = new SendMessageDto
                {
                    ID_Chat = _chatId,
                    ID_User = _userId,
                    ContentMessages = NewMessage,
                    Files = fileIds
                };
                await _messageService.SendMessageAsync(dto);
                NewMessage = string.Empty;
                SelectedFiles.Clear();
                LoadMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 