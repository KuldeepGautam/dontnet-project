namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixPAViewModel : AppendixBaseViewModel
{
    public List<PublicAccountReceiptPaymentDto> Records { get; set; } = new();
    public SavePublicAccountReceiptPaymentDto NewRecord { get; set; } = new();
    public List<AppendixVIIAMajorHeadOptionDto> MajorHeads { get; set; } = new();
}
