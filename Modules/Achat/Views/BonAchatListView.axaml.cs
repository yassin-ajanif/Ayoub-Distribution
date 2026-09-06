using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Achat.Views;

public partial class BonAchatListView : UserControl
{
    public BonAchatListView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is ViewModels.BonAchatListViewModel vm)
            vm.LoadCommand.Execute(null);
    }

    private void OnRowContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is ContextMenu cm && cm.PlacementTarget is { DataContext: { } dc })
            cm.DataContext = dc;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
