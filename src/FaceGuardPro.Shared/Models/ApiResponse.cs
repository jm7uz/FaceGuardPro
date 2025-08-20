using FaceGuardPro.Shared.Enums;

namespace FaceGuardPro.Shared.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public ApiResponseStatus StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = ApiResponseStatus.Success
        };
    }

    public static ApiResponse<T> SuccessResult(string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            StatusCode = ApiResponseStatus.Success
        };
    }

    public static ApiResponse<T> ErrorResult(string message, ApiResponseStatus statusCode = ApiResponseStatus.InternalServerError)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> ErrorResult(List<string> errors, string message = "Validation failed", ApiResponseStatus statusCode = ApiResponseStatus.ValidationError)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> NotFoundResult(string message = "Resource not found")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = ApiResponseStatus.NotFound
        };
    }

    public static ApiResponse<T> UnauthorizedResult(string message = "Unauthorized access")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = ApiResponseStatus.Unauthorized
        };
    }

    public static ApiResponse<T> BadRequestResult(string message = "Bad request")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = ApiResponseStatus.BadRequest
        };
    }
}

public class PagedResponse<T> : ApiResponse<T>
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    public static PagedResponse<T> SuccesResponse(T data, int totalCount, int pageNumber, int pageSize, string message = "Data retrieved successfully")
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = ApiResponseStatus.Success,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages
        };
    }
}