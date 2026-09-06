using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
