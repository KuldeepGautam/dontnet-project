namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixIIIViewModel : AppendixBaseViewModel
{
    public List<AppendixCnaSnaBalanceDto> Records { get; set; } = new();
    public SaveAppendixCnaSnaBalanceDto NewRecord { get; set; } = new();
    public List<AppendixIIICategoryOptionDto> Categories { get; set; } = new();
}
