using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;

namespace ReSharper.Reflection
{
    public interface IReflectedReference : ILateBoundReference, ICompleteableReference, IStringLiteralReference, IQualifiableReferenceWithGlobalSymbolTable, IPrefferedReference
    {
        bool IsInternalValid { get; }
    }
}