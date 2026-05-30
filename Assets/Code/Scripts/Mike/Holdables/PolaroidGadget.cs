using PurrNet;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PolaroidGadget : PlayerItem, IInteractable<PlayerBody>
{
    public static readonly string PICTURE_META_KEY = "Picture";
    [SerializeField] private ItemData pictureItemData;
    [SerializeField] private Vector2Int textureSize = new(256, 256);
    public Task<bool> CanInteract(PlayerBody interactor) => Task.FromResult(!interactor.Inventory.IsInventoryFull());
    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        Debug.Log("Took Snapshot");
        Texture2D snapshot = await TakeSnapshot(interactor.PlayerCamera, textureSize.x, textureSize.y);
        return await RegisterPicture_ServerRpc(interactor, snapshot.EncodeToPNG());
    }

    [ServerRpc] private Task<bool> RegisterPicture_ServerRpc(PlayerBody interactor, byte[] snaphot)
    {
        Dictionary<string, object> metadata = new()
        {
            [PICTURE_META_KEY] = snaphot
        };

        Debug.Log("Received Rpc");
        bool success = interactor.Inventory.TryAddExact(pictureItemData, 1, metadata);
        Debug.Log("Image Added Succesffully" + success);
        return Task.FromResult(success);
    }

    private async Task<Texture2D> TakeSnapshot(Camera camera, int width = 256, int height = 256)
    {
        // Create a temporary RenderTexture for the camera to render into
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
        camera.targetTexture = rt;
        camera.Render();

        Debug.Log("Did camera Render");

        // Read pixels from the RenderTexture into a Texture2D
        RenderTexture.active = rt;
        Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        snapshot.Apply();

        Debug.Log("Applied Texture");

        // Clean up
        camera.targetTexture = null;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Debug.Log("Released Temporary");

        return snapshot;
    }
}
