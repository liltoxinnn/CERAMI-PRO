using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Application.Interfaces;

/// <summary>
/// Attribue les numéros de documents, au format « PRÉFIXE-ANNÉE-0001 ».
/// Les préfixes proviennent des paramètres de l'atelier.
/// </summary>
public interface IReferenceNumberService
{
    Task<string> GenererAsync(TypeDocument type, CancellationToken cancellationToken = default);
}
