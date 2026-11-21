using JobFitAI.Web.Services;

namespace JobFitAI.Tests;

[TestClass]
public sealed class MockFitAnalyserTests
{
    private readonly MockFitAnalyser _analyser = new();

    [TestMethod]
    public async Task Finds_matches_and_gaps()
    {
        var result = await _analyser.AnalyseAsync(TestData.JobAd, TestData.Cv);

        var matched = result.Matches.Select(m => m.Requirement).ToList();
        var missing = result.Gaps.Select(g => g.Requirement).ToList();
        Assert.IsTrue(matched.Contains("C#"));
        Assert.IsTrue(matched.Contains("Git and version control"));
        Assert.IsTrue(missing.Contains("React"));
        Assert.IsTrue(missing.Contains("Cloud (Azure or AWS)"));
        Assert.AreEqual("mock", result.Source);
    }

    [TestMethod]
    public async Task Score_is_the_percentage_of_required_skills_found()
    {
        var result = await _analyser.AnalyseAsync(TestData.JobAd, TestData.Cv);

        var required = result.Matches.Count + result.Gaps.Count;
        var expected = (int)Math.Round(100.0 * result.Matches.Count / required);
        Assert.AreEqual(expected, result.Score);
    }

    [TestMethod]
    public async Task Cv_with_every_skill_scores_100_and_apply()
    {
        var result = await _analyser.AnalyseAsync("We need C# and SQL.", "I use C# and SQL every day.");

        Assert.AreEqual(100, result.Score);
        Assert.AreEqual("apply", result.Verdict);
        Assert.AreEqual(0, result.Gaps.Count);
    }

    [TestMethod]
    public async Task Cv_with_no_matching_skills_scores_0_and_skip()
    {
        var result = await _analyser.AnalyseAsync("We need Python and Docker.", "I enjoy painting and hiking.");

        Assert.AreEqual(0, result.Score);
        Assert.AreEqual("skip", result.Verdict);
        Assert.AreEqual(0, result.Matches.Count);
    }

    [TestMethod]
    public async Task Advert_without_known_skills_gets_a_neutral_score()
    {
        var result = await _analyser.AnalyseAsync("Friendly person wanted for a fun role.", TestData.Cv);

        Assert.AreEqual(50, result.Score);
        Assert.AreEqual(0, result.Matches.Count + result.Gaps.Count);
    }

    [TestMethod]
    public async Task Matching_ignores_capital_letters()
    {
        var result = await _analyser.AnalyseAsync("Experience with PYTHON required.", "I write python scripts.");

        Assert.AreEqual(100, result.Score);
    }
}
