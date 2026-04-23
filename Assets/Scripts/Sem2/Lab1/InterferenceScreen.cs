using UnityEngine;

public class InterferenceScreen : MonoBehaviour
{
    [Header("Screen resolution")]
    public int resolution = 512;

    [Header("Screen position")]
    public float screenDistance = 5f;

    [Header("Source positions (X)")]
    public float source1X = -1f;
    public float source2X =  1f;

    [Header("Wavelengths")]
    public float wavelength1 = 0.5f;
    public float wavelength2 = 0.5f;

    [Header("Source amplitudes")]
    public float amplitude1 = 1f;
    public float amplitude2 = 1f;

    Texture2D texture;

    void Start()
    {
        texture = new Texture2D(resolution, 1);
        texture.filterMode = FilterMode.Bilinear;
        GetComponent<Renderer>().material.mainTexture = texture;
        UpdatePattern();
    }

    void Update()
    {
        UpdatePattern();
    }

    void UpdatePattern()
    {
        for (int i = 0; i < resolution; i++)
        {
            float x = Mathf.Lerp(-5f, 5f, (float)i / (resolution - 1));

            float r1 = Mathf.Sqrt((x - source1X) * (x - source1X) +
                                   screenDistance * screenDistance);

            float r2 = Mathf.Sqrt((x - source2X) * (x - source2X) +
                                   screenDistance * screenDistance);

            float phase1 = 2f * Mathf.PI * r1 / wavelength1;
            float phase2 = 2f * Mathf.PI * r2 / wavelength2;

            float phaseDifference = phase2 - phase1;

            float intensity =
                amplitude1 * amplitude1 +
                amplitude2 * amplitude2 +
                2f * amplitude1 * amplitude2 * Mathf.Cos(phaseDifference);

            intensity /= (amplitude1 + amplitude2) * (amplitude1 + amplitude2);

            Color color = new Color(intensity, intensity, intensity);
            texture.SetPixel(i, 0, color);
        }

        texture.Apply();
    }

    // ===== UI METHODS =====

    public void SetWavelength1(float value)
    {
        wavelength1 = value;
    }

    public void SetWavelength2(float value)
    {
        wavelength2 = value;
    }

    public void SetSource1X(float value)
    {
        source1X = value;
    }

    public void SetSource2X(float value)
    {
        source2X = value;
    }

    public void SetScreenDistance(float value)
    {
        screenDistance = value;
    }

    public void SetAmplitude1(float value)
    {
        amplitude1 = value;
    }

    public void SetAmplitude2(float value)
    {
        amplitude2 = value;
    }
}