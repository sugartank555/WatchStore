namespace WatchStore.Models.ViewModels
{
    public class ProductIndexVm
    {
        public IEnumerable<Product> Products { get; set; } = Enumerable.Empty<Product>();

        // Dữ liệu cho filter
        public List<string> Brands { get; set; } = new();

        // Trạng thái filter hiện tại
        public string? Q { get; set; }        // từ khóa tìm kiếm
        public string? Brand { get; set; }    // brand đang chọn
    }
}
