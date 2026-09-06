using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Stock;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Achat.Models;
using GestionCommerciale.Modules.Achat.Services;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Achat.ViewModels;

public partial class BonAchatEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IAppSettingsService _settings;
    private readonly IBonAchatWorkflowService _factureWorkflow;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IUiPreferencesService _uiPreferences;
    private readonly IPdfService _pdf;
    private readonly IPdfPrintService _pdfPrint;
    private readonly IStockMovementService _stock;

    public BonAchatEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IAppSettingsService settings,
        IBonAchatWorkflowService factureWorkflow,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IUiPreferencesService uiPreferences,
        IPdfService pdf,
        IPdfPrintService pdfPrint,
        IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _settings = settings;
        _factureWorkflow = factureWorkflow;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _uiPreferences = uiPreferences;
        _pdf = pdf;
        _pdfPrint = pdfPrint;
        _stock = stock;
        _locale.CultureApplied += (_, _) =>
        {
            RefreshBonAchatUi();
            UpdateBonAchatTotalLines();
        };
        LineGridColumns.PropertyChanged += OnLineGridColumnsPropertyChanged;
        _uiPreferences.LoadDocumentLineColumns("bon_achat", LineGridColumns);
        Title = _locale.T("Ba_Title");
        RefreshBonAchatUi();
        FournisseurLookup.BindSelection(() => FournisseurId, c => SelectedFournisseur = c);
    }

    public ClientCategoryFilter FournisseurLookup { get; } = new();
    public ObservableCollection<GestionCommerciale.Modules.Tiers.Models.Tiers> Fournisseurs => FournisseurLookup.Clients;
    public ObservableCollection<GestionCommerciale.Modules.Stock.Models.Produit> Produits { get; } = [];
    public ObservableCollection<BonAchatLineRow> Lignes { get; } = [];
    public ObservableCollection<BonAchatPaiementRowViewModel> Paiements { get; } = [];

    [ObservableProperty] private int? _bonAchatId;
    [ObservableProperty] private int _fournisseurId;
    [ObservableProperty] private GestionCommerciale.Modules.Tiers.Models.Tiers? _selectedFournisseur;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private DateTimeOffset _dateEcheance = new(DateTime.Today.AddDays(30));
    [ObservableProperty] private bool _estPayee;
    [ObservableProperty] private decimal _remiseGlobale;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private decimal _totalHt;
    [ObservableProperty] private decimal _totalTva;
    [ObservableProperty] private decimal _totalTtc;
    [ObservableProperty] private decimal _montantPaye;
    [ObservableProperty] private bool _canEditDraft;

    [ObservableProperty] private decimal _paiementMontant;
    [ObservableProperty] private DateTimeOffset _paiementDate = new(DateTime.Today);
    [ObservableProperty] private ModePaiement _paiementMode = ModePaiement.Especes;
    [ObservableProperty] private string _paiementReference = string.Empty;
    [ObservableProperty] private BonAchatLineRow? _selectedLine;
    [ObservableProperty] private string _addLineSearchText = string.Empty;
    [ObservableProperty] private object? _addLineCatalogPick;

    [ObservableProperty] private string _btnPdf = string.Empty;
    [ObservableProperty] private string _btnPrint = string.Empty;
    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _menuDeleteBonAchat = string.Empty;
    [ObservableProperty] private string _lblFactPayee = string.Empty;
    [ObservableProperty] private string _lblPaid = string.Empty;
    [ObservableProperty] private string _lblUnpaid = string.Empty;
    [ObservableProperty] private string _lblFournisseur = string.Empty;
    [ObservableProperty] private string _wmFournisseurSearch = string.Empty;
    [ObservableProperty] private string _lblDateFacture = string.Empty;
    [ObservableProperty] private string _lblDateEcheance = string.Empty;
    [ObservableProperty] private string _btnRemoveLine = string.Empty;
    [ObservableProperty] private string _lblCatalogHintFacture = string.Empty;
    [ObservableProperty] private string _lblTotals = string.Empty;
    [ObservableProperty] private string _devise = string.Empty;
    [ObservableProperty] private string _totalHtLabel = string.Empty;
    [ObservableProperty] private string _totalTvaLabel = string.Empty;
    [ObservableProperty] private string _totalTtcLabel = string.Empty;
    [ObservableProperty] private string _montantPayeLine = string.Empty;
    [ObservableProperty] private string _lblPaymentsRecorded = string.Empty;
    [ObservableProperty] private string _lblMontant = string.Empty;
    [ObservableProperty] private string _lblPaymentDate = string.Empty;
    [ObservableProperty] private string _lblMode = string.Empty;
    [ObservableProperty] private string _lblReference = string.Empty;
    [ObservableProperty] private string _wmRefShort = string.Empty;
    [ObservableProperty] private string _lblNewPayment = string.Empty;
    [ObservableProperty] private string _btnAddPayment = string.Empty;
    [ObservableProperty] private string _btnDelete = string.Empty;
    [ObservableProperty] private string _btnCancel = string.Empty;
    [ObservableProperty] private string _payEditTooltip = string.Empty;
    [ObservableProperty] private string _lblDocLineColumnsHint = string.Empty;
    [ObservableProperty] private string _lblDocColRef = string.Empty;
    [ObservableProperty] private string _lblDocColDesignation = string.Empty;
    [ObservableProperty] private string _lblDocColQte = string.Empty;
    [ObservableProperty] private string _lblDocColCond = string.Empty;
    [ObservableProperty] private string _wmDocLineUnite = string.Empty;
    [ObservableProperty] private string _lblDocColPuHt = string.Empty;
    [ObservableProperty] private string _lblDocColRemise = string.Empty;
    [ObservableProperty] private string _lblDocColTva = string.Empty;
    [ObservableProperty] private string _lblDocColMontantHt = string.Empty;
    [ObservableProperty] private string _lblDocColMontantTtc = string.Empty;

    public DocumentLineGridColumnState LineGridColumns { get; } = new();
    public bool ShowTotalTva => LineGridColumns.ShowTva && LineGridColumns.ShowMontantTtc;
    public bool ShowTotalTtc => LineGridColumns.ShowMontantTtc && LineGridColumns.ShowTva;
    public bool HighlightHtTotal => !ShowTotalTtc;

    public AutoCompleteFilterPredicate<object?> ProduitAutocompleteFilter => ProductAutoComplete.ItemFilter;
    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;

    private bool _suppressAddLinePick;

    private void OnLineGridColumnsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentLineGridColumnState.ShowTva) or nameof(DocumentLineGridColumnState.ShowMontantTtc))
        {
            OnPropertyChanged(nameof(ShowTotalTva));
            OnPropertyChanged(nameof(ShowTotalTtc));
            OnPropertyChanged(nameof(HighlightHtTotal));
            RefreshTotals();
        }
        _uiPreferences.SaveDocumentLineColumns("bon_achat", LineGridColumns);
    }

    private void RefreshBonAchatUi()
    {
        BtnPdf = _locale.T("Btn_Pdf");
        BtnPrint = _locale.T("Btn_Print");
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        MenuDeleteBonAchat = _locale.T("Ba_MenuDelete");
        LblFournisseur = _locale.T("Lbl_Supplier");
        WmFournisseurSearch = _locale.T("Wm_SearchSupplier");
        LblDateFacture = _locale.T("Lbl_DateFacture");
        LblDateEcheance = _locale.T("Lbl_DateEcheance");
        BtnRemoveLine = _locale.T("Btn_RemoveLine");
        LblCatalogHintFacture = _locale.T("Lbl_CatalogHintFacture");
        LblTotals = _locale.T("Lbl_Totals");
        LblPaymentsRecorded = _locale.T("Lbl_PaymentsRecorded");
        LblMontant = _locale.T("Lbl_Montant");
        LblPaymentDate = _locale.T("Lbl_PaymentDate");
        LblMode = _locale.T("Lbl_Mode");
        LblReference = _locale.T("Lbl_Reference");
        WmRefShort = _locale.T("Lbl_RefShort");
        LblNewPayment = _locale.T("Lbl_NewPayment");
        BtnAddPayment = _locale.T("Btn_AddPayment");
        BtnDelete = _locale.T("Btn_Delete");
        BtnCancel = _locale.T("Btn_Cancel");
        PayEditTooltip = _locale.T("Pay_EditTooltip");
        LblFactPayee = _locale.T("Ba_LblPayee");
        LblPaid = _locale.T("Ba_Paid");
        LblUnpaid = _locale.T("Ba_Unpaid");
        LblDocLineColumnsHint = _locale.T("DocLine_ColumnsHint");
        LblDocColRef = _locale.T("DocLine_ColRef");
        LblDocColDesignation = _locale.T("DocLine_ColDesignation");
        LblDocColQte = _locale.T("DocLine_ColQte");
        LblDocColCond = _locale.T("DocLine_ColCond");
        WmDocLineUnite = _locale.T("DocLine_WmUnite");
        LblDocColPuHt = _locale.T("DocLine_ColPuHt");
        LblDocColRemise = _locale.T("DocLine_ColRemise");
        LblDocColTva = _locale.T("DocLine_ColTva");
        LblDocColMontantHt = _locale.T("DocLine_ColMontantHt");
        LblDocColMontantTtc = _locale.T("DocLine_ColMontantTtc");
    }

    private void UpdateBonAchatTotalLines()
    {
        TotalHtLabel = _locale.Tf("Doc_FmtHt", TotalHt, Devise).TrimEnd();
        TotalTvaLabel = _locale.Tf("Doc_FmtTva", TotalTva, Devise).TrimEnd();
        TotalTtcLabel = _locale.Tf("Doc_FmtTtc", TotalTtc, Devise).TrimEnd();
        MontantPayeLine = _locale.Tf("Doc_FmtPaye", MontantPaye);
    }

    partial void OnDeviseChanged(string value) => UpdateBonAchatTotalLines();

    public Array ModesPaiement => Enum.GetValues(typeof(ModePaiement));

    private bool CanExecuteAddPaiement() => BonAchatId.HasValue;

    partial void OnMontantPayeChanged(decimal value) => UpdateBonAchatTotalLines();

    partial void OnBonAchatIdChanged(int? value)
    {
        AddPaiementCommand.NotifyCanExecuteChanged();
        RemoveBonAchatCommand.NotifyCanExecuteChanged();
    }

    private bool CanRemoveBonAchat() => BonAchatId != null;

    [RelayCommand(CanExecute = nameof(CanRemoveBonAchat))]
    private async Task RemoveBonAchatAsync(CancellationToken cancellationToken)
    {
        if (BonAchatId is not { } id) return;

        if (!await _dialog.ConfirmAsync(_locale.T("Ba_Title"), _locale.Tf("Ba_ConfirmDelete", Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.BonsAchat.Include(f => f.Lignes).Include(f => f.Paiements).FirstAsync(f => f.Id == id, cancellationToken);
            await _stock.ResyncBonAchatStockAsync(
                db, entity.Id, entity.Numero, Enumerable.Empty<(int ProduitId, decimal Quantite)>(), _session.UserId, cancellationToken);
            db.BonsAchat.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);

            await _dialog.ShowInfoAsync(_locale.T("Ba_Title"), _locale.T("Ba_Deleted"), cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Ba_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReloadPaiementsList(IEnumerable<PaiementBonAchat> paiements)
    {
        Paiements.Clear();
        foreach (var p in paiements.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id))
            Paiements.Add(new BonAchatPaiementRowViewModel(this, p));
    }

    public async Task CommitPaiementRowAsync(BonAchatPaiementRowViewModel row, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        if (BonAchatId == null || row.Montant <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), _locale.T("Pay_ErrAmount"), cancellationToken);
            return;
        }

        try
        {
            IsBusy = true;
            await _factureWorkflow.UpdatePaiementAsync(
                BonAchatId.Value,
                row.Id,
                row.Montant,
                row.Date.DateTime,
                row.Mode,
                row.Reference,
                cancellationToken);
            await LoadAsync(BonAchatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeletePaiementRowAsync(BonAchatPaiementRowViewModel row, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        if (BonAchatId == null) return;
        if (!await _dialog.ConfirmAsync(_locale.T("Pay_Title"), _locale.T("Pay_ConfirmDelete"), cancellationToken))
            return;

        try
        {
            IsBusy = true;
            await _factureWorkflow.DeletePaiementAsync(BonAchatId.Value, row.Id, cancellationToken);
            await LoadAsync(BonAchatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void HookLines()
    {
        foreach (var row in Lignes)
            row.PropertyChanged += LineChanged;
    }

    private void LineChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshTotals();
        if (e.PropertyName == nameof(BonAchatLineRow.ProduitId) && sender is BonAchatLineRow changed && changed.ProduitId != 0)
            ConsolidateDuplicateProductLines();
    }

    partial void OnAddLineCatalogPickChanged(object? value)
    {
        if (_suppressAddLinePick) return;
        if (value is not GestionCommerciale.Modules.Stock.Models.Produit p) return;
        _suppressAddLinePick = true;
        var existing = Lignes.FirstOrDefault(l => l.ProduitId == p.Id && p.Id != 0);
        if (existing != null)
        {
            existing.Quantite += 1;
            SelectedLine = existing;
        }
        else
        {
            var row = new BonAchatLineRow();
            row.ApplyCatalogProduct(p);
            row.Quantite = 1;
            row.PropertyChanged += LineChanged;
            Lignes.Add(row);
            SelectedLine = row;
        }
        DocumentLineSearchHelper.ClearAfterCatalogPick(() =>
        {
            _suppressAddLinePick = true;
            AddLineCatalogPick = null;
            AddLineSearchText = string.Empty;
            _suppressAddLinePick = false;
        });
        RefreshTotals();
    }

    private void ConsolidateDuplicateProductLines()
    {
        foreach (var g in Lignes.Where(l => l.ProduitId != 0).GroupBy(l => l.ProduitId).ToList())
        {
            if (g.Count() < 2) continue;
            var ordered = g.OrderBy(l => Lignes.IndexOf(l)).ToList();
            var keep = ordered[0];
            var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
            foreach (var line in ordered.Skip(1))
            {
                if (ReferenceEquals(SelectedLine, line))
                    SelectedLine = keep;
                line.PropertyChanged -= LineChanged;
                Lignes.Remove(line);
            }
            keep.Quantite += extraQty;
        }
    }

    private void RefreshTotals()
    {
        var includeTvaInTotals = ShowTotalTtc;
        var lines = Lignes.Select(l => new BonAchatLigne
        {
            Quantite = l.Quantite,
            PrixUnitaireHT = l.PrixUnitaireHt,
            Remise = l.Remise,
            TauxTVA = includeTvaInTotals ? l.TauxTva : 0
        });
        var (ht, tva, ttc) = DocumentTotalsHelper.BonAchatTotals(lines, RemiseGlobale);
        TotalHt = ht;
        TotalTva = tva;
        TotalTtc = ttc;
        UpdateBonAchatTotalLines();
        RefreshSuggestedPaiementMontant();
    }

    private void RefreshSuggestedPaiementMontant()
    {
        if (!BonAchatId.HasValue) return;
        var fullTtc = ComputeFullPaymentTtc();
        PaiementMontant = Math.Round(Math.Max(0, fullTtc - MontantPaye), 2);
    }

    private decimal ComputeFullPaymentTtc() =>
        DocumentTotalsHelper.BonAchatTtc(
            Lignes.Select(l => new BonAchatLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHt,
                Remise = l.Remise,
                TauxTVA = l.TauxTva
            }),
            RemiseGlobale);

    private async Task<bool> ValidatePaymentsAgainstTtcAsync(decimal ttc, decimal totalPayments, CancellationToken cancellationToken)
    {
        if (!DocumentTotalsHelper.PaymentsExceedTtc(ttc, totalPayments))
            return true;

        await _dialog.ShowErrorAsync(
            _locale.T("Pay_Title"),
            _locale.Tf("Pay_ErrPaymentsExceedTtc", totalPayments, ttc),
            cancellationToken);
        return false;
    }

    partial void OnRemiseGlobaleChanged(decimal value) => RefreshTotals();

    partial void OnSelectedFournisseurChanged(GestionCommerciale.Modules.Tiers.Models.Tiers? value)
    {
        var id = value?.Id ?? 0;
        if (FournisseurId == id) return;
        FournisseurId = id;
    }

    partial void OnFournisseurIdChanged(int value)
    {
        if (SelectedFournisseur?.Id == value) return;
        FournisseurLookup.EnsureCategoryFor(value);
        SelectedFournisseur = Fournisseurs.FirstOrDefault(c => c.Id == value);
    }

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        BonAchatId = id;
        var cfg = await _settings.GetAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);
        Lignes.Clear();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await LoadLookupsAsync(db, cancellationToken);

        if (id == null)
        {
            Numero = _locale.T("Ba_NewNumPlaceholder");
            FournisseurId = Fournisseurs.FirstOrDefault()?.Id ?? 0;
            Date = new DateTimeOffset(DateTime.Today);
            DateEcheance = Date.AddDays(30);
            EstPayee = false;
            CanEditDraft = true;
            Title = _locale.T("Ba_NewTitle");
            MontantPaye = 0;
            Paiements.Clear();
            RefreshTotals();
            return;
        }

        var f = await db.BonsAchat.Include(x => x.Lignes).Include(x => x.Paiements).FirstAsync(x => x.Id == id, cancellationToken);
        Numero = f.Numero;
        FournisseurId = f.FournisseurId;
        Date = new DateTimeOffset(f.Date);
        DateEcheance = new DateTimeOffset(f.DateEcheance);
        EstPayee = f.EstPayee;
        RemiseGlobale = f.RemiseGlobale;
        Note = f.Note;
        foreach (var l in f.Lignes)
        {
            var prod = Produits.FirstOrDefault(p => p.Id == l.ProduitId);
            var row = new BonAchatLineRow
            {
                ProduitId = l.ProduitId,
                Reference = prod?.Reference ?? string.Empty,
                Designation = l.Designation,
                Conditionnement = l.Conditionnement,
                Quantite = l.Quantite,
                PrixUnitaireHt = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTva = l.TauxTVA
            };
            Lignes.Add(row);
        }

        HookLines();
        MontantPaye = f.Paiements.Sum(p => p.Montant);
        ReloadPaiementsList(f.Paiements);
        DocumentTotalsHelper.SyncBonAchatTotalTtc(f);
        if (db.Entry(f).Property(x => x.TotalTtc).IsModified)
            await db.SaveChangesAsync(cancellationToken);
        CanEditDraft = true;
        Title = _locale.Tf("Ba_TitleNum", Numero);
        RefreshTotals();
    }

    private async Task LoadLookupsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Fournisseur || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(cancellationToken);
        FournisseurLookup.ReplaceAll(clients);

        var produits = await db.Produits.AsNoTracking().Where(p => p.Actif)
            .SelectForListWithoutImageData().ToListAsync(cancellationToken);
        Produits.Clear();
        foreach (var p in produits) Produits.Add(p);
    }

    public void Load(int? id) => _ = LoadAsync(id, CancellationToken.None);

    [RelayCommand]
    private void AddLine()
    {
        var p = Produits.FirstOrDefault();
        var row = new BonAchatLineRow
        {
            ProduitId = p?.Id ?? 0,
            Reference = p?.Reference ?? string.Empty,
            Designation = p?.Designation ?? string.Empty,
            Conditionnement = p?.Unite ?? string.Empty,
            Quantite = 1,
            PrixUnitaireHt = p?.PrixVenteHT ?? 0,
            Remise = 0,
            TauxTva = p?.TauxTVA ?? 20
        };
        row.PropertyChanged += LineChanged;
        Lignes.Add(row);
        RefreshTotals();
    }

    [RelayCommand]
    private void RemoveLine(BonAchatLineRow? row)
    {
        if (row == null) return;
        row.PropertyChanged -= LineChanged;
        Lignes.Remove(row);
        RefreshTotals();
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedLine == null) return;
        RemoveLine(SelectedLine);
        SelectedLine = null;
    }

    [RelayCommand]
    private async Task SaveDraftAsync(CancellationToken cancellationToken)
    {
        if (FournisseurId == 0 || !Lignes.Any())
        {
            await _dialog.ShowErrorAsync(_locale.T("Ba_Title"), _locale.T("Ba_ErrFournisseurLines"), cancellationToken);
            return;
        }

        if (DocumentTotalsHelper.IsEffectivelyZeroTotal(ComputeFullPaymentTtc()))
        {
            await _dialog.ShowErrorAsync(_locale.T("Ba_Title"), _locale.T("Doc_ErrZeroTtc"), cancellationToken);
            return;
        }

        if (BonAchatId != null)
        {
            await using var checkDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var paid = await checkDb.PaiementsBonAchat.AsNoTracking()
                .Where(p => p.BonAchatId == BonAchatId)
                .SumAsync(p => p.Montant, cancellationToken);
            if (!await ValidatePaymentsAgainstTtcAsync(ComputeFullPaymentTtc(), paid, cancellationToken))
                return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            BonAchat entity;
            if (BonAchatId == null)
            {
                var num = await _numbers.NextBonAchatAsync(cancellationToken);
                entity = new BonAchat
                {
                    Numero = num,
                    FournisseurId = FournisseurId,
                    Date = Date.DateTime,
                    DateEcheance = DateEcheance.DateTime,
                    EstPayee = EstPayee,
                    RemiseGlobale = RemiseGlobale,
                    Note = Note,
                    CreatedByUserId = _session.UserId
                };
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new BonAchatLigne
                    {
                        ProduitId = l.ProduitId,
                        Designation = l.Designation,
                        Conditionnement = l.Conditionnement,
                        Quantite = l.Quantite,
                        PrixUnitaireHT = l.PrixUnitaireHt,
                        Remise = l.Remise,
                        TauxTVA = l.TauxTva
                    });
                }

                DocumentTotalsHelper.SyncBonAchatTotalTtc(entity);
                db.BonsAchat.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                BonAchatId = entity.Id;
            }
            else
            {
                entity = await db.BonsAchat.Include(f => f.Lignes).FirstAsync(f => f.Id == BonAchatId, cancellationToken);

                entity.FournisseurId = FournisseurId;
                entity.Date = Date.DateTime;
                entity.DateEcheance = DateEcheance.DateTime;
                entity.EstPayee = EstPayee;
                entity.RemiseGlobale = RemiseGlobale;
                entity.Note = Note;
                db.BonAchatLignes.RemoveRange(entity.Lignes);
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new BonAchatLigne
                    {
                        ProduitId = l.ProduitId,
                        Designation = l.Designation,
                        Conditionnement = l.Conditionnement,
                        Quantite = l.Quantite,
                        PrixUnitaireHT = l.PrixUnitaireHt,
                        Remise = l.Remise,
                        TauxTVA = l.TauxTva
                    });
                }

                DocumentTotalsHelper.SyncBonAchatTotalTtc(entity);
                await db.SaveChangesAsync(cancellationToken);
            }

            await ResyncStockAsync(db, entity, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            Numero = entity.Numero;
            await _dialog.ShowInfoAsync(_locale.T("Ba_Title"), _locale.T("Ba_Saved"), cancellationToken);
            await LoadAsync(BonAchatId, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAddPaiement))]
    private async Task AddPaiementAsync(CancellationToken cancellationToken)
    {
        if (IsBusy) return;

        if (!BonAchatId.HasValue)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), _locale.T("Pay_ErrSaveFirst"), cancellationToken);
            return;
        }

        if (PaiementMontant <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), _locale.T("Pay_ErrAmount"), cancellationToken);
            return;
        }

        var fullTtc = ComputeFullPaymentTtc();
        if (!await ValidatePaymentsAgainstTtcAsync(fullTtc, MontantPaye + PaiementMontant, cancellationToken))
            return;

        try
        {
            IsBusy = true;
            await _factureWorkflow.AddPaiementAsync(BonAchatId.Value, new PaiementBonAchat
            {
                Montant = PaiementMontant,
                Date = PaiementDate.DateTime,
                Mode = PaiementMode,
                Reference = PaiementReference,
                CreatedByUserId = _session.UserId
            }, cancellationToken);
            PaiementMontant = 0;
            PaiementReference = string.Empty;
            PaiementDate = new DateTimeOffset(DateTime.Today);
            await LoadAsync(BonAchatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<BonAchatListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (BonAchatId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBonAchatPdfBytesAsync(cancellationToken);
            if (bytes == null) return;
            var ok = await _dialog.SavePickedFileBytesAsync(_locale.T("Export_PdfPicker"), $"{Numero}.pdf", new[] { "*.pdf" }, bytes, cancellationToken);
            if (ok)
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Export_Done"), cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Export_Pdf"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PrintAsync(CancellationToken cancellationToken)
    {
        if (BonAchatId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBonAchatPdfBytesAsync(cancellationToken);
            if (bytes == null) return;
            await _pdfPrint.PrintPdfAsync(bytes, Numero, cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Btn_Print"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<byte[]?> BuildBonAchatPdfBytesAsync(CancellationToken cancellationToken)
    {
        if (BonAchatId is not { } id) return null;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.BonsAchat.Include(x => x.Lignes).Include(x => x.Paiements).FirstAsync(x => x.Id == id, cancellationToken);
        var fournisseur = await db.Tiers.AsNoTracking().FirstAsync(t => t.Id == f.FournisseurId, cancellationToken);
        return await _pdf.BuildBonAchatPdfAsync(f, DocumentPartyPdfInfo.FromTiers(fournisseur), cancellationToken);
    }

    private Task ResyncStockAsync(AppDbContext db, BonAchat entity, CancellationToken cancellationToken)
    {
        var lines = entity.Lignes
            .Where(l => l.ProduitId > 0)
            .Select(l => (l.ProduitId, l.Quantite));
        return _stock.ResyncBonAchatStockAsync(
            db, entity.Id, entity.Numero, lines, _session.UserId, cancellationToken);
    }
}
