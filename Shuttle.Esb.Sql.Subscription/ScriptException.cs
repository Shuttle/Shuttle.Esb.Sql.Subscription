using System;

namespace Shuttle.Esb.Sql.Subscription
{
	public class ScriptException : Exception
	{
		public ScriptException(string message)
			: base(message)
		{
		}
	}
}