using System;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharper.Reflection
{
    [ReferenceProviderFactory]
    public class ReflectedReferenceProviderFactory : IReferenceProviderFactory
    {
        public ReflectedReferenceProviderFactory(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, ReflectedReferenceProviderValidator validator)
        {
            lifetime.AddBracket(() => validator.OnChanged += FireOnChanged, () => validator.OnChanged -= FireOnChanged);
        }

        public event Action OnChanged;

        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file)
        {
            if (!(file is ICSharpFile))
                return null;
            var projectFile = sourceFile.ToProjectFile();
            if (projectFile == null)
                return null;

            return new ReflectedReferenceProvider();
        }

        private void FireOnChanged()
        {
            var action = OnChanged;
            if (action != null)
                action();
        }
    }
}
