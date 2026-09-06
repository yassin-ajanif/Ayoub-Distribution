using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Shared.Views;

public partial class DatePresetChipsBar : UserControl
{
    public DatePresetChipsBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
