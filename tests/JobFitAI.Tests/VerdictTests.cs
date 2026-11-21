using JobFitAI.Web.Models;

namespace JobFitAI.Tests;

[TestClass]
public sealed class VerdictTests
{
    // The boundaries are where bugs hide, so test both sides of each one.
    [TestMethod]
    [DataRow(0, "skip")]
    [DataRow(44, "skip")]
    [DataRow(45, "stretch")]
    [DataRow(69, "stretch")]
    [DataRow(70, "apply")]
    [DataRow(100, "apply")]
    public void Score_maps_to_the_right_verdict(int score, string expected)
    {
        Assert.AreEqual(expected, Verdicts.FromScore(score));
    }
}
