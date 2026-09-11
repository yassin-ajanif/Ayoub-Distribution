using GestionCommerciale.Modules.BonRetourFournisseur.ViewModels;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Auth.ViewModels;
using GestionCommerciale.Modules.Charges.ViewModels;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.FactureFournisseur.Services;
using GestionCommerciale.Modules.Sortie.Services;
using GestionCommerciale.Modules.Sortie.ViewModels;
using GestionCommerciale.Modules.Achat.Services;
using GestionCommerciale.Modules.Achat.ViewModels;
using GestionCommerciale.Modules.Reporting.Services;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Stock.ViewModels;
using GestionCommerciale.Modules.Tiers.ViewModels;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGestionCommerciale(this IServiceCollection services)
    {
        var cs = DatabasePath.GetConnectionString();
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(cs));

        services.AddSingleton<RootNavigator>();
        services.AddSingleton<IRootNavigator>(sp => sp.GetRequiredService<RootNavigator>());
        services.AddSingleton<WorkspaceNavigator>();
        services.AddSingleton<IWorkspaceNavigator>(sp => sp.GetRequiredService<WorkspaceNavigator>());
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ICurrentUserSession, CurrentUserSession>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<IUiPreferencesService, UiPreferencesService>();
        services.AddSingleton<ILocaleService, LocaleService>();
        services.AddSingleton<IDocumentNumberService, DocumentNumberService>();
        services.AddSingleton<IStockMovementService, StockMovementService>();
        services.AddSingleton<IClientAccountStatementService, ClientAccountStatementService>();
        services.AddSingleton<IClientBulkPaymentService, ClientBulkPaymentService>();
        services.AddSingleton<ISupplierBulkPaymentService, SupplierBulkPaymentService>();
        services.AddSingleton<ISupplierAccountStatementService, SupplierAccountStatementService>();
        services.AddSingleton<IBonSortieWorkflowService, BonSortieWorkflowService>();
        services.AddSingleton<IBonAchatWorkflowService, BonAchatWorkflowService>();
        services.AddSingleton<IBonRetourWorkflowService, BonRetourWorkflowService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IAppUpdateService, AppUpdateService>();
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<IPdfPrintService, PdfPrintService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IPeriodicBackupService, PeriodicBackupService>();
        services.AddSingleton<IProductImportExportService, ProductImportExportService>();
        services.AddSingleton<VirtualKeyboardService>();
        services.AddSingleton<PerformanceTestService>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AppShellViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddTransient<TiersListViewModel>();
        services.AddTransient<TiersDetailViewModel>();
        services.AddTransient<StockMainViewModel>();
        services.AddTransient<ProduitsViewModel>();
        services.AddTransient<BonSortieListViewModel>();
        services.AddTransient<BonSortieEditViewModel>();
        services.AddTransient<BonAchatListViewModel>();
        services.AddTransient<BonAchatEditViewModel>();
        services.AddTransient<BonRetourListViewModel>();
        services.AddTransient<BonRetourEditViewModel>();
        services.AddTransient<BonRetourFournisseurListViewModel>();
        services.AddTransient<BonRetourFournisseurEditViewModel>();
        services.AddTransient<ChargeListViewModel>();
        services.AddTransient<ChargeEditViewModel>();
        services.AddSingleton<ReportingViewModel>();
        services.AddTransient<ReportsListViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
