using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.FactureFournisseur.Services;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Tiers.ViewModels;

public sealed class ClientLedgerDisplayRow
{
    public string DateText { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public string Observation { get; init; } = string.Empty;
    public string DebitText { get; init; } = string.Empty;
    public string CreditText { get; init; } = string.Empty;
    public string BalanceText { get; init; } = string.Empty;
}

internal enum LedgerRangeKind
{
    All,
    Today,
    Week,
    Month,
    Year,
    Custom
}

public partial class TiersDetailViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ILocaleService _locale;
    private readonly IClientAccountStatementService _clientLedgerService;
    private readonly IClientBulkPaymentService _bulkPayment;
    private readonly ISupplierBulkPaymentService _supplierBulkPayment;
    private readonly ISupplierAccountStatementService _supplierLedgerService;
    private readonly IPdfService _pdf;
    private readonly IAppSettingsService _settings;

    private TiersListScope _returnScope = TiersListScope.Clients;
    private string _devise = "DH";

    public TiersListScope ListScope => _returnScope;

    public TiersDetailViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ILocaleService locale,
        IClientAccountStatementService clientLedgerService,
        IClientBulkPaymentService bulkPayment,
        ISupplierBulkPaymentService supplierBulkPayment,
        ISupplierAccountStatementService supplierLedgerService,
        IPdfService pdf,
        IAppSettingsService settings)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _locale = locale;
        _clientLedgerService = clientLedgerService;
        _bulkPayment = bulkPayment;
        _supplierBulkPayment = supplierBulkPayment;
        _supplierLedgerService = supplierLedgerService;
        _pdf = pdf;
        _settings = settings;
        Title = _locale.T("TiersDetail_Title");
        RebuildTypeOptions();
        BulkPayModes.Clear();
        foreach (ModePaiement mode in Enum.GetValues(typeof(ModePaiement)))
        {
            if (mode == ModePaiement.Credit) continue;
            BulkPayModes.Add(mode);
        }
        _locale.CultureApplied += (_, _) =>
        {
            RefreshDetailUi();
            if (TiersId.HasValue)
                _ = LoadAsync(TiersId.Value, CancellationToken.None);
        };
        RefreshDetailUi();
    }

    [ObservableProperty] private string _btnBackList = string.Empty;
    [ObservableProperty] private string _wmNom = string.Empty;
    [ObservableProperty] private string _wmIce = string.Empty;
    [ObservableProperty] private string _wmAdresse = string.Empty;
    [ObservableProperty] private string _wmVille = string.Empty;
    [ObservableProperty] private string _wmTelephone = string.Empty;
    [ObservableProperty] private string _wmEmail = string.Empty;
    [ObservableProperty] private string _wmConditions = string.Empty;
    [ObservableProperty] private string _chkActif = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;

    [ObservableProperty] private string _lblLedgerTitle = string.Empty;
    [ObservableProperty] private string _lblSoldeActuel = string.Empty;
    [ObservableProperty] private string _soldeActuelText = string.Empty;
    [ObservableProperty] private string _lblLedgerPeriod = string.Empty;
    [ObservableProperty] private string _periodSoldeText = string.Empty;
    [ObservableProperty] private string _btnLedgerToday = string.Empty;
    [ObservableProperty] private string _btnLedgerWeek = string.Empty;
    [ObservableProperty] private string _btnLedgerMonth = string.Empty;
    [ObservableProperty] private string _btnLedgerYear = string.Empty;
    [ObservableProperty] private string _btnLedgerAll = string.Empty;
    [ObservableProperty] private string _lblLedgerFrom = string.Empty;
    [ObservableProperty] private string _lblLedgerTo = string.Empty;
    [ObservableProperty] private DateTime? _ledgerFrom;
    [ObservableProperty] private DateTime? _ledgerTo;
    [ObservableProperty] private bool _isLedgerRangeAll = true;
    [ObservableProperty] private bool _isLedgerRangeToday;
    [ObservableProperty] private bool _isLedgerRangeWeek;
    [ObservableProperty] private bool _isLedgerRangeMonth;
    [ObservableProperty] private bool _isLedgerRangeYear;
    [ObservableProperty] private string _btnPdfLedger = string.Empty;
    [ObservableProperty] private string _lblLedgerDate = string.Empty;
    [ObservableProperty] private string _lblLedgerDesignation = string.Empty;
    [ObservableProperty] private string _lblLedgerObservation = string.Empty;
    [ObservableProperty] private string _lblLedgerDebit = string.Empty;
    [ObservableProperty] private string _lblLedgerCredit = string.Empty;
    [ObservableProperty] private string _lblLedgerBalance = string.Empty;
    [ObservableProperty] private string _lblLedgerEmpty = string.Empty;
    [ObservableProperty] private string _lblLedgerSaveFirst = string.Empty;
    [ObservableProperty] private bool _showLedger;
    [ObservableProperty] private bool _showLedgerSaveFirst;
    [ObservableProperty] private bool _showLedgerEmpty;
    [ObservableProperty] private bool _showBulkPay;

    [ObservableProperty] private string _lblBulkPayTitle = string.Empty;
    [ObservableProperty] private string _lblBulkPayAmount = string.Empty;
    [ObservableProperty] private string _lblBulkPayMode = string.Empty;
    [ObservableProperty] private string _lblBulkPayDate = string.Empty;
    [ObservableProperty] private string _wmBulkPayRef = string.Empty;
    [ObservableProperty] private string _btnBulkPay = string.Empty;
    [ObservableProperty] private decimal _bulkPayAmount;
    [ObservableProperty] private ModePaiement _bulkPayMode = ModePaiement.Especes;
    [ObservableProperty] private DateTime? _bulkPayDate = DateTime.Today;
    [ObservableProperty] private string _bulkPayReference = string.Empty;

    public ObservableCollection<ClientLedgerDisplayRow> LedgerRows { get; } = [];
    public ObservableCollection<TypeTiers> Types { get; } = [];
    private readonly List<ClientAccountStatementRow> _ledgerSource = [];
    private bool _suppressLedgerRange;
    public ObservableCollection<ModePaiement> BulkPayModes { get; } = [];

    public bool CanEditType => _returnScope == TiersListScope.Fournisseurs;

    public string TypeLabel => _locale.T("TypeTiers_Vendeur");

    [ObservableProperty] private int? _tiersId;
    [ObservableProperty] private TypeTiers _type = TypeTiers.Client;
    [ObservableProperty] private string _nom = string.Empty;
    [ObservableProperty] private string _ice = string.Empty;
    [ObservableProperty] private string _adresse = string.Empty;
    [ObservableProperty] private string _ville = string.Empty;
    [ObservableProperty] private string _telephone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _conditionsPaiement = string.Empty;
    [ObservableProperty] private bool _actif = true;

    private void RefreshDetailUi()
    {
        BtnBackList = _locale.T("Btn_BackList");
        WmNom = _locale.T("Wm_Nom");
        WmIce = _locale.T("Wm_Ice");
        WmAdresse = _locale.T("Wm_Adresse");
        WmVille = _locale.T("Wm_Ville");
        WmTelephone = _locale.T("Wm_Telephone");
        WmEmail = _locale.T("Wm_Email");
        WmConditions = _locale.T("Wm_ConditionsPaiement");
        ChkActif = _locale.T("Lbl_Actif");
        BtnSave = _locale.T("Btn_Save");
        LblLedgerTitle = _returnScope == TiersListScope.Fournisseurs
            ? _locale.T("SupplierLedger_Title")
            : _locale.T("ClientLedger_Title");
        RefreshSoldeLabel();
        BtnPdfLedger = _locale.T("Btn_Pdf");
        LblLedgerDate = _locale.T("ClientLedger_ColDate");
        LblLedgerDesignation = _locale.T("ClientLedger_ColDesignation");
        LblLedgerObservation = _locale.T("ClientLedger_ColObservation");
        LblLedgerDebit = _locale.T("ClientLedger_ColDebit");
        LblLedgerCredit = _locale.T("ClientLedger_ColCredit");
        LblLedgerBalance = _locale.T("ClientLedger_ColBalance");
        LblLedgerEmpty = _returnScope == TiersListScope.Fournisseurs
            ? _locale.T("SupplierLedger_Empty")
            : _locale.T("ClientLedger_Empty");
        LblLedgerSaveFirst = _returnScope == TiersListScope.Fournisseurs
            ? _locale.T("SupplierLedger_SaveFirst")
            : _locale.T("ClientLedger_SaveFirst");
        BtnLedgerToday = _locale.T("ClientLedger_RangeToday");
        BtnLedgerWeek = _locale.T("ClientLedger_RangeWeek");
        BtnLedgerMonth = _locale.T("ClientLedger_RangeMonth");
        BtnLedgerYear = _locale.T("ClientLedger_RangeYear");
        BtnLedgerAll = _locale.T("ClientLedger_RangeAll");
        LblLedgerFrom = _locale.T("ClientLedger_RangeFrom");
        LblLedgerTo = _locale.T("ClientLedger_RangeTo");
        RefreshPeriodLabel();
        LblBulkPayTitle = _returnScope == TiersListScope.Fournisseurs
            ? _locale.T("SupplierLedger_BulkPayTitle")
            : _locale.T("ClientLedger_BulkPayTitle");
        LblBulkPayAmount = _returnScope == TiersListScope.Fournisseurs
            ? _locale.T("SupplierLedger_BulkPayAmount")
            : _locale.T("ClientLedger_BulkPayAmount");
        LblBulkPayMode = _locale.T("ClientLedger_BulkPayMode");
        LblBulkPayDate = _locale.T("ClientLedger_BulkPayDate");
        WmBulkPayRef = _locale.T("ClientLedger_BulkPayRef");
        BtnBulkPay = _returnScope == TiersListScope.Fournisseurs
            ? _locale.T("SupplierLedger_BulkPayBtn")
            : _locale.T("ClientLedger_BulkPayBtn");
        UpdateShowBulkPay();
    }

    private void UpdateShowBulkPay() =>
        ShowBulkPay = ShowLedger
            && (_returnScope == TiersListScope.Clients || _returnScope == TiersListScope.Fournisseurs)
            && TiersId.HasValue
            && !ShowLedgerSaveFirst;

    private void RefreshSoldeLabel()
    {
        var name = string.IsNullOrWhiteSpace(Nom) ? _locale.T("TypeTiers_Vendeur") : Nom.Trim();
        LblSoldeActuel = _locale.Tf("ClientLedger_SoldeActuel", name);
    }

    partial void OnNomChanged(string value) => RefreshSoldeLabel();

    private void RebuildTypeOptions()
    {
        Types.Clear();
        switch (_returnScope)
        {
            case TiersListScope.Clients:
                Types.Add(TypeTiers.Client);
                break;
            case TiersListScope.Fournisseurs:
                Types.Add(TypeTiers.Fournisseur);
                Types.Add(TypeTiers.LesDeux);
                break;
        }
        OnPropertyChanged(nameof(CanEditType));
        OnPropertyChanged(nameof(TypeLabel));
    }

    public void Load(int? tiersId) => Load(tiersId, TiersListScope.Clients);

    public void Load(int? tiersId, TiersListScope returnScope)
    {
        _returnScope = returnScope;
        RebuildTypeOptions();
        TiersId = tiersId;
        LedgerRows.Clear();
        _ledgerSource.Clear();
        SoldeActuelText = string.Empty;
        PeriodSoldeText = string.Empty;
        ShowLedger = returnScope == TiersListScope.Clients || returnScope == TiersListScope.Fournisseurs;
        ShowLedgerSaveFirst = tiersId == null && ShowLedger;
        ShowLedgerEmpty = false;
        ResetBulkPayFields();
        RefreshDetailUi();

        if (tiersId == null)
        {
            Nom = string.Empty;
            Ice = string.Empty;
            Adresse = string.Empty;
            Ville = string.Empty;
            Telephone = string.Empty;
            Email = string.Empty;
            ConditionsPaiement = string.Empty;
            Type = returnScope == TiersListScope.Fournisseurs ? TypeTiers.Fournisseur : TypeTiers.Client;
            Actif = true;
            Title = returnScope == TiersListScope.Fournisseurs
                ? _locale.T("TiersDetail_NewSupplier")
                : _locale.T("TiersDetail_NewClient");
            return;
        }

        _ = LoadAsync(tiersId.Value, CancellationToken.None);
    }

    private async Task LoadAsync(int id, CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var cfg = await _settings.GetAsync(cancellationToken);
            _devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "DH" : cfg.Devise.Trim();

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var t = await db.Tiers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (t == null) return;

            Type = t.Type;
            if (!Types.Contains(Type))
                Types.Add(Type);

            Nom = t.Nom;
            Ice = t.ICE;
            Adresse = t.Adresse;
            Ville = t.Ville;
            Telephone = t.Telephone;
            Email = t.Email;
            ConditionsPaiement = t.ConditionsPaiement;
            Actif = t.Actif;
            Title = _returnScope == TiersListScope.Fournisseurs
                ? _locale.Tf("Tiers_TitleSupplierFmt", t.Nom)
                : _locale.Tf("Tiers_TitleClientFmt", t.Nom);

            ShowLedgerSaveFirst = false;
            var isClient = t.Type is TypeTiers.Client or TypeTiers.LesDeux;
            var isSupplier = t.Type is TypeTiers.Fournisseur or TypeTiers.LesDeux;
            ShowLedger = _returnScope switch
            {
                TiersListScope.Clients => isClient,
                TiersListScope.Fournisseurs => isSupplier,
                _ => false
            };

            if (ShowLedger)
                await LoadLedgerAsync(id, cancellationToken);
            else
            {
                LedgerRows.Clear();
                _ledgerSource.Clear();
                SoldeActuelText = string.Empty;
                PeriodSoldeText = string.Empty;
                ShowLedgerEmpty = false;
            }

            UpdateShowBulkPay();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetBulkPayFields()
    {
        BulkPayAmount = 0;
        BulkPayMode = ModePaiement.Especes;
        BulkPayDate = DateTime.Today;
        BulkPayReference = string.Empty;
    }

    private async Task LoadLedgerAsync(int tiersId, CancellationToken cancellationToken)
    {
        var statement = _returnScope == TiersListScope.Fournisseurs
            ? await _supplierLedgerService.GetStatementAsync(tiersId, cancellationToken)
            : await _clientLedgerService.GetStatementAsync(tiersId, cancellationToken);
        _ledgerSource.Clear();
        _ledgerSource.AddRange(statement.Rows);
        SoldeActuelText = FormatAmount(statement.SoldeActuel);
        ApplyLedgerFilter();
    }

    partial void OnLedgerFromChanged(DateTime? value)
    {
        if (_suppressLedgerRange) return;
        SetLedgerPreset(LedgerRangeKind.Custom);
        ApplyLedgerFilter();
    }

    partial void OnLedgerToChanged(DateTime? value)
    {
        if (_suppressLedgerRange) return;
        SetLedgerPreset(LedgerRangeKind.Custom);
        ApplyLedgerFilter();
    }

    [RelayCommand]
    private void SetLedgerAll() => ApplyPreset(LedgerRangeKind.All);

    [RelayCommand]
    private void SetLedgerToday() => ApplyPreset(LedgerRangeKind.Today);

    [RelayCommand]
    private void SetLedgerWeek() => ApplyPreset(LedgerRangeKind.Week);

    [RelayCommand]
    private void SetLedgerMonth() => ApplyPreset(LedgerRangeKind.Month);

    [RelayCommand]
    private void SetLedgerYear() => ApplyPreset(LedgerRangeKind.Year);

    private void ApplyPreset(LedgerRangeKind kind)
    {
        var today = DateTime.Today;
        SetLedgerPreset(kind);
        switch (kind)
        {
            case LedgerRangeKind.Today:
                SetLedgerDates(today, today);
                break;
            case LedgerRangeKind.Week:
                var weekStart = StartOfWeek(today);
                SetLedgerDates(weekStart, weekStart.AddDays(6));
                break;
            case LedgerRangeKind.Month:
                SetLedgerDates(new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)));
                break;
            case LedgerRangeKind.Year:
                SetLedgerDates(new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31));
                break;
            default:
                SetLedgerDates(null, null);
                break;
        }

        ApplyLedgerFilter();
    }

    private void SetLedgerDates(DateTime? from, DateTime? to)
    {
        _suppressLedgerRange = true;
        LedgerFrom = from;
        LedgerTo = to;
        _suppressLedgerRange = false;
    }

    private void SetLedgerPreset(LedgerRangeKind kind)
    {
        IsLedgerRangeAll = kind == LedgerRangeKind.All;
        IsLedgerRangeToday = kind == LedgerRangeKind.Today;
        IsLedgerRangeWeek = kind == LedgerRangeKind.Week;
        IsLedgerRangeMonth = kind == LedgerRangeKind.Month;
        IsLedgerRangeYear = kind == LedgerRangeKind.Year;
    }

    private void ApplyLedgerFilter()
    {
        var from = LedgerFrom?.Date;
        var to = LedgerTo?.Date;
        if (from.HasValue && to.HasValue && from > to)
            (from, to) = (to, from);

        LedgerRows.Clear();
        foreach (var row in _ledgerSource)
        {
            if (!IsInLedgerRange(row.Date, from, to))
                continue;

            LedgerRows.Add(new ClientLedgerDisplayRow
            {
                DateText = row.IsAllocationDetail ? string.Empty : row.Date.ToString("dd/MM/yyyy"),
                Designation = row.IsAllocationDetail ? "    " + row.Designation : row.Designation,
                Observation = row.IsAllocationDetail ? FormatAmount(row.AllocationAmount) : row.Observation,
                DebitText = row.Debit > 0 ? FormatAmount(row.Debit) : string.Empty,
                CreditText = row.Credit > 0 ? FormatAmount(row.Credit) : string.Empty,
                BalanceText = row.IsAllocationDetail ? string.Empty : FormatAmount(row.Balance)
            });
        }

        PeriodSoldeText = FormatAmount(ClosingBalanceForRange(from, to));
        RefreshPeriodLabel();
        ShowLedgerEmpty = LedgerRows.Count == 0;
    }

    private decimal ClosingBalanceForRange(DateTime? from, DateTime? to)
    {
        if (from == null && to == null)
            return _ledgerSource.Count == 0 ? 0 : _ledgerSource[^1].Balance;

        var end = to ?? DateTime.MaxValue.Date;
        ClientAccountStatementRow? last = null;
        foreach (var row in _ledgerSource)
        {
            if (row.IsAllocationDetail || row.Date.Date > end)
                continue;
            last = row;
        }

        return last?.Balance ?? 0;
    }

    private void RefreshPeriodLabel()
    {
        if (IsLedgerRangeToday)
        {
            LblLedgerPeriod = _locale.T("ClientLedger_PeriodToday");
            return;
        }

        if (IsLedgerRangeWeek)
        {
            LblLedgerPeriod = _locale.T("ClientLedger_PeriodWeek");
            return;
        }

        if (IsLedgerRangeMonth)
        {
            LblLedgerPeriod = _locale.T("ClientLedger_PeriodMonth");
            return;
        }

        if (IsLedgerRangeYear)
        {
            LblLedgerPeriod = _locale.T("ClientLedger_PeriodYear");
            return;
        }

        if (IsLedgerRangeAll || (LedgerFrom == null && LedgerTo == null))
        {
            LblLedgerPeriod = _locale.T("ClientLedger_PeriodSoldeAll");
            return;
        }

        var from = LedgerFrom?.Date;
        var to = LedgerTo?.Date;
        if (from.HasValue && to.HasValue && from > to)
            (from, to) = (to, from);

        var fromText = (from ?? to)!.Value.ToString("dd/MM/yyyy");
        var toText = (to ?? from)!.Value.ToString("dd/MM/yyyy");
        LblLedgerPeriod = _locale.Tf("ClientLedger_PeriodSolde", fromText, toText);
    }

    private static bool IsInLedgerRange(DateTime date, DateTime? from, DateTime? to)
    {
        var day = date.Date;
        if (from.HasValue && day < from.Value) return false;
        if (to.HasValue && day > to.Value) return false;
        return true;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private List<ClientAccountStatementRow> LedgerRowsInRange()
    {
        var from = LedgerFrom?.Date;
        var to = LedgerTo?.Date;
        if (from.HasValue && to.HasValue && from > to)
            (from, to) = (to, from);
        return _ledgerSource.Where(r => IsInLedgerRange(r.Date, from, to)).ToList();
    }

    private string FormatAmount(decimal amount) => CurrencyHelper.Format(amount, _devise);

    [RelayCommand]
    private async Task BulkPayAsync(CancellationToken cancellationToken)
    {
        if (TiersId is not { } tiersId || !ShowBulkPay) return;

        var isSupplier = _returnScope == TiersListScope.Fournisseurs;
        var titleKey = isSupplier ? "SupplierLedger_BulkPayTitle" : "ClientLedger_BulkPayTitle";

        var amount = Math.Round(BulkPayAmount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
        {
            await _dialog.ShowErrorAsync(
                _locale.T(titleKey),
                _locale.T(isSupplier ? "SupplierLedger_BulkPayErrAmount" : "ClientLedger_BulkPayErrAmount"),
                cancellationToken);
            return;
        }

        try
        {
            if (isSupplier)
                await BulkPaySupplierAsync(tiersId, amount, titleKey, cancellationToken);
            else
                await BulkPayClientAsync(tiersId, amount, titleKey, cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T(titleKey), ex.Message, cancellationToken);
        }
    }

    private async Task BulkPayClientAsync(int clientId, decimal amount, string titleKey, CancellationToken cancellationToken)
    {
        var openDocs = await _bulkPayment.GetOpenDocumentsAsync(clientId, cancellationToken);
        if (openDocs.Count == 0)
        {
            await _dialog.ShowErrorAsync(
                _locale.T(titleKey),
                _locale.T("ClientLedger_BulkPayErrNone"),
                cancellationToken);
            return;
        }

        var totalRemaining = Math.Round(openDocs.Sum(d => d.Remaining), 2, MidpointRounding.AwayFromZero);
        if (amount > totalRemaining + DocumentTotalsHelper.PaiementTtcTolerance)
        {
            await _dialog.ShowErrorAsync(
                _locale.T(titleKey),
                _locale.Tf("ClientLedger_BulkPayErrOver", FormatAmount(totalRemaining)),
                cancellationToken);
            return;
        }

        BulkPaymentPreview preview;
        try
        {
            preview = await _bulkPayment.PreviewAsync(clientId, amount, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await _dialog.ShowErrorAsync(_locale.T(titleKey), ex.Message, cancellationToken);
            return;
        }

        var confirmMessage = BuildClientBulkPayPreviewMessage(preview);
        var ok = await _dialog.ConfirmAsync(
            _locale.T("ClientLedger_BulkPayPreviewTitle"),
            confirmMessage,
            cancellationToken);
        if (!ok) return;

        IsBusy = true;
        try
        {
            await _bulkPayment.ApplyAsync(new ClientBulkPaymentRequest(
                clientId,
                amount,
                BulkPayDate?.Date ?? DateTime.Today,
                BulkPayMode,
                BulkPayReference), cancellationToken);

            ResetBulkPayFields();
            await LoadLedgerAsync(clientId, cancellationToken);
            await _dialog.ShowInfoAsync(
                _locale.T(titleKey),
                _locale.T("ClientLedger_BulkPayDone"),
                cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BulkPaySupplierAsync(int fournisseurId, decimal amount, string titleKey, CancellationToken cancellationToken)
    {
        var openDocs = await _supplierBulkPayment.GetOpenDocumentsAsync(fournisseurId, cancellationToken);
        if (openDocs.Count == 0)
        {
            await _dialog.ShowErrorAsync(
                _locale.T(titleKey),
                _locale.T("SupplierLedger_BulkPayErrNone"),
                cancellationToken);
            return;
        }

        var totalRemaining = Math.Round(openDocs.Sum(d => d.Remaining), 2, MidpointRounding.AwayFromZero);
        if (amount > totalRemaining + DocumentTotalsHelper.PaiementTtcTolerance)
        {
            await _dialog.ShowErrorAsync(
                _locale.T(titleKey),
                _locale.Tf("SupplierLedger_BulkPayErrOver", FormatAmount(totalRemaining)),
                cancellationToken);
            return;
        }

        SupplierBulkPaymentPreview preview;
        try
        {
            preview = await _supplierBulkPayment.PreviewAsync(fournisseurId, amount, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await _dialog.ShowErrorAsync(_locale.T(titleKey), ex.Message, cancellationToken);
            return;
        }

        var confirmMessage = BuildSupplierBulkPayPreviewMessage(preview);
        var ok = await _dialog.ConfirmAsync(
            _locale.T("SupplierLedger_BulkPayPreviewTitle"),
            confirmMessage,
            cancellationToken);
        if (!ok) return;

        IsBusy = true;
        try
        {
            await _supplierBulkPayment.ApplyAsync(new SupplierBulkPaymentRequest(
                fournisseurId,
                amount,
                BulkPayDate?.Date ?? DateTime.Today,
                BulkPayMode,
                BulkPayReference), cancellationToken);

            ResetBulkPayFields();
            await LoadLedgerAsync(fournisseurId, cancellationToken);
            await _dialog.ShowInfoAsync(
                _locale.T(titleKey),
                _locale.T("SupplierLedger_BulkPayDone"),
                cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string BuildClientBulkPayPreviewMessage(BulkPaymentPreview preview)
    {
        var sb = new StringBuilder();
        sb.AppendLine(_locale.Tf("ClientLedger_BulkPayPreviewHeader", FormatAmount(preview.RequestedAmount)));

        foreach (var line in preview.Lines)
        {
            sb.AppendLine();
            var designation = _locale.Tf("ClientLedger_BonSortieFmt", line.Numero);
            sb.AppendLine(designation);
            sb.AppendLine(_locale.Tf("ClientLedger_BulkPayPreviewApplied", FormatAmount(line.Amount)));
            if (line.WillBeFullyPaid)
                sb.AppendLine(_locale.T("ClientLedger_BulkPayPreviewStatusPaid"));
            else
                sb.AppendLine(_locale.Tf("ClientLedger_BulkPayPreviewRemaining", FormatAmount(line.RemainingAfter)));
        }

        sb.AppendLine();
        sb.Append(_locale.T("ClientLedger_BulkPayPreviewConfirm"));
        return sb.ToString();
    }

    private string BuildSupplierBulkPayPreviewMessage(SupplierBulkPaymentPreview preview)
    {
        var sb = new StringBuilder();
        sb.AppendLine(_locale.Tf("SupplierLedger_BulkPayPreviewHeader", FormatAmount(preview.RequestedAmount)));

        foreach (var line in preview.Lines)
        {
            sb.AppendLine();
            var designation = _locale.Tf("SupplierLedger_BonAchatFmt", line.Numero);
            sb.AppendLine(designation);
            sb.AppendLine(_locale.Tf("SupplierLedger_BulkPayPreviewApplied", FormatAmount(line.Amount)));
            if (line.WillBeFullyPaid)
                sb.AppendLine(_locale.T("SupplierLedger_BulkPayPreviewStatusPaid"));
            else
                sb.AppendLine(_locale.Tf("SupplierLedger_BulkPayPreviewRemaining", FormatAmount(line.RemainingAfter)));
        }

        sb.AppendLine();
        sb.Append(_locale.T("SupplierLedger_BulkPayPreviewConfirm"));
        return sb.ToString();
    }

    [RelayCommand]
    private async Task ExportLedgerPdfAsync(CancellationToken cancellationToken)
    {
        if (TiersId is not { } id || !ShowLedger) return;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var tiers = await db.Tiers.AsNoTracking().FirstAsync(t => t.Id == id, cancellationToken);
            if (_ledgerSource.Count == 0)
                await LoadLedgerAsync(id, cancellationToken);
            var from = LedgerFrom?.Date;
            var to = LedgerTo?.Date;
            if (from.HasValue && to.HasValue && from > to)
                (from, to) = (to, from);
            var statement = new ClientAccountStatementResult
            {
                Rows = LedgerRowsInRange(),
                SoldeActuel = ClosingBalanceForRange(from, to)
            };
            var bytes = _returnScope == TiersListScope.Fournisseurs
                ? await _pdf.BuildSupplierAccountStatementPdfAsync(
                    tiers, statement, DocumentPartyPdfInfo.FromTiers(tiers), cancellationToken)
                : await _pdf.BuildClientAccountStatementPdfAsync(
                    tiers, statement, DocumentPartyPdfInfo.FromTiers(tiers), cancellationToken);
            var fileName = $"Etat-{tiers.Nom}.pdf";
            var ok = await _dialog.SavePickedFileBytesAsync(
                _locale.T("Export_PdfPicker"), fileName, new[] { "*.pdf" }, bytes, cancellationToken);
            if (ok)
            {
                var title = _returnScope == TiersListScope.Fournisseurs
                    ? _locale.T("SupplierLedger_Title")
                    : _locale.T("ClientLedger_Title");
                await _dialog.ShowInfoAsync(title, _locale.T("Export_Done"), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var title = _returnScope == TiersListScope.Fournisseurs
                ? _locale.T("SupplierLedger_Title")
                : _locale.T("ClientLedger_Title");
            await _dialog.ShowErrorAsync(title, ex.Message, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Nom))
        {
            await _dialog.ShowErrorAsync(_locale.T("Dlg_Validation"), _locale.T("Tiers_ErrName"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (TiersId == null)
            {
                var t = new Models.Tiers
                {
                    Type = Type,
                    Categorie = CategorieTiers.Officiel,
                    Nom = Nom.Trim(),
                    ICE = Ice.Trim(),
                    Adresse = Adresse.Trim(),
                    Ville = Ville.Trim(),
                    Telephone = Telephone.Trim(),
                    Email = Email.Trim(),
                    ConditionsPaiement = ConditionsPaiement.Trim(),
                    Actif = Actif
                };
                db.Tiers.Add(t);
                await db.SaveChangesAsync(cancellationToken);
                TiersId = t.Id;
            }
            else
            {
                var t = await db.Tiers.FirstAsync(x => x.Id == TiersId, cancellationToken);
                t.Type = Type;
                t.Nom = Nom.Trim();
                t.ICE = Ice.Trim();
                t.Adresse = Adresse.Trim();
                t.Ville = Ville.Trim();
                t.Telephone = Telephone.Trim();
                t.Email = Email.Trim();
                t.ConditionsPaiement = ConditionsPaiement.Trim();
                t.Actif = Actif;
                await db.SaveChangesAsync(cancellationToken);
            }

            await _dialog.ShowInfoAsync(_locale.T("Tiers_InfoTitle"), _locale.T("Tiers_Saved"), cancellationToken);
            if (TiersId.HasValue)
                await LoadAsync(TiersId.Value, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<TiersListViewModel>();
        list.Configure(_returnScope);
        _workspace.Open(list);
    }
}
