using System;
using System.Collections.Generic;
using System.Linq;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription
{
    public static class SubscriptionServiceExtensions
    {
        public static void Subscribe(this ISubscriptionService subscriptionManager, IEnumerable<string> messageTypeFullNames)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(messageTypeFullNames);
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string messageTypeFullName)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(new[] { messageTypeFullName });
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, IEnumerable<Type> messageTypes)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(messageTypes.Select(messageType => messageType.FullName).ToList());
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, Type messageType)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(new[] { messageType.FullName });
        }

        public static void Subscribe<T>(this ISubscriptionService subscriptionManager)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(new[] { typeof(T).FullName });
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string connectionStringName, IEnumerable<string> messageTypeFullNames)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(connectionStringName, messageTypeFullNames);
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string connectionStringName, string messageTypeFullName)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(connectionStringName, new[] { messageTypeFullName });
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string connectionStringName, IEnumerable<Type> messageTypes)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(connectionStringName, messageTypes.Select(messageType => messageType.FullName).ToList());
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string connectionStringName, Type messageType)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(connectionStringName, new[] { messageType.FullName });
        }

        public static void Subscribe<T>(this ISubscriptionService subscriptionManager, string connectionStringName)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(connectionStringName, new[] { typeof(T).FullName });
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string providerName, string connectionString, IEnumerable<string> messageTypeFullNames)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(providerName, connectionString, messageTypeFullNames);
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string providerName, string connectionString, string messageTypeFullName)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(providerName, connectionString, new[] { messageTypeFullName });
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string providerName, string connectionString, IEnumerable<Type> messageTypes)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(providerName, connectionString, messageTypes.Select(messageType => messageType.FullName).ToList());
        }

        public static void Subscribe(this ISubscriptionService subscriptionManager, string providerName, string connectionString, Type messageType)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(providerName, connectionString, new[] { messageType.FullName });
        }

        public static void Subscribe<T>(this ISubscriptionService subscriptionManager, string providerName, string connectionString)
        {
            GuardedSubscriptionService(subscriptionManager).Subscribe(providerName, connectionString, new[] { typeof(T).FullName });
        }

        private static SubscriptionService GuardedSubscriptionService(ISubscriptionService subscriptionManager)
        {
            Guard.AgainstNull(subscriptionManager, nameof(subscriptionManager));

            var result = subscriptionManager as SubscriptionService;

            if (result == null)
            {
                throw new InvalidOperationException(string.Format(Resources.SubscriptionManagerCastException, subscriptionManager.GetType().FullName));
            }

            return result;
        }
    }
}