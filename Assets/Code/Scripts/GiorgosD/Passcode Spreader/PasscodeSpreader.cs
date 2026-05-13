using System.Collections.Generic;
using System.Linq;
using PurrNet;
using TMPro;
using UnityEngine;

public class PasscodeSpreader : NetworkBehaviour
{
    [SerializeField] private TextMeshPro[] texts;

    private void Awake()
    {
        DisableTexts();
    }

    #region Sread logic
    /// <summary>
    /// Spreads the input between different TextMeshes (Adds the remainder of the input to the last mesh) and Shuffles them.
    /// </summary>
    /// <param name="input"> its a string that OnPasswordGenerated passes. </param>
    public void Spread(string input)
    {
        if (!isServer) return;
        if (texts == null || texts.Length == 0 || string.IsNullOrEmpty(input)) return;
        
        List<int> indices = Enumerable.Range(0, texts.Length).ToList();
        
        ShufleIndices(indices);

        SyncPasscodes(indices.ToArray(), input);  // Made it a ToArray() cause i heard array is easier than list to pass on network
    }

    /// <summary>
    /// Shufles the indices (idk what you expected).
    /// </summary>
    /// <param name="indices"></param>
    private void ShufleIndices(List<int> indices)
    {
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[randIndex];
            indices[randIndex] = temp;
        }
    }
    #endregion

    #region  Network Sync
    /// <summary>
    /// Syncs the Passcode TextMeshes on Network.
    /// </summary>
    [ObserversRpc(bufferLast: true)]
    private void SyncPasscodes(int[] indices, string input)
    {
        DisableTexts();
        
        int textLength = input.Length;
        int meshCount = texts.Length;

        for (int i = 0; i < meshCount; i++)
        {
            if (i >= textLength) break;
            
            int targetIndex = indices[i];
            TextMeshPro textTarget = texts[targetIndex];

            if (i == meshCount - 1 && textLength > meshCount)
            {
                textTarget.text = input.Substring(i);
            }
            else
            {
                textTarget.text = input[i].ToString();
            }
            
            textTarget.gameObject.SetActive(true);
        }
    }
    #endregion
    
    #region DisableTextMeshes
    /// <summary>
    /// Disables all TextMeshes for the passcode spreader
    /// </summary>
    private void DisableTexts()
    {
        foreach (TextMeshPro text in texts)
        {
            if (text != null)
            {
                text.text = string.Empty;
                text.gameObject.SetActive(false);
            }
        }
    }
    #endregion
}
