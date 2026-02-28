using System.Runtime.CompilerServices;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
	// Define this to use C#9 feature `record` in .NET Framework 4.X
	internal sealed class IsExternalInit { }
}
#endif
