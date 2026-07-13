using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PurrNet;
using TMPro;

public class PasscodeSpreader : NetworkBehaviour
{
    [SerializeField, Tooltip("For Tutorial: digits are assigned to the TextMeshes in order.")] private bool orderedSpread = false;
    [SerializeField] private TextMeshPro[] texts;
    [SerializeField] private bool applySymbols = true;

    private void Awake() => DisableTexts();

    #region Sread logic
    /// <summary>
    /// Spreads the input between different TextMeshes (Adds the remainder of the input to the last mesh) and Shuffles them.
    /// </summary>
    /// <param name="input"> its a string that OnPasswordGenerated passes. </param>
    public void Spread(KeypadInteractable.PasswordData passwordData)
    {
        if (!isServer) return;
        if (texts == null || texts.Length == 0 || string.IsNullOrEmpty(passwordData.digits)) return;
        
        int[] indices = Enumerable.Range(0, texts.Length).ToArray();
        
        ShufleIndices(indices);
        SyncPasscodes(indices, passwordData);
    }

    /// <summary>
    /// Shufles the indices (idk what you expected).
    /// </summary>
    /// <param name="indices"></param>
    private void ShufleIndices(IList<int> indices)
    {
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int randIndex = orderedSpread ? i : Random.Range(0, i + 1);
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
    private void SyncPasscodes(int[] indices, KeypadInteractable.PasswordData passwordData)
    {
        DisableTexts();
        
        int textLength = passwordData.digits.Length;
        int meshCount = texts.Length;

        for (int i = 0; i < meshCount; i++)
        {
            if (i >= textLength) break;
            
            int targetIndex = indices[i];
            TextMeshPro textTarget = texts[targetIndex];

            if (i == meshCount - 1 && textLength > meshCount)
            {
                textTarget.text = passwordData.digits.Substring(i);
                // Place symbols next to each digit
                if (applySymbols) for (int j = i; j < textLength; j++) textTarget.text = textTarget.text.Insert(j, $"<sprite={passwordData.glyphIndicies[j]}>");
            }
            else textTarget.text = passwordData.digits[i].ToString() + (applySymbols ? $"<sprite={passwordData.glyphIndicies[i]}>" : string.Empty);
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
