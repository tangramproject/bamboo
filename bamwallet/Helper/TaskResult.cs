using System;

namespace BAMWallet.Helper
{
    public class TaskResult<T>
    {
        private TaskResult() { }

        public bool Success { get; private set; }
        public T Result { get; private set; }
        public dynamic NonSuccessMessage { get; private set; }
        public Exception Exception { get; private set; }

        public static TaskResult<T> CreateSuccess(T result)
        {
            return new TaskResult<T> { Success = result != null, Result = result };
        }

        public static TaskResult<T> CreateSuccess(dynamic successMessage)
        {
            return new TaskResult<T> { Success = !string.IsNullOrEmpty(successMessage), Result = successMessage };
        }

        public static TaskResult<T> CreateFailure(dynamic nonSuccessMessage)
        {
            return new TaskResult<T> { Success = false, Result = default, NonSuccessMessage = nonSuccessMessage };
        }

        public static TaskResult<T> CreateFailure(Exception ex)
        {
            return new TaskResult<T>
            {
                Success = false,
                NonSuccessMessage = $"{ex.Message}{Environment.NewLine}{ex.StackTrace ?? string.Empty}",
                Exception = ex,
                Result = default,
            };
        }
    }
}
