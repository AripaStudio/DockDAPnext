#nullable enable
using System;

namespace DockDAPnext.Service
{
    // The standard result of every service operation (AI_Rules 17): success flag, value,
    // error and message travel together, so a method never signals failure with null.
    public class OperationResult<T>
    {
        public bool IsSuccess { get; init; }

        public T? Value { get; init; }

        public Exception? Exception { get; init; }

        public string? Message { get; init; }

        public static OperationResult<T> Ok(T value, string? message = null) =>
            new OperationResult<T> { IsSuccess = true, Value = value, Message = message };

        public static OperationResult<T> Fail(string message, Exception? exception = null, T? value = default) =>
            new OperationResult<T> { IsSuccess = false, Value = value, Exception = exception, Message = message };
    }
}
