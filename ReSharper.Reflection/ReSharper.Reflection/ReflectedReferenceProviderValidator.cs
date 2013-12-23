using System;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Asp.CustomReferences;
using JetBrains.ReSharper.Psi.Web.Util;

namespace ReSharper.Reflection
{
    [SolutionComponent]
    public class ReflectedReferenceProviderValidator
    {
        private bool myProjectModelReady;

        public ReflectedReferenceProviderValidator(Lifetime lifetime, IShellLocks shellLocks, ChangeManager changeManager, ISettingsStore settingsStore, ISolution solution)
        {
            var providerValidator = this;
            changeManager.Changed2.Advise(lifetime, Handler(solution, providerValidator));
            settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
                .GetValueProperty<MvcCustomReferencesSettings, bool>(lifetime, mvcSettings => mvcSettings.Enabled)
                .Change.Advise_NoAcknowledgement(lifetime,
                    () =>
                    {
                        if (lifetime.IsTerminated)
                            return;
                        shellLocks.ExecuteOrQueueReadLockEx(lifetime, "ReflectedReferenceProviderValidator", () => shellLocks.ExecuteWithWriteLock(providerValidator.FireOnChanged));
                    });
        }

        public event Action OnChanged;

        private static Action<ChangeEventArgs> Handler(ISolution solution, ReflectedReferenceProviderValidator providerValidator)
        {
            return args =>
            {
                var change = args.ChangeMap.GetChange<ProjectModelChange>(solution);
                if (change == null) return;
                providerValidator.myProjectModelReady = providerValidator.myProjectModelReady || change.ContainsChangeType(ProjectModelChangeType.PROJECT_MODEL_CACHES_READY);
                if (!providerValidator.myProjectModelReady || ReferencedAssembliesServiceEx.IsMvcAssemblyReferenceChange(change) == null) return;
                providerValidator.FireOnChanged();
            };
        }

        private void FireOnChanged()
        {
            if (!myProjectModelReady || OnChanged == null)
                return;

            OnChanged();
        }
    }
}
