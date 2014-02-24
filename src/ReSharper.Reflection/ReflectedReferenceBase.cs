using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace ReSharper.Reflection
{
    public abstract class ReflectedReferenceBase<T> : TreeReferenceBase<T>, IReflectedReference
        where T : IExpression
    {
        protected ReflectedReferenceBase([NotNull] T owner)
            : base(owner)
        {
            Assertion.Assert(owner.IsValid() && myOwner.IsConstantValue() || myOwner.ConstantValue.IsString(), "Bad constant");
        }

        bool IQualifiableReferenceBase.IsQualified
        {
            get { return false; }
        }

        ResolveResultWithInfo IReferenceWithGlobalSymbolTable.Resolve(ISymbolTable symbolTable, IAccessContext context)
        {
            return Resolve();
        }

        IQualifier IReferenceWithQualifier.GetQualifier()
        {
            return null;
        }

        public virtual bool IsInternalValid
        {
            get { return myOwner.ConstantValue.IsString(); }
        }

        public Refers RefersToDeclaredElement(IDeclaredElement declaredElement)
        {
            var resolveResult = Resolve().Result;
            if (Equals(resolveResult.DeclaredElement, declaredElement))
                return Refers.YES;
            return !resolveResult.Candidates.Any(element => Equals(element, declaredElement)) ? Refers.NO : Refers.MAYBE;
        }

        public virtual ISymbolTable GetCompletionSymbolTable()
        {
            return GetReferenceSymbolTable(false);
        }

        public override IAccessContext GetAccessContext()
        {
            return new DefaultAccessContext(myOwner);
        }

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
        {
            return BindTo(element);
        }

        public override string GetName()
        {
            return myOwner.ConstantValue.Value as string ?? "???";
        }

        public override bool IsValid()
        {
            return base.IsValid() && IsInternalValid;
        }

        public override TreeTextRange GetTreeTextRange()
        {
            var owner = myOwner as ICSharpLiteralExpression;
            if (owner == null) return myOwner.GetTreeTextRange();

            var contentTextRange = owner.GetLiteralContentTextRange();
            if (contentTextRange == TextRange.InvalidRange)
                return TreeTextRange.InvalidRange;
            
            return TreeTextRange.FromLength(owner.GetTreeStartOffset() + contentTextRange.StartOffset, contentTextRange.Length);
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var list = GetReferenceSymbolTable(true).GetAllSymbolInfos().Select(info => info.GetDeclaredElement()).ToList();

            switch (list.Count)
            {
                case 0:
                    return ResolveResultWithInfo.Unresolved;
                case 1:
                    return new ResolveResultWithInfo(ResolveResultFactory.CreateResolveResult(list.First()), ResolveErrorType.OK);
                default:
                    return new ResolveResultWithInfo(ResolveResultFactory.CreateResolveResult(list), ResolveErrorType.MULTIPLE_CANDIDATES);
            }
        }

        private IExpression BindTo(string value)
        {
            Assertion.Assert(myOwner.ConstantValue.IsString(), "myOwner.ConstantValue.IsString()");
            var literalByExpression = StringLiteralAltererUtil.CreateStringLiteralByExpression(myOwner);
            literalByExpression.Replace((string) myOwner.ConstantValue.Value, value, myOwner.GetPsiModule());
            return literalByExpression.Expression;
        }

        /// <summary/>
        /// <param name="element"/>
        /// <returns>
        /// element holds reference (new one, if rebinded, or old one - if not binded)
        /// </returns>
        protected IExpression InternalBindTo(IDeclaredElement element)
        {
            var symbol = GetReferenceSymbolTable(false).GetAllSymbolInfos().FirstOrDefault(info => Equals(info.GetDeclaredElement(), element));
            if (symbol != null)
                return BindTo(PrepareName(symbol));
            
            return myOwner;
        }

        protected virtual string PrepareName(ISymbolInfo symbol)
        {
            return symbol.ShortName;
        }
    }
}
