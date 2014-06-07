using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace ReSharper.Reflection
{
    [SolutionComponent]
    public class ReflectionUtil
    {
        private static readonly Key<CachedPsiValue<ICollection<IClass>>> key = new Key<CachedPsiValue<ICollection<IClass>>>("CachedTypesKey");

        [NotNull]
        public static IEnumerable<IDeclaredElement> GetTypeMembers([CanBeNull] IClass @class, [NotNull] IPsiModule module, ReflectedMemberKind kind)
        {
            if (!module.IsValid())
                return EmptyList<IMethod>.InstanceList;

            switch (kind)
            {
                case ReflectedMemberKind.None:
                    break;
                case ReflectedMemberKind.Method:
                    return GetTypeMethods(@class);
                case ReflectedMemberKind.Property:
                    return GetTypeProperties(@class);
                case ReflectedMemberKind.Event:
                    return GetTypeEvents(@class);
                case ReflectedMemberKind.Field:
                    return GetTypeFields(@class);
                case ReflectedMemberKind.Any:
                    return GetTypeMembers(@class);
                default:
                    throw new ArgumentOutOfRangeException("kind");
            }

            return EmptyList<IDeclaredElement>.InstanceList;
        }

        private static IEnumerable<IDeclaredElement> GetTypeMembers(IClass @class)
        {
            return SelfAndSuperClasses(@class)
                .SelectMany(c => c.GetMembers())
                .Distinct();
        }

        private static IEnumerable<IDeclaredElement> GetTypeFields(IClass @class)
        {
            return SelfAndSuperClasses(@class)
                .SelectMany(c => c.Fields)
                .Distinct();
        }

        private static IEnumerable<IDeclaredElement> GetTypeProperties(IClass @class)
        {
            return SelfAndSuperClasses(@class)
                .SelectMany(c => c.Properties)
                .Distinct();
        }
        
        private static IEnumerable<IDeclaredElement> GetTypeEvents(IClass @class)
        {
            return SelfAndSuperClasses(@class)
                .SelectMany(c => c.Events)
                .Distinct();
        }

        private static IEnumerable<IDeclaredElement> GetTypeMethods(IClass @class)
        {
            return SelfAndSuperClasses(@class)
                .SelectMany(c => c.Methods)
                .Distinct();
        }

        private static IEnumerable<IClass> SelfAndSuperClasses(IClass @class)
        {
            for (var c = @class; c != null && c.IsValid(); c = c.GetSuperClass())
                yield return c;
        }

        public static IEnumerable<IClass> GetReflectedTypes([CanBeNull] IArgumentsOwner argumentsOwner)
        {
            if (argumentsOwner == null || !argumentsOwner.IsValid())
                return EmptyList<IClass>.InstanceList;

            return argumentsOwner.UserData
                .GetOrCreateData(key, argumentsOwner, owner => owner.CreateCachedValue(GetTypesNonCached(owner)))
                .GetValue(argumentsOwner, GetTypesNonCached);
        }

        private static ICollection<IClass> GetTypesNonCached([NotNull] IArgumentsOwner argumentsOwner)
        {
            argumentsOwner.AssertIsValid("argumentsOwner is invalid");
            var invocationExpression = argumentsOwner as IInvocationExpression;
            if (invocationExpression != null)
            {
                var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                if (referenceExpression != null)
                {
                    var typeofExpression = referenceExpression.QualifierExpression as ITypeofExpression;
                    if (typeofExpression != null)
                    {
                        IType type = typeofExpression.ArgumentType;
                        IClass @class = type.GetClassType();
                        return new[] {@class};
                    }
                    var gettypeExpression = referenceExpression.QualifierExpression as IInvocationExpression;
                    if (gettypeExpression != null && gettypeExpression.Reference != null && gettypeExpression.Reference.GetName() == "GetType")
                    {
                        var method = gettypeExpression.Reference.Resolve().DeclaredElement as IMethod;
                        if (method != null)
                        {
                            var containingType = method.GetContainingType();
                            if (containingType != null)
                            {
                                var containingTypeName = containingType.GetClrName().FullName;
                                if (containingTypeName == "System.Object")
                                {
                                    var typeDeclaration = gettypeExpression.GetContainingNode<ITypeDeclaration>();
                                    if (typeDeclaration != null)
                                    {
                                        return new[] {typeDeclaration.DeclaredElement as IClass};
                                    }
                                }
                                if (containingTypeName == "System.Type" && method.IsStatic)
                                {
                                    if (gettypeExpression.Arguments.Count > 0)
                                    {
                                        var literalExpression = gettypeExpression.Arguments[0].Expression as ICSharpLiteralExpression;
                                        if (literalExpression != null && literalExpression.IsConstantValue() && literalExpression.ConstantValue.IsString())
                                        {
                                            var typeName = new ClrTypeName((string) literalExpression.ConstantValue.Value);

                                            var name = new DeclaredTypeFromCLRName(typeName, gettypeExpression.GetPsiModule(), gettypeExpression.GetResolveContext());
                                            return new[] { name.GetTypeElement() as IClass };
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return EmptyList<IClass>.InstanceList;
        }

        public static IResolveInfo CheckResolveResult([NotNull] IResolveInfo result)
        {
            //TODO: remove when type checked.
            if (result == ResolveErrorType.MULTIPLE_CANDIDATES)
                result = ResolveErrorType.DYNAMIC;
            if (result != ResolveErrorType.NOT_RESOLVED)
                return result;

            //return ReflectedResolveErrorType.ReflectedMemberNotResolved; 
            return ResolveErrorType.IGNORABLE;
        }
    }
}