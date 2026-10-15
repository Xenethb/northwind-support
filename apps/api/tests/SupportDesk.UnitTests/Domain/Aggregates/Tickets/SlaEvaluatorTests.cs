
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaEvaluatorTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    private static readonly DateTime CreatedAt =
        Now.AddHours(-1);

    [Fact]
    public void Evaluate_WithoutADueDate_IsNotApplicable()
    {
        var result = SlaEvaluator.Evaluate(
            null,
            null,
            CreatedAt,
            Now);

        Assert.Equal(SlaStatus.NotApplicable, result);
    }

    [Fact]
    public void Evaluate_ResolvedOnTime_IsMet()
    {
        var result = SlaEvaluator.Evaluate(
            Now.AddDays(1),
            Now,
            CreatedAt,
            Now);

        Assert.Equal(SlaStatus.Met, result);
    }

    [Fact]
    public void Evaluate_ResolvedLate_IsBreached()
    {
        var result = SlaEvaluator.Evaluate(
            Now,
            Now.AddMinutes(1),
            CreatedAt,
            Now);

        Assert.Equal(SlaStatus.Breached, result);
    }

    [Fact]
    public void Evaluate_OpenAndPastDue_IsBreached()
    {
        var result = SlaEvaluator.Evaluate(
            Now.AddMinutes(-1),
            null,
            CreatedAt,
            Now);

        Assert.Equal(SlaStatus.Breached, result);
    }

    [Fact]
    public void Evaluate_OpenAndBeforeDue_IsWithinSla()
    {
        var result = SlaEvaluator.Evaluate(
            Now.AddHours(1),
            null,
            CreatedAt,
            Now);

        Assert.Equal(SlaStatus.WithinSla, result);
    }

    [Fact]
    public void Evaluate_OpenAndNearDue_IsAtRisk()
    {
        var result = SlaEvaluator.Evaluate(
            Now.AddMinutes(10),
            null,
            Now.AddMinutes(-90),
            Now,
            25);

        Assert.Equal(SlaStatus.AtRisk, result);
    }
}
