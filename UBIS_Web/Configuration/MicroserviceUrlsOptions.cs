namespace UBIS.Web.Configuration;

/// <summary>Binds to "MicroserviceUrls". Base addresses default to localhost dev ports.</summary>
public class MicroserviceUrlsOptions
{
    public string Aim { get; set; } = "http://localhost:5101";

    public string MenuGenerator { get; set; } = "http://localhost:5102";

    public string Email { get; set; } = "http://localhost:5004";

    public string LogWriter { get; set; } = "http://localhost:5002";

    public string UserProfile { get; set; } = "http://localhost:5104";

    public string PreBudget { get; set; } = "http://localhost:5010";

    public string Ecl { get; set; } = "http://localhost:5011";

    public string Reporting { get; set; } = "http://localhost:5003";
}
