# SQL

```
PM> Install-Package Shuttle.Esb.Sql.Subscription
```

Contains a sql-based `ISubscriptionService` implementation.  The subscription service caches all subscriptions but a timeout for the cache may be set.  If you need to be sure that a new subscriber is picked up by a publisher the safest is to restart the relevant publisher(s).

## Registration

The required components may be registered by calling:

```c#
services.AddSqlSubscription(builder => 
{
    // defaults
    builder.ConnectionStringName = "Subscription";
    builder.Schema = "dbo";
    builder.CacheTimeout = TimeSpan.FromMinutes(5);

    builder.UseSqlServer(); // SqlServer
    builder.UseNpgsql(); // Postgres
});
```

The `SubscriptionService` that implements the `ISubscriptionService` interface makes use of the `SubscriptionOptions` configured with the `ServiceBusOptions` to register, or ensure, any subscriptions:

```c#
services.AddServiceBus(builder => 
{
	builder.Subscription.SubscribeType = SubscribeType.Normal; // default

    // add subscription to message types directly; else below options on builder
    builder.Subscription.MessageTypes.Add(messageType);

    // using type
    builder.AddSubscription(typeof(Event1));
    builder.AddSubscription(typeof(Event2));

    // using a full type name
    builder.AddSubscription(typeof(Event1).FullName);
    builder.AddSubscription(typeof(Event2).FullName);

    // using a generic
    builder.AddSubscription<Event1>();
    builder.AddSubscription<Event2>();
});
```

And the JSON configuration structure:

```json
{
  "Shuttle": {
    "SqlSubscription": {
      "ConnectionStringName": "Subscription",
      "Schema": "dbo",
      "CacheTimeout": "00:05:00"
    },
    "ServiceBus": {
      "Subscription": {
        "SubscribeType": "Normal",
        "MessageTypes": [
          "message-type-a",
          "message-type-b"
        ]
      }
    }
  }
}
```

## Options

| Option | Default	| Description | 
| --- | --- | --- |
| `ConnectionStringName`	 | Subscription | The name of the `ConnectionString` to use to connect to the subscription store. |
| `CacheTimeout` | `00:05:00` | How long event subscribers should be cached for before refreshing the list. |

When moving to a non-development environment it is recommended that you make use of the `Ensure` option for the `SubscribeType`.

## Supported providers

- `Microsoft.Data.SqlClient`
- `Npgsql` / thanks to [hopla](https://github.com/hopla)

If you'd like support for another SQL-based provider please feel free to give it a bash and send a pull request if you *do* go this route.  You are welcome to create an issue and assistance will be provided where possible.

