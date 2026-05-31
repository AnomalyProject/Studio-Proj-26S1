using PurrNet;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PolaroidGadget : PlayerItem, IInteractable<PlayerBody>
{
    #region Metadata Keys
    public static readonly string PICTURE_META_KEY = "Picture";
    const string CHARGES_META_KEY = "Charges";
    #endregion

    #region Serialized Fields
    [SerializeField] private int totalCharges = 4;
    [SerializeField, Min(.1f)] private float cooldownSeconds = 1;
    [SerializeField] private ItemData pictureItemData;
    [SerializeField] private Vector2Int textureSize = new(256, 256);
    [SerializeField] private RawImage previewDisplay;
    [SerializeField] private TextMeshProUGUI chargeDisplay;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Light flash;
    [SerializeField, Min(.1f)] private float flashLifetime = .2f;
    [SerializeField] private UnityEvent OnPictureTaken;
    #endregion

    SyncVar<int> remainingCharges = new SyncVar<int>(-1, ownerAuth: false);
    private RenderTexture _renderTexture;
    private float currentCooldown;

    #region Unity Lifecycle
    private void OnEnable()
    {
        _renderTexture = new RenderTexture(textureSize.x, textureSize.y, 24);
        previewCamera.targetTexture = _renderTexture;
        previewCamera.enabled = true;
        previewDisplay.texture = _renderTexture;
        flash.enabled = false;
    }
    private void OnDisable()
    {
        previewCamera.targetTexture = null;
        _renderTexture.Release();
        Destroy(_renderTexture);
        CancelInvoke(nameof(CloseFlash));
    }
    private void Update()
    {
        if (currentCooldown <= 0) return;
        currentCooldown -= Time.deltaTime;
    }
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if(!asServer && isOwner)
        {
            remainingCharges.onChanged += HandleChargesChangedVisuals;
            HandleChargesChangedVisuals(remainingCharges.value);
        }

        if (!asServer) return;

        remainingCharges.value = GetMeta<int>(CHARGES_META_KEY, totalCharges);
        remainingCharges.onChanged += (value) =>
        {
            SetMeta_Server(CHARGES_META_KEY, value);
        };
    }
    #endregion

    #region Interaction
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        bool requirementsMet = interactor.Inventory.EmptySlots > 0 && remainingCharges.value > 0 && currentCooldown <= 0;
        return Task.FromResult(requirementsMet);
    }
    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        bool canInteract = await CanInteract(interactor);
        if (!canInteract) return false;

        currentCooldown = cooldownSeconds;
        Texture2D snapshot = await ReadSnapshotAsync();
        if (snapshot == null) return false;
        return await RegisterPicture_ServerRpc(interactor, snapshot.EncodeToPNG());
    }
    #endregion

    #region Syncing
    [ServerRpc] private Task<bool> RegisterPicture_ServerRpc(PlayerBody interactor, byte[] snapshot)
    {
        Dictionary<string, object> metadata = new() 
        { 
            [PICTURE_META_KEY] = snapshot 
        };

        bool success = interactor.Inventory.TryAddExact(pictureItemData, 1, metadata);

        if (success)
        {
            remainingCharges.value--;
            InvokeOnPictureTaken();
        }
        return Task.FromResult(success);
    }
    [ObserversRpc] private void InvokeOnPictureTaken() => OnPictureTaken.Invoke();
    #endregion

    #region Helpers
    private void HandleChargesChangedVisuals(int value)
    {
        chargeDisplay.text = value.ToString();

        bool enoughCharges = value > 0;
        previewDisplay.enabled = enoughCharges;
        previewCamera.enabled = enoughCharges;
        chargeDisplay.enabled = enoughCharges;
    }
    private async Task<Texture2D> ReadSnapshotAsync()
    {
        var tcs = new TaskCompletionSource<Texture2D>();

        AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGBA32, request =>
        {
            if (request.hasError)
            {
                tcs.SetResult(null);
                return;
            }

            Texture2D snapshot = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
            snapshot.SetPixelData(request.GetData<byte>(), 0);
            snapshot.Apply();
            tcs.SetResult(snapshot);
        });

        return await tcs.Task;
    }
    public void DoFlash()
    {
        if (!flash) return;

        flash.enabled = true;
        Invoke(nameof(CloseFlash), flashLifetime);
    }
    private void CloseFlash() => flash.enabled = false;
    #endregion
}