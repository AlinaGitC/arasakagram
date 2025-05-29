using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using arasakagram.Api;

namespace arasakagram.Services
{
    public class SignalRService
    {
        private HubConnection _connection;
        public event Action<int, string, DateTime, List<FileDto>>? MessageReceived;
        public event Action<int, int>? GroupMembersChanged;
        public event Action<int>? MessageDeleted;
        public event Action<int>? ChatDeleted;

        public async Task ConnectAsync(string url, int chatId)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"{url}/chatHub")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<int, string, DateTime, List<FileDto>>("ReceiveMessage", (userId, text, timestamp, files) =>
            {
                MessageReceived?.Invoke(userId, text, timestamp, files);
            });

            await _connection.StartAsync();
            await _connection.InvokeAsync("JoinChat", chatId);

            _connection.On<int, int>("GroupMembersChanged", (chatId, membersCount) =>
            {
                GroupMembersChanged?.Invoke(chatId, membersCount);
            });

            _connection.On<int>("MessageDeleted", (messageId) =>
            {
                MessageDeleted?.Invoke(messageId);
            });

            _connection.On<int>("ChatDeleted", (chatId) =>
            {
                ChatDeleted?.Invoke(chatId);
            });
        }

        public async Task DisconnectAsync(int chatId)
        {
            if (_connection != null)
            {
                await _connection.InvokeAsync("LeaveChat", chatId);
                await _connection.StopAsync();
            }
        }
    }
} 