using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ProbetaControl.Models;
using ProbetaControl.Services;
using ProbetaControl.ViewModels.Infrastructure;

namespace ProbetaControl.ViewModels.Settings
{
    /// <summary>
    /// Usuarios (administrators): create, edit, reset a password, archive / restore. The rules (at least one active
    /// administrator, not your own user) are checked by <see cref="UserService"/>; refusals show in the banner.
    /// </summary>
    public sealed class UsersSectionViewModel : SettingsSection
    {
        private readonly UserService _users;
        private readonly AppSession _session;
        private readonly IDialogService _dialogs;
        private UserRow _selectedItem;
        private bool _includeArchived;

        public UsersSectionViewModel(UserService users, AppSession session, IDialogService dialogs)
            : base(SettingsPageViewModel.AdminGroup, "Usuarios", "Icon.User")
        {
            _users = users;
            _session = session;
            _dialogs = dialogs;

            NewCommand = new AsyncRelayCommand(() => OpenEditorAsync(new UserEditorViewModel(_users, _session, null)), () => !IsBusy);
            EditCommand = new AsyncRelayCommand(
                () => OpenEditorAsync(new UserEditorViewModel(_users, _session, SelectedItem.User)), () => SelectedItem != null && !IsBusy);
            ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync, () => SelectedItem != null && !SelectedItem.IsCurrent && !IsBusy);
            ToggleArchiveCommand = new AsyncRelayCommand(ToggleArchiveAsync, () => SelectedItem != null && !SelectedItem.IsCurrent && !IsBusy);
        }

        public ObservableCollection<UserRow> Items { get; } = new ObservableCollection<UserRow>();

        public UserRow SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnPropertyChanged(nameof(ArchiveText));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IncludeArchived
        {
            get => _includeArchived;
            set
            {
                if (SetProperty(ref _includeArchived, value))
                {
                    _ = ReloadAsync(SelectedItem?.User.Id);
                }
            }
        }

        public string ArchiveText => SelectedItem?.IsArchived == true ? "Restaurar" : "Archivar";

        public string CountText => Items.Count == 1 ? "1 usuario" : $"{Items.Count} usuarios";

        public ICommand NewCommand { get; }

        public ICommand EditCommand { get; }

        /// <summary>"Restablecer contraseña…": a temporary password the user must change at the next login.</summary>
        public ICommand ResetPasswordCommand { get; }

        public ICommand ToggleArchiveCommand { get; }

        public override Task LoadAsync() => ReloadAsync(SelectedItem?.User.Id);

        private async Task<bool> ReloadAsync(int? selectId)
        {
            return await RunAsync(async () =>
            {
                var users = await _users.ListAsync(IncludeArchived);
                Items.Clear();
                foreach (var user in users)
                {
                    Items.Add(new UserRow(user, user.Id == _session.UserId));
                }

                SelectedItem = selectId.HasValue ? Items.FirstOrDefault(r => r.User.Id == selectId.Value) : null;
                OnPropertyChanged(nameof(CountText));
            });
        }

        private async Task OpenEditorAsync(UserEditorViewModel editor)
        {
            if (_dialogs.Show(editor))
            {
                await ReloadAsync(editor.SavedId);
            }
        }

        private async Task ResetPasswordAsync()
        {
            var row = SelectedItem;
            if (_dialogs.Show(new ResetPasswordViewModel(_users, row.User)))
            {
                await ReloadAsync(row.User.Id);
                ShowMessage(MessageKind.Success,
                    $"Contraseña de \"{row.User.UserName}\" restablecida. Deberá elegir una nueva al entrar.");
            }
        }

