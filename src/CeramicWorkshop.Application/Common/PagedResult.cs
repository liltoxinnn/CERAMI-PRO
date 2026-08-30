namespace CeramicWorkshop.Application.Common;

/// <summary>Page de résultats destinée aux tableaux de l'interface.</summary>
public class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> elements, int total, int page, int taillePage)
    {
        Elements = elements;
        Total = total;
        Page = page;
        TaillePage = taillePage;
    }

    public IReadOnlyList<T> Elements { get; }
    public int Total { get; }
    public int Page { get; }
    public int TaillePage { get; }
    public int NombrePages => TaillePage > 0 ? (int)Math.Ceiling(Total / (double)TaillePage) : 0;
    public bool PagePrecedenteDisponible => Page > 1;
    public bool PageSuivanteDisponible => Page < NombrePages;
}

/// <summary>Paramètres de pagination et de recherche communs à toutes les listes.</summary>
public class PagedRequest
{
    private const int TaillePageMaximale = 200;
    private int _taillePage = 25;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int TaillePage
    {
        get => _taillePage;
        set => _taillePage = value switch
        {
            < 1 => 25,
            > TaillePageMaximale => TaillePageMaximale,
            _ => value
        };
    }

    /// <summary>Texte recherché (nom, référence, téléphone…).</summary>
    public string? Recherche { get; set; }
}
