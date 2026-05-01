using DealSpot.Domain.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Abstractions.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);
        var response = await next();
        if (response is Result { IsFailure: true } failedResult)
            logger.LogWarning("Request {RequestName} failed: {Error}", requestName, failedResult.Error);
        else
            logger.LogInformation("Request {RequestName} completed successfully", requestName);
        return response;
    }
}

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count == 0) return await next();

        var error = new Error("Validation.Failed", string.Join("; ", failures.Select(f => f.ErrorMessage)));

        var resultType = typeof(TResponse);
        if (resultType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        var method = typeof(Result).GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
            .MakeGenericMethod(resultType.GetGenericArguments()[0]);
        return (TResponse)method.Invoke(null, [error])!;
    }
}
