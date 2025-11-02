namespace Broker.Core.Routing;


////TopicTrie (fast topic matching)
//// Broker.Core/Routing/TopicTrie.cs (simple implementation)
//public class TopicTrie : ITopicMatcher
//{
//    private class Node
//    {
//        public Dictionary<string, Node> Children = new();
//        public List<SubscriptionEntry> Subscribers = new();
//    }

//    private readonly Node _root = new();

//    public void Add(string topicFilter, SubscriptionEntry entry)
//    {
//        var parts = topicFilter.Split('/');
//        var node = _root;
//        foreach (var p in parts)
//        {
//            if (!node.Children.TryGetValue(p, out var next))
//            {
//                next = new Node();
//                node.Children[p] = next;
//            }
//            node = next;
//        }
//        node.Subscribers.Add(entry);
//    }

//    public void Remove(string topicFilter, string clientId)
//    {
//        // simple remove (non-recursive prune omitted)
//        var parts = topicFilter.Split('/');
//        var node = _root;
//        foreach (var p in parts)
//        {
//            if (!node.Children.TryGetValue(p, out var next)) return;
//            node = next;
//        }
//        node.Subscribers.RemoveAll(s => s.ClientId == clientId);
//    }

//    public IEnumerable<SubscriptionEntry> Match(string topic)
//    {
//        // naive recursive match supporting + and # wildcards; implement efficient DFS
//        var result = new List<SubscriptionEntry>();
//        var parts = topic.Split('/');
//        MatchRecursive(_root, parts, 0, result);
//        return result;
//    }

//    private void MatchRecursive(Node node, string[] parts, int idx, List<SubscriptionEntry> result)
//    {
//        if (node == null) return;
//        if (idx == parts.Length)
//        {
//            result.AddRange(node.Subscribers);
//            if (node.Children.TryGetValue("#", out var sharpNode)) result.AddRange(sharpNode.Subscribers);
//            return;
//        }

//        var part = parts[idx];
//        // direct match
//        if (node.Children.TryGetValue(part, out var child)) MatchRecursive(child, parts, idx + 1, result);
//        // single-level +
//        if (node.Children.TryGetValue("+", out var plus)) MatchRecursive(plus, parts, idx + 1, result);
//        // multi-level #
//        if (node.Children.TryGetValue("#", out var sharp)) result.AddRange(sharp.Subscribers);
//    }
//}
//public interface ITopicMatcher
//{
//    void Add(string topicFilter, SubscriptionEntry entry);
//    void Remove(string topicFilter, string clientId);
//    IEnumerable<SubscriptionEntry> Match(string topic);
//}

//public sealed class TopicTrie : ITopicMatcher
//{
//    private sealed class Node
//    {
//        public ConcurrentDictionary<string, Node> Children { get; } = new();
//        public ConcurrentBag<SubscriptionEntry> Subscribers { get; } = new();
//    }

//    private readonly Node _root = new();

//    public void Add(string topicFilter, SubscriptionEntry entry)
//    {
//        var parts = topicFilter.Split('/');
//        var node = _root;
//        foreach (var p in parts)
//        {
//            node = node.Children.GetOrAdd(p, _ => new Node());
//        }
//        node.Subscribers.Add(entry);
//    }

//    public void Remove(string topicFilter, string clientId)
//    {
//        var parts = topicFilter.Split('/');
//        var node = _root;
//        foreach (var p in parts)
//        {
//            if (!node.Children.TryGetValue(p, out var next)) return;
//            node = next;
//        }
//        var survivors = node.Subscribers.Where(s => s.ClientId != clientId).ToArray();
//        // rebuild bag (cheap but simple)
//        while (!node.Subscribers.IsEmpty) node.Subscribers.TryTake(out _);
//        foreach (var s in survivors) node.Subscribers.Add(s);
//    }

//    public IEnumerable<SubscriptionEntry> Match(string topic)
//    {
//        var parts = topic.Split('/');
//        var result = new List<SubscriptionEntry>();
//        MatchRecursive(_root, parts, 0, result);
//        return result;
//    }

//    private void MatchRecursive(Node node, string[] parts, int idx, List<SubscriptionEntry> outList)
//    {
//        if (node == null) return;
//        if (idx == parts.Length)
//        {
//            outList.AddRange(node.Subscribers);
//            if (node.Children.TryGetValue("#", out var sharp)) outList.AddRange(sharp.Subscribers);
//            return;
//        }

//        var part = parts[idx];
//        if (node.Children.TryGetValue(part, out var child)) MatchRecursive(child, parts, idx + 1, outList);
//        if (node.Children.TryGetValue("+", out var plus)) MatchRecursive(plus, parts, idx + 1, outList);
//        if (node.Children.TryGetValue("#", out var sharp2)) outList.AddRange(sharp2.Subscribers);
//    }
//}

////TopicTrie (fast topic matching)
//// Broker.Core/Routing/TopicTrie.cs (simple implementation)
//public class TopicTrie : ITopicMatcher
//{
//    private class Node
//    {
//        public Dictionary<string, Node> Children = new();
//        public List<SubscriptionEntry> Subscribers = new();
//    }

//    private readonly Node _root = new();

//    public void Add(string topicFilter, SubscriptionEntry entry)
//    {
//        var parts = topicFilter.Split('/');
//        var node = _root;
//        foreach (var p in parts)
//        {
//            if (!node.Children.TryGetValue(p, out var next))
//            {
//                next = new Node();
//                node.Children[p] = next;
//            }
//            node = next;
//        }
//        node.Subscribers.Add(entry);
//    }

//    public void Remove(string topicFilter, string clientId)
//    {
//        // simple remove (non-recursive prune omitted)
//        var parts = topicFilter.Split('/');
//        var node = _root;
//        foreach (var p in parts)
//        {
//            if (!node.Children.TryGetValue(p, out var next)) return;
//            node = next;
//        }
//        node.Subscribers.RemoveAll(s => s.ClientId == clientId);
//    }

//    public IEnumerable<SubscriptionEntry> Match(string topic)
//    {
//        // naive recursive match supporting + and # wildcards; implement efficient DFS
//        var result = new List<SubscriptionEntry>();
//        var parts = topic.Split('/');
//        MatchRecursive(_root, parts, 0, result);
//        return result;
//    }

//    private void MatchRecursive(Node node, string[] parts, int idx, List<SubscriptionEntry> result)
//    {
//        if (node == null) return;
//        if (idx == parts.Length)
//        {
//            result.AddRange(node.Subscribers);
//            if (node.Children.TryGetValue("#", out var sharpNode)) result.AddRange(sharpNode.Subscribers);
//            return;
//        }

//        var part = parts[idx];
//        // direct match
//        if (node.Children.TryGetValue(part, out var child)) MatchRecursive(child, parts, idx + 1, result);
//        // single-level +
//        if (node.Children.TryGetValue("+", out var plus)) MatchRecursive(plus, parts, idx + 1, result);
//        // multi-level #
//        if (node.Children.TryGetValue("#", out var sharp)) result.AddRange(sharp.Subscribers);
//    }
//}