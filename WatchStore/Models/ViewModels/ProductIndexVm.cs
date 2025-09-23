namespace WatchStore.Models.ViewModels
{
    public class ProductIndexVm
    {
        public PagedResult<Product> Paging { get; set; } = new();
        public List<string> Brands { get; set; } = new();
        public string? Q { get; set; }
        public string? Brand { get; set; }

        // Alias để tương thích view cũ (nếu có)
        public IEnumerable<Product> Products => Paging.Items;

    }
}
