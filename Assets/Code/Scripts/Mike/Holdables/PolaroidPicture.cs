using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PolaroidPicture : PlayerItem
{
    [SerializeField] private RawImage imageUI;

    protected override async void OnSpawned()
    {
        base.OnSpawned();

        if (TryGetMeta<byte[]>(PolaroidGadget.PICTURE_META_KEY, out byte[] pngBytes))
        {
            await PrintPicture(pngBytes);
        }
    }

    private async Task PrintPicture(byte[] pngBytes)
    {
        Debug.Log("Found Meta for Picture");
        Texture2D imageTexture = new(2, 2);
        imageTexture.LoadImage(pngBytes);
        imageUI.texture = imageTexture;
    }
}