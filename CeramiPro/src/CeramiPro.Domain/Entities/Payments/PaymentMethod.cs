using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Payments;

/// <summary>Mode de règlement : espèces, virement bancaire, carte, autre.</summary>
public class PaymentMethod : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool RequiresReference { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
