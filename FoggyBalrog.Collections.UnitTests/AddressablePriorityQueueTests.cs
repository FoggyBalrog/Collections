namespace FoggyBalrog.Collections.UnitTests;

public class AddressablePriorityQueueTests
{
    [Fact(DisplayName = "Dequeue returns keys by ascending priority")]
    public void Dequeue_WhenItemsEnqueued_ShouldReturnAscendingPriorities()
    {
        var queue = new AddressablePriorityQueue<string, int>();

        queue.Enqueue("slow", 30);
        queue.Enqueue("fast", 10);
        queue.Enqueue("middle", 20);

        Assert.Equal("fast", queue.Dequeue(out int firstPriority));
        Assert.Equal(10, firstPriority);
        Assert.Equal("middle", queue.Dequeue(out int secondPriority));
        Assert.Equal(20, secondPriority);
        Assert.Equal("slow", queue.Dequeue(out int thirdPriority));
        Assert.Equal(30, thirdPriority);
    }

    [Fact(DisplayName = "Peek and dequeue APIs report empty queues consistently")]
    public void EmptyQueue_WhenPeekedOrDequeued_ShouldThrowOrReturnFalse()
    {
        var queue = new AddressablePriorityQueue<string, int>();

        Assert.Throws<InvalidOperationException>(() => queue.Peek());
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue(out _));
        Assert.False(queue.TryPeek(out _, out _));
        Assert.False(queue.TryDequeue(out _, out _));
    }

    [Fact(DisplayName = "TryPeek returns the current minimum without removing it")]
    public void TryPeek_WhenQueueHasItems_ShouldReturnMinimumWithoutRemoving()
    {
        var queue = new AddressablePriorityQueue<string, int>();
        queue.Enqueue("b", 2);
        queue.Enqueue("a", 1);

        Assert.True(queue.TryPeek(out string? key, out int priority));
        Assert.Equal("a", key);
        Assert.Equal(1, priority);
        Assert.Equal(2, queue.Count);
    }

    [Fact(DisplayName = "Duplicate keys are rejected or updated according to API contract")]
    public void DuplicateKeys_WhenInserted_ShouldFollowApiContract()
    {
        var queue = new AddressablePriorityQueue<string, int>();

        queue.Enqueue("a", 10);

        Assert.Throws<ArgumentException>(() => queue.Enqueue("a", 20));
        Assert.False(queue.TryEnqueue("a", 20));
        Assert.False(queue.EnqueueOrUpdate("a", 5));
        Assert.True(queue.TryGetPriority("a", out int priority));
        Assert.Equal(5, priority);
        Assert.Throws<ArgumentException>(() => new AddressablePriorityQueue<string, int>([("a", 1), ("a", 2)]));
    }

    [Fact(DisplayName = "UpdatePriority handles priority decreases and increases")]
    public void UpdatePriority_WhenPriorityChanges_ShouldRestoreHeapOrder()
    {
        var queue = new AddressablePriorityQueue<string, int>();
        queue.Enqueue("a", 10);
        queue.Enqueue("b", 20);
        queue.Enqueue("c", 30);

        queue.UpdatePriority("c", 1);

        Assert.Equal("c", queue.Peek());

        queue.UpdatePriority("c", 40);

        Assert.Equal("a", queue.Dequeue());
        Assert.Equal("b", queue.Dequeue());
        Assert.Equal("c", queue.Dequeue());
    }

    [Fact(DisplayName = "Compare-equal priority updates still store the new priority")]
    public void UpdatePriority_WhenComparerSaysPrioritiesAreEqual_ShouldStoreNewPriority()
    {
        var comparer = Comparer<int>.Create((x, y) => (x / 10).CompareTo(y / 10));
        var queue = new AddressablePriorityQueue<string, int>(comparer);

        queue.Enqueue("a", 11);
        queue.UpdatePriority("a", 19);

        Assert.True(queue.TryGetPriority("a", out int priority));
        Assert.Equal(19, priority);
    }

    [Fact(DisplayName = "Remove deletes arbitrary keys and reports removed priority")]
    public void Remove_WhenKeyExists_ShouldRemoveEntryAndReturnPriority()
    {
        var queue = new AddressablePriorityQueue<string, int>();
        queue.Enqueue("a", 10);
        queue.Enqueue("b", 20);
        queue.Enqueue("c", 5);
        queue.Enqueue("d", 15);

        Assert.True(queue.Remove("d", out int removedPriority));
        Assert.Equal(15, removedPriority);
        Assert.False(queue.Contains("d"));
        Assert.False(queue.Remove("missing", out _));

        Assert.Equal("c", queue.Dequeue());
        Assert.Equal("a", queue.Dequeue());
        Assert.Equal("b", queue.Dequeue());
    }

    [Fact(DisplayName = "Custom priority comparer can make the queue a max-heap")]
    public void CustomPriorityComparer_WhenDescending_ShouldDequeueLargestPriorityFirst()
    {
        var queue = new AddressablePriorityQueue<string, int>(Comparer<int>.Create((x, y) => y.CompareTo(x)));

        queue.Enqueue("small", 1);
        queue.Enqueue("large", 3);
        queue.Enqueue("middle", 2);

        Assert.Equal("large", queue.Dequeue());
        Assert.Equal("middle", queue.Dequeue());
        Assert.Equal("small", queue.Dequeue());
    }

    [Fact(DisplayName = "Custom key comparer controls key equality")]
    public void CustomKeyComparer_WhenCaseInsensitive_ShouldMatchEquivalentKeys()
    {
        var queue = new AddressablePriorityQueue<string, int>(
            initialCapacity: 0,
            priorityComparer: null,
            keyComparer: StringComparer.OrdinalIgnoreCase);

        queue.Enqueue("Alpha", 10);

        Assert.True(queue.Contains("alpha"));
        Assert.False(queue.TryEnqueue("ALPHA", 1));
        Assert.True(queue.TryUpdatePriority("alpha", 1));
        Assert.Equal("Alpha", queue.Dequeue());
    }

    [Fact(DisplayName = "Bulk constructor heapifies and supports non-collection enumerables")]
    public void Constructor_WhenGivenItems_ShouldHeapify()
    {
        static IEnumerable<(string Key, int Priority)> YieldItems()
        {
            yield return ("c", 3);
            yield return ("a", 1);
            yield return ("b", 2);
        }

        var queue = new AddressablePriorityQueue<string, int>(YieldItems());

        Assert.Equal("a", queue.Dequeue());
        Assert.Equal("b", queue.Dequeue());
        Assert.Equal("c", queue.Dequeue());
    }

    [Fact(DisplayName = "Capacity APIs grow and trim without corrupting entries")]
    public void CapacityApis_WhenUsed_ShouldPreserveQueueContents()
    {
        var queue = new AddressablePriorityQueue<int, int>();

        int ensured = queue.EnsureCapacity(32);

        Assert.True(ensured >= 32);
        Assert.True(queue.Capacity >= 32);

        for (int i = 0; i < 5; i++)
        {
            queue.Enqueue(i, 5 - i);
        }

        queue.TrimExcess();

        Assert.True(queue.Capacity >= queue.Count);
        Assert.Equal(4, queue.Dequeue());
        Assert.Equal(3, queue.Dequeue());
        Assert.Equal(2, queue.Dequeue());
        Assert.Equal(1, queue.Dequeue());
        Assert.Equal(0, queue.Dequeue());
    }

    [Fact(DisplayName = "Random operations match a simple reference model")]
    public void RandomOperations_WhenComparedToReferenceModel_ShouldStayConsistent()
    {
        var queue = new AddressablePriorityQueue<int, int>();
        var reference = new Dictionary<int, int>();
        var random = new Random(42);

        for (int step = 0; step < 5_000; step++)
        {
            int key = random.Next(0, 200);
            int operation = random.Next(0, 5);

            if (operation == 0)
            {
                int priority = random.Next(-1_000, 1_000);
                bool expectedAdded = !reference.ContainsKey(key);
                bool actualAdded = queue.EnqueueOrUpdate(key, priority);
                reference[key] = priority;
                Assert.Equal(expectedAdded, actualAdded);
            }
            else if (operation == 1 && reference.Count > 0)
            {
                int existingKey = reference.Keys.ElementAt(random.Next(reference.Count));
                int priority = random.Next(-1_000, 1_000);
                Assert.True(queue.TryUpdatePriority(existingKey, priority));
                reference[existingKey] = priority;
            }
            else if (operation == 2 && reference.Count > 0)
            {
                int existingKey = reference.Keys.ElementAt(random.Next(reference.Count));
                Assert.True(queue.Remove(existingKey, out int removedPriority));
                Assert.Equal(reference[existingKey], removedPriority);
                reference.Remove(existingKey);
            }
            else if (operation == 3 && reference.Count > 0)
            {
                int expectedPriority = reference.Values.Min();
                Assert.True(queue.TryDequeue(out int dequeuedKey, out int dequeuedPriority));
                Assert.True(reference.ContainsKey(dequeuedKey));
                Assert.Equal(reference[dequeuedKey], dequeuedPriority);
                Assert.Equal(expectedPriority, dequeuedPriority);
                reference.Remove(dequeuedKey);
            }
            else
            {
                Assert.Equal(reference.Count, queue.Count);
            }

            Assert.Equal(reference.Count, queue.Count);
            foreach ((int referenceKey, int referencePriority) in reference)
            {
                Assert.True(queue.TryGetPriority(referenceKey, out int actualPriority));
                Assert.Equal(referencePriority, actualPriority);
            }

            if (reference.Count > 0)
            {
                Assert.True(queue.TryPeek(out int peekedKey, out int peekedPriority));
                Assert.True(reference.ContainsKey(peekedKey));
                Assert.Equal(reference[peekedKey], peekedPriority);
                Assert.Equal(reference.Values.Min(), peekedPriority);
            }
        }
    }
}
