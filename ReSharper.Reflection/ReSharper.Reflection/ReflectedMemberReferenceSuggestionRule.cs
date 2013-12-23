using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExpectedTypes;

namespace ReSharper.Reflection
{
    [Language(typeof (CSharpLanguage))]
    public class ReflectedMemberReferenceSuggestionRule : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        protected override AutocompletionBehaviour GetAutocompletionBehaviour(CSharpCodeCompletionContext specificContext)
        {
            return AutocompletionBehaviour.AutocompleteWithReplace;
        }

        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.TerminatedContext.Reference is ReflectedMemberReference;
        }

        protected override void TransformItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
        {
            if (!IsAvailable(context))
                return;
            var list = collector.Items.ToList();
            collector.Clear();
            foreach (var lookupItem in list)
            {
                var methodsLookupItem = lookupItem as MethodsLookupItem;
                if (methodsLookupItem != null)
                {
                    var textLookupItem = new TextLookupItem(methodsLookupItem.Text, methodsLookupItem.Image);
                    textLookupItem.InitializeRanges(methodsLookupItem.Ranges, context.BasicContext);
                    collector.AddAtDefaultPlace(textLookupItem);
                }
                else if (!(lookupItem is CSharpTypeElementLookupItem))
                    collector.AddAtDefaultPlace(lookupItem);
            }
        }

        protected override void DecorateItems(CSharpCodeCompletionContext context, IEnumerable<ILookupItem> items)
        {
            foreach (var item in items.OfType<TextLookupItemBase>())
            {
                item.TailType = TailType.None;
            }
        }
    }
}
