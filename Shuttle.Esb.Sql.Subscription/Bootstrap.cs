using Shuttle.Core.Container;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Subscription
{
	public class Bootstrap : IComponentRegistryBootstrap
	{
		public void Register(IComponentRegistry registry)
		{
			Guard.AgainstNull(registry, "registry");

			registry.AttemptRegister<IScriptProviderConfiguration, ScriptProviderConfiguration>();
			registry.AttemptRegister<IScriptProvider, ScriptProvider>();

			registry.AttemptRegisterInstance<ISubscriptionConfiguration>(SubscriptionSection.Configuration());
			registry.AttemptRegister<ISubscriptionManager, SubscriptionManager>();
		}
	}
}