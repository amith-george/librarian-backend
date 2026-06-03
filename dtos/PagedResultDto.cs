using System.Collections.Generic;

namespace LibraryAppApi.DTOs
{
    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        
        public int TotalPages => TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
    }
}