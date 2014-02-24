using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Special;

namespace ReSharper.Reflection
{
    public class ReflectedReferenceProvider : IReferenceFactory
    {
        public IReference[] GetReferences(ITreeNode element, IReference[] oldReferences)
        {
            var expression = element as ICSharpExpression;
            if (expression != null)
            {
                if (oldReferences != null && oldReferences.Length > 0)
                {
                    if (oldReferences
                        .Select(reference => reference as IReflectedReference)
                        .All(reference => reference != null && reference.GetTreeNode() == expression && reference.IsInternalValid))
                    {
                        return oldReferences;
                    }
                }

                expression.AssertIsValid("element is not valid");
                if (expression.ConstantValue.IsString())
                {
                    var argument = expression.GetContainingNode<IArgument>();
                    if (argument != null && argument.Expression == expression)
                    {
                        var argumentsOwner = argument.Invocation as IInvocationExpression;
                        if (argumentsOwner != null)
                        {
                            var invokedExpression = argumentsOwner.InvokedExpression as IReferenceExpression;
                            if (invokedExpression != null && invokedExpression.QualifierExpression != null)
                            {
                                var declaredType = invokedExpression.QualifierExpression.Type() as IDeclaredType;

                                if (declaredType != null && declaredType.GetClrName().FullName == "System.Type")
                                {
                                    var kind = GetMemberKind(argumentsOwner);
                                    if (kind != null)
                                    {
                                        var parameter = argument
                                            .IfNotNull(d => d.MatchingParameter)
                                            .IfNotNull(p => p.Element);

                                        if (parameter != null && parameter.ShortName == "name")
                                        {
                                            return new IReference[]
                                            {
                                                new ReflectedMemberReference(kind.Value, expression, argumentsOwner)
                                            };
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return EmptyArray<IReference>.Instance;
        }

        [ContractAnnotation("null => null")]
        private static ReflectedMemberKind? GetMemberKind(ICSharpInvocationInfo argumentsOwner)
        {
            if (argumentsOwner != null && argumentsOwner.Reference != null)
            {
                switch (argumentsOwner.Reference.GetName())
                {
                    case "GetMethod":
                        return ReflectedMemberKind.Method;
                    case "GetProperty":
                        return ReflectedMemberKind.Property;
                    case "GetField":
                        return ReflectedMemberKind.Field;
                    case "GetEvent":
                        return ReflectedMemberKind.Event;
                    case "GetMember":
                        return ReflectedMemberKind.Any;
                }
            }
            return null;
        }

        public bool HasReference(ITreeNode element, ICollection<string> names)
        {
            var expression = element as ICSharpExpression;
            if (expression != null && expression.ConstantValue.IsString())
            {
                expression.AssertIsValid("element is not valid");

                var argument = expression.GetContainingNode<IArgument>();
                if (argument != null && argument.Expression == expression)
                {
                    var argumentsOwner = argument.Invocation as ICSharpArgumentsOwner;
                    if (GetMemberKind(argumentsOwner) != null)
                    {
                        return names.Contains((string) expression.ConstantValue.Value);
                    }
                }
            }
            return false;
        }
    }
}