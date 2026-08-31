using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>Sens du flux financier : argent reçu du client ou versé au fournisseur.</summary>
public enum PaymentDirection
{
    [Libelle("Encaissement")] Encaissement = 0,
    [Libelle("Décaissement")] Decaissement = 1
}
