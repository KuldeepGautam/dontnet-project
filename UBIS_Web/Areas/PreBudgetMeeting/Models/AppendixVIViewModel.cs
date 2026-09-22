namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixVIViewModel : AppendixBaseViewModel
{
    public List<AppendixNonTaxRevenueDto> Records { get; set; } = new();
    public SaveAppendixNonTaxRevenueDto NewRecord { get; set; } = new();
    public List<AppendixVIReceiptTypeOptionDto> ReceiptTypes { get; set; } = new();
}
