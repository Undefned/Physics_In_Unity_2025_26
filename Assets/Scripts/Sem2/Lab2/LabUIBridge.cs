using UnityEngine;

public class LabUIBridge : MonoBehaviour
{
    [Header("Ссылки")]
    public EmitterController emitterController;
    public RayTracer rayTracer;
    public OpticalMedium targetMedium;

    public void SetIncidentAngle(float value)
    {
        emitterController.incidentAngle = value;
        emitterController.ApplyCurrentAngle();
    }

    public void SetRefractiveIndex(float value)
    {
        targetMedium.refractiveIndex = Mathf.Max(1f, value);
    }

    public void SetAbsorption(float value)
    {
        targetMedium.absorption = Mathf.Max(0f, value);
    }

    public void SetMaxInteractions(float value)
    {
        rayTracer.maxInteractions = Mathf.Max(1, Mathf.RoundToInt(value));
        rayTracer.TraceRay();
    }

    public void SetTraceMode(int mode)
    {
        mode = Mathf.Clamp(mode, 0, 2);
        rayTracer.traceMode = (RayTracer.TraceMode)mode;
        rayTracer.TraceRay();
    }
}
