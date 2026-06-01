using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

public class PolaroidPicture : PlayerItem
{
    [SerializeField] private RawImage imageUI;

    protected override async void OnSpawned()
    {
        base.OnSpawned();
        if (TryGetMeta<byte[]>(PolaroidGadget.PICTURE_META_KEY, out byte[] pngBytes)) await PrintPicture(pngBytes);
    }

    private async Task PrintPicture(byte[] pngBytes)
    {
        Texture2D imageTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        imageTexture.LoadImage(pngBytes);
        imageUI.texture = imageTexture;
    }
}