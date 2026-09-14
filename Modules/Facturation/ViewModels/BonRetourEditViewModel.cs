using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.BonRetourFournisseur.ViewModels;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Stock;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Facturation.ViewModels;

public partial class BonRetourLineRow : ObservableObject
{
    [ObservableProperty] private int _produitId;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private string _conditionnement = string.Empty;
    [ObservableProperty] private decimal _quantite = 1;
    [ObservableProperty] private decimal _prixUnitaireHt;
    [ObservableProperty] private decimal _remise;
    [ObservableProperty] private decimal _tauxTva;

    public decimal MontantHt => DocumentTotalsHelper.LigneHT(Quantite, PrixUnitaireHt, Remise);
    public decimal MontantTtc => MontantHt * (1 + TauxTva / 100m);

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();
    partial void OnRemiseChanged(decimal value) => NotifyMontants();
    partial void OnTauxTvaChanged(decimal value) => NotifyMontants();

    public void ApplyCatalogProduct(Produit p)
    {
        ProduitId = p.Id;
        Reference = p.Reference;
        Designation = p.Designation;
        Conditionnement = p.Unite;
        PrixUnitaireHt = p.PrixVenteHT;
        TauxTva = p.TauxTVA;
        NotifyMontants();
    }

    private void NotifyMontants()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
    }
}

