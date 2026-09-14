using GestionCommerciale.Modules.Achat.Models;
using GestionCommerciale.Modules.BonRetourFournisseur.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Sortie.Models;
using GestionCommerciale.Modules.Sortie.ViewModels;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services.Pdf;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace GestionCommerciale.Shared.Services;

public sealed class PdfService : IPdfService
{
    private readonly IAppSettingsService _settings;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IUiPreferencesService _uiPreferences;

    public PdfService(
        IAppSettingsService settings,
        IDbContextFactory<AppDbContext> dbFactory,
        IUiPreferencesService uiPreferences)
    {
        _settings = settings;
        _dbFactory = dbFactory;
        _uiPreferences = uiPreferences;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly CultureInfo PdfCulture = CultureInfo.GetCultureInfo("fr-FR");

    private static string FmtQty(decimal value) => value.ToString("#,##0.##", PdfCulture);

    private static string FmtUnitPrice(decimal value) => value.ToString("N2", PdfCulture);

    private static string FmtTvaPct(decimal value) => value.ToString("#,##0.##", PdfCulture);

    private static string FmtMoney(decimal value) => value.ToString("N2", PdfCulture);

    public async Task<byte[]> BuildBonSortiePdfAsync(BonSortie doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var meta = await LoadProductMetaAsync(doc.Lignes.Select(l => l.ProduitId), cancellationToken);
        var totals = DocumentTotalsHelper.BonSortieTotals(doc.Lignes, doc.RemiseGlobale);
        var vis = _uiPreferences.GetDocumentLineColumnVisibility("bon_sortie");
        var lineData = new List<StandardPdfLine>();
        decimal promoTotalTtc = 0;
        foreach (var l in doc.Lignes)
        {
            var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            var ttc = lht * (1 + l.TauxTVA / 100m);
            var puCell = FmtUnitPrice(l.PrixUnitaireHT);
            var htCell = FmtMoney(lht);
            var ttcCell = FmtMoney(ttc);
            if (BonSortieLineRow.LooksLikePromo(l.Designation)
                && meta.TryGetValue(l.ProduitId, out var pm)
                && pm.PrixVenteHt > 0)
            {
                var catalogHt = DocumentTotalsHelper.LigneHT(l.Quantite, pm.PrixVenteHt, 0);
                var catalogTtc = catalogHt * (1 + l.TauxTVA / 100m);
                promoTotalTtc += catalogTtc;
                puCell = PdfPromoCell.Encode(FmtUnitPrice(pm.PrixVenteHt), FmtUnitPrice(0));
                htCell = PdfPromoCell.Encode(FmtMoney(catalogHt), FmtMoney(0));
                ttcCell = PdfPromoCell.Encode(FmtMoney(catalogTtc), FmtMoney(0));
            }

            lineData.Add(new StandardPdfLine(
                RefCell(meta, l.ProduitId),
                l.Designation,
                FmtQty(l.Quantite),
                l.Conditionnement,
                puCell,
                FmtTvaPct(l.TauxTVA),
                FmtMoney(l.Remise),
                htCell,
                ttcCell));
        }

        var (cols, rows) = BuildStandardPdfTable(vis, supportsLineRemise: true, "Qté", lineData);

        var docLines = new List<PdfKeyValueLine>
        {
            new("N°", doc.Numero),
            new("Date", doc.Date.ToString("dd/MM/yyyy")),
            new("Échéance", doc.DateEcheance.ToString("dd/MM/yyyy"))
        };

        if (doc.RemiseGlobale > 0)
            docLines.Add(new("Remise globale", $"{doc.RemiseGlobale:N2} %"));

        var model = BaseModel(cfg, "BON DE SORTIE", docLines, PartyLines(party, "الموزع"), cols, rows, totals, doc.Note, vis.ShowMontantTtc, promoTotalTtc > 0 ? promoTotalTtc : null);
        return CommercialDocumentPdfRenderer.Render(model, TryLoadLogoBytes(cfg.SocieteLogoPath));
    }

    public async Task<byte[]> BuildBonAchatPdfAsync(BonAchat doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var meta = await LoadProductMetaAsync(doc.Lignes.Select(l => l.ProduitId), cancellationToken);
        var totals = DocumentTotalsHelper.BonAchatTotals(doc.Lignes, doc.RemiseGlobale);
        var vis = _uiPreferences.GetDocumentLineColumnVisibility("bon_achat");
        var lineData = new List<StandardPdfLine>();
        foreach (var l in doc.Lignes)
        {
            var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            var ttc = lht * (1 + l.TauxTVA / 100m);
            lineData.Add(new StandardPdfLine(
                RefCell(meta, l.ProduitId),
                l.Designation,
                FmtQty(l.Quantite),
                l.Conditionnement,
                FmtUnitPrice(l.PrixUnitaireHT),
                FmtTvaPct(l.TauxTVA),
                FmtMoney(l.Remise),
                FmtMoney(lht),
                FmtMoney(ttc)));
        }

        var (cols, rows) = BuildStandardPdfTable(vis, supportsLineRemise: true, "Qté", lineData);

        var docLines = new List<PdfKeyValueLine>
        {
            new("N°", doc.Numero),
            new("Date", doc.Date.ToString("dd/MM/yyyy")),
            new("Échéance", doc.DateEcheance.ToString("dd/MM/yyyy"))
        };

        if (doc.RemiseGlobale > 0)
            docLines.Add(new("Remise globale", $"{doc.RemiseGlobale:N2} %"));

        var model = BaseModel(cfg, "BON D'ACHAT", docLines, PartyLines(party, "Fournisseur"), cols, rows, totals, doc.Note, vis.ShowMontantTtc);
        return CommercialDocumentPdfRenderer.Render(model, TryLoadLogoBytes(cfg.SocieteLogoPath));
    }

    public async Task<byte[]> BuildBonRetourPdfAsync(BonRetour bonRetour, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var meta = await LoadProductMetaAsync(bonRetour.Lignes.Select(l => l.ProduitId), cancellationToken);
        var totals = DocumentTotalsHelper.BonRetourTotals(bonRetour.Lignes);
        var vis = _uiPreferences.GetDocumentLineColumnVisibility("bon_retour");
        var lineData = new List<StandardPdfLine>();
        foreach (var l in bonRetour.Lignes)
        {
            var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            var ttc = lht * (1 + l.TauxTVA / 100m);
            lineData.Add(new StandardPdfLine(
                RefCell(meta, l.ProduitId),
                l.Designation,
                FmtQty(l.Quantite),
                string.IsNullOrWhiteSpace(l.Conditionnement) ? UniteCell(meta, l.ProduitId) : l.Conditionnement,
                FmtUnitPrice(l.PrixUnitaireHT),
                FmtTvaPct(l.TauxTVA),
                FmtMoney(l.Remise),
                FmtMoney(lht),
                FmtMoney(ttc)));
        }

        var (cols, rows) = BuildStandardPdfTable(vis, supportsLineRemise: true, "Qté", lineData);

        var note = $"{bonRetour.Motif}\nRetour marchandise : {(bonRetour.RetourMarchandise ? "Oui" : "Non")}";
        var docLines = new List<PdfKeyValueLine>
        {
            new("N°", bonRetour.Numero),
            new("Date", bonRetour.Date.ToString("dd/MM/yyyy"))
        };

        var model = BaseModel(cfg, "BON DE RETOUR", docLines, PartyLines(party, "Vendeur"), cols, rows, totals, note, vis.ShowMontantTtc);
        return CommercialDocumentPdfRenderer.Render(model, TryLoadLogoBytes(cfg.SocieteLogoPath));
    }

    public async Task<byte[]> BuildBonRetourFournisseurPdfAsync(BonRetourFournisseur doc, DocumentPartyPdfInfo party, CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var meta = await LoadProductMetaAsync(doc.Lignes.Select(l => l.ProduitId), cancellationToken);
        var totals = DocumentTotalsHelper.BonRetourFournisseurTotals(doc.Lignes);
        var vis = _uiPreferences.GetDocumentLineColumnVisibility("bonRetourFournisseur");
        var lineData = new List<StandardPdfLine>();
        foreach (var l in doc.Lignes)
        {
            var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
            var ttc = lht * (1 + l.TauxTVA / 100m);
            lineData.Add(new StandardPdfLine(
                RefCell(meta, l.ProduitId),
                l.Designation,
                FmtQty(l.Quantite),
                string.IsNullOrWhiteSpace(l.Conditionnement) ? UniteCell(meta, l.ProduitId) : l.Conditionnement,
                FmtUnitPrice(l.PrixUnitaireHT),
                FmtTvaPct(l.TauxTVA),
                FmtMoney(l.Remise),
                FmtMoney(lht),
                FmtMoney(ttc)));
        }

        var (cols, rows) = BuildStandardPdfTable(vis, supportsLineRemise: true, "Qté", lineData);

        var note = $"{doc.Motif}\nRetour marchandise : {(doc.RetourMarchandise ? "Oui" : "Non")}";
        var docLines = new List<PdfKeyValueLine>
        {
            new("N°", doc.Numero),
            new("Date", doc.Date.ToString("dd/MM/yyyy"))
        };

        var model = BaseModel(cfg, "BON DE RETOUR FOURNISSEUR", docLines, PartyLines(party, "Fournisseur"), cols, rows, totals, note, vis.ShowMontantTtc);
        return CommercialDocumentPdfRenderer.Render(model, TryLoadLogoBytes(cfg.SocieteLogoPath));
    }

    public async Task<byte[]> BuildClientAccountStatementPdfAsync(
        Tiers client,
        ClientAccountStatementResult statement,
        DocumentPartyPdfInfo party,
        CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "DH" : cfg.Devise.Trim();
        return ClientAccountStatementPdfRenderer.Render(
            cfg.SocieteNom,
            devise,
            client,
            party,
            statement,
            TryLoadLogoBytes(cfg.SocieteLogoPath));
    }

    public async Task<byte[]> BuildSupplierAccountStatementPdfAsync(
        Tiers fournisseur,
        ClientAccountStatementResult statement,
        DocumentPartyPdfInfo party,
        CancellationToken cancellationToken = default)
    {
        var cfg = await _settings.GetAsync(cancellationToken);
        var devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "DH" : cfg.Devise.Trim();
        return SupplierAccountStatementPdfRenderer.Render(
            cfg.SocieteNom,
            devise,
            fournisseur,
            party,
            statement,
            TryLoadLogoBytes(cfg.SocieteLogoPath));
    }

    private static CommercialDocumentPdfModel BaseModel(
        AppSettingsRow cfg,
        string kind,
        IReadOnlyList<PdfKeyValueLine> docLines,
        IReadOnlyList<PdfKeyValueLine> partyLines,
        IReadOnlyList<PdfTableColumn> columns,
        List<IReadOnlyList<string>> rows,
        (decimal ht, decimal tva, decimal ttc) totals,
        string? note,
        bool showTaxAndTtcInTotalsBox = true,
        decimal? promoTotalTtc = null)
    {
        var qtyCol = FindQtyColumnIndex(columns);
        var refCol = FindRefColumnIndex(columns);
        decimal sumQty = 0;
        var refCount = 0;
        var qtyParse = CultureInfo.GetCultureInfo("fr-FR");
        foreach (var r in rows)
        {
            if (qtyCol >= 0 && qtyCol < r.Count && decimal.TryParse(r[qtyCol], NumberStyles.Any, qtyParse, out var q))
                sumQty += q;
            if (refCol >= 0 && refCol < r.Count && !string.IsNullOrWhiteSpace(r[refCol]) && r[refCol] != "—")
                refCount++;
        }

        if (refCount == 0)
            refCount = rows.Count;

        int leadingSpan;
        string[] summaryValues;
        if (qtyCol > 0 && qtyCol < columns.Count)
        {
            leadingSpan = qtyCol;
            summaryValues = new string[columns.Count - qtyCol];
            for (var i = 0; i < summaryValues.Length; i++)
                summaryValues[i] = i == 0 ? FmtQty(sumQty) : "";
        }
        else
        {
            leadingSpan = columns.Count;
            summaryValues = [];
        }

        var currencyWord = cfg.Devise.ToUpperInvariant() switch
        {
            "MAD" or "DH" => "dirhams",
            "EUR" => "euros",
            "USD" => "dollars",
            _ => cfg.Devise
        };
        var amountForWords = showTaxAndTtcInTotalsBox ? totals.ttc : totals.ht;
        var amountWords = cfg.UiLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase)
            ? MoneyFrenchWords.FormatArabicFallback(amountForWords, cfg.Devise)
            : MoneyFrenchWords.Format(amountForWords, currencyWord);

        return new CommercialDocumentPdfModel
        {
            CompanyName = cfg.SocieteNom,
            DocumentKindLabel = kind,
            DocumentInfoLines = docLines,
            PartyInfoLines = partyLines,
            Columns = columns,
            Rows = rows,
            SummaryRow = rows.Count > 0
                ? new PdfTableSummaryRow
                {
                    LeadingSpan = leadingSpan,
                    Label = $"Total : {refCount} référence(s)",
                    Values = summaryValues
                }
                : null,
            TotalHt = totals.ht,
            TotalTva = totals.tva,
            TotalTtc = totals.ttc,
            PromoTotalTtc = promoTotalTtc,
            Devise = cfg.Devise,
            AmountInWords = amountWords,
            Note = note,
            FooterLines = BuildFooterLines(cfg),
            ShowTaxAndTtcInTotalsBox = showTaxAndTtcInTotalsBox
        };
    }

