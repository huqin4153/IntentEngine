using System;

namespace IntentEngine.Models
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public long ElapsedMs { get; set; }

        public ApiResponse() { }

        public static ApiResponse Ok(object data = null, string message = "操作成功")
        {
            return new ApiResponse { Success = true, Message = message, Data = data };
        }

        public static ApiResponse Fail(string message, object data = null)
        {
            return new ApiResponse { Success = false, Message = message, Data = data };
        }
    }
}
