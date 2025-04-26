using System;
using System.Collections.Generic;

/// <summary>
/// A simple priority queue that stores elements sorted by their priority (lower values = higher priority).
/// </summary>
public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private List<(TElement element, TPriority priority)> elements = new();
    public int Count => elements.Count;

    /// <summary>
    /// Adds an element to the queue with its associated priority.
    /// O(N) insertion (sorted).
    /// </summary>
    public void Enqueue(TElement item, TPriority priority)
    {
        int index = elements.FindIndex(e => priority.CompareTo(e.priority) < 0);
        if (index < 0)
            elements.Add((item, priority));
        else
            elements.Insert(index, (item, priority));
    }

    /// <summary>
    /// Removes and returns the element with the highest priority (lowest priority value).
    /// </summary>
    public TElement Dequeue()
    {
        if (elements.Count == 0)
            throw new InvalidOperationException("PriorityQueue is empty");

        TElement item = elements[0].element;
        elements.RemoveAt(0);
        return item;
    }

    /// <summary>
    /// Peeks at the element with the highest priority without removing it.
    /// </summary>
    public TElement Peek()
    {
        if (elements.Count == 0)
            throw new InvalidOperationException("PriorityQueue is empty");

        return elements[0].element;
    }

    /// <summary>
    /// Clears all elements from the queue.
    /// </summary>
    public void Clear() => elements.Clear();
}
