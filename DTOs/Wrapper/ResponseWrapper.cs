namespace TrainBookingAppMVC.DTOs.Wrapper
{
    public class ResponseWrapper<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ResponseWrapper<T> SuccessResponse(T data, string message = "Operation completed successfully")
        {
            return new ResponseWrapper<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ResponseWrapper<T> ErrorResponse(string message)
        {
            return new ResponseWrapper<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }

    public class ResponseWrapper
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ResponseWrapper SuccessResponse(string message = "Operation completed successfully")
        {
            return new ResponseWrapper
            {
                Success = true,
                Message = message
            };
        }

        public static ResponseWrapper ErrorResponse(string message)
        {
            return new ResponseWrapper
            {
                Success = false,
                Message = message
            };
        }
    }
}