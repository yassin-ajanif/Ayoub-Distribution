using System.Globalization;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public sealed class ChargeListRow
{
    public required Charge Doc { get; init; }
    public string TypeNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string Libelle { get; init; } = string.Empty;
    public string TtcLabel { get; init; } = string.Empty;
    public string NotePreview { get; init; } = string.Empty;

    public static ChargeListRow Create(Charge doc, string typeNom, string devise)
    {
        return new ChargeListRow
        {
            Doc = doc,
            TypeNom = typeNom,
            DateShort = doc.Date.ToString("d", CultureInfo.CurrentCulture),
            Libelle = doc.Libelle,
            TtcLabel = $"{doc.MontantTtc:N2} {devise}",
            NotePreview = DocumentListFormat.NotePreview(doc.Note),
        };
    }
}
