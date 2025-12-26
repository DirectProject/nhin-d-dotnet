using System;
using System.Collections.Generic;

namespace Health.Direct.Admin.Console.Models.Pagination
{
    public class PaginatedList<T> : List<T>
    {
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalItemCount { get; }
        public int PageCount => (int)Math.Ceiling((double)TotalItemCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < PageCount;

        public PaginatedList(IEnumerable<T> items, int pageNumber, int pageSize, int totalItemCount)
            : base(items)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalItemCount = totalItemCount;
        }
    }
}