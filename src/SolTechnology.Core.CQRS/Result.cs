﻿#nullable enable
using System.Text.Json.Serialization;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.CQRS
{
    public record Result
    {
        public bool IsSuccess { get; init; }

        [JsonIgnore]
        public bool IsFailure => !IsSuccess;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Error? Error { get; init; }

        // Non-generic accessor for the payload carried by Result<T>. Returns null on the base
        // type (no data) and is overridden in Result<T>. A null payload — both from the base
        // and from Result<T>.Success(default) — is rendered as 204 No Content by the HTTP
        // boundary; callers that need a 200 with an explicit null body should send a sentinel
        // value, not default(T).
        public virtual object? GetData() => null;


        public static Result Success()
        {
            return new Result
            {
                IsSuccess = true
            };
        }

        public static Task<Result> SuccessAsTask()
        {
            return Task.FromResult(new Result
            {
                IsSuccess = true
            });
        }

        public static Result Fail(string message)
        {
            return new Result
            {
                Error = new Error
                {
                    Message = message
                },
                IsSuccess = false
            };
        }

        public static Result Fail(Error error)
        {
            return new Result
            {
                Error = error,
                IsSuccess = false
            };
        }

        public static Task<Result> FailAsTask(string message)
        {
            return Task.FromResult(new Result
            {
                Error = new Error
                {
                    Message = message
                },
                IsSuccess = false
            });
        }

        public static Task<Result> FailAsTask(Error error)
        {
            return Task.FromResult(new Result
            {
                Error = error,
                IsSuccess = false
            });
        }
    }

    public record Result<T> : Result
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public T? Data { get; set; }

        // Non-generic view of Data for boundary code (HTTP filter, message bus, gRPC) that
        // sees Result as object and cannot bind T. Avoids reflection over Result<T>.Data —
        // required for AOT / trim where reflection over generic instantiations is unsafe.
        public override object? GetData() => Data;

        public static implicit operator Result<T>(T value)
        {
            return Success(value);
        }

        public static implicit operator Result<T>(Exception e)
        {
            return Fail(e.Message);
        }

        public static implicit operator Result<T>(Error e)
        {
            return Fail(e);
        }

        public static Result<T> Success(T data)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public new static Result<T> Fail(string message)
        {
            return new Result<T>
            {
                Error = new Error
                {
                    Message = message
                },
                IsSuccess = false
            };
        }

        public new static Result<T> Fail(Error error)
        {
            return new Result<T>
            {
                Error = error,
                IsSuccess = false
            };
        }


        public new static Task<Result<T>> FailAsTask(string message)
        {
            return Task.FromResult(new Result<T>
            {
                Error = new Error
                {
                    Message = message
                },
                IsSuccess = false
            });
        }

        public static Task<Result<T>> SuccessAsTask(T data)
        {
            return Task.FromResult(new Result<T>
            {
                IsSuccess = true,
                Data = data
            });
        }
    }
}
