namespace Broker.Core.Routing;

/// <summary>
/// Matches MQTT topic names against topic filters with wildcard support.
/// </summary>
public static class TopicMatcher
{
    /// <summary>
    /// Checks if a topic name matches a topic filter.
    /// </summary>
    /// <param name="topicFilter">The topic filter (may contain wildcards: +, #).</param>
    /// <param name="topicName">The topic name to match.</param>
    /// <returns>True if the topic name matches the filter, false otherwise.</returns>
    public static bool Matches(string topicFilter, string topicName)
    {
        if (string.IsNullOrEmpty(topicFilter) || string.IsNullOrEmpty(topicName))
        {
            return false;
        }

        // Exact match
        if (topicFilter == topicName)
        {
            return true;
        }

        // Split into levels
        string[] filterLevels = topicFilter.Split('/', StringSplitOptions.None);
        string[] topicLevels = topicName.Split('/', StringSplitOptions.None);

        int filterIndex = 0;
        int topicIndex = 0;

        while (filterIndex < filterLevels.Length && topicIndex < topicLevels.Length)
        {
            string filterLevel = filterLevels[filterIndex];
            string topicLevel = topicLevels[topicIndex];

            // Multi-level wildcard (#) - matches remaining topic levels
            if (filterLevel == "#")
            {
                // # must be the last level in the filter
                return filterIndex == filterLevels.Length - 1;
            }

            // Single-level wildcard (+) - matches exactly one level
            if (filterLevel == "+")
            {
                // Move to next level in both filter and topic
                filterIndex++;
                topicIndex++;
                continue;
            }

            // Exact level match
            if (filterLevel == topicLevel)
            {
                filterIndex++;
                topicIndex++;
                continue;
            }

            // No match
            return false;
        }

        // Both must be exhausted for a match
        // Exception: if filter ends with #, it can match zero or more levels
        if (filterIndex < filterLevels.Length)
        {
            // Check if remaining filter levels are just #
            if (filterIndex == filterLevels.Length - 1 && filterLevels[filterIndex] == "#")
            {
                return true;
            }
            return false;
        }

        // If topic has more levels but filter doesn't end with #, no match
        if (topicIndex < topicLevels.Length)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a topic filter according to MQTT specification.
    /// </summary>
    /// <param name="topicFilter">The topic filter to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValidTopicFilter(string topicFilter)
    {
        if (string.IsNullOrEmpty(topicFilter))
        {
            return false;
        }

        // Topic filter cannot be longer than 65535 bytes (UTF-8 encoded)
        if (System.Text.Encoding.UTF8.GetByteCount(topicFilter) > 65535)
        {
            return false;
        }

        string[] levels = topicFilter.Split('/', StringSplitOptions.None);

        for (int i = 0; i < levels.Length; i++)
        {
            string level = levels[i];

            // # can only appear as the last level
            if (level == "#" && i != levels.Length - 1)
            {
                return false;
            }

            // + and # cannot appear together in the same level
            if (level.Contains('+') && level.Contains('#'))
            {
                return false;
            }

            // + must be the entire level (cannot be part of a string)
            if (level.Contains('+') && level != "+")
            {
                return false;
            }

            // # must be the entire level (cannot be part of a string)
            if (level.Contains('#') && level != "#")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates a topic name according to MQTT specification.
    /// </summary>
    /// <param name="topicName">The topic name to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValidTopicName(string topicName)
    {
        if (string.IsNullOrEmpty(topicName))
        {
            return false;
        }

        // Topic name cannot be longer than 65535 bytes (UTF-8 encoded)
        if (System.Text.Encoding.UTF8.GetByteCount(topicName) > 65535)
        {
            return false;
        }

        // Topic names cannot contain wildcards
        return !(topicName.Contains('+') || topicName.Contains('#'));
    }
}