public partial class BonRetourEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IBonRetourWorkflowService _workflow;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IUiPreferencesService _uiPreferences;
    private readonly IPdfService _pdf;
    private readonly IPdfPrintService _pdfPrint;
    private readonly IStockMovementService _stock;
    private readonly IAppSettingsService _settings;

    public BonRetourEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IBonRetourWorkflowService workflow,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IUiPreferencesService uiPreferences,
        IPdfService pdf,
        IPdfPrintService pdfPrint,
        IStockMovementService stock,
        IAppSettingsService settings)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _workflow = workflow;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _uiPreferences = uiPreferences;
        _pdf = pdf;
        _pdfPrint = pdfPrint;
        _stock = stock;
        _settings = settings;
        _locale.CultureApplied += (_, _) =>
        {
            RefreshBonRetourUi();
            UpdateTotalLines();
        };
        LineGridColumns.PropertyChanged += OnLineGridColumnsPropertyChanged;
        _uiPreferences.LoadDocumentLineColumns("bon_retour", LineGridColumns);
        Title = _locale.T("Brt_Title");
        RefreshBonRetourUi();
        ClientLookup.BindSelection(() => ClientId, c => SelectedClient = c);
        _ = LoadClientsAsync(CancellationToken.None);
    }

    public ClientCategoryFilter ClientLookup { get; } = new();
    public ObservableCollection<GestionCommerciale.Modules.Tiers.Models.Tiers> Clients => ClientLookup.Clients;
    public ObservableCollection<Produit> Produits { get; } = [];
    public ObservableCollection<BonRetourLineRow> Lignes { get; } = [];

    [ObservableProperty] private int? _bonRetourId;
    partial void OnBonRetourIdChanged(int? value)
    {
        RemoveBonRetourCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanToBrf));
        ToBrfCommand.NotifyCanExecuteChanged();
    }
    [ObservableProperty] private int? _factureId;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private GestionCommerciale.Modules.Tiers.Models.Tiers? _selectedClient;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private string _motif = string.Empty;
    [ObservableProperty] private bool _retourMarchandise = true;
    [ObservableProperty] private decimal _totalHt;
    [ObservableProperty] private decimal _totalTva;
    [ObservableProperty] private decimal _totalTtc;
    [ObservableProperty] private bool _canEditDraft;
    [ObservableProperty] private BonRetourLineRow? _selectedLine;
    [ObservableProperty] private string _addLineSearchText = string.Empty;
    [ObservableProperty] private object? _addLineCatalogPick;

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnPdf = string.Empty;
    [ObservableProperty] private string _btnPrint = string.Empty;
    [ObservableProperty] private string _btnToBrf = string.Empty;
    [ObservableProperty] private int? _linkedBrfId;
    [ObservableProperty] private string _linkedBrfNumero = string.Empty;
    [ObservableProperty] private string _menuDeleteBonRetour = string.Empty;
    [ObservableProperty] private string _lblClient = string.Empty;
    [ObservableProperty] private string _wmClientSearch = string.Empty;
    [ObservableProperty] private string _lblDateBonRetour = string.Empty;
    [ObservableProperty] private string _btnRemoveLine = string.Empty;
    [ObservableProperty] private string _lblCatalogHint = string.Empty;
    [ObservableProperty] private string _lblTotals = string.Empty;
    [ObservableProperty] private string _devise = string.Empty;
    [ObservableProperty] private string _totalHtLabel = string.Empty;
    [ObservableProperty] private string _totalTvaLabel = string.Empty;
    [ObservableProperty] private string _totalTtcLabel = string.Empty;
    [ObservableProperty] private string _wmMotif = string.Empty;
    [ObservableProperty] private string _chkRetourStock = string.Empty;
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
    public bool CanToBrf => BonRetourId != null;
    public bool HasLinkedBrf => LinkedBrfId is > 0 && !string.IsNullOrWhiteSpace(LinkedBrfNumero);

    partial void OnLinkedBrfIdChanged(int? value) => OnPropertyChanged(nameof(HasLinkedBrf));
    partial void OnLinkedBrfNumeroChanged(string value) => OnPropertyChanged(nameof(HasLinkedBrf));

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
        _uiPreferences.SaveDocumentLineColumns("bon_retour", LineGridColumns);
    }

    private void RefreshBonRetourUi()
    {
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        BtnPdf = _locale.T("Btn_Pdf");
        BtnPrint = _locale.T("Btn_Print");
        BtnToBrf = _locale.T("Btn_ToBrf");
        MenuDeleteBonRetour = _locale.T("Brt_MenuDelete");
        LblClient = _locale.T("Lbl_Client");
        WmClientSearch = _locale.T("Wm_SearchClient");
        LblDateBonRetour = _locale.T("Lbl_DateBonRetour");
        BtnRemoveLine = _locale.T("Btn_RemoveLine");
        LblCatalogHint = _locale.T("Lbl_CatalogHintBonRetour");
        LblTotals = _locale.T("Lbl_Totals");
        WmMotif = _locale.T("Lbl_Motif");
        ChkRetourStock = _locale.T("Lbl_ReturnStock");
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

    private void UpdateTotalLines()
    {
        TotalHtLabel = _locale.Tf("Doc_FmtHt", TotalHt, Devise).TrimEnd();
        TotalTvaLabel = _locale.Tf("Doc_FmtTva", TotalTva, Devise).TrimEnd();
        TotalTtcLabel = _locale.Tf("Doc_FmtTtc", TotalTtc, Devise).TrimEnd();
    }

    partial void OnDeviseChanged(string value) => RefreshTotals();

    private async Task LoadDeviseAsync(CancellationToken cancellationToken)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);
    }

    private void RefreshTotals()
    {
        var includeTva = ShowTotalTtc;
        var lines = Lignes.Select(l => new BonRetourLigne
        {
            Quantite = l.Quantite,
            PrixUnitaireHT = l.PrixUnitaireHt,
            Remise = l.Remise,
            TauxTVA = includeTva ? l.TauxTva : 0
        });
        var (ht, tva, ttc) = DocumentTotalsHelper.BonRetourTotals(lines);
        TotalHt = ht;
        TotalTva = tva;
        TotalTtc = ttc;
        UpdateTotalLines();
    }

    partial void OnSelectedClientChanged(GestionCommerciale.Modules.Tiers.Models.Tiers? value)
    {
        var id = value?.Id ?? 0;
        if (ClientId == id) return;
        ClientId = id;
    }

    partial void OnClientIdChanged(int value)
    {
        if (SelectedClient?.Id == value) return;
        ClientLookup.EnsureCategoryFor(value);
        SelectedClient = Clients.FirstOrDefault(c => c.Id == value);
    }

    partial void OnAddLineCatalogPickChanged(object? value)
    {
        if (_suppressAddLinePick) return;
        if (value is not Produit p) return;

        var existing = Lignes.FirstOrDefault(l => l.ProduitId == p.Id && l.ProduitId != 0);
        if (existing is not null)
        {
            existing.Quantite++;
            SelectedLine = existing;
        }
        else
        {
            var row = new BonRetourLineRow();
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

    private void LineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BonRetourLineRow.MontantHt) or nameof(BonRetourLineRow.MontantTtc))
            RefreshTotals();
    }

    [RelayCommand]
    private void RemoveLine(BonRetourLineRow? line)
    {
        if (line is null) return;
        line.PropertyChanged -= LineChanged;
        Lignes.Remove(line);
        RefreshTotals();
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedLine is null) return;
        RemoveLine(SelectedLine);
    }

    private async Task LoadClientsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(ct);
        ClientLookup.ReplaceAll(clients);
    }

    private async Task LoadProduitsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var produits = await db.Produits.AsNoTracking().Where(p => p.Actif)
            .SelectForListWithoutImageData().ToListAsync(ct);
        Produits.Clear();
        foreach (var p in produits) Produits.Add(p);
    }

    public void Load(int? id)
    {
        if (id == null)
            _ = LoadNewAsync(CancellationToken.None);
        else
            _ = LoadExistingAsync(id.Value, CancellationToken.None);
    }

    private async Task LoadNewAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessBonRetour)
        {
            await _dialog.ShowErrorAsync(_locale.T("Brt_Title"), _locale.T("Brt_ErrDenied"), cancellationToken);
            return;
        }

        foreach (var l in Lignes) l.PropertyChanged -= LineChanged;
        BonRetourId = null;
        FactureId = null;
        ClientId = Clients.FirstOrDefault()?.Id ?? 0;
        Lignes.Clear();
        Numero = _locale.T("Brt_DraftPlaceholder");
        Date = new DateTimeOffset(DateTime.Today);
        Motif = string.Empty;
        RetourMarchandise = true;
        LinkedBrfId = null;
        LinkedBrfNumero = string.Empty;
        CanEditDraft = true;
        await LoadDeviseAsync(cancellationToken);
        await LoadProduitsAsync(cancellationToken);
        RefreshTotals();
        Title = _locale.T("Brt_NewTitle");
    }

    public void LoadExisting(int bonRetourId) => _ = LoadExistingAsync(bonRetourId, CancellationToken.None);

    private async Task LoadExistingAsync(int bonRetourId, CancellationToken cancellationToken)
    {
        if (!_session.CanAccessBonRetour)
        {
            await _dialog.ShowErrorAsync(_locale.T("Brt_Title"), _locale.T("Brt_ErrDenied"), cancellationToken);
            return;
        }

        foreach (var l in Lignes) l.PropertyChanged -= LineChanged;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var avoir = await db.BonsRetour.Include(x => x.Lignes).FirstAsync(x => x.Id == bonRetourId, cancellationToken);
        BonRetourId = avoir.Id;
        FactureId = avoir.FactureId;
        ClientId = avoir.ClientId;
        Numero = avoir.Numero;
        Date = new DateTimeOffset(avoir.Date);
        Motif = avoir.Motif;
        RetourMarchandise = avoir.RetourMarchandise;
        Lignes.Clear();
        foreach (var l in avoir.Lignes)
        {
            var prod = Produits.FirstOrDefault(p => p.Id == l.ProduitId);
            var row = new BonRetourLineRow
            {
                ProduitId = l.ProduitId,
                Reference = prod?.Reference ?? l.Designation,
                Designation = l.Designation,
                Conditionnement = l.Conditionnement,
                Quantite = l.Quantite,
                PrixUnitaireHt = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTva = l.TauxTVA
            };
            row.PropertyChanged += LineChanged;
            Lignes.Add(row);
        }

        CanEditDraft = true;
        await LoadDeviseAsync(cancellationToken);
        await LoadProduitsAsync(cancellationToken);
        RefreshTotals();
        Title = _locale.Tf("Brt_TitleNum", Numero);
        await RefreshLinkedBrfAsync(cancellationToken);
    }

    private async Task RefreshLinkedBrfAsync(CancellationToken cancellationToken)
    {
        if (BonRetourId is not int id)
        {
            LinkedBrfId = null;
            LinkedBrfNumero = string.Empty;
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var linked = await db.BonsRetourFournisseurs.AsNoTracking()
            .Where(b => b.BonRetourId == id)
            .Select(b => new { b.Id, b.Numero })
            .FirstOrDefaultAsync(cancellationToken);
        LinkedBrfId = linked?.Id;
        LinkedBrfNumero = linked?.Numero ?? string.Empty;
    }

    [RelayCommand]
    private void AddLine()
    {
        var p = Produits.FirstOrDefault();
        var row = new BonRetourLineRow();
        if (p != null)
            row.ApplyCatalogProduct(p);
        row.PropertyChanged += LineChanged;
        Lignes.Add(row);
        RefreshTotals();
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessBonRetour) return;
        if (ClientId == 0 || !Lignes.Any())
        {
            await _dialog.ShowErrorAsync(_locale.T("Brt_Title"), _locale.T("Brt_ErrClientLines"), cancellationToken);
            return;
        }

        if (DocumentTotalsHelper.IsEffectivelyZeroTotal(TotalTtc))
        {
            await _dialog.ShowErrorAsync(_locale.T("Brt_Title"), _locale.T("Doc_ErrZeroTtc"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            BonRetour entity;
            if (BonRetourId == null)
            {
                var num = await _numbers.NextBonRetourAsync(cancellationToken);
                entity = new BonRetour
                {
                    Numero = num,
                    FactureId = FactureId,
                    ClientId = ClientId,
                    Date = Date.DateTime,
                    Motif = Motif,
                    RetourMarchandise = RetourMarchandise,
                    CreatedByUserId = _session.UserId
                };
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new BonRetourLigne
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

                db.BonsRetour.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                BonRetourId = entity.Id;
                Numero = entity.Numero;
                Title = _locale.Tf("Brt_TitleNum", Numero);
            }
            else
            {
                entity = await db.BonsRetour.Include(a => a.Lignes).FirstAsync(a => a.Id == BonRetourId, cancellationToken);
                entity.FactureId = FactureId;
                entity.ClientId = ClientId;
                entity.Date = Date.DateTime;
                entity.Motif = Motif;
                entity.RetourMarchandise = RetourMarchandise;
                db.BonRetourLignes.RemoveRange(entity.Lignes);
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new BonRetourLigne
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
            }

            await _stock.SyncBonRetourStockAsync(
                db,
                entity.Id,
                entity.Numero,
                RetourMarchandise,
                Lignes.Select(l => (l.ProduitId, l.Quantite)),
                _session.UserId,
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            CanEditDraft = false;
            await _dialog.ShowInfoAsync(_locale.T("Brt_Title"), _locale.T("Brt_Saved"), cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (BonRetourId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBonRetourPdfBytesAsync(cancellationToken);
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
        if (BonRetourId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBonRetourPdfBytesAsync(cancellationToken);
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

    private async Task<byte[]?> BuildBonRetourPdfBytesAsync(CancellationToken cancellationToken)
    {
        if (BonRetourId is not { } id) return null;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var a = await db.BonsRetour.Include(x => x.Lignes).FirstAsync(x => x.Id == id, cancellationToken);
        var client = await db.Tiers.AsNoTracking().FirstAsync(t => t.Id == a.ClientId, cancellationToken);
        return await _pdf.BuildBonRetourPdfAsync(a, DocumentPartyPdfInfo.FromTiers(client), cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanToBrf))]
    private async Task ToBrfAsync(CancellationToken cancellationToken)
    {
        if (BonRetourId is not { } id) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var existingBrf = await db.BonsRetourFournisseurs.AsNoTracking()
                .Where(b => b.BonRetourId == id)
                .Select(b => new { b.Id, b.Numero })
                .FirstOrDefaultAsync(cancellationToken);
            if (existingBrf != null)
            {
                LinkedBrfId = existingBrf.Id;
                LinkedBrfNumero = existingBrf.Numero;
                OpenLinkedBrf();
                return;
            }

            var vm = _sp.GetRequiredService<BonRetourFournisseurEditViewModel>();
            await vm.LoadFromBonRetourAsync(id, cancellationToken);
            _workspace.Open(vm);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Brt_Title"), ex.Message, cancellationToken);
        }
    }

    [RelayCommand]
    private void OpenLinkedBrf()
    {
        if (LinkedBrfId is not int id) return;
        var vm = _sp.GetRequiredService<BonRetourFournisseurEditViewModel>();
        vm.Load(id);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void Back()
    {
        var vm = _sp.GetRequiredService<BonRetourListViewModel>();
        _workspace.Open(vm);
    }

    private bool CanRemoveBonRetour() => BonRetourId != null;

    [RelayCommand(CanExecute = nameof(CanRemoveBonRetour))]
    private async Task RemoveBonRetourAsync(CancellationToken cancellationToken)
    {
        if (BonRetourId is not { } id) return;

        if (!await _dialog.ConfirmAsync(_locale.T("Brt_Title"), _locale.Tf("Brt_ConfirmDelete", Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var linkedBrf = await db.BonsRetourFournisseurs.AsNoTracking()
                .Where(b => b.BonRetourId == id)
                .Select(b => b.Numero)
                .FirstOrDefaultAsync(cancellationToken);
            if (linkedBrf != null)
            {
                await _dialog.ShowErrorAsync(_locale.T("Brt_Title"), _locale.Tf("Brt_ErrDeleteReferencedBrf", linkedBrf), cancellationToken);
                return;
            }

            var tracked = await db.BonsRetour.Include(a => a.Lignes).FirstAsync(a => a.Id == id, cancellationToken);
            await _stock.SyncBonRetourStockAsync(db, id, tracked.Numero, false, [], null, cancellationToken);
            db.BonsRetour.Remove(tracked);
            await db.SaveChangesAsync(cancellationToken);
            await _dialog.ShowInfoAsync(_locale.T("Brt_Title"), _locale.T("Brt_Deleted"), cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Brt_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