    private static int FindQtyColumnIndex(IReadOnlyList<PdfTableColumn> columns)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].Header.Contains("livr", StringComparison.OrdinalIgnoreCase))
                return i;
        }

        for (var i = 0; i < columns.Count; i++)
        {
            var h = columns[i].Header.ToLowerInvariant();
            if (h.Contains("qté") || h.Contains("qte"))
                return i;
        }

        return -1;
    }

    private static int FindRefColumnIndex(IReadOnlyList<PdfTableColumn> columns)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            var h = columns[i].Header.Trim();
            if (h.StartsWith("Réf", StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private (List<PdfTableColumn> Columns, List<IReadOnlyList<string>> Rows) BuildStandardPdfTable(
        DocumentLineColumnVisibility visibility,
        bool supportsLineRemise,
        string qtyHeader,
        IReadOnlyList<StandardPdfLine> lines)
    {
        var v = supportsLineRemise ? visibility : visibility with { ShowRemise = false };
        var columns = BuildStandardColumnList(v, qtyHeader);
        if (columns.Count == 0)
            return BuildStandardPdfTable(DocumentLineColumnVisibility.AllVisible, supportsLineRemise, qtyHeader, lines);

        var rows = new List<IReadOnlyList<string>>(lines.Count);
        foreach (var line in lines)
            rows.Add(BuildStandardDataRow(v, line));

        return (columns, rows);
    }

    private static List<PdfTableColumn> BuildStandardColumnList(DocumentLineColumnVisibility v, string qtyHeader)
    {
        var columns = new List<PdfTableColumn>();
        if (v.ShowReference)
            columns.Add(new PdfTableColumn("Référence", 0.7f, PdfTextAlignment.Start));
        if (v.ShowDesignation)
            columns.Add(new PdfTableColumn("Désignation", 2.5f, PdfTextAlignment.Start));
        if (v.ShowQuantite)
            columns.Add(new PdfTableColumn(qtyHeader, 0.35f, PdfTextAlignment.Center));
        if (v.ShowConditionnement)
            columns.Add(new PdfTableColumn("Ute", 0.25f, PdfTextAlignment.Center));
        if (v.ShowPuHt)
            columns.Add(new PdfTableColumn("PU HT", 0.55f, PdfTextAlignment.Center));
        if (v.ShowTva)
            columns.Add(new PdfTableColumn("Tva", 0.25f, PdfTextAlignment.Center));
        if (v.ShowRemise)
            columns.Add(new PdfTableColumn("Rem. %", 0.35f, PdfTextAlignment.Center));
        if (v.ShowMontantHt)
            columns.Add(new PdfTableColumn("Mnt HT", 0.55f, PdfTextAlignment.Center));
        if (v.ShowMontantTtc)
            columns.Add(new PdfTableColumn("Mnt TTC", 0.55f, PdfTextAlignment.Center));
        return columns;
    }

    private static List<string> BuildStandardDataRow(DocumentLineColumnVisibility v, StandardPdfLine line)
    {
        var cells = new List<string>();
        if (v.ShowReference)
            cells.Add(line.Ref);
        if (v.ShowDesignation)
            cells.Add(line.Designation);
        if (v.ShowQuantite)
            cells.Add(line.Quantite);
        if (v.ShowConditionnement)
            cells.Add(line.Unite);
        if (v.ShowPuHt)
            cells.Add(line.PuHt);
        if (v.ShowTva)
            cells.Add(line.Tva);
        if (v.ShowRemise)
            cells.Add(line.Remise);
        if (v.ShowMontantHt)
            cells.Add(line.MntHt);
        if (v.ShowMontantTtc)
            cells.Add(line.MntTtc);
        return cells;
    }

    private readonly record struct StandardPdfLine(
        string Ref,
        string Designation,
        string Quantite,
        string Unite,
        string PuHt,
        string Tva,
        string Remise,
        string MntHt,
        string MntTtc);

    private const string EmptyPartyFieldPlaceholder = "—";

    private static List<PdfKeyValueLine> PartyLines(DocumentPartyPdfInfo p, string roleLabel)
    {
        var list = new List<PdfKeyValueLine> { new(roleLabel, p.Nom, EmphasizeValue: true) };
        if (!string.IsNullOrWhiteSpace(p.Ice))
            list.Add(new("ICE", p.Ice));
        if (!string.IsNullOrWhiteSpace(p.Adresse))
            list.Add(new("Adresse", p.Adresse));
        list.Add(new("Téléphone", string.IsNullOrWhiteSpace(p.Telephone) ? EmptyPartyFieldPlaceholder : p.Telephone));
        list.Add(new("Email", string.IsNullOrWhiteSpace(p.Email) ? EmptyPartyFieldPlaceholder : p.Email));
        return list;
    }

    private sealed record ProductPdfMeta(string Ref, string Unite, decimal PrixVenteHt = 0);

    private static string RefCell(Dictionary<int, ProductPdfMeta> meta, int produitId) =>
        produitId > 0 && meta.TryGetValue(produitId, out var m) && !string.IsNullOrWhiteSpace(m.Ref) ? m.Ref : "—";

    private static string UniteCell(Dictionary<int, ProductPdfMeta> meta, int produitId) =>
        produitId > 0 && meta.TryGetValue(produitId, out var m) ? m.Unite : string.Empty;

    private static string ConditionnementCell(string? conditionnement, Dictionary<int, ProductPdfMeta> meta, int produitId) =>
        string.IsNullOrWhiteSpace(conditionnement) ? UniteCell(meta, produitId) : conditionnement.Trim();

    private async Task<Dictionary<int, ProductPdfMeta>> LoadProductMetaAsync(IEnumerable<int> productIds, CancellationToken cancellationToken)
    {
        var ids = productIds.Where(x => x > 0).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, ProductPdfMeta>();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Produits.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new ProductPdfMeta(p.Reference ?? "", p.Unite ?? "", p.PrixVenteHT), cancellationToken);
    }

    private static IReadOnlyList<string> BuildFooterLines(AppSettingsRow cfg)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(cfg.SocieteAdresse))
            lines.Add(cfg.SocieteAdresse.Trim());
        if (!string.IsNullOrWhiteSpace(cfg.SocieteICE))
            lines.Add($"ICE : {cfg.SocieteICE.Trim()}");

        if (!string.IsNullOrWhiteSpace(cfg.SocieteMentionsLegales))
        {
            foreach (var part in cfg.SocieteMentionsLegales.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                lines.Add(part);
        }

        return lines;
    }

    private static byte[]? TryLoadLogoBytes(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }
        catch
        {
            return null;
        }
    }
}
