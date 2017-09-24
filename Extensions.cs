/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open/blob/dotnet-core/LICENSE.md
 */

using System;
using System.Diagnostics;

namespace Open.Diagnostics
{
	public static class Extensions
	{
		public static void WriteToDebug(this Exception ex, bool findInner = false)
		{
			if(ex==null)
				throw new NullReferenceException();

			if (findInner)
			{
				const string seeinner = "See the inner exception for details.";
				var message = ex.Message.Trim();
				while (ex.InnerException != null && message.EndsWith(seeinner))
				{
					ex = ex.InnerException;
					message.Replace(seeinner, ex.Message.Trim());
				}
			}
			Debug.WriteLine("=== EXCEPTION ===\n" + ex.ToString() + "\n=============\n");
		}
	}
}
