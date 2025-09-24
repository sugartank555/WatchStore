using WatchStore.Models;

namespace WatchStore.Models.ViewModels
{
    public class AdminOrdersIndexVm
    {
        public List<Order> Items { get; set; } = new();

        public string? Q { get; set; }
        public string? Status { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}
