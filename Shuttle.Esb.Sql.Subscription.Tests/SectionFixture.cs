using System;
using System.IO;
using NUnit.Framework;
using Shuttle.Core.Configuration;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
	[TestFixture]
	public class SectionFixture
	{
		protected SubscriptionSection GetSection(string file)
		{
			return ConfigurationSectionProvider.OpenFile<SubscriptionSection>("shuttle", "subscription", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@".\files\{0}", file)));
		}

		[Test]
		[TestCase("Subscription.config")]
		[TestCase("Subscription-Grouped.config")]
		public void Should_be_able_to_get_full_section(string file)
		{
			var section = GetSection(file);

			Assert.IsNotNull(section);

			Assert.AreEqual("connection-string-name", section.ConnectionStringName);
			Assert.AreEqual(SubscribeOption.Ensure, section.Subscribe);
			Assert.AreEqual(TimeSpan.FromMinutes(10), section.CacheTimeout);
		}
	}
}
