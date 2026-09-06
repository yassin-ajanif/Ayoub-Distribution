using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ChargeEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;

    public ChargeEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        Title = _locale.T("Charges_NewTitle");
        RefreshUi();
        _locale.CultureApplied += (_, _) => RefreshUi();
    }

    public ObservableCollection<TypeCharge> TypesDisponibles { get; } = [];
    public ObservableCollection<TypeCharge> TypesGestion { get; } = [];

    [ObservableProperty] private int? _chargeId;
    [ObservableProperty] private int _typeChargeId;
    [ObservableProperty] private TypeCharge? _selectedType;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private string _libelle = string.Empty;
    [ObservableProperty] private decimal _montantTtc;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private string _newTypeNom = string.Empty;
    [ObservableProperty] private TypeCharge? _selectedTypeGestion;

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnDelete = string.Empty;
    [ObservableProperty] private string _lblType = string.Empty;
    [ObservableProperty] private string _lblDate = string.Empty;
    [ObservableProperty] private string _lblLibelle = string.Empty;
    [ObservableProperty] private string _wmLibelle = string.Empty;
    [ObservableProperty] private string _lblTtc = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;
    [ObservableProperty] private string _lblTypesPanel = string.Empty;
    [ObservableProperty] private string _wmNewType = string.Empty;
    [ObservableProperty] private string _btnAddType = string.Empty;
    [ObservableProperty] private string _btnToggleActif = string.Empty;
    [ObservableProperty] private string _colNom = string.Empty;
    [ObservableProperty] private string _colActif = string.Empty;
    [ObservableProperty] private string _menuDeleteType = string.Empty;
    [ObservableProperty] private string _menuEditType = string.Empty;

    public bool CanDelete => ChargeId.HasValue;

    private void RefreshUi()
    {
        BtnBack = _locale.T("Btn_BackList");
        BtnSave = _locale.T("Btn_Save");
        BtnDelete = _locale.T("Btn_Delete");
        LblType = _locale.T("Charges_ColType");
        LblDate = _locale.T("Charges_LblDate");
        LblLibelle = _locale.T("Charges_ColLibelle");
        WmLibelle = _locale.T("Charges_WmLibelle");
        LblTtc = _locale.T("DevisList_ColTtc");
        LblNote = _locale.T("Lbl_Note");
        LblTypesPanel = _locale.T("Charges_TypesPanel");
        WmNewType = _locale.T("Charges_WmNewType");
        BtnAddType = _locale.T("Btn_Add");
        BtnToggleActif = _locale.T("Btn_ToggleActif");
        ColNom = _locale.T("Charges_ColNom");
        ColActif = _locale.T("Lbl_ColActif");
        MenuDeleteType = _locale.T("Charges_MenuDeleteType");
        MenuEditType = _locale.T("Charges_MenuEditType");
        Title = ChargeId.HasValue
            ? _locale.Tf("Charges_EditTitle", Libelle)
            : _locale.T("Charges_NewTitle");
    }

    partial void OnChargeIdChanged(int? value)
    {
        OnPropertyChanged(nameof(CanDelete));
        DeleteCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTypeChanged(TypeCharge? value)
    {
        if (value != null)
            TypeChargeId = value.Id;
    }

    partial void OnTypeChargeIdChanged(int value)
    {
        if (SelectedType?.Id == value) return;
        SelectedType = TypesDisponibles.FirstOrDefault(t => t.Id == value)
            ?? TypesGestion.FirstOrDefault(t => t.Id == value);
    }

    public void Load(int? id) => _ = LoadAsync(id, CancellationToken.None);

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        ChargeId = id;

        if (id == null)
        {
            await ReloadTypesAsync(cancellationToken);
            Date = new DateTimeOffset(DateTime.Today);
            Libelle = string.Empty;
            MontantTtc = 0;
            Note = string.Empty;
            TypeChargeId = TypesDisponibles.FirstOrDefault()?.Id ?? 0;
            SelectedType = TypesDisponibles.FirstOrDefault();
            Title = _locale.T("Charges_NewTitle");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var c = await db.Charges.AsNoTracking().FirstAsync(x => x.Id == id, cancellationToken);
        TypeChargeId = c.TypeChargeId;
        await ReloadTypesAsync(cancellationToken);
        SelectedType = TypesDisponibles.FirstOrDefault(t => t.Id == c.TypeChargeId)
            ?? TypesGestion.FirstOrDefault(t => t.Id == c.TypeChargeId);
        Date = new DateTimeOffset(c.Date);
        Libelle = c.Libelle;
        MontantTtc = c.MontantTtc;
        Note = c.Note;
        Title = _locale.Tf("Charges_EditTitle", c.Libelle);
    }

    private async Task ReloadTypesAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var all = await db.TypesCharges.AsNoTracking().OrderBy(t => t.Nom).ToListAsync(cancellationToken);

        var selectedTypeId = SelectedType?.Id ?? TypeChargeId;
        var selectedGestionId = SelectedTypeGestion?.Id;

        // Avalonia ComboBox/ListBox can crash if ItemsSource is cleared while SelectedItem is set.
        SelectedType = null;
        SelectedTypeGestion = null;

        TypesGestion.Clear();
        foreach (var t in all) TypesGestion.Add(t);

        TypesDisponibles.Clear();
        foreach (var t in all.Where(t => t.Actif || t.Id == TypeChargeId || t.Id == selectedTypeId))
            TypesDisponibles.Add(t);

        if (selectedTypeId > 0)
        {
            SelectedType = TypesDisponibles.FirstOrDefault(t => t.Id == selectedTypeId)
                ?? TypesGestion.FirstOrDefault(t => t.Id == selectedTypeId);
            if (SelectedType != null)
                TypeChargeId = SelectedType.Id;
        }

        if (selectedGestionId is int gid)
            SelectedTypeGestion = TypesGestion.FirstOrDefault(t => t.Id == gid);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (TypeChargeId <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), _locale.T("Charges_ErrType"), cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(Libelle))
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), _locale.T("Charges_ErrLibelle"), cancellationToken);
            return;
        }

        if (MontantTtc <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), _locale.T("Charges_ErrTtc"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            Charge entity;
            if (ChargeId == null)
            {
                entity = new Charge
                {
                    TypeChargeId = TypeChargeId,
                    Date = Date.DateTime.Date,
                    Libelle = Libelle.Trim(),
                    MontantTtc = MontantTtc,
                    Note = Note?.Trim() ?? string.Empty,
                    CreatedByUserId = _session.UserId
                };
                db.Charges.Add(entity);
            }
            else
            {
                entity = await db.Charges.FirstAsync(c => c.Id == ChargeId, cancellationToken);
                entity.TypeChargeId = TypeChargeId;
                entity.Date = Date.DateTime.Date;
                entity.Libelle = Libelle.Trim();
                entity.MontantTtc = MontantTtc;
                entity.Note = Note?.Trim() ?? string.Empty;
            }

            await db.SaveChangesAsync(cancellationToken);
            ChargeId = entity.Id;
            await _dialog.ShowInfoAsync(_locale.T("Charges_Title"), _locale.T("Charges_Saved"), cancellationToken);
            await LoadAsync(ChargeId, cancellationToken);
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), detail, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (ChargeId is not { } id) return;
        if (!await _dialog.ConfirmAsync(_locale.T("Charges_Title"),
                _locale.Tf("Charges_ConfirmDelete", Libelle), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.Charges.FirstAsync(c => c.Id == id, cancellationToken);
            db.Charges.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await _dialog.ShowInfoAsync(_locale.T("Charges_Title"), _locale.T("Charges_Deleted"), cancellationToken);
            Back();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddTypeAsync(CancellationToken cancellationToken)
    {
        var nom = NewTypeNom?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nom))
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), _locale.T("Charges_ErrTypeNom"), cancellationToken);
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (await db.TypesCharges.AnyAsync(t => t.Nom.ToLower() == nom.ToLower(), cancellationToken))
            {
                await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), _locale.T("Charges_ErrTypeExists"), cancellationToken);
                return;
            }

            var t = new TypeCharge
            {
                Nom = nom,
                Actif = true,
                CreatedByUserId = _session.UserId
            };
            db.TypesCharges.Add(t);
            await db.SaveChangesAsync(cancellationToken);
            NewTypeNom = string.Empty;
            TypeChargeId = t.Id;
            await ReloadTypesAsync(cancellationToken);
            SelectedType = TypesDisponibles.FirstOrDefault(x => x.Id == t.Id);
            SelectedTypeGestion = TypesGestion.FirstOrDefault(x => x.Id == t.Id);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), ex.Message, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task ToggleTypeActifAsync(CancellationToken cancellationToken)
    {
        if (SelectedTypeGestion == null) return;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var t = await db.TypesCharges.FirstAsync(x => x.Id == SelectedTypeGestion.Id, cancellationToken);
        t.Actif = !t.Actif;
        await db.SaveChangesAsync(cancellationToken);
        await ReloadTypesAsync(cancellationToken);
        SelectedTypeGestion = TypesGestion.FirstOrDefault(x => x.Id == t.Id);
    }

    [RelayCommand]
    private async Task EditTypeAsync(TypeCharge? type, CancellationToken cancellationToken)
    {
        if (type == null) return;

        var nouveauNom = await _dialog.ShowPromptAsync(
            _locale.T("Charges_Title"),
            _locale.Tf("Charges_EditTypePrompt", type.Nom),
            cancellationToken,
            type.Nom);

        if (nouveauNom is null) return;

        nouveauNom = nouveauNom.Trim();
        if (string.IsNullOrWhiteSpace(nouveauNom))
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), _locale.T("Charges_ErrTypeNom"), cancellationToken);
            return;
        }

        if (string.Equals(nouveauNom, type.Nom, StringComparison.Ordinal))
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.TypesCharges.AnyAsync(
                t => t.Id != type.Id && t.Nom.ToLower() == nouveauNom.ToLower(),
                cancellationToken))
        {
            await _dialog.ShowErrorAsync(_locale.T("Charges_Title"), _locale.T("Charges_ErrTypeExists"), cancellationToken);
            return;
        }

        var entity = await db.TypesCharges.FirstAsync(t => t.Id == type.Id, cancellationToken);
        entity.Nom = nouveauNom;
        await db.SaveChangesAsync(cancellationToken);

        await ReloadTypesAsync(cancellationToken);
        SelectedTypeGestion = TypesGestion.FirstOrDefault(t => t.Id == type.Id);
        if (TypeChargeId == type.Id)
            SelectedType = TypesDisponibles.FirstOrDefault(t => t.Id == type.Id)
                ?? TypesGestion.FirstOrDefault(t => t.Id == type.Id);
    }

    [RelayCommand]
    private async Task DeleteTypeAsync(TypeCharge? type, CancellationToken cancellationToken)
    {
        if (type == null) return;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var inUse = await db.Charges.AsNoTracking().AnyAsync(c => c.TypeChargeId == type.Id, cancellationToken);
        if (inUse)
        {
            await _dialog.ShowErrorAsync(
                _locale.T("Charges_Title"),
                _locale.Tf("Charges_ErrTypeInUse", type.Nom),
                cancellationToken);
            return;
        }

        if (!await _dialog.ConfirmAsync(
                _locale.T("Charges_Title"),
                _locale.Tf("Charges_ConfirmDeleteType", type.Nom),
                cancellationToken))
            return;

        var entity = await db.TypesCharges.FirstAsync(t => t.Id == type.Id, cancellationToken);
        db.TypesCharges.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        if (TypeChargeId == type.Id)
        {
            TypeChargeId = 0;
            SelectedType = null;
        }

        if (SelectedTypeGestion?.Id == type.Id)
            SelectedTypeGestion = null;

        await ReloadTypesAsync(cancellationToken);
        if (SelectedType == null)
            SelectedType = TypesDisponibles.FirstOrDefault();
    }

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<ChargeListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }
}
