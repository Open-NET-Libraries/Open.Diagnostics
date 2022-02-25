using System;
using System.Diagnostics.CodeAnalysis;

#if DEBUG
using System.Diagnostics;
#endif

namespace Open.Diagnostics;

public static class Extensions
{
	[SuppressMessage("Roslynator", "RCS1175:Unused this parameter.", Justification = "Debug only.")]
	[SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "Debug only.")]
	public static void WriteToDebug(this Exception ex, bool findInner = false)
	{
#if DEBUG
		if (ex == null)
			throw new NullReferenceException();

		if (findInner)
		{
			const string seeinner = "See the inner exception for details.";
			var message = ex.Message.Trim();
			while (ex.InnerException != null && message.EndsWith(seeinner))
			{
				ex = ex.InnerException;
				message = message.Replace(seeinner, ex.Message.Trim());
			}
		}
		Debug.WriteLine("=== EXCEPTION ===\n" + ex + "\n=============\n");
#endif
	}
}
