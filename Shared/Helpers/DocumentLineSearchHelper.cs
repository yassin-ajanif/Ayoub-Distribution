using Avalonia.Threading;

namespace GestionCommerciale.Shared.Helpers;

/// <summary>
/// Avalonia AutoCompleteBox rewrites <c>Text</c> from ValueMember after a pick;
/// clearing immediately is often overwritten, so we clear again on the next UI tick.
/// </summary>
public static class DocumentLineSearchHelper
{
    public static void ClearAfterCatalogPick(Action clear)
    {
        clear();
        Dispatcher.UIThread.Post(clear, DispatcherPriority.Input);
    }
}
