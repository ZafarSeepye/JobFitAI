using JobFitAI.Web.Models;
using JobFitAI.Web.Validation;

namespace JobFitAI.Tests;

[TestClass]
public sealed class ValidatorTests
{
    [TestMethod]
    public void Valid_request_has_no_errors()
    {
        var errors = AnalyseRequestValidator.Validate(new AnalyseRequest(TestData.JobAd, TestData.Cv));

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Null_request_is_rejected()
    {
        var errors = AnalyseRequestValidator.Validate(null);

        Assert.IsTrue(errors.ContainsKey("request"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("     ")]
    public void Missing_job_ad_is_rejected(string? jobAd)
    {
        var errors = AnalyseRequestValidator.Validate(new AnalyseRequest(jobAd, TestData.Cv));

        Assert.IsTrue(errors.ContainsKey("jobAd"));
        Assert.IsFalse(errors.ContainsKey("cv"));
    }

    [TestMethod]
    public void Text_one_character_under_the_minimum_is_rejected()
    {
        var errors = AnalyseRequestValidator.Validate(
            new AnalyseRequest(TestData.OfLength(AnalyseRequestValidator.MinChars - 1), TestData.Cv));

        Assert.IsTrue(errors["jobAd"][0].Contains("too short"));
    }

    [TestMethod]
    public void Text_exactly_at_the_limits_is_accepted()
    {
        var errors = AnalyseRequestValidator.Validate(new AnalyseRequest(
            TestData.OfLength(AnalyseRequestValidator.MinChars),
            TestData.OfLength(AnalyseRequestValidator.MaxChars)));

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Text_one_character_over_the_maximum_is_rejected()
    {
        var errors = AnalyseRequestValidator.Validate(
            new AnalyseRequest(TestData.JobAd, TestData.OfLength(AnalyseRequestValidator.MaxChars + 1)));

        Assert.IsTrue(errors["cv"][0].Contains("too long"));
    }

    [TestMethod]
    public void Spaces_around_the_text_do_not_count_towards_the_minimum()
    {
        var padded = new string(' ', 500) + "short" + new string(' ', 500);

        var errors = AnalyseRequestValidator.Validate(new AnalyseRequest(padded, TestData.Cv));

        Assert.IsTrue(errors.ContainsKey("jobAd"));
    }
}
