using System;
namespace News360test
{
	public class ParseException : Exception
	{
		public int index = 0;

		public ParseException()
		{
		}

		public ParseException(string message)
			: base(message)
		{
		}

		public ParseException(string message, int index)
			: base(message)
		{
			this.index = index;
		}

		public ParseException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
