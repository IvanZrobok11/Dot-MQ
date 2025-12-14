using Broker.Core.Routing;
using FluentAssertions;

namespace TestProject1.Routing;

public class TopicMatcherTests
{
    [Fact]
    public void Matches_ExactMatch_ShouldReturnTrue()
    {
        TopicMatcher.Matches("sensors/temperature", "sensors/temperature").Should().BeTrue();
    }

    [Fact]
    public void Matches_DifferentTopics_ShouldReturnFalse()
    {
        TopicMatcher.Matches("sensors/temperature", "sensors/humidity").Should().BeFalse();
    }

    [Fact]
    public void Matches_SingleLevelWildcard_ShouldMatch()
    {
        TopicMatcher.Matches("sensors/+/temperature", "sensors/livingroom/temperature").Should().BeTrue();
        TopicMatcher.Matches("sensors/+/temperature", "sensors/kitchen/temperature").Should().BeTrue();
        TopicMatcher.Matches("sensors/+/temperature", "sensors/temperature").Should().BeFalse();
    }

    [Fact]
    public void Matches_MultiLevelWildcard_ShouldMatch()
    {
        TopicMatcher.Matches("sensors/#", "sensors/temperature").Should().BeTrue();
        TopicMatcher.Matches("sensors/#", "sensors/livingroom/temperature").Should().BeTrue();
        TopicMatcher.Matches("sensors/#", "sensors/kitchen/humidity/current").Should().BeTrue();
        TopicMatcher.Matches("sensors/#", "sensors").Should().BeTrue();
    }

    [Fact]
    public void Matches_MultiLevelWildcardAtEnd_ShouldMatch()
    {
        TopicMatcher.Matches("home/+/#", "home/kitchen/temperature").Should().BeTrue();
        TopicMatcher.Matches("home/+/#", "home/kitchen/sensors/temperature").Should().BeTrue();
    }

    [Fact]
    public void Matches_MultiLevelWildcardNotAtEnd_ShouldReturnFalse()
    {
        // # must be the last level
        TopicMatcher.Matches("sensors/#/temperature", "sensors/livingroom/temperature").Should().BeFalse();
    }

    [Fact]
    public void Matches_EmptyStrings_ShouldReturnFalse()
    {
        TopicMatcher.Matches("", "sensors/temperature").Should().BeFalse();
        TopicMatcher.Matches("sensors/temperature", "").Should().BeFalse();
        TopicMatcher.Matches("", "").Should().BeFalse();
    }

    [Fact]
    public void Matches_NullStrings_ShouldReturnFalse()
    {
        TopicMatcher.Matches(null!, "sensors/temperature").Should().BeFalse();
        TopicMatcher.Matches("sensors/temperature", null!).Should().BeFalse();
    }

    [Fact]
    public void IsValidTopicFilter_ValidFilters_ShouldReturnTrue()
    {
        TopicMatcher.IsValidTopicFilter("sensors/temperature").Should().BeTrue();
        TopicMatcher.IsValidTopicFilter("sensors/+").Should().BeTrue();
        TopicMatcher.IsValidTopicFilter("sensors/#").Should().BeTrue();
        TopicMatcher.IsValidTopicFilter("+/temperature").Should().BeTrue();
    }

    [Fact]
    public void IsValidTopicFilter_InvalidFilters_ShouldReturnFalse()
    {
        TopicMatcher.IsValidTopicFilter("").Should().BeFalse();
        TopicMatcher.IsValidTopicFilter(null!).Should().BeFalse();
        TopicMatcher.IsValidTopicFilter("sensors/#/temperature").Should().BeFalse(); // # not at end
        TopicMatcher.IsValidTopicFilter("sensors/temp+").Should().BeFalse(); // + not entire level
        TopicMatcher.IsValidTopicFilter("sensors/temp#").Should().BeFalse(); // # not entire level
    }

    [Fact]
    public void IsValidTopicName_ValidNames_ShouldReturnTrue()
    {
        TopicMatcher.IsValidTopicName("sensors/temperature").Should().BeTrue();
        TopicMatcher.IsValidTopicName("home/kitchen/sensors/temperature").Should().BeTrue();
    }

    [Fact]
    public void IsValidTopicName_InvalidNames_ShouldReturnFalse()
    {
        TopicMatcher.IsValidTopicName("").Should().BeFalse();
        TopicMatcher.IsValidTopicName(null!).Should().BeFalse();
        TopicMatcher.IsValidTopicName("sensors/+").Should().BeFalse(); // Contains wildcard
        TopicMatcher.IsValidTopicName("sensors/#").Should().BeFalse(); // Contains wildcard
    }
}

