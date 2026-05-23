namespace FoggyBalrog.Collections.UnitTests;

public class CircularBufferTests
{
    [Fact(DisplayName = "Constructor validates capacity")]
    public void Constructor_WhenCapacityIsNotPositive_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(-1));
    }

    [Fact(DisplayName = "Constructor defaults to overwrite behavior")]
    public void Constructor_WhenFullBehaviorOmitted_ShouldOverwrite()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);

        Assert.Equal([2, 3, 4], buffer);
    }

    [Fact(DisplayName = "Constructor rejects undefined full behavior")]
    public void Constructor_WhenFullBehaviorIsUndefined_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CircularBuffer<int>(3, (CircularBufferFullBehavior)42));
    }

    [Fact(DisplayName = "Push and pop work from both ends without wraparound")]
    public void PushAndPop_WhenNotWrapped_ShouldUseLogicalFrontToBackOrder()
    {
        var buffer = new CircularBuffer<int>(5);

        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushFront(1);
        buffer.PushFront(0);

        Assert.Equal(4, buffer.Count);
        Assert.False(buffer.IsEmpty);
        Assert.False(buffer.IsFull);
        Assert.Equal([0, 1, 2, 3], buffer);
        Assert.Equal(0, buffer.PeekFront());
        Assert.Equal(3, buffer.PeekBack());
        Assert.Equal(0, buffer.PopFront());
        Assert.Equal(3, buffer.PopBack());
        Assert.Equal([1, 2], buffer);
    }

    [Fact(DisplayName = "Push and pop work after internal wraparound")]
    public void PushAndPop_WhenWrapped_ShouldPreserveLogicalOrder()
    {
        var buffer = new CircularBuffer<int>(4);

        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);
        Assert.Equal(1, buffer.PopFront());
        Assert.Equal(2, buffer.PopFront());

        buffer.PushBack(5);
        buffer.PushBack(6);

        Assert.Equal([3, 4, 5, 6], buffer);
        Assert.Equal(3, buffer.PopFront());
        Assert.Equal(6, buffer.PopBack());
        Assert.Equal([4, 5], buffer);
    }

    [Fact(DisplayName = "Overwrite behavior drops front on PushBack and back on PushFront")]
    public void Push_WhenFullAndOverwriteBehavior_ShouldDropOppositeEnd()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);

        Assert.Equal([2, 3, 4], buffer);
        Assert.True(buffer.IsFull);

        buffer.PushFront(1);

        Assert.Equal([1, 2, 3], buffer);
        Assert.True(buffer.IsFull);
    }

    [Fact(DisplayName = "Throw behavior rejects full pushes without mutation")]
    public void Push_WhenFullAndThrowBehavior_ShouldThrowWithoutMutation()
    {
        var buffer = new CircularBuffer<int>(2, CircularBufferFullBehavior.Throw);
        buffer.PushBack(1);
        buffer.PushBack(2);

        Assert.Throws<InvalidOperationException>(() => buffer.PushBack(3));
        Assert.Equal([1, 2], buffer);

        Assert.Throws<InvalidOperationException>(() => buffer.PushFront(0));
        Assert.Equal([1, 2], buffer);
    }

    [Fact(DisplayName = "Indexer gets and sets logical indexes after wraparound")]
    public void Indexer_WhenWrapped_ShouldUseLogicalIndexes()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);
        buffer.PopFront();
        buffer.PopFront();
        buffer.PushBack(5);
        buffer.PushBack(6);

        Assert.Equal(3, buffer[0]);
        Assert.Equal(6, buffer[3]);

        buffer[1] = 40;
        buffer[3] = 60;

        Assert.Equal([3, 40, 5, 60], buffer);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[buffer.Count]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[buffer.Count] = 7);
    }

    [Fact(DisplayName = "Empty pop and peek APIs throw or return false consistently")]
    public void EmptyBuffer_WhenPoppedOrPeeked_ShouldThrowOrReturnFalse()
    {
        var buffer = new CircularBuffer<string>(2);

        Assert.Throws<InvalidOperationException>(() => buffer.PopFront());
        Assert.Throws<InvalidOperationException>(() => buffer.PopBack());
        Assert.Throws<InvalidOperationException>(() => buffer.PeekFront());
        Assert.Throws<InvalidOperationException>(() => buffer.PeekBack());
        Assert.False(buffer.TryPopFront(out string? frontPop));
        Assert.False(buffer.TryPopBack(out string? backPop));
        Assert.False(buffer.TryPeekFront(out string? frontPeek));
        Assert.False(buffer.TryPeekBack(out string? backPeek));
        Assert.Null(frontPop);
        Assert.Null(backPop);
        Assert.Null(frontPeek);
        Assert.Null(backPeek);
    }

    [Fact(DisplayName = "Try APIs return items when buffer is not empty")]
    public void TryApis_WhenBufferHasItems_ShouldReturnTrue()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);

        Assert.True(buffer.TryPeekFront(out int frontPeek));
        Assert.Equal(1, frontPeek);
        Assert.True(buffer.TryPeekBack(out int backPeek));
        Assert.Equal(2, backPeek);
        Assert.True(buffer.TryPopBack(out int backPop));
        Assert.Equal(2, backPop);
        Assert.True(buffer.TryPopFront(out int frontPop));
        Assert.Equal(1, frontPop);
        Assert.True(buffer.IsEmpty);
    }

    [Fact(DisplayName = "CopyTo validates destination and copies front to back")]
    public void CopyTo_WhenCalled_ShouldValidateAndCopyInLogicalOrder()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);
        buffer.PopFront();
        buffer.PopFront();
        buffer.PushBack(5);
        buffer.PushBack(6);
        int[] destination = [-1, -1, -1, -1, -1, -1];

        buffer.CopyTo(destination, 1);

        Assert.Equal([-1, 3, 4, 5, 6, -1], destination);
        Assert.Throws<ArgumentNullException>(() => buffer.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyTo(destination, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CopyTo(destination, destination.Length + 1));
        Assert.Throws<ArgumentException>(() => buffer.CopyTo(destination, 3));
    }

    [Fact(DisplayName = "Clear resets state and allows reuse")]
    public void Clear_WhenCalled_ShouldResetStateAndAllowReuse()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        buffer.Clear();

        Assert.Empty(buffer);
        Assert.Equal(3, buffer.Capacity);
        Assert.True(buffer.IsEmpty);
        Assert.False(buffer.IsFull);

        buffer.PushFront(4);
        buffer.PushBack(5);

        Assert.Equal([4, 5], buffer);
    }

    [Fact(DisplayName = "Reference-containing buffers release removed and cleared slots")]
    public void References_WhenRemovedOverwrittenOrCleared_ShouldBeCollectible()
    {
        WeakReference poppedReference = CreatePoppedReference();
        WeakReference overwrittenReference = CreateOverwrittenReference();
        WeakReference clearedReference = CreateClearedReference();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(poppedReference.IsAlive);
        Assert.False(overwrittenReference.IsAlive);
        Assert.False(clearedReference.IsAlive);
    }

    private static WeakReference CreatePoppedReference()
    {
        var buffer = new CircularBuffer<object>(2);
        object item = new();
        var reference = new WeakReference(item);
        buffer.PushBack(item);
        item = null!;
        buffer.PopFront();
        return reference;
    }

    private static WeakReference CreateOverwrittenReference()
    {
        var buffer = new CircularBuffer<object>(2);
        object item = new();
        var reference = new WeakReference(item);
        buffer.PushBack(item);
        buffer.PushBack(new object());
        item = null!;
        buffer.PushBack(new object());
        return reference;
    }

    private static WeakReference CreateClearedReference()
    {
        var buffer = new CircularBuffer<object>(2);
        object item = new();
        var reference = new WeakReference(item);
        buffer.PushBack(item);
        item = null!;
        buffer.Clear();
        return reference;
    }
}
