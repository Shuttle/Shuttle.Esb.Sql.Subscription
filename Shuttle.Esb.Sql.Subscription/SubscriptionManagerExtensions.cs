using System;
using System.Collections.Generic;
using System.Linq;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription
{
    public static class SubscriptionManagerExtensions
    {
        public static void Subscribe(this ISubscriptionManager subscriptionManager,
            IEnumerable<string> messageTypeFullNames)
        {
            GuardedSubscriptionManager(subscriptionManager).Subscribe(messageTypeFullNames);
        }

        private static SubscriptionManager GuardedSubscriptionManager(ISubscriptionManager subscriptionManager)
        {
            Guard.AgainstNull(subscriptionManager, nameof(subscriptionManager));

            var result = subscriptionManager as SubscriptionManager;

            if (result == null)
            {
                throw new InvalidOperationException(string.Format(Resources.SubscriptionManagerCastException, subscriptionManager.GetType().FullName));
            }

            return result;
        }

        public static void Subscribe(this ISubscriptionManager subscriptionManager, string messageTypeFullName)
        {
            GuardedSubscriptionManager(subscriptionManager).Subscribe(new[] { messageTypeFullName });
        }

        public static void Subscribe(this ISubscriptionManager subscriptionManager, IEnumerable<Type> messageTypes)
        {
            GuardedSubscriptionManager(subscriptionManager).Subscribe(messageTypes.Select(messageType => messageType.FullName).ToList());
        }

        public static void Subscribe(this ISubscriptionManager subscriptionManager, Type messageType)
        {
            GuardedSubscriptionManager(subscriptionManager).Subscribe(new[] { messageType.FullName });
        }

        public static void Subscribe<T>(this ISubscriptionManager subscriptionManager)
        {
            GuardedSubscriptionManager(subscriptionManager).Subscribe(new[] { typeof(T).FullName });
        }
    }
}