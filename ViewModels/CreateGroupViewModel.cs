using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using arasakagram.Api;
using arasakagram.Services;
using System.Threading.Tasks;

namespace arasakagram.ViewModels
{
    public class CreateGroupViewModel : ObservableObject
    {
        private readonly GroupService _groupService;
        public CreateGroupViewModel(GroupService groupService)
        {
            _groupService = groupService;
            CreateGroupCommand = new AsyncRelayCommand(CreateGroupAsync);
        }

        private string _name;
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        public IAsyncRelayCommand CreateGroupCommand { get; }
        public event System.Action GroupCreated;

        private async Task CreateGroupAsync()
        {
            var dto = new CreateGroupDto
            {
                Name = Name,
                CreatorUserId = AuthService.CurrentUserId ?? 0
            };
            await _groupService.CreateGroupAsync(dto);
            GroupCreated?.Invoke();
        }
    }
} 