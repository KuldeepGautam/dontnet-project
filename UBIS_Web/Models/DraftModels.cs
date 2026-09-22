namespace UBIS.Web.Models;

public class SaveDraftRequest
{
    public string Key { get; set; } = string.Empty;

    public string DataJson { get; set; } = string.Empty;
}

public class DraftEntry
{
    public string DataJson { get; set; } = string.Empty;

    public DateTime SavedAtUtc { get; set; }
}
