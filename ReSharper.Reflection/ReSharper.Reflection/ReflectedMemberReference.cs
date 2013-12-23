using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace ReSharper.Reflection
{
    public sealed class ReflectedMemberReference : ReflectedReferenceBase<IExpression>
    {
        private readonly IArgumentsOwner _arguments;
        private readonly ReflectedMemberKind _kind;

        public ReflectedMemberReference(ReflectedMemberKind kind, [NotNull] IExpression owner, [NotNull] IArgumentsOwner arguments)
            : base(owner)
        {
            _kind = kind;
            _arguments = arguments;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            IPsiModule module = myOwner.GetPsiModule();
            if (!module.IsValid())
                return EmptySymbolTable.INSTANCE;

            var symbolTable = new SymbolTable(myOwner.GetPsiServices());

            foreach (var @class in ReflectionUtil.GetReflectedTypes(_arguments))
            {
                if (@class != null)
                {
                    foreach (var method in ReflectionUtil.GetTypeMembers(@class, module, _kind))
                    {
                        if (!useReferenceName || Match(method))
                            symbolTable.AddSymbol(method);
                    }
                }
            }
            return symbolTable;
        }

        private bool Match(IDeclaredElement method)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(method.ShortName, GetName()))
                return false;

            //TODO: check types
            //IArgument types = _arguments.Arguments.FirstOrDefault(a => a.MatchingParameter.Element.ShortName == "types");
            //if (types == null)
            //    return true;
            
            return true;
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            IExpression expression = InternalBindTo(element);
            if (myOwner.Equals(expression)) return this;

            return expression.FindReference<ReflectedMemberReference>() ?? this;
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            ResolveResultWithInfo resolveInfo = base.ResolveWithoutCache();
            return new ResolveResultWithInfo(resolveInfo.Result, ReflectionUtil.CheckResolveResult(resolveInfo.Info));
        }

        protected override string PrepareName(ISymbolInfo symbol)
        {
            var method = symbol.GetDeclaredElement() as IMethod;
            Assertion.AssertNotNull(method, "method == null");
            return method.ShortName;
        }
    }
}