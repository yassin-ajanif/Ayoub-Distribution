namespace GestionCommerciale.Modules.Facturation.Services;

internal static class ReglementAllocationLines
{
    public static Dictionary<int, List<(string Numero, decimal Amount)>> ByGroup(
        IEnumerable<(int GroupeId, string Numero, decimal Amount)> slices)
    {
        return slices
            .GroupBy(s => s.GroupeId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.Numero, StringComparer.OrdinalIgnoreCase)
                    .Select(d => (Numero: d.Key, Amount: d.Sum(x => x.Amount)))
                    .Where(x => x.Amount > 0)
                    .OrderBy(x => x.Numero, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }
}
