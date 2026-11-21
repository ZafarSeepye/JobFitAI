namespace JobFitAI.Tests;

/// <summary>Shared inputs for the tests.</summary>
internal static class TestData
{
    public const string JobAd = """
        Junior Software Developer, Pretoria.
        We are looking for a junior developer to join our .NET team.
        Requirements: working knowledge of C# and .NET, experience with SQL and relational databases,
        an understanding of Git and version control, and the ability to write unit tests.
        Nice to have: experience with Azure or another cloud platform, and exposure to React.
        """;

    public const string Cv = """
        Final-year Software Development student.
        Skills: C#, .NET, ASP.NET Core, SQL Server, HTML, CSS, JavaScript, Git, unit testing with xUnit.
        Project: Clinic Booking System built with ASP.NET Core and a SQL Server database,
        with unit tests for the booking rules. Worked in a team of three using Git.
        """;

    /// <summary>A string of the given length, for boundary tests.</summary>
    public static string OfLength(int length) => new('a', length);
}
