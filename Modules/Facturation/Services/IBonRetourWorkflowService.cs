namespace GestionCommerciale.Modules.Facturation.Services;

public interface IBonRetourWorkflowService
{
    Task CreerEtValiderAsync(int bonRetourId, int? userId, CancellationToken cancellationToken = default);
}
