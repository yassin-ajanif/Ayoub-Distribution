using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Tiers.Models;

namespace GestionCommerciale.Shared.Helpers;

public partial class ClientCategoryFilter : ObservableObject
{
    private readonly List<Tiers> _all = [];

    public ObservableCollection<Tiers> Clients { get; } = [];

    public void BindSelection(Func<int> getClientId, Action<Tiers?> setSelectedClient)
    {
        // Kept for call-site compatibility; selection is driven by the autocomplete.
    }

    public void ReplaceAll(IEnumerable<Tiers> clients)
    {
        _all.Clear();
        _all.AddRange(clients);
        Refresh();
    }

    public void EnsureCategoryFor(int clientId)
    {
        // No category filter anymore.
    }

    private void Refresh()
    {
        Clients.Clear();
        foreach (var c in _all.OrderBy(c => c.Nom))
            Clients.Add(c);
    }
}
