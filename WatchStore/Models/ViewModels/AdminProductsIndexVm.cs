namespace WatchStore.Models.ViewModels
{
    public class AdminProductsIndexVm
    {
        public List<Product> Items { get; set; } = new();
        public List<string> Brands { get; set; } = new();

        public string? Q { get; set; }
        public string? Brand { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}
