namespace Shuttle.Esb.Sql.Subscription
{
    public enum SubscribeType
    {
        Normal = 0,
        Ensure = 1,
        Ignore = 2
    }

    public class SubscriptionOptions
    {
        public const string SectionName = "Shuttle:Subscription";

        public SubscribeType SubscribeType { get; set; }
        public string ConnectionStringName { get; set; }
    }
}