
using PurrNet;
using System.Collections;
using UnityEngine;

public class LightBrokenAnomaly : NetworkBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField] private float normalIntensity = 10f;

    private Material mat;
    private Coroutine anomalyLoop;

    private void Awake()
    {
        // Cache material
        if (!targetRenderer)
            targetRenderer = GetComponent<Renderer>();

        mat = targetRenderer.material;
        SetEmission(normalIntensity);
    }

    // Called when anomaly starts
    public void EnableAnomaly()
    {
        StartCoroutine(StartAfterApply());
    }

    private IEnumerator StartAfterApply()
    {
        // Wait for ModificationApplier.Apply()
        yield return null;

        if (anomalyLoop != null)
            StopCoroutine(anomalyLoop);

        anomalyLoop = StartCoroutine(AnomalyLoop());
    }

    // Called when anomaly ends
    public void DisableAnomaly()
    {
        if (anomalyLoop != null)
        {
            StopCoroutine(anomalyLoop);
            anomalyLoop = null;
        }

        SetEmission(normalIntensity);
    }
    //Networking RPCs
    [ServerRpc]
    private void StartAnomalyServerRpc()
    {
        StartAnomalyObserversRpc();
    }

    [ServerRpc]
    private void StopAnomalyServerRpc()
    {
        StopAnomalyObserversRpc();
    }

    [ObserversRpc]
    private void StartAnomalyObserversRpc()
    {
        StartCoroutine(StartAfterApply());
    }

    [ObserversRpc]
    private void StopAnomalyObserversRpc()
    {
        if (anomalyLoop != null)
        {
            StopCoroutine(anomalyLoop);
            anomalyLoop = null;
        }

        SetEmission(normalIntensity);
    }

    // Broken light effect
    private IEnumerator AnomalyLoop()
    {
        while (true)
        {
            SetEmission(normalIntensity);
            yield return new WaitForSeconds(0.15f);

            SetEmission(0f);
            yield return new WaitForSeconds(0.06f);

            SetEmission(normalIntensity);
            yield return new WaitForSeconds(0.08f);

            SetEmission(0f);
            yield return new WaitForSeconds(0.04f);

            SetEmission(normalIntensity * 0.5f);
            yield return new WaitForSeconds(0.1f);

            SetEmission(normalIntensity);

            // Delay until next flicker burst
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        }
    }

    private void SetEmission(float intensity)
    {
        if (!mat) return;

        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor * intensity);
    }
}