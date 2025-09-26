using Microsoft.AspNetCore.Identity;

namespace WatchStore.Models.ViewModels
{
    public class AdminUsersIndexVm
    {
        public List<IdentityUser> Items { get; set; } = new();
        public Dictionary<string, IList<string>> UserRoles { get; set; } = new(); // userId -> roles

        public string? Q { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    }

    public class CreateUserVm
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public bool IsAdmin { get; set; } = false; // tick để thêm vào role Admin
    }

    public class EditUserRolesVm
    {
        public string UserId { get; set; } = default!;
        public string? Email { get; set; }
        public List<string> AllRoles { get; set; } = new();       // tất cả roles trong hệ thống
        public List<string> UserRoles { get; set; } = new();      // roles hiện tại của user
        public List<string> SelectedRoles { get; set; } = new();  // roles sau khi submit
    }
}
