using Api.Domain;

namespace Api.Tests;

public class MeetingOverlapTests
{
    [Fact]
    public void OverlapsWith_WhenDifferentDays_ReturnsFalse()
    {
        var m1 = new Meeting
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        var m2 = new Meeting
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        Assert.False(m1.OverlapsWith(m2));
        Assert.False(m2.OverlapsWith(m1));
    }

    [Fact]
    public void OverlapsWith_WhenIdenticalIntervalOnSameDay_ReturnsTrue()
    {
        var m1 = new Meeting
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        var m2 = new Meeting
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        Assert.True(m1.OverlapsWith(m2));
        Assert.True(m2.OverlapsWith(m1));
    }

    [Fact]
    public void OverlapsWith_WhenPartialOverlap_ReturnsTrue()
    {
        var m1 = new Meeting
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        var m2 = new Meeting
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(9, 30),
            EndTime = new TimeOnly(11, 30)
        };

        Assert.True(m1.OverlapsWith(m2));
        Assert.True(m2.OverlapsWith(m1));
    }

    [Fact]
    public void OverlapsWith_WhenEnclosingInterval_ReturnsTrue()
    {
        var outer = new Meeting
        {
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0)
        };

        var inner = new Meeting
        {
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30)
        };

        Assert.True(outer.OverlapsWith(inner));
        Assert.True(inner.OverlapsWith(outer));
    }

    [Fact]
    public void OverlapsWith_WhenAdjacentIntervals_ReturnsFalse()
    {
        // Meeting 1 ends exactly when Meeting 2 starts
        var m1 = new Meeting
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        var m2 = new Meeting
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(10, 30),
            EndTime = new TimeOnly(12, 30)
        };

        Assert.False(m1.OverlapsWith(m2));
        Assert.False(m2.OverlapsWith(m1));
    }

    [Fact]
    public void OverlapsWith_WhenDisjointIntervals_ReturnsFalse()
    {
        var m1 = new Meeting
        {
            DayOfWeek = DayOfWeek.Thursday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        var m2 = new Meeting
        {
            DayOfWeek = DayOfWeek.Thursday,
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(15, 0)
        };

        Assert.False(m1.OverlapsWith(m2));
        Assert.False(m2.OverlapsWith(m1));
    }

    [Fact]
    public void OverlapsWith_WhenOtherIsNull_ReturnsFalse()
    {
        var m1 = new Meeting
        {
            DayOfWeek = DayOfWeek.Saturday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30)
        };

        Assert.False(m1.OverlapsWith(null));
    }
}
