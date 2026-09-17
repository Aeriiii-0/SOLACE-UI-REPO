using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models.Api
{
    public class BaseResponse
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class BaseResponse<T> : BaseResponse
    {
        public T Data { get; set; }
        public static BaseResponse<T> Success(T data) => new BaseResponse<T> { Succeeded = true, Data = data };
        public static BaseResponse<T> Fail(params string[] errors) => new BaseResponse<T> { Succeeded = false, Errors = new List<string>(errors) };
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
