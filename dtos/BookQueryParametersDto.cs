namespace LibraryAppApi.DTOs
{
    public class BookQueryParametersDto
    {
        public string? SearchTerm { get; set; }

        public int? CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public bool OnlyAvailable { get; set; } = false;

        private int _pageSize = 10;
        private const int MaxPageSize = 50;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}