using System;
using System.IO;
using NUnit.Framework;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
	[TestFixture]
	public class SqlSectionFixture
	{
		protected SubscriptionSection GetSection(string file)
		{
			return ConfigurationSectionProvider.OpenFile<SubscriptionSection>("shuttle", "subscription", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@".\SubscriptionSection\files\{0}", file)));
		}

		[Test]
		[TestCase("Subscription.config")]
		[TestCase("Subscription-Grouped.config")]
		public void Should_be_able_to_get_full_section(string file)
		{
			var section = GetSection(file);

			Assert.IsNotNull(section);

			Assert.AreEqual("subscription-connection-string-name", section.SubscriptionManagerConnectionStringName);
			Assert.IsFalse(section.IgnoreSubscribe);
		}
	}
}