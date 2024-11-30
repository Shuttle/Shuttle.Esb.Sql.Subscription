using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription;

public class SqlSubscriptionOptionsValidator : IValidateOptions<SqlSubscriptionOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlSubscriptionOptions options)
    {
        Guard.AgainstNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
        {
            return ValidateOptionsResult.Fail(Resources.ConnectionStringOptionException);
        }

        if (string.IsNullOrWhiteSpace(options.Schema))
        {
            return ValidateOptionsResult.Fail(Resources.SchemaOptionException);
        }

        return ValidateOptionsResult.Success;

    }
}