using UnityEngine;

public class LabUIBridge : MonoBehaviour
{
    [Header("Ссылки")]
    public EmitterController emitterController;
    public RayTracer rayTracer;
    public OpticalMedium targetMedium;

    public void SetIncidentAngle(float value)
    {
        if (emitterController == null)
        {
            return;
        }

        emitterController.incidentAngle = value;
        emitterController.ApplyCurrentAngle();

        if (rayTracer != null)
        {
            rayTracer.TraceRay();
        }
    }

    public void SetRefractiveIndex(float value)
    {
        if (targetMedium == null)
        {
            return;
        }

        targetMedium.refractiveIndex = Mathf.Max(1f, value);

        if (rayTracer != null)
        {
            rayTracer.TraceRay();
        }
    }

    public void SetAbsorption(float value)
    {
        if (targetMedium == null)
        {
            return;
        }

        targetMedium.absorption = Mathf.Max(0f, value);

        if (rayTracer != null)
        {
            rayTracer.TraceRay();
        }
    }

    public void SetMaxInteractions(float value)
    {
        if (rayTracer == null)
        {
            return;
        }

        rayTracer.maxInteractions = Mathf.Max(1, Mathf.RoundToInt(value));
        rayTracer.TraceRay();
    }

    public void SetTraceMode(int mode)
    {
        if (rayTracer == null)
        {
            return;
        }

        mode = Mathf.Clamp(mode, 0, 2);
        rayTracer.traceMode = (RayTracer.TraceMode)mode;
        rayTracer.TraceRay();
    }

    public void TraceNow()
    {
        if (rayTracer != null)
        {
            rayTracer.TraceRay();
        }
    }
}
