using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ChargeListViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly IAppSettingsService _settings;
    private readonly ILocaleService _locale;

    public ChargeListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        IAppSettingsService settings,
        ILocaleService locale)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _settings = settings;
        _locale = locale;
        Title = _locale.T("Charges_Title");
        RefreshUi();
        _locale.CultureApplied += (_, _) => RefreshUi();
        Pagination = new PaginationHelper(() => _ = LoadPageAsync(CancellationToken.None));
    }

    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnFilterDate = string.Empty;
    [ObservableProperty] private string _searchWatermark = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;
    [ObservableProperty] private string _colType = string.Empty;
    [ObservableProperty] private string _colDate = string.Empty;
    [ObservableProperty] private string _colLibelle = string.Empty;
    [ObservableProperty] private string _colTtc = string.Empty;
    [ObservableProperty] private string _colNote = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ChargeListRow? _selected;

    private DateTime? _dateFrom, _dateTo;

    public ObservableCollection<ChargeListRow> Rows { get; } = [];
    public PaginationHelper Pagination { get; }

    private void RefreshUi()
    {
        Title = _locale.T("Charges_Title");
        BtnNew = _locale.T("Btn_New");
        BtnFilterDate = _locale.T("Btn_FilterDate");
        SearchWatermark = _locale.T("Charges_SearchWm");
        MenuDelete = _locale.T("Charges_MenuDelete");
        ColType = _locale.T("Charges_ColType");
        ColDate = _locale.T("DevisList_ColDate");
        ColLibelle = _locale.T("Charges_ColLibelle");
        ColTtc = _locale.T("DevisList_ColTtc");
        ColNote = _locale.T("Lbl_Note");
        UpdateBtnFilterDateText();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadPageAsync(CancellationToken.None, true);

    private async Task LoadPageAsync(CancellationToken ct, bool resetPage = false)
    {
        IsBusy = true;
        try
        {
            if (resetPage)
                Pagination.CurrentPage = 1;

            var cfg = await _settings.GetAsync(ct);
            var devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise.Trim();

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var q = db.Charges.AsNoTracking().AsQueryable();
            if (_dateFrom.HasValue) q = q.Where(c => c.Date >= _dateFrom.Value);
            if (_dateTo.HasValue) q = q.Where(c => c.Date <= _dateTo.Value);

            var search = SearchText?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(c => EF.Functions.Like(c.Libelle, $"%{search}%")
                    || EF.Functions.Like(c.Note, $"%{search}%")
                    || db.TypesCharges.AsNoTracking().Any(t => t.Id == c.TypeChargeId && EF.Functions.Like(t.Nom, $"%{search}%")));
            }

            var total = await q.CountAsync(ct);
            var docs = await q.OrderByDescending(c => c.Date).ThenByDescending(c => c.Id)
                .Skip(Pagination.Skip).Take(Pagination.PageSize)
                .ToListAsync(ct);
            var typeIds = docs.Select(d => d.TypeChargeId).Distinct().ToList();
            var types = typeIds.Count == 0
                ? new Dictionary<int, string>()
                : await db.TypesCharges.AsNoTracking()
                    .Where(t => typeIds.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id, t => t.Nom, ct);

            var selId = Selected?.Doc.Id;
            Rows.Clear();
            foreach (var d in docs)
                Rows.Add(ChargeListRow.Create(d, types.GetValueOrDefault(d.TypeChargeId, "?"), devise));
            Pagination.TotalCount = total;
            if (selId is { } id)
                Selected = Rows.FirstOrDefault(x => x.Doc.Id == id);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), ex.Message, ct);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken ct) => LoadPageAsync(ct, true);

    [RelayCommand]
    private void New()
    {
        var vm = _sp.GetRequiredService<ChargeEditViewModel>();
        vm.Load(null);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (Selected == null) return;
        var vm = _sp.GetRequiredService<ChargeEditViewModel>();
        vm.Load(Selected.Doc.Id);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (Selected == null) return;
        if (!await _dialog.ConfirmAsync(_locale.T("Charges_Title"),
                _locale.Tf("Charges_ConfirmDelete", Selected.Libelle), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.Charges.FirstAsync(c => c.Id == Selected.Doc.Id, cancellationToken);
            db.Charges.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await LoadAsync(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FilterDateAsync(CancellationToken cancellationToken)
    {
        var range = await _dialog.PickDateRangeAsync(_locale.T("Btn_FilterDate"), cancellationToken);
        if (range == null) return;
        if (range.Value.from == DateTime.MinValue && range.Value.to == DateTime.MinValue)
        {
            _dateFrom = null;
            _dateTo = null;
        }
        else
        {
            _dateFrom = range.Value.from;
            _dateTo = range.Value.to;
        }
        UpdateBtnFilterDateText();
        await LoadAsync(cancellationToken);
    }

    private void UpdateBtnFilterDateText()
    {
        if (_dateFrom == null || _dateTo == null)
            BtnFilterDate = _locale.T("Btn_FilterDate");
        else
            BtnFilterDate = $"{_dateFrom:dd/MM/yy} — {_dateTo:dd/MM/yy}";
    }
}
