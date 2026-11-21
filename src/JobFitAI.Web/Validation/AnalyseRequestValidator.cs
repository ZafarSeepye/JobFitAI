using JobFitAI.Web.Models;

namespace JobFitAI.Web.Validation;

/// <summary>Checks an incoming request before any analysis runs.
/// The browser validates too, but the server never trusts the browser.</summary>
public static class AnalyseRequestValidator
{
    public const int MinChars = 200;
    public const int MaxChars = 8000;

    /// <summary>Returns field name → error messages. An empty dictionary means the request is valid.</summary>
    public static Dictionary<string, string[]> Validate(AnalyseRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["request"] = ["Send a JSON body with jobAd and cv."];
            return errors;
        }

        Check(request.JobAd, "jobAd", "job advert", errors);
        Check(request.Cv, "cv", "CV", errors);
        return errors;
    }

    private static void Check(string? value, string field, string label, Dictionary<string, string[]> errors)
    {
        var text = value?.Trim() ?? "";

        if (text.Length == 0)
            errors[field] = [$"Paste the {label}."];
        else if (text.Length < MinChars)
            errors[field] = [$"The {label} is too short to analyse. Paste at least {MinChars} characters."];
        else if (text.Length > MaxChars)
            errors[field] = [$"The {label} is too long. Keep it under {MaxChars:N0} characters."];
    }
}
