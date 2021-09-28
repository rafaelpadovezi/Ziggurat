using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;

namespace DotNetCore.CAP.Contrib.Idempotency.Pipeline
{
    public class ValidatorPipeline<TMessage> : IPipeline<TMessage>
        where TMessage : IMessage
    {
        private readonly IValidator<TMessage> _validator;

        public ValidatorPipeline(IValidator<TMessage> validator) => _validator = validator;
        public Task<Result<TMessage>> ExecuteAsync(TMessage message)
        {
            //Add async validator
            var result = _validator.Validate(message);
            if (result.IsValid is false)
                return Task.FromResult(Result.Success(message));

            var errorsMessageFromResult = result.Errors.Select(error => error.ErrorMessage);
            var errors = string.Join("\n", errorsMessageFromResult);
            return Task.FromResult(Result.Failure<TMessage>($"The message not is valid, {errors}"));
        }
    }
}