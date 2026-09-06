using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.BonRetourFournisseur.Views;

public partial class BonRetourFournisseurEditView : UserControl
{
    public BonRetourFournisseurEditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
