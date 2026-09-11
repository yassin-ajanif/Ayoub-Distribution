using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using GestionCommerciale.Modules.Reporting.ViewModels;

namespace GestionCommerciale.Modules.Reporting.Views;

public partial class ReportsListView : UserControl
{
    public ReportsListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnProfitFilterCardTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ReportsListViewModel vm || sender is not Border border)
            return;

        switch (border.Tag as string)
        {
            case "Margin":
                vm.FilterProfitMarginCommand.Execute(null);
                break;
            case "Ventes":
                // "Total ventes" uses the same rows as the "Margin" filter (sale documents).
                vm.FilterProfitMarginCommand.Execute(null);
                break;
            case "BonsRetourClient":
                vm.FilterProfitBonsRetourClientCommand.Execute(null);
                break;
            case "Purchases":
                vm.FilterProfitPurchasesCommand.Execute(null);
                break;
            case "BonsRetourFournisseur":
                vm.FilterProfitBonsRetourFournisseurCommand.Execute(null);
                break;
            case "Charges":
                vm.FilterProfitChargesCommand.Execute(null);
                break;
            case "Promo":
                vm.FilterProfitPromoCommand.Execute(null);
                break;
            case "All":
                vm.FilterProfitAllCommand.Execute(null);
                break;
        }
    }

    private void OnProfitRowTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ReportsListViewModel vm)
            return;
        if (sender is not Border { DataContext: ReportProfitChargeRow row })
            return;

        vm.OpenProfitDocumentCommand.Execute(row);
    }

    private void OnCustomerDocNumeroTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ReportsListViewModel vm)
            return;
        if (sender is not TextBlock { DataContext: ReportSaleByCustomerDocRow row })
            return;

        e.Handled = true;
        vm.OpenCustomerDocumentCommand.Execute(row);
    }
}
