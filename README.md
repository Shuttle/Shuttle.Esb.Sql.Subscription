# Shuttle.Esb.Sql.Subscription

Sql RDBS implementation of the `IQueue` interface for use with Shuttl.Esb.

# Supported providers

Currently only the `System.Data.SqlClient` provider name is supported but this can easily be extended.  Feel free to give it a bash and please send a pull request if you *do* go this route.  You are welcome to create an issue and assistance will be provided where able.

## Configuration

~~~xml
<configuration>
  <configSections>
    <section name='sqlSubscription' type="Shuttle.Esb.Sql.Subscription.SqlSection, Shuttle.Esb.Sql.Subscription"/>
  </configSections>
  
  <sqlSubscription
	subscriptionManagerConnectionStringName="Subscription"
	idempotenceServiceConnectionStringName="Idempotence"
  />
  .
  .
  .
<configuration>
~~~

# SubscriptionManager

A Sql Server based `ISubscriptionManager` implementation is also provided.  The subscription manager caches all subscriptions forever so should a new subscriber be added be sure to restart the publisher endpoint service.

# IdempotenceService

A `IIdempotenceService` implementation is also available for Sql Server.