using UnityEngine;

public class LabScenePreset : MonoBehaviour
{
    [Header("Scene References")]
    public Transform mainCamera;
    public Transform emitter;
    public EmitterController emitterController;
    public RayTracer rayTracer;

    [Header("Media")]
    public Transform glass;
    public OpticalMedium glassMedium;
    public Transform water;
    public OpticalMedium waterMedium;
    public Transform ground;

    [ContextMenu("Apply Snell Preset")]
    public void ApplySnellPreset()
    {
        if (ground != null)
        {
            ground.position = new Vector3(0f, 0f, 0f);
            ground.rotation = Quaternion.identity;
            ground.localScale = new Vector3(2f, 1f, 2f);
            ground.gameObject.SetActive(true);
        }

        if (glass != null)
        {
            glass.position = new Vector3(0f, 1f, 5f);
            glass.rotation = Quaternion.identity;
            glass.localScale = new Vector3(6f, 2f, 6f);
            glass.gameObject.SetActive(true);
        }

        if (glassMedium != null)
        {
            glassMedium.mediumName = "Glass";
            glassMedium.refractiveIndex = 1.5f;
            glassMedium.externalRefractiveIndex = 1.0f;
            glassMedium.absorption = 0.0f;
        }

        if (water != null)
        {
            water.gameObject.SetActive(false);
        }

        if (emitter != null)
        {
            emitter.position = new Vector3(-4f, 1f, 0f);
            emitter.rotation = Quaternion.identity;
        }

        if (emitterController != null)
        {
            emitterController.baseDirection = Vector3.forward;
            emitterController.rotationAxis = Vector3.up;
            emitterController.incidentAngle = 30f;
            emitterController.ApplyCurrentAngle();
        }

        if (rayTracer != null)
        {
            rayTracer.traceMode = RayTracer.TraceMode.FullOptics;
            rayTracer.defaultRefractiveIndex = 1.0f;
            rayTracer.maxInteractions = 12;
            rayTracer.maxDistance = 60f;
            rayTracer.rayOffset = 0.002f;
            rayTracer.TraceRay();
        }

        if (mainCamera != null)
        {
            mainCamera.position = new Vector3(0f, 6f, -10f);
            mainCamera.rotation = Quaternion.Euler(20f, 0f, 0f);
        }
    }

    [ContextMenu("Apply TIR Preset")]
    public void ApplyTirPreset()
    {
        if (ground != null)
        {
            ground.position = new Vector3(0f, 0f, 0f);
            ground.rotation = Quaternion.identity;
            ground.localScale = new Vector3(2f, 1f, 2f);
            ground.gameObject.SetActive(false);
        }

        if (glass != null)
        {
            glass.position = new Vector3(0f, 1f, 5f);
            glass.rotation = Quaternion.identity;
            glass.localScale = new Vector3(6f, 2f, 6f);
            glass.gameObject.SetActive(true);
        }

        if (glassMedium != null)
        {
            glassMedium.mediumName = "Glass";
            glassMedium.refractiveIndex = 1.5f;
            glassMedium.externalRefractiveIndex = 1.0f;
            glassMedium.absorption = 0.0f;
        }

        if (water != null)
        {
            water.gameObject.SetActive(false);
        }

        if (emitter != null)
        {
            emitter.position = new Vector3(0f, 1f, 5f);
            emitter.rotation = Quaternion.identity;
        }

        if (emitterController != null)
        {
            emitterController.baseDirection = Vector3.right;
            emitterController.rotationAxis = Vector3.up;
            emitterController.incidentAngle = 45f;
            emitterController.ApplyCurrentAngle();
        }

        if (rayTracer != null)
        {
            rayTracer.traceMode = RayTracer.TraceMode.FullOptics;
            rayTracer.defaultRefractiveIndex = 1.0f;
            rayTracer.maxInteractions = 12;
            rayTracer.maxDistance = 60f;
            rayTracer.rayOffset = 0.002f;
            rayTracer.TraceRay();
        }

        if (mainCamera != null)
        {
            mainCamera.position = new Vector3(0f, 5f, -6f);
            mainCamera.rotation = Quaternion.Euler(25f, 0f, 0f);
        }
    }
}
