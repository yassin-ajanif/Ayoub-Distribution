using System.Diagnostics;
using System.Globalization;
using GestionCommerciale.Shared.Database;
using Microsoft.Data.Sqlite;

namespace GestionCommerciale.Shared.Services;

public class PerformanceTestService
{
    private const int ProductCount = 10_000;
    private const int ClientCount = 1_000;
    private const int DocumentCount = 50_000;
    private const int DocumentsPerDay = 10;
    private const int InitialProductStock = 10_000;
    private const string BsOrigineType = "BS";

    private static readonly Random Rng = Random.Shared;
    private readonly string _cs;

    public PerformanceTestService()
    {
        _cs = DatabasePath.GetConnectionString();
    }

    public async Task<string> RunAsync(IProgress<string> progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        await using var conn = new SqliteConnection(_cs);
        await conn.OpenAsync(ct);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA synchronous=OFF; PRAGMA journal_mode=WAL; PRAGMA cache_size=-500000; PRAGMA temp_store=MEMORY;";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var max = await GetMaxIdsAsync(conn, ct);
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var startDate = DateTime.Today;
        var dayCount = DocumentCount / DocumentsPerDay;

        progress.Report($"Création de {ProductCount:N0} produits...");
        await InsertProductsAsync(conn, max.ProdId, now, ct);

        progress.Report($"Création de {ClientCount:N0} clients...");
        await InsertClientsAsync(conn, max.TiersId, now, ct);

        progress.Report($"Création de {DocumentCount:N0} bons de sortie (~{DocumentsPerDay}/jour sur {dayCount:N0} jours)...");
        var docs = await InsertBonSortieHeadersAsync(conn, max, now, startDate, ct);

        progress.Report("Création des lignes de bons de sortie...");
        var lines = await InsertBonSortieLinesAsync(conn, max, docs, ct);

        progress.Report("Mise à jour des totaux TTC des bons de sortie...");
        await UpdateBonSortieTotalTtcAsync(conn, docs, ct);

        progress.Report("Création des paiements clients (soldes)...");
        var paiementCount = await InsertPaiementsAsync(conn, max.PaiementId, docs, now, ct);

        progress.Report("Création des mouvements de stock (sorties)...");
        var mvtCount = await InsertStockMovementsAsync(conn, max.MouvementId, max.ProdId, lines, now, ct);

        sw.Stop();
        var e = sw.Elapsed;
        return $"Terminé en {e.Hours}h {e.Minutes}m {e.Seconds}s ({e.TotalSeconds:F1}s) — {ProductCount:N0} produits, {DocumentCount:N0} bons de sortie, {paiementCount:N0} paiements, {mvtCount:N0} mouvements stock sur {dayCount:N0} jours (~{dayCount / 365.25:F1} ans à {DocumentsPerDay}/jour).";
    }

    private static async Task<(long ProdId, long TiersId, long DocId, long LigneId, long PaiementId, long MouvementId)>
        GetMaxIdsAsync(SqliteConnection conn, CancellationToken ct)
    {
        async Task<long> Max(string table)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT IFNULL(MAX(Id),0) FROM \"{table}\"";
            var r = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(r);
        }

        return (
            await Max("Produits"),
            await Max("Tiers"),
            await Max("BonsSortie"),
            await Max("BonSortieLignes"),
            await Max("PaiementsBonSortie"),
            await Max("MouvementsStock")
        );
    }

    private static string DateForDocumentIndex(DateTime startDate, int docIndex)
    {
        var dayOffset = docIndex / DocumentsPerDay;
        return startDate.AddDays(dayOffset).ToString("yyyy-MM-dd");
    }

    private static decimal ComputeTtc(decimal ht, decimal tva, decimal remiseGlobalePct)
    {
        if (remiseGlobalePct > 0)
        {
            var factor = 1 - remiseGlobalePct / 100m;
            ht *= factor;
            tva *= factor;
        }

        return ht + tva;
    }

    private static async Task InsertProductsAsync(SqliteConnection conn, long startId, string now, CancellationToken ct)
    {
        const int batch = 500;
        var designs = new[] { "Ordinateur portable", "Souris sans fil", "Clavier mécanique", "Écran 24\"", "Disque dur SSD", "Carte mémoire", "Imprimante", "Scanner", "Webcam HD", "Casque audio", "Enceinte Bluetooth", "Hub USB", "Câble HDMI", "Adaptateur secteur", "Batterie externe", "Sacoche ordinateur", "Tapis de souris", "Support téléphone", "Ventilateur USB", "Lampe LED" };

        for (var i = 0; i < ProductCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO Produits (Id,CreatedAt,UpdatedAt,Reference,CodeBarre,Designation,Unite,PrixAchatHT,PrixVenteHT,TauxTVA,StockActuel,StockMinimum,Actif) VALUES ");
            var end = Math.Min(i + batch, ProductCount);
            for (var j = i; j < end; j++)
            {
                var id = startId + 1 + j;
                var desig = $"{designs[j % designs.Length]} #{j}";
                var pa = Rng.Next(500, 500_000) / 100m;
                var pv = pa + Rng.Next(200, 300_000) / 100m;
                var tva = Rng.NextDouble() < 0.7 ? 20m : Rng.NextDouble() < 0.5 ? 14m : 10m;
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}','PROD-{j:D5}',NULL,'{Escape(desig)}','U',{pa:F2},{pv:F2},{tva:F1},{InitialProductStock},{Rng.Next(0,51)},1)");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private static async Task InsertClientsAsync(SqliteConnection conn, long startId, string now, CancellationToken ct)
    {
        const int batch = 500;
        for (var i = 0; i < ClientCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO Tiers (Id,CreatedAt,UpdatedAt,Type,Nom,ICE,Adresse,Ville,Telephone,Email,ConditionsPaiement,Actif) VALUES ");
            var end = Math.Min(i + batch, ClientCount);
            for (var j = i; j < end; j++)
            {
                var id = startId + 1 + j;
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}',0,'Client test {j}','','Casablanca','Casablanca','','','',1)");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private sealed class DocMeta
    {
        public required long Id { get; init; }
        public required DateTime Date { get; init; }
        public required bool EstPayee { get; init; }
        public required decimal RemiseGlobale { get; init; }
        public decimal TotalHt { get; set; }
        public decimal TotalTva { get; set; }
        public decimal TotalTtc { get; set; }
    }

    private static async Task<DocMeta[]> InsertBonSortieHeadersAsync(SqliteConnection conn,
        (long ProdId, long TiersId, long DocId, long LigneId, long PaiementId, long MouvementId) max,
        string now, DateTime startDate, CancellationToken ct)
    {
        const int batch = 500;
        var startDoc = max.DocId + 1;
        var clientStart = max.TiersId + 1;
        var meta = new DocMeta[DocumentCount];

        for (var i = 0; i < DocumentCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO BonsSortie (Id,CreatedAt,UpdatedAt,CreatedByUserId,Numero,ClientId,Date,DateEcheance,EstPayee,RemiseGlobale,TotalTtc,Note) VALUES ");
            var end = Math.Min(i + batch, DocumentCount);
            for (var j = i; j < end; j++)
            {
                var id = startDoc + j;
                var clientId = clientStart + Rng.Next(0, ClientCount);
                var date = DateTime.Parse(DateForDocumentIndex(startDate, j));
                var echeance = date.AddDays(Rng.Next(15, 61));
                var estPayee = Rng.NextDouble() < 0.5;
                var remiseGlobale = Rng.NextDouble() < 0.15 ? Rng.Next(0, 501) / 100m : 0m;
                meta[j] = new DocMeta
                {
                    Id = id,
                    Date = date,
                    EstPayee = estPayee,
                    RemiseGlobale = remiseGlobale
                };
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}',NULL,'BS-{date.Year}-{j:D6}',{clientId},'{date:yyyy-MM-dd}','{echeance:yyyy-MM-dd}',{(estPayee ? 1 : 0)},{remiseGlobale:F2},0,'')");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }

        return meta;
    }

    private static async Task<List<(long DocId, long ProdId, decimal Qty)>> InsertBonSortieLinesAsync(SqliteConnection conn,
        (long ProdId, long TiersId, long DocId, long LigneId, long PaiementId, long MouvementId) max,
        DocMeta[] docs, CancellationToken ct)
    {
        const int batch = 1000;
        var startDoc = max.DocId + 1;
        var startLigne = max.LigneId + 1;
        var prodStart = max.ProdId + 1;
        var ligneIdx = 0;
        System.Text.StringBuilder? sb = null;
        var seeds = new List<(long DocId, long ProdId, decimal Qty)>(DocumentCount * 3);

        for (var i = 0; i < DocumentCount; i++)
        {
            var docId = startDoc + i;
            var meta = docs[i];
            var linesPerDoc = Rng.Next(1, 6);
            for (var li = 0; li < linesPerDoc; li++)
            {
                if (ligneIdx % batch == 0)
                {
                    if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
                    sb = new System.Text.StringBuilder();
                    sb.Append("INSERT INTO BonSortieLignes (Id,CreatedAt,UpdatedAt,CreatedByUserId,BonSortieId,ProduitId,Designation,Quantite,PrixUnitaireHT,Remise,TauxTVA,Conditionnement) VALUES ");
                }

                var id = startLigne + ligneIdx;
                var prodId = prodStart + Rng.Next(0, ProductCount);
                var qty = Rng.Next(1, 11);
                var pu = Rng.Next(1000, 500_000) / 100m;
                var remise = Rng.NextDouble() < 0.2 ? Rng.Next(0, 1001) / 100m : 0m;
                var tva = Rng.NextDouble() < 0.7 ? 20m : 10m;
                var desig = $"Produit {prodId}";
                var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var lht = qty * pu * (1 - remise / 100m);
                meta.TotalHt += lht;
                meta.TotalTva += lht * (tva / 100m);

                if (ligneIdx % batch > 0) sb!.Append(',');
                sb!.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}',NULL,{docId},{prodId},'{Escape(desig)}',{qty},{pu:F2},{remise:F2},{tva:F1},'U')");
                seeds.Add((docId, prodId, qty));
                ligneIdx++;
            }
        }

        if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
        return seeds;
    }

    private static async Task UpdateBonSortieTotalTtcAsync(SqliteConnection conn, DocMeta[] docs, CancellationToken ct)
    {
        const int batch = 500;
        for (var i = 0; i < docs.Length; i++)
            docs[i].TotalTtc = ComputeTtc(docs[i].TotalHt, docs[i].TotalTva, docs[i].RemiseGlobale);

        for (var i = 0; i < docs.Length; i += batch)
        {
            var end = Math.Min(i + batch, docs.Length);
            var sb = new System.Text.StringBuilder();
            for (var j = i; j < end; j++)
            {
                var f = docs[j];
                if (j > i) sb.Append(';');
                sb.Append(CultureInfo.InvariantCulture, $"UPDATE BonsSortie SET TotalTtc={f.TotalTtc:F2} WHERE Id={f.Id}");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private static async Task<int> InsertPaiementsAsync(SqliteConnection conn, long startPaiementId, DocMeta[] docs, string now, CancellationToken ct)
    {
        const int batch = 500;
        var paiementId = startPaiementId;
        var count = 0;
        System.Text.StringBuilder? sb = null;
        var batchCount = 0;

        foreach (var f in docs)
        {
            if (f.TotalTtc <= 0) continue;

            decimal montant;
            DateTime date;
            if (f.EstPayee)
            {
                montant = f.TotalTtc;
                date = f.Date.AddDays(Rng.Next(0, 31));
            }
            else if (Rng.NextDouble() < 0.45)
            {
                montant = Math.Round(f.TotalTtc * Rng.Next(20, 81) / 100m, 2);
                date = f.Date.AddDays(Rng.Next(5, 91));
            }
            else continue;

            if (montant <= 0) continue;

            if (batchCount % batch == 0)
            {
                if (sb != null)
                {
                    await ExecAsync(conn, sb.ToString(), ct);
                    sb = null;
                }
                sb = new System.Text.StringBuilder();
                sb.Append("INSERT INTO PaiementsBonSortie (Id,CreatedAt,UpdatedAt,CreatedByUserId,BonSortieId,Montant,Date,Mode,Reference) VALUES ");
            }
            else
            {
                sb!.Append(',');
            }

            paiementId++;
            var mode = Rng.Next(0, 6);
            var reference = $"REF-{paiementId:D7}";
            sb!.Append(CultureInfo.InvariantCulture, $"({paiementId},'{now}','{now}',NULL,{f.Id},{montant:F2},'{date:yyyy-MM-dd}',{mode},'{reference}')");
            count++;
            batchCount++;
        }

        if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
        return count;
    }

    private static async Task<int> InsertStockMovementsAsync(
        SqliteConnection conn,
        long startMouvementId,
        long prodStartId,
        List<(long DocId, long ProdId, decimal Qty)> lines,
        string now,
        CancellationToken ct)
    {
        const int batch = 1000;
        const int sortieType = 1;
        var stockByProd = new Dictionary<long, decimal>(ProductCount);
        for (var p = 1; p <= ProductCount; p++)
            stockByProd[prodStartId + p] = InitialProductStock;

        var mouvementId = startMouvementId;
        var count = 0;
        System.Text.StringBuilder? sb = null;

        foreach (var docGroup in lines.GroupBy(l => l.DocId).OrderBy(g => g.Key))
        {
            var docId = docGroup.Key;
            foreach (var prodGroup in docGroup.GroupBy(l => l.ProdId))
            {
                var prodId = prodGroup.Key;
                var qty = prodGroup.Sum(l => l.Qty);
                stockByProd.TryGetValue(prodId, out var stockAvant);

                if (count % batch == 0)
                {
                    if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
                    sb = new System.Text.StringBuilder();
                    sb.Append("INSERT INTO MouvementsStock (Id,CreatedAt,UpdatedAt,ProduitId,Type,StockAvant,Quantite,OrigineType,OrigineId,Note) VALUES ");
                }
                else
                {
                    sb!.Append(',');
                }

                mouvementId++;
                var note = $"BS-{docId}";
                sb!.Append(CultureInfo.InvariantCulture, $"({mouvementId},'{now}','{now}',{prodId},{sortieType},{stockAvant:F2},{qty:F2},'{BsOrigineType}',{docId},'{Escape(note)}')");
                stockByProd[prodId] = stockAvant - qty;
                count++;
            }
        }

        if (sb != null) await ExecAsync(conn, sb.ToString(), ct);

        const int updateBatch = 500;
        var prodIds = stockByProd.Keys.OrderBy(k => k).ToList();
        for (var i = 0; i < prodIds.Count; i += updateBatch)
        {
            var end = Math.Min(i + updateBatch, prodIds.Count);
            var sbUpdate = new System.Text.StringBuilder();
            for (var j = i; j < end; j++)
            {
                var prodId = prodIds[j];
                if (j > i) sbUpdate.Append(';');
                sbUpdate.Append(CultureInfo.InvariantCulture, $"UPDATE Produits SET StockActuel={stockByProd[prodId]:F2} WHERE Id={prodId}");
            }
            await ExecAsync(conn, sbUpdate.ToString(), ct);
        }

        return count;
    }

    private static async Task ExecAsync(SqliteConnection conn, string sql, CancellationToken ct)
    {
        await using var tx = conn.BeginTransaction();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        await cmd.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static string Escape(string s) => s.Replace("'", "''");
}
