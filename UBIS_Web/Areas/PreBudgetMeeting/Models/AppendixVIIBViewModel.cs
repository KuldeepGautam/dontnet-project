namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIIBViewModel : AppendixBaseViewModel
{
    public List<AppendixCommercialUndertakingReceiptsDto> Records { get; set; } = new();
    public SaveAppendixCommercialUndertakingReceiptsDto NewRecord { get; set; } = new();
    public List<AppendixVIIBSchemeOptionDto> Schemes { get; set; } = new();
    public List<AppendixVIIBTransactionTypeOptionDto> TransactionTypes { get; set; } = new();
}
