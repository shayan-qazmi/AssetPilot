using System;

namespace Inventory_Management_System.Helpers
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data)
        {
            return new ServiceResult<T> { Success = true, Data = data };
        }

        public static ServiceResult<T> Fail(string errorMessage)
        {
            return new ServiceResult<T> { Success = false, ErrorMessage = errorMessage };
        }
    }
}
