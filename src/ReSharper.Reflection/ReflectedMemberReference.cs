using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
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
            var module = myOwner.GetPsiModule();
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

        private bool Match(IDeclaredElement declaredElement)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(declaredElement.ShortName, GetName()))
                return false;

            //TODO: check access

            var method = declaredElement as IMethod;
            if (method == null)
                return true;

            var typesArgument = _arguments.Arguments
                .Where(a => a.MatchingParameter != null)
                .FirstOrDefault(a => a.MatchingParameter.Element.ShortName == "types");
            if (typesArgument == null)
                return true;

            var arrayCreationExpression = typesArgument.Expression as IArrayCreationExpression;
            if (arrayCreationExpression == null)
                return true;

            var elementInitializers = arrayCreationExpression.ArrayInitializer.ElementInitializers;
            var methodParameters = method.Parameters;
            if (elementInitializers.Count != methodParameters.Count)
                return false;

            for (int i = 0; i < elementInitializers.Count; i++)
            {
                var expressionInitializer = elementInitializers[i] as IExpressionInitializer;
                if (expressionInitializer == null)
                    return true;

                var typeofExpression = expressionInitializer.Value as ITypeofExpression;
                if (typeofExpression == null)
                    return true;


                var argumentType = typeofExpression.ArgumentType;
                if (!Equals(argumentType, methodParameters[i].Type))
                    return false;
            }

            return true;
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            var expression = InternalBindTo(element);
            if (myOwner.Equals(expression)) return this;

            return expression.FindReference<ReflectedMemberReference>() ?? this;
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveInfo = base.ResolveWithoutCache();
            return new ResolveResultWithInfo(resolveInfo.Result, ReflectionUtil.CheckResolveResult(resolveInfo.Info));
        }

        protected override string PrepareName(ISymbolInfo symbol)
        {
            var element = symbol.GetDeclaredElement();
            Assertion.AssertNotNull(element, "element == null");
            return element.ShortName;
        }
    }
}