using System.Data;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Subscription
{
	public class Columns
	{
		public static Column<string> InboxWorkQueueUri = new Column<string>("InboxWorkQueueUri", DbType.AnsiString, 265);
		public static Column<string> MessageType = new Column<string>("MessageType", DbType.AnsiString, 265);
	}
}