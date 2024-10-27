﻿using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Subscription;

public class ScriptProvider : IScriptProvider
{
    private readonly Core.Data.IScriptProvider _scriptProvider;

    public ScriptProvider(IOptionsMonitor<ConnectionStringOptions> connectionStringOptions, IOptions<ScriptProviderOptions> options)
    {
        Guard.AgainstNull(Guard.AgainstNull(options).Value);

        _scriptProvider = new Core.Data.ScriptProvider(connectionStringOptions, Options.Create(new ScriptProviderOptions
        {
            ResourceNameFormat = string.IsNullOrEmpty(options.Value.ResourceNameFormat)
                ? "Shuttle.Esb.Sql.Subscription..scripts.{ProviderName}.{ScriptName}.sql"
                : options.Value.ResourceNameFormat,
            ResourceAssembly = options.Value.ResourceAssembly ?? typeof(SubscriptionService).Assembly,
            FileNameFormat = options.Value.FileNameFormat,
            ScriptFolder = options.Value.ScriptFolder
        }));
    }

    public string Get(string connectionStringName, string scriptName)
    {
        return _scriptProvider.Get(connectionStringName, scriptName);
    }
}