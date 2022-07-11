using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Shuttle.Esb.Sql.Subscription.Tests
{
	[TestFixture]
	public class SubscriptionOptionsFixture
	{
		protected SubscriptionOptions GetOptions()
		{
			var result = new SubscriptionOptions();

			new ConfigurationBuilder()
				.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\appsettings.json")).Build()
				.GetRequiredSection($"{SubscriptionOptions.SectionName}").Bind(result);

			return result;
		}

		[Test]
		public void Should_be_able_to_get_full_settings()
		{
			var options = GetOptions();

			Assert.IsNotNull(options);

			Assert.AreEqual("connection-string-name", options.ConnectionStringName);
			Assert.AreEqual(SubscribeType.Ensure, options.SubscribeType);
		}
	}
}
