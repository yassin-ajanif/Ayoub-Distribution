using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GestionCommerciale.Modules.Sortie.ViewModels;

namespace GestionCommerciale.Modules.Sortie.Views;

public partial class BonSortieEditView : UserControl
{
    public BonSortieEditView()
    {
        InitializeComponent();
    }

    private void OnHeaderContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is ContextMenu cm && cm.PlacementTarget is { DataContext: { } dc })
            cm.DataContext = dc;
    }

    private void OnLineContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is not ListBoxItem item) return;
        ApplyPromoClass(item);
        if (item.DataContext is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnLinePropertyChanged;
            npc.PropertyChanged += OnLinePropertyChanged;
        }
    }

    private void OnLineContainerClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (e.Container is not ListBoxItem item) return;
        if (item.DataContext is INotifyPropertyChanged npc)
            npc.PropertyChanged -= OnLinePropertyChanged;
        item.Classes.Remove("promo");
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BonSortieLineRow.IsPromo)) return;
        if (sender is null) return;
        if (this.FindControl<ListBox>("LinesList")?.ContainerFromItem(sender) is ListBoxItem item)
            ApplyPromoClass(item);
    }

    private static void ApplyPromoClass(ListBoxItem item)
    {
        if (item.DataContext is BonSortieLineRow { IsPromo: true })
            item.Classes.Add("promo");
        else
            item.Classes.Remove("promo");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
