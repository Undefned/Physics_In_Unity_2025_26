using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public class SnellLawExperiment : MonoBehaviour
{
    [Header("Ссылки")]
    public EmitterController emitterController;
    public RayTracer rayTracer;

    [Header("Параметры серии")]
    [Range(0f, 89f)] public float startAngle = 0f;
    [Range(0f, 89f)] public float endAngle = 80f;
    [Range(1f, 30f)] public float step = 5f;

    [Header("Результат (CSV)")]
    [TextArea(6, 18)]
    public string csvResult;

    [System.Serializable]
    public struct SampleRow
    {
        public float incidentAngle;
        public float refractedAngle;
        public float n1;
        public float n2;
        public bool tir;
    }

    public List<SampleRow> rows = new List<SampleRow>();

    [ContextMenu("Run Snell Series")]
    public void RunSeries()
    {
        if (emitterController == null || rayTracer == null)
        {
            Debug.LogWarning("SnellLawExperiment: назначьте emitterController и rayTracer.");
            return;
        }

        rows.Clear();

        float originalAngle = emitterController.incidentAngle;

        for (float a = startAngle; a <= endAngle + 0.001f; a += step)
        {
            emitterController.incidentAngle = a;
            emitterController.ApplyCurrentAngle();
            rayTracer.TraceRay();

            if (rayTracer.lastSamples.Count == 0)
            {
                continue;
            }

            RayTracer.InteractionSample s = rayTracer.lastSamples[0];
            rows.Add(new SampleRow
            {
                incidentAngle = s.incidentAngle,
                refractedAngle = s.refractedAngle,
                n1 = s.n1,
                n2 = s.n2,
                tir = s.totalInternalReflection,
            });
        }

        emitterController.incidentAngle = originalAngle;
        emitterController.ApplyCurrentAngle();
        rayTracer.TraceRay();

        csvResult = BuildCsv(rows);
        Debug.Log(csvResult);
    }

    private string BuildCsv(List<SampleRow> sampleRows)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("incident_deg,refracted_deg,n1,n2,total_internal_reflection");

        for (int i = 0; i < sampleRows.Count; i++)
        {
            SampleRow r = sampleRows[i];
            sb.Append(r.incidentAngle.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.refractedAngle.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.n1.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.n2.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
            sb.AppendLine(r.tir ? "1" : "0");
        }

        return sb.ToString();
    }
}
