namespace SubOrbitV2.Application.Common.Models;

public class PaginatedResult<T> : Result<IEnumerable<T>>
{
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }
    public int PageSize { get; private set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    private PaginatedResult() { }

    public static PaginatedResult<T> Create(IEnumerable<T> data, int count, int pageNumber, int pageSize)
    {
        return new PaginatedResult<T>
        {
            Data = data,
            TotalCount = count,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize),
            IsSuccess = true
        };
    }
}