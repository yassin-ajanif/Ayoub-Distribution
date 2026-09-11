namespace GestionCommerciale.Modules.Facturation.Services;

public enum ClientAccountEntryKind
{
    Facture = 0,
    BonSortie = 1,
    BonRetour = 2,
    Paiement = 3
}

public sealed class ClientAccountStatementRow
{
    public DateTime Date { get; init; }
    public ClientAccountEntryKind Kind { get; init; }
    public long TieBreakId { get; init; }
    public string Designation { get; init; } = string.Empty;
    public string Observation { get; init; } = string.Empty;
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal Balance { get; init; }
    /// <summary>Breakdown line under a grouped payment. Does not change the balance.</summary>
    public bool IsAllocationDetail { get; init; }
    public decimal AllocationAmount { get; init; }
}

public sealed class ClientAccountStatementResult
{
    public required IReadOnlyList<ClientAccountStatementRow> Rows { get; init; }
    public decimal SoldeActuel { get; init; }
}
