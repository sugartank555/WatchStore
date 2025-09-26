using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Models.ViewModels;

namespace WatchStore.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class AccountsController : Controller
    {
        private readonly UserManager<IdentityUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public AccountsController(UserManager<IdentityUser> users, RoleManager<IdentityRole> roles)
        {
            _users = users;
            _roles = roles;
        }

        // GET: /Dashboard/Accounts
        // ?q=...&page=1&pageSize=20
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _users.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                    (u.Email != null && u.Email.ToLower().Contains(kw)));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var items = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // load roles cho page đang hiển thị
            var rolesDict = new Dictionary<string, IList<string>>();
            foreach (var u in items)
            {
                rolesDict[u.Id] = await _users.GetRolesAsync(u);
            }

            var vm = new AdminUsersIndexVm
            {
                Items = items,
                UserRoles = rolesDict,
                Q = q,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            return View(vm);
        }

        // GET: /Dashboard/Accounts/Create
        public IActionResult Create() => View(new CreateUserVm());

        // POST: /Dashboard/Accounts/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVm model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("", "Email và mật khẩu là bắt buộc.");
                return View(model);
            }

            var exists = await _users.FindByEmailAsync(model.Email);
            if (exists != null)
            {
                ModelState.AddModelError("", "Email đã tồn tại.");
                return View(model);
            }

            var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true, LockoutEnabled = true };
            var result = await _users.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            if (model.IsAdmin)
            {
                if (!await _roles.RoleExistsAsync("Admin"))
                    await _roles.CreateAsync(new IdentityRole("Admin"));
                await _users.AddToRoleAsync(user, "Admin");
            }
            else
            {
                if (!await _roles.RoleExistsAsync("User"))
                    await _roles.CreateAsync(new IdentityRole("User"));
                await _users.AddToRoleAsync(user, "User");
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Dashboard/Accounts/EditRoles/{id}
        public async Task<IActionResult> EditRoles(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new EditUserRolesVm
            {
                UserId = user.Id,
                Email = user.Email!,
                AllRoles = await _roles.Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync(),
                UserRoles = (await _users.GetRolesAsync(user)).OrderBy(r => r).ToList()
            };
            vm.SelectedRoles = new List<string>(vm.UserRoles);
            return View(vm);
        }

        // POST: /Dashboard/Accounts/EditRoles
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(EditUserRolesVm model)
        {
            var user = await _users.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var current = await _users.GetRolesAsync(user);
            var toAdd = model.SelectedRoles.Except(current).ToList();
            var toRemove = current.Except(model.SelectedRoles).ToList();

            foreach (var r in toAdd)
                if (await _roles.RoleExistsAsync(r))
                    await _users.AddToRoleAsync(user, r);

            foreach (var r in toRemove)
                await _users.RemoveFromRoleAsync(user, r);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/Accounts/Lock/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(10); // khóa dài
            await _users.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/Accounts/Unlock/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnd = null;
            await _users.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/Accounts/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Không xoá chính mình
            if (User.Identity?.Name?.Equals(user.Email, StringComparison.OrdinalIgnoreCase) == true)
            {
                TempData["Error"] = "Bạn không thể xoá tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            await _users.DeleteAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}
