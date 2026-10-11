using Statecraft.Sim;

public class OpenEventHorizonTests
{
    const string OpenEvent = @"id: horizon_event
title: '{prime_minister} decides'
text: '{prime_minister} must decide.'
from: 1948-06-01
options:
  - id: accept
    text: Accept
";

    static Content Load(string? horizon, string? death = null, string? ev = null)
    {
        var rules = Fixture.Files["rules.yaml"] + (horizon == null ? "" : $"\nopen_events_until: {horizon}\n");
        var people = Fixture.Files["people.yaml"];
        if (death != null) people = people.Replace("    name: Pm One\n", $"    name: Pm One\n    died: {death}\n");
        return ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = rules, ["people.yaml"] = people },
                                               new Dictionary<string, string> { ["horizon.yaml"] = ev ?? OpenEvent }));
    }

    [Fact]
    public void WithoutAHorizonOpenEventsStillNeedIndefiniteCoverage()
    {
        var ex = Assert.Throws<ContentException>(() => Load(null, "1949-06-01"));
        Assert.Contains("has no holder on 1949-06-01", ex.Message);
    }

    [Fact]
    public void ADeathInsideTheDeclaredWindowStillFailsCoverage()
    {
        var ex = Assert.Throws<ContentException>(() => Load("1949-06-01", "1949-05-31"));
        Assert.Contains("has no holder on 1949-05-31", ex.Message);
    }

    [Theory]
    [InlineData("1949-06-01")]
    [InlineData("1949-06-02")]
    public void ADeathAtOrAfterTheExclusiveHorizonDoesNotExtendTheWindow(string death)
    {
        var c = Load("1949-06-01", death);
        Assert.Equal(new DateTime(1949, 6, 1), c.Events.Single().To);
        Assert.Null(Offices.Holder(c, "prime_minister", DateTime.Parse(death)));
        Assert.Contains("Pm One", EventSystem.Render(c, c.Events.Single(), new DateTime(1950, 1, 1)));
    }

    [Fact]
    public void AnUnfiredEventCannotFirstTriggerAfterTheHorizon()
    {
        var c = Load("1949-06-01", "1949-06-01");
        var s = new GameState { Date = new DateTime(1949, 6, 1) };
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Empty(s.PendingEvents);
    }

    [Fact]
    public void ACrossingTurnAndPendingChoiceUseTheLastInWindowHolder()
    {
        var c = Load("1949-06-01", "1949-06-01");
        var s = new GameState { Date = new DateTime(1949, 6, 2) };
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why, new DateTime(1949, 5, 31));
        Assert.Equal(new[] { "horizon_event" }, s.PendingEvents);
        Assert.Contains(why, w => w.Message == "New: Pm One decides");
        Assert.Null(EventSystem.Choose(c, s, "horizon_event", "accept", why));
        Assert.Contains(why, w => w.Message == "Chosen: Accept");
    }

    [Fact]
    public void AnExplicitWindowIsNotClippedByTheOpenEventHorizon()
    {
        var ev = OpenEvent.Replace("from: 1948-06-01", "from: 1949-07-01\nto: 1949-08-01");
        var c = Load("1949-06-01", ev: ev);
        Assert.Equal(new DateTime(1949, 8, 1), c.Events.Single().To);
    }

    [Theory]
    [InlineData("1948-06-01", "from: 1948-06-01")]
    [InlineData("1948-06-01", "from: 1948-07-01")]
    public void TheHorizonMustBeAfterEveryOtherwiseOpenEventStart(string horizon, string from)
    {
        var ex = Assert.Throws<ContentException>(() => Load(horizon, ev: OpenEvent.Replace("from: 1948-06-01", from)));
        Assert.Equal("events[horizon_event].to", ex.Field);
        Assert.Contains("starts on or after rules.yaml open_events_until (1948-06-01)", ex.Message);
    }

    [Fact]
    public void TheHorizonMustBeAfterTheGameStart()
    {
        var ex = Assert.Throws<ContentException>(() => Load("1948-05-15", ev: OpenEvent.Replace("from: 1948-06-01", "from: 1948-01-01")));
        Assert.Equal("open_events_until", ex.Field);
    }
}