        private async Task ToggleArchiveAsync()
        {
            var row = SelectedItem;
            var archive = !row.IsArchived;
            var confirmed = archive
                ? _dialogs.Confirm("Archivar", $"¿Archivar el usuario \"{row.User.UserName}\"?\n\nNo podrá iniciar sesión. No se borra: podrá restaurarlo con \"Mostrar archivados\".", "Archivar", destructive: true)
                : _dialogs.Confirm("Restaurar", $"¿Restaurar el usuario \"{row.User.UserName}\"? Podrá volver a iniciar sesión.", "Restaurar");
            if (!confirmed)
            {
                return;
            }

            if (await RunAsync(() => _users.SetArchivedAsync(row.User, archive)))
            {
                await ReloadAsync(row.User.Id);
            }
        }
    }

    /// <summary>One user in the list, with the texts the grid shows.</summary>
    public sealed class UserRow
    {
        public UserRow(User user, bool isCurrent)
        {
            User = user;
            IsCurrent = isCurrent;
        }

        public User User { get; }

        /// <summary>The signed-in user (can't archive or reset themselves here).</summary>
        public bool IsCurrent { get; }

        public string UserName => User.UserName;

        public string FullName => User.FullName;

        public string RoleText => DisplayText.Role(User.Role);

        public bool IsArchived => User.IsArchived;

        /// <summary>Highlighted in the grid.</summary>
        public bool MustChangePassword => !User.IsArchived && User.MustChangePassword;

        public string StatusText => User.IsArchived ? "Archivado"
            : IsCurrent ? "Sesión actual"
            : User.MustChangePassword ? "Debe cambiar la contraseña"
            : "Activo";

        public override string ToString() => UserName;
    }

    /// <summary>New user (with a first password) or edit user name, full name and role.</summary>
    public sealed class UserEditorViewModel : EditorDialogViewModel
    {
        private readonly UserService _users;
        private readonly User _record;
        private string _userName = string.Empty;
        private string _fullName = string.Empty;
        private string _role = UserRoles.Operator;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;

        /// <param name="user">null for a new user; otherwise the row to edit (a copy is edited).</param>
        public UserEditorViewModel(UserService users, AppSession session, User user)
        {
            _users = users;
            IsNew = user == null;
            _record = user == null ? new User() : new User
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Role = user.Role,
                MustChangePassword = user.MustChangePassword,
                IsArchived = user.IsArchived,
                RowVersion = user.RowVersion,
            };

            if (!IsNew)
            {
                _userName = user.UserName ?? string.Empty;
                _fullName = user.FullName ?? string.Empty;
                _role = user.Role;
            }

            IsSelf = !IsNew && user.Id == session.UserId;
        }

        public override string Title => IsNew ? "Nuevo usuario" : $"Editar usuario \"{_record.UserName}\"";

        public bool IsNew { get; }

        /// <summary>The signed-in administrator editing themselves: the role can't change.</summary>
        public bool IsSelf { get; }

        public bool CanChangeRole => !IsSelf;

        public IReadOnlyList<RoleOption> RoleOptions { get; } = new[]
        {
            new RoleOption(UserRoles.Operator, "Operador"),
            new RoleOption(UserRoles.Admin, "Administrador"),
        };

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        /// <summary>One of <see cref="UserRoles"/> (combo box SelectedValue).</summary>
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        /// <summary>First password (new user only); the user must change it at the first login.</summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value ?? string.Empty);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value ?? string.Empty);
        }

        public string PasswordRule => $"Mínimo {AuthService.MinPasswordLength} caracteres. El usuario elegirá una nueva al entrar por primera vez.";

        protected override async Task SaveCoreAsync()
        {
            _record.UserName = UserName?.Trim();
            _record.FullName = FullName;
            _record.Role = Role;

            if (IsNew)
            {
                if (Password != ConfirmPassword)
                {
                    throw new ValidationException(nameof(ConfirmPassword), "Las contraseñas no coinciden.");
                }

                await _users.CreateAsync(_record, Password);
            }
            else
            {
                await _users.UpdateAsync(_record);
            }

            SavedId = _record.Id;
        }
    }

    /// <summary>"Restablecer contraseña": an administrator sets a temporary password.</summary>
    public sealed class ResetPasswordViewModel : EditorDialogViewModel
    {
        private readonly UserService _users;
        private readonly User _user;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;

        public ResetPasswordViewModel(UserService users, User user)
        {
            _users = users;
            _user = user;
        }

        public override string Title => "Restablecer contraseña";

        public string Explanation =>
            $"Contraseña temporal para \"{_user.UserName}\". Al entrar, el programa le pedirá elegir una nueva " +
            $"(mínimo {AuthService.MinPasswordLength} caracteres).";

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value ?? string.Empty);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value ?? string.Empty);
        }

        protected override async Task SaveCoreAsync()
        {
            if (Password != ConfirmPassword)
            {
                throw new ValidationException(nameof(ConfirmPassword), "Las contraseñas no coinciden.");
            }

            await _users.ResetPasswordAsync(_user.Id, Password);
            SavedId = _user.Id;
        }
    }

    public sealed class RoleOption
    {
        public RoleOption(string value, string label)
        {
            Value = value;
            Label = label;
        }

        public string Value { get; }

        public string Label { get; }

        public override string ToString() => Label;
    }
}
