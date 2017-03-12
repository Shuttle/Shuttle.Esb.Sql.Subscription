using System;
using System.IO;
using NUnit.Framework;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
	[TestFixture]
	public class SqlSectionFixture
	{
		protected SqlSection GetSection(string file)
		{
			return ConfigurationSectionProvider.OpenFile<SqlSection>("shuttle", "sqlSubscription", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@".\SqlSection\files\{0}", file)));
		}

		[Test]
		[TestCase("Sql.config")]
		[TestCase("Sql-Grouped.config")]
		public void Should_be_able_to_get_full_section(string file)
		{
			var section = GetSection(file);

			Assert.IsNotNull(section);

			Assert.AreEqual("subscription-connection-string-name", section.SubscriptionManagerConnectionStringName);
			Assert.IsFalse(section.IgnoreSubscribe);
		}
	}
}