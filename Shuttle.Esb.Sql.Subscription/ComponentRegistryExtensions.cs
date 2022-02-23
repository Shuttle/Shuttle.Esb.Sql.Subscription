using Shuttle.Core.Container;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription
{
	public static class ComponentRegistryExtensions
	{
		public static void RegisterSubscription(this IComponentRegistry registry)
		{
			Guard.AgainstNull(registry, "registry");

			registry.AttemptRegister<IScriptProviderConfiguration, ScriptProviderConfiguration>();
			registry.AttemptRegister<IScriptProvider, ScriptProvider>();

			registry.AttemptRegisterInstance<ISubscriptionConfiguration>(SubscriptionSection.Configuration());
			registry.AttemptRegister<ISubscriptionManager, SubscriptionManager>();
		}
	}
}