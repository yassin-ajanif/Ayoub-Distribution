using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.BonRetourFournisseur.Models;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Stock;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.BonRetourFournisseur.ViewModels;

public partial class BonRetourFournisseurEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IUiPreferencesService _uiPreferences;
    private readonly IPdfService _pdf;
    private readonly IPdfPrintService _pdfPrint;
    private readonly IAppSettingsService _settings;
    private readonly IStockMovementService _stock;

    public BonRetourFournisseurEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IUiPreferencesService uiPreferences,
        IPdfService pdf,
        IPdfPrintService pdfPrint,
        IAppSettingsService settings,
        IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _uiPreferences = uiPreferences;
        _pdf = pdf;
        _pdfPrint = pdfPrint;
        _settings = settings;
        _stock = stock;
        _locale.CultureApplied += (_, _) =>
        {
            RefreshUi();
            UpdateTotalLines();
        };
        LineGridColumns.PropertyChanged += OnLineGridColumnsPropertyChanged;
        _uiPreferences.LoadDocumentLineColumns("bonRetourFournisseur", LineGridColumns);
        Title = _locale.T("Brf_Title");
        RefreshUi();
        _ = LoadFournisseursAsync(CancellationToken.None);
    }

    public ObservableCollection<GestionCommerciale.Modules.Tiers.Models.Tiers> Fournisseurs { get; } = [];
    public ObservableCollection<Produit> Produits { get; } = [];
    public ObservableCollection<BonRetourFournisseurLineRow> Lignes { get; } = [];

    [ObservableProperty] private int? _bonRetourFournisseurId;
    [ObservableProperty] private int? _sourceBonRetourId;
    [ObservableProperty] private string _linkedBrtNumero = string.Empty;
    [ObservableProperty] private int _fournisseurId;
    [ObservableProperty] private GestionCommerciale.Modules.Tiers.Models.Tiers? _selectedFournisseur;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private string _motif = string.Empty;
    [ObservableProperty] private bool _retourMarchandise = true;
    [ObservableProperty] private decimal _totalHt;
    [ObservableProperty] private decimal _totalTva;
    [ObservableProperty] private decimal _totalTtc;
    [ObservableProperty] private bool _canEditDraft = true;
    [ObservableProperty] private BonRetourFournisseurLineRow? _selectedLine;
    [ObservableProperty] private string _addLineSearchText = string.Empty;
    [ObservableProperty] private object? _addLineCatalogPick;

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnPdf = string.Empty;
    [ObservableProperty] private string _btnPrint = string.Empty;
    [ObservableProperty] private string _lblFournisseur = string.Empty;
    [ObservableProperty] private string _wmFournisseurSearch = string.Empty;
    [ObservableProperty] private string _lblDate = string.Empty;
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
    public bool HasLinkedBrt => SourceBonRetourId is > 0 && !string.IsNullOrWhiteSpace(LinkedBrtNumero);

    partial void OnSourceBonRetourIdChanged(int? value) => OnPropertyChanged(nameof(HasLinkedBrt));
    partial void OnLinkedBrtNumeroChanged(string value) => OnPropertyChanged(nameof(HasLinkedBrt));

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
        _uiPreferences.SaveDocumentLineColumns("bonRetourFournisseur", LineGridColumns);
    }

    private void RefreshUi()
    {
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        BtnPdf = _locale.T("Btn_Pdf");
        BtnPrint = _locale.T("Btn_Print");
        LblFournisseur = _locale.T("Brf_LblFournisseur");
        WmFournisseurSearch = _locale.T("Wm_SearchClient");
        LblDate = _locale.T("Brf_LblDate");
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
        var lines = Lignes.Select(l => new BonRetourFournisseurLigne
        {
            Quantite = l.Quantite,
            PrixUnitaireHT = l.PrixUnitaireHt,
            Remise = l.Remise,
            TauxTVA = includeTva ? l.TauxTva : 0
        });
        var (ht, tva, ttc) = DocumentTotalsHelper.BonRetourFournisseurTotals(lines);
        TotalHt = ht;
        TotalTva = tva;
        TotalTtc = ttc;
        UpdateTotalLines();
    }

    partial void OnSelectedFournisseurChanged(GestionCommerciale.Modules.Tiers.Models.Tiers? value)
    {
        var id = value?.Id ?? 0;
        if (FournisseurId == id) return;
        FournisseurId = id;
    }

    partial void OnFournisseurIdChanged(int value)
    {
        if (SelectedFournisseur?.Id == value) return;
        SelectedFournisseur = Fournisseurs.FirstOrDefault(f => f.Id == value);
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
            var row = new BonRetourFournisseurLineRow();
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
        if (e.PropertyName is nameof(BonRetourFournisseurLineRow.MontantHt) or nameof(BonRetourFournisseurLineRow.MontantTtc))
            RefreshTotals();
    }

    [RelayCommand]
    private void RemoveLine(BonRetourFournisseurLineRow? line)
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

    private async Task LoadFournisseursAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var list = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Fournisseur || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(ct);
        Fournisseurs.Clear();
        foreach (var f in list) Fournisseurs.Add(f);
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
        foreach (var l in Lignes) l.PropertyChanged -= LineChanged;
        if (id == null)
            _ = LoadNewAsync(CancellationToken.None);
        else
            _ = LoadExistingAsync(id.Value, CancellationToken.None);
    }

    private async Task LoadNewAsync(CancellationToken cancellationToken)
    {
        BonRetourFournisseurId = null;
        SourceBonRetourId = null;
        LinkedBrtNumero = string.Empty;
        FournisseurId = Fournisseurs.FirstOrDefault()?.Id ?? 0;
        Lignes.Clear();
        Numero = _locale.T("Brf_DraftPlaceholder");
        Date = new DateTimeOffset(DateTime.Today);
        Motif = string.Empty;
        RetourMarchandise = true;
        CanEditDraft = true;
        await LoadDeviseAsync(cancellationToken);
        await LoadProduitsAsync(cancellationToken);
        RefreshTotals();
        Title = _locale.T("Brf_NewTitle");
    }

    private async Task LoadExistingAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var doc = await db.Set<Models.BonRetourFournisseur>().Include(x => x.Lignes)
            .FirstAsync(x => x.Id == id, cancellationToken);
        BonRetourFournisseurId = doc.Id;
        SourceBonRetourId = doc.BonRetourId;
        LinkedBrtNumero = doc.BonRetourId is int brtId
            ? await db.BonsRetour.AsNoTracking().Where(b => b.Id == brtId).Select(b => b.Numero).FirstOrDefaultAsync(cancellationToken) ?? string.Empty
            : string.Empty;
        FournisseurId = doc.FournisseurId;
        Numero = doc.Numero;
        Date = new DateTimeOffset(doc.Date);
        Motif = IsAutoDepuisMotif(doc.Motif, LinkedBrtNumero) ? string.Empty : doc.Motif;
        RetourMarchandise = doc.RetourMarchandise;
        Lignes.Clear();
        foreach (var l in doc.Lignes)
        {
            var prod = Produits.FirstOrDefault(p => p.Id == l.ProduitId);
            var row = new BonRetourFournisseurLineRow
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
        Title = _locale.Tf("Brf_TitleNum", Numero);
    }

    public async Task LoadFromBonRetourAsync(int bonRetourId, CancellationToken cancellationToken = default)
    {
        foreach (var l in Lignes) l.PropertyChanged -= LineChanged;
        BonRetourFournisseurId = null;
        SourceBonRetourId = bonRetourId;
        FournisseurId = Fournisseurs.FirstOrDefault()?.Id ?? 0;
        Lignes.Clear();
        Numero = _locale.T("Brf_DraftPlaceholder");
        Date = new DateTimeOffset(DateTime.Today);
        Motif = string.Empty;
        RetourMarchandise = true;
        CanEditDraft = true;

        await LoadDeviseAsync(cancellationToken);
        await LoadProduitsAsync(cancellationToken);
        await LoadFournisseursAsync(cancellationToken);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var brt = await db.BonsRetour.AsNoTracking()
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == bonRetourId, cancellationToken);
        LinkedBrtNumero = brt.Numero;

        foreach (var l in brt.Lignes)
        {
            var prod = Produits.FirstOrDefault(p => p.Id == l.ProduitId);
            var row = new BonRetourFournisseurLineRow();
            if (prod != null)
            {
                row.ApplyCatalogProduct(prod);
            }
            else
            {
                row.ProduitId = l.ProduitId;
                row.Designation = l.Designation;
                row.Conditionnement = l.Conditionnement;
                row.PrixUnitaireHt = l.PrixUnitaireHT;
                row.TauxTva = l.TauxTVA;
            }

            row.Quantite = l.Quantite;
            row.Remise = l.Remise;
            row.PropertyChanged += LineChanged;
            Lignes.Add(row);
        }

        RefreshTotals();
        Title = _locale.T("Brf_FromBrt");
    }

    [RelayCommand]
    private void AddLine()
    {
        var p = Produits.FirstOrDefault();
        var row = new BonRetourFournisseurLineRow();
        if (p != null)
            row.ApplyCatalogProduct(p);
        row.PropertyChanged += LineChanged;
        Lignes.Add(row);
        RefreshTotals();
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!Lignes.Any())
        {
            await _dialog.ShowErrorAsync(_locale.T("Brf_Title"), _locale.T("Brf_ErrLines"), cancellationToken);
            return;
        }

        if (DocumentTotalsHelper.IsEffectivelyZeroTotal(TotalTtc))
        {
            await _dialog.ShowErrorAsync(_locale.T("Brf_Title"), _locale.T("Doc_ErrZeroTtc"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            Models.BonRetourFournisseur entity;
            if (BonRetourFournisseurId == null)
            {
                var num = await _numbers.NextBonRetourFournisseurAsync(cancellationToken);
                entity = new Models.BonRetourFournisseur
                {
                    Numero = num,
                    BonRetourId = SourceBonRetourId,
                    FournisseurId = FournisseurId,
                    Date = Date.DateTime,
                    Motif = Motif,
                    RetourMarchandise = RetourMarchandise,
                    CreatedByUserId = _session.UserId
                };
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new Models.BonRetourFournisseurLigne
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

                db.BonsRetourFournisseurs.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                BonRetourFournisseurId = entity.Id;
                Numero = entity.Numero;
                Title = _locale.Tf("Brf_TitleNum", Numero);
            }
            else
            {
                entity = await db.BonsRetourFournisseurs.Include(x => x.Lignes)
                    .FirstAsync(x => x.Id == BonRetourFournisseurId, cancellationToken);
                entity.FournisseurId = FournisseurId;
                entity.BonRetourId = SourceBonRetourId;
                entity.Date = Date.DateTime;
                entity.Motif = Motif;
                entity.RetourMarchandise = RetourMarchandise;
                db.BonRetourFournisseurLignes.RemoveRange(entity.Lignes);
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new Models.BonRetourFournisseurLigne
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

            await _stock.SyncBonRetourFournisseurStockAsync(
                db,
                entity.Id,
                entity.Numero,
                RetourMarchandise,
                Lignes.Select(l => (l.ProduitId, l.Quantite)),
                _session.UserId,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await _dialog.ShowInfoAsync(_locale.T("Brf_Title"), _locale.T("Brf_Saved"), cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (BonRetourFournisseurId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBonRetourFournisseurPdfBytesAsync(cancellationToken);
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
        if (BonRetourFournisseurId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildBonRetourFournisseurPdfBytesAsync(cancellationToken);
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

    private async Task<byte[]?> BuildBonRetourFournisseurPdfBytesAsync(CancellationToken cancellationToken)
    {
        if (BonRetourFournisseurId is not { } id) return null;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var a = await db.BonsRetourFournisseurs.Include(x => x.Lignes).FirstAsync(x => x.Id == id, cancellationToken);
        var fournisseur = await db.Tiers.AsNoTracking().FirstAsync(t => t.Id == a.FournisseurId, cancellationToken);
        return await _pdf.BuildBonRetourFournisseurPdfAsync(a, DocumentPartyPdfInfo.FromTiers(fournisseur), cancellationToken);
    }

    [RelayCommand]
    private void OpenLinkedBrt()
    {
        if (SourceBonRetourId is not int id) return;
        var vm = _sp.GetRequiredService<BonRetourEditViewModel>();
        vm.LoadExisting(id);
        _workspace.Open(vm);
    }

    private static bool IsAutoDepuisMotif(string motif, string brtNumero)
    {
        if (string.IsNullOrWhiteSpace(motif) || string.IsNullOrWhiteSpace(brtNumero)) return false;
        var trimmed = motif.Trim();
        return trimmed.Equals($"Depuis {brtNumero}", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals($"من {brtNumero}", StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void Back()
    {
        var vm = _sp.GetRequiredService<BonRetourFournisseurListViewModel>();
        _workspace.Open(vm);
    }
}
