using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.BonRetourFournisseur.ViewModels;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Charges.ViewModels;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.Sortie.ViewModels;
using GestionCommerciale.Modules.Achat.ViewModels;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Modules.Stock.ViewModels;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Modules.Tiers.ViewModels;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Auth.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly PerformanceTestService _testService;

    public AppShellViewModel(
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        PerformanceTestService testService)
    {
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _testService = testService;
        UserLabel = session.Nom ?? string.Empty;
        _workspace.CurrentPageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(WorkspaceCurrentPage));
            UpdateActiveNav();
        };
        _locale.CultureApplied += (_, _) => RefreshShellLabels();
        RefreshShellLabels();
        _workspace.Open(_sp.GetRequiredService<HomeViewModel>());
        UpdateActiveNav();
    }

    public BaseViewModel? WorkspaceCurrentPage => _workspace.CurrentPage;

    [ObservableProperty] private string _userLabel = string.Empty;

    [ObservableProperty] private string _navHome = string.Empty;
    [ObservableProperty] private string _navPos = string.Empty;
    [ObservableProperty] private string _navVente = string.Empty;
    [ObservableProperty] private string _navAchat = string.Empty;
    [ObservableProperty] private string _navClients = string.Empty;
    [ObservableProperty] private string _navDevis = string.Empty;
    [ObservableProperty] private string _navBcc = string.Empty;
    [ObservableProperty] private string _navBl = string.Empty;
    [ObservableProperty] private string _navBonSortie = string.Empty;
    [ObservableProperty] private string _navBonAchat = string.Empty;
    [ObservableProperty] private string _navFactures = string.Empty;
    [ObservableProperty] private string _navBonRetour = string.Empty;
    [ObservableProperty] private string _navBonRetourFournisseur = string.Empty;
    [ObservableProperty] private string _navCharges = string.Empty;
    [ObservableProperty] private string _navFournisseurs = string.Empty;
    [ObservableProperty] private string _navBc = string.Empty;
    [ObservableProperty] private string _navBr = string.Empty;
    [ObservableProperty] private string _navFacturesFournisseur = string.Empty;
    [ObservableProperty] private string _navStockAdmin = string.Empty;
    [ObservableProperty] private string _navStock = string.Empty;
    [ObservableProperty] private string _navProduits = string.Empty;
    [ObservableProperty] private string _navReports = string.Empty;
    [ObservableProperty] private string _navSettings = string.Empty;

    [ObservableProperty] private bool _isTestRunning;
    [ObservableProperty] private string _testProgress = string.Empty;

    [ObservableProperty] private bool _isNavHomeActive;
    [ObservableProperty] private bool _isNavPosActive;
    [ObservableProperty] private bool _isNavClientsActive;
    [ObservableProperty] private bool _isNavFournisseursActive;
    [ObservableProperty] private bool _isNavDevisActive;
    [ObservableProperty] private bool _isNavBccActive;
    [ObservableProperty] private bool _isNavBlActive;
    [ObservableProperty] private bool _isNavBonSortieActive;
    [ObservableProperty] private bool _isNavBonAchatActive;
    [ObservableProperty] private bool _isNavFacturesActive;
    [ObservableProperty] private bool _isNavBonRetourActive;
    [ObservableProperty] private bool _isNavBonRetourFournisseurActive;
    [ObservableProperty] private bool _isNavChargesActive;
    [ObservableProperty] private bool _isNavBcActive;
    [ObservableProperty] private bool _isNavBrActive;
    [ObservableProperty] private bool _isNavFacturesFournisseurActive;
    [ObservableProperty] private bool _isNavStockActive;
    [ObservableProperty] private bool _isNavProduitsActive;
    [ObservableProperty] private bool _isNavReportsActive;
    [ObservableProperty] private bool _isNavSettingsActive;

    private void RefreshShellLabels()
    {
        NavHome = _locale.T("Nav_Home");
        NavPos = _locale.T("Nav_Pos");
        NavVente = _locale.T("Nav_Vente");
        NavAchat = _locale.T("Nav_Achat");
        NavClients = _locale.T("Nav_Clients");
        NavDevis = _locale.T("Nav_Devis");
        NavBcc = _locale.T("Nav_BCC");
        NavBl = _locale.T("Nav_BL");
        NavBonSortie = _locale.T("Nav_BonSortie");
        NavBonAchat = _locale.T("Nav_BonAchat");
        NavFactures = _locale.T("Nav_Factures");
        NavBonRetour = _locale.T("Nav_BonRetour");
        NavBonRetourFournisseur = _locale.T("Nav_BonRetourFournisseur");
        NavCharges = _locale.T("Nav_Charges");
        NavFournisseurs = _locale.T("Nav_Fournisseurs");
        NavBc = _locale.T("Nav_BC");
        NavBr = _locale.T("Nav_BR");
        NavFacturesFournisseur = _locale.T("Nav_FacturesFournisseur");
        NavStockAdmin = _locale.T("Nav_StockAdmin");
        NavStock = _locale.T("Nav_Stock");
        NavProduits = _locale.T("Nav_Produits");
        NavReports = _locale.T("Nav_Reports");
        NavSettings = _locale.T("Nav_Settings");
        Title = NavHome;
    }

    [ObservableProperty] private bool _venteNavExpanded = true;
    [ObservableProperty] private bool _achatNavExpanded;
    [ObservableProperty] private bool _footerNavExpanded;
    private bool _suppressNavAccordion;

    public string VenteNavArrow => VenteNavExpanded ? "\u25BC" : "\u25B6";
    public string AchatNavArrow => AchatNavExpanded ? "\u25BC" : "\u25B6";
    public string FooterNavArrow => FooterNavExpanded ? "\u25BC" : "\u25B6";

    partial void OnVenteNavExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(VenteNavArrow));
        if (value && !_suppressNavAccordion)
            CollapseOtherNavSections(except: 0);
    }

    partial void OnAchatNavExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(AchatNavArrow));
        if (value && !_suppressNavAccordion)
            CollapseOtherNavSections(except: 1);
    }

    partial void OnFooterNavExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(FooterNavArrow));
        if (value && !_suppressNavAccordion)
            CollapseOtherNavSections(except: 2);
    }

    private void CollapseOtherNavSections(int except)
    {
        _suppressNavAccordion = true;
        try
        {
            if (except != 0) VenteNavExpanded = false;
            if (except != 1) AchatNavExpanded = false;
            if (except != 2) FooterNavExpanded = false;
        }
        finally
        {
            _suppressNavAccordion = false;
        }
    }

    private void ExpandNavSectionForCurrentPage()
    {
        var p = _workspace.CurrentPage;
        var section = 0;
        if (p is TiersListViewModel tl && tl.Scope == TiersListScope.Fournisseurs
            || p is TiersDetailViewModel td && td.ListScope == TiersListScope.Fournisseurs
            || p is BonAchatListViewModel or BonAchatEditViewModel
            || p is BonRetourFournisseurListViewModel or BonRetourFournisseurEditViewModel
            || p is ChargeListViewModel or ChargeEditViewModel)
            section = 1;
        else if (p is StockMainViewModel or ProduitsViewModel)
            section = 2;
        else if (p is HomeViewModel or ReportsListViewModel or SettingsViewModel)
            return;

        _suppressNavAccordion = true;
        try
        {
            VenteNavExpanded = section == 0;
            AchatNavExpanded = section == 1;
            FooterNavExpanded = section == 2;
        }
        finally
        {
            _suppressNavAccordion = false;
        }
    }

    [RelayCommand]
    private void ToggleVenteNav() => VenteNavExpanded = !VenteNavExpanded;

    [RelayCommand]
    private void ToggleAchatNav() => AchatNavExpanded = !AchatNavExpanded;

    [RelayCommand]
    private void ToggleFooterNav() => FooterNavExpanded = !FooterNavExpanded;

    public bool ShowNavClients => _session.CanAccessClients;
    public bool ShowNavFournisseurs => _session.CanAccessFournisseurs;
    public bool ShowNavStock => _session.CanAccessStock;
    public bool ShowNavProduits => _session.CanAccessStock;
    public bool ShowNavDevis => _session.CanAccessDevis;
    public bool ShowNavBCC => _session.CanAccessDevis;
    public bool ShowNavBL => _session.CanAccessBL;
    public bool ShowNavBonSortie => _session.CanAccessSortie;
    public bool ShowNavBonAchat => _session.CanAccessAchat;
    public bool ShowNavBR => _session.CanAccessBR;
    public bool ShowNavBC => _session.CanAccessBC;
    public bool ShowNavFactures => _session.CanAccessFacturation;
    public bool ShowNavBonRetour => _session.CanAccessBonRetour;
    public bool ShowNavFacturesFournisseur => _session.CanAccessFacturation;
    public bool ShowNavBonRetourFournisseur => _session.CanAccessBonRetour;
    public bool ShowNavCharges => _session.CanAccessCharges;
    public bool ShowNavReports => _session.CanAccessReporting;
    public bool ShowNavSettings => _session.CanAccessSettings;

    [RelayCommand]
    private void GoHome() => _workspace.Open(_sp.GetRequiredService<HomeViewModel>());

    [RelayCommand]
    private void GoPos() { }

    [RelayCommand]
    private void GoClients()
    {
        var vm = _sp.GetRequiredService<TiersListViewModel>();
        vm.Configure(TiersListScope.Clients);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void GoFournisseurs()
    {
        var vm = _sp.GetRequiredService<TiersListViewModel>();
        vm.Configure(TiersListScope.Fournisseurs);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void GoStock() => _workspace.Open(_sp.GetRequiredService<StockMainViewModel>());

    [RelayCommand]
    private void GoProduits() => _workspace.Open(_sp.GetRequiredService<ProduitsViewModel>());

    [RelayCommand]
    private void GoReports()
    {
        var vm = _sp.GetRequiredService<ReportsListViewModel>();
        _workspace.Open(vm);
        vm.GoProfitChargesCommand.Execute(null);
    }

    [RelayCommand]
    private void GoBonSortie() => _workspace.Open(_sp.GetRequiredService<BonSortieListViewModel>());

    [RelayCommand]
    private void GoBonAchat() => _workspace.Open(_sp.GetRequiredService<BonAchatListViewModel>());

    [RelayCommand]
    private void GoBonRetour() => _workspace.Open(_sp.GetRequiredService<BonRetourListViewModel>());

    [RelayCommand]
    private void GoBonRetourFournisseur() => _workspace.Open(_sp.GetRequiredService<BonRetourFournisseurListViewModel>());

    [RelayCommand]
    private void GoCharges() => _workspace.Open(_sp.GetRequiredService<ChargeListViewModel>());

    [RelayCommand]
    private void GoSettings() => _workspace.Open(_sp.GetRequiredService<SettingsViewModel>());

    [RelayCommand]
    private async Task RunPerfTestAsync(CancellationToken ct)
    {
        if (IsTestRunning) return;
        IsTestRunning = true;
        TestProgress = string.Empty;
        try
        {
            var progress = new Progress<string>(msg => TestProgress = msg);
            var result = await _testService.RunAsync(progress, ct);
            TestProgress = result;
        }
        finally
        {
            IsTestRunning = false;
        }
    }

    private void UpdateActiveNav()
    {
        var p = _workspace.CurrentPage;
        IsNavHomeActive = p is HomeViewModel;
        IsNavPosActive = false;
        IsNavClientsActive = p is TiersListViewModel tl && tl.Scope == TiersListScope.Clients
            || p is TiersDetailViewModel td && td.ListScope == TiersListScope.Clients;
        IsNavFournisseursActive = p is TiersListViewModel tiersList && tiersList.Scope == TiersListScope.Fournisseurs
            || p is TiersDetailViewModel tiersDetail && tiersDetail.ListScope == TiersListScope.Fournisseurs;
        IsNavDevisActive = false;
        IsNavBccActive = false;
        IsNavBlActive = false;
        IsNavBonSortieActive = p is BonSortieListViewModel or BonSortieEditViewModel;
        IsNavBonAchatActive = p is BonAchatListViewModel or BonAchatEditViewModel;
        IsNavFacturesActive = false;
        IsNavBonRetourActive = p is BonRetourListViewModel or BonRetourEditViewModel;
        IsNavBonRetourFournisseurActive = p is BonRetourFournisseurListViewModel or BonRetourFournisseurEditViewModel;
        IsNavChargesActive = p is ChargeListViewModel or ChargeEditViewModel;
        IsNavBcActive = false;
        IsNavBrActive = false;
        IsNavFacturesFournisseurActive = false;
        IsNavStockActive = p is StockMainViewModel;
        IsNavProduitsActive = p is ProduitsViewModel;
        IsNavReportsActive = p is ReportsListViewModel;
        IsNavSettingsActive = p is SettingsViewModel;
        ExpandNavSectionForCurrentPage();
    }
}
