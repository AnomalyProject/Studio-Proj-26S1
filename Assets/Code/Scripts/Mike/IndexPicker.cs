using System;

/// <summary>
/// Construct with the Length of a collection. 
/// <see cref="GetNext"/> will always return a random unused index from that collection avoiding repetition.
/// </summary>
public class IndexPicker
{
    int[] availableIndices;
    int current = 0;
    int lastUsed = -1;
    public int Length => availableIndices.Length;

    public IndexPicker(int collectionLength) => Reset(collectionLength);

    public int GetNext()
    {
        if (current >= availableIndices.Length) Reshuffle();

        lastUsed = availableIndices[current++];
        return lastUsed;
    }

    public void Reshuffle()
    {
        current = 0;
        availableIndices.Shuffle();

        if (availableIndices[0] == lastUsed) availableIndices.Swap(0, availableIndices.Length - 1);
    }

    public void Reset(int collectionLength)
    {
        if (collectionLength <= 0) throw new ArgumentException("Collection length must be > 0");

        availableIndices = new int[collectionLength];
        for (int i = 0; i < availableIndices.Length; i++) availableIndices[i] = i;
        Reshuffle();
    }
}