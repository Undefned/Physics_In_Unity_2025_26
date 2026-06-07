using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeisenbergSimulation : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider leftClampSlider;
    public Slider rightClampSlider;
    
    [Header("UI Display")]
    public TMP_Text deltaXText;
    public TMP_Text deltaPText;
    public TMP_Text waveTypeText;  // Добавляем текст для отображения текущего типа
    
    [Header("Scene Objects")]
    public Transform particle;
    public Transform leftClamp;
    public Transform rightClamp;
    
    [Header("Graphs")]
    public LineRenderer positionGraph;
    public LineRenderer momentumGraph;
    
    [Header("Graph Settings")]
    public int graphPoints = 200;
    public float graphWidth = 6f;
    public float positionGraphY = -2.5f;
    public float momentumGraphY = 2.5f;
    
    [Header("Physics")]
    public float minClampDistance = 0.4f;
    public float hbar = 1f;
    public float jitterStrength = 14f;
    public float velocityDamping = 0.995f;
    
    [Header("Graph Colors")]
    public Color positionGraphColor = Color.cyan;
    public Color momentumGraphColor = Color.green;
    
    [Header("Particle Color")]
    public float particleMass = 1f;
    public float momentumForMaxRed = 6f;
    public float colorLerpSpeed = 6f;
    public Color lowMomentumColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color highMomentumColor = Color.red;
    
    public enum WavePacketType
    {
        Gaussian,
        Exponential,
        Rectangular
    }
    
    private WavePacketType wavePacketType = WavePacketType.Gaussian;
    private float velocity;
    private float particleX;
    private SpriteRenderer particleSpriteRenderer;
    private Renderer particleRenderer;
    
    void Start()
    {
        // Настройка слайдеров
        SetupSliders();
        
        // Настройка графиков
        SetupGraphs();
        
        // Инициализация частицы
        particleX = particle.position.x;
        particleSpriteRenderer = particle.GetComponent<SpriteRenderer>();
        particleRenderer = particle.GetComponent<Renderer>();
        SetParticleColor(lowMomentumColor);
        
        // Начальные позиции клэмпов
        UpdateClampsFromSliders();
        UpdateWaveTypeText();
    }
    
    void SetupSliders()
    {
        leftClampSlider.minValue = -5f;
        leftClampSlider.maxValue = 0f;
        leftClampSlider.onValueChanged.AddListener(OnLeftClampChanged);
        
        rightClampSlider.minValue = 0f;
        rightClampSlider.maxValue = 5f;
        rightClampSlider.onValueChanged.AddListener(OnRightClampChanged);
        
        leftClampSlider.value = -2f;
        rightClampSlider.value = 2f;
    }
    
    void SetupGraphs()
    {
        positionGraph.startColor = positionGraphColor;
        positionGraph.endColor = positionGraphColor;
        momentumGraph.startColor = momentumGraphColor;
        momentumGraph.endColor = momentumGraphColor;
        
        positionGraph.material = new Material(Shader.Find("Sprites/Default"));
        momentumGraph.material = new Material(Shader.Find("Sprites/Default"));
        
        positionGraph.positionCount = graphPoints;
        momentumGraph.positionCount = graphPoints;
    }
    
    void OnLeftClampChanged(float value)
    {
        float rightX = rightClampSlider.value;
        if (value + minClampDistance > rightX)
        {
            value = rightX - minClampDistance;
            leftClampSlider.value = value;
        }
        
        leftClamp.position = new Vector3(value, leftClamp.position.y, 0);
    }
    
    void OnRightClampChanged(float value)
    {
        float leftX = leftClampSlider.value;
        if (value - minClampDistance < leftX)
        {
            value = leftX + minClampDistance;
            rightClampSlider.value = value;
        }
        
        rightClamp.position = new Vector3(value, rightClamp.position.y, 0);
    }
    
    void UpdateClampsFromSliders()
    {
        leftClamp.position = new Vector3(leftClampSlider.value, leftClamp.position.y, 0);
        rightClamp.position = new Vector3(rightClampSlider.value, rightClamp.position.y, 0);
    }
    
    void Update()
    {
        UpdateClampsFromSliders();
        UpdateParticle();
        DrawGraphs();
        UpdateUIText();
    }
    
    void UpdateParticle()
    {
        float deltaX = Mathf.Abs(rightClamp.position.x - leftClamp.position.x);
        if (deltaX < 0.01f) deltaX = 0.01f;
        
        float deltaP = hbar / (2f * deltaX);
        
        velocity += Random.Range(-deltaP, deltaP) * Time.deltaTime * jitterStrength;
        velocity *= velocityDamping;
        
        particleX += velocity * Time.deltaTime;
        
        if (particleX < leftClamp.position.x)
        {
            particleX = leftClamp.position.x;
            velocity = Mathf.Abs(velocity) * 0.5f;
        }
        
        if (particleX > rightClamp.position.x)
        {
            particleX = rightClamp.position.x;
            velocity = -Mathf.Abs(velocity) * 0.5f;
        }
        
        particle.position = new Vector3(particleX, particle.position.y, 0);
        UpdateParticleColor();
    }
    
    void DrawGraphs()
    {
        float deltaX = Mathf.Abs(rightClamp.position.x - leftClamp.position.x);
        if (deltaX < 0.01f) deltaX = 0.01f;
        float deltaP = hbar / (2f * deltaX);
        
        DrawPositionGraph(deltaX);
        DrawMomentumGraph(deltaP);
    }
    
    void DrawPositionGraph(float deltaX)
    {
        float center = (leftClamp.position.x + rightClamp.position.x) / 2f;
        
        for (int i = 0; i < graphPoints; i++)
        {
            float t = i / (float)(graphPoints - 1);
            float x = -graphWidth / 2f + t * graphWidth;
            float value = GetPositionProbability(x, center, deltaX);
            positionGraph.SetPosition(i, new Vector3(x, positionGraphY + value, 0));
        }
    }
    
    void DrawMomentumGraph(float deltaP)
    {
        float width = deltaP * 8f + 0.2f;
        
        for (int i = 0; i < graphPoints; i++)
        {
            float t = i / (float)(graphPoints - 1);
            float p = -graphWidth / 2f + t * graphWidth;
            float value = GetMomentumProbability(p, width);
            momentumGraph.SetPosition(i, new Vector3(p, momentumGraphY + value, 0));
        }
    }
    
    float GetPositionProbability(float x, float center, float deltaX)
    {
        float dx = x - center;
        
        switch (wavePacketType)
        {
            case WavePacketType.Gaussian:
                return Mathf.Exp(-(dx * dx) / (2f * deltaX * deltaX)) * 1.2f;
            case WavePacketType.Exponential:
                return Mathf.Exp(-Mathf.Abs(dx) / deltaX) * 1.2f;
            case WavePacketType.Rectangular:
                return Mathf.Abs(dx) < deltaX / 2f ? 1.0f : 0.0f;
            default:
                return 0;
        }
    }
    
    float GetMomentumProbability(float p, float width)
    {
        switch (wavePacketType)
        {
            case WavePacketType.Gaussian:
                return Mathf.Exp(-(p * p) / (2f * width * width)) * 1.2f;
            case WavePacketType.Exponential:
                return 1f / (1f + p * p / (width * width)) * 1.2f;
            case WavePacketType.Rectangular:
                float sinc = Mathf.Abs(p) < 0.001f ? 1f : Mathf.Sin(p / width) / (p / width);
                return Mathf.Abs(sinc) * 1.2f;
            default:
                return 0;
        }
    }
    
    void UpdateUIText()
    {
        float deltaX = Mathf.Abs(rightClamp.position.x - leftClamp.position.x);
        float deltaP = hbar / (2f * deltaX);
        
        deltaXText.text = $"Δx = {deltaX:F3}";
        deltaPText.text = $"Δp = {deltaP:F3}";
    }
    
    void UpdateWaveTypeText()
    {
        if (waveTypeText != null)
        {
            switch (wavePacketType)
            {
                case WavePacketType.Gaussian:
                    waveTypeText.text = "Пакет: Гаусс";
                    break;
                case WavePacketType.Exponential:
                    waveTypeText.text = "Пакет: Экспоненциальный";
                    break;
                case WavePacketType.Rectangular:
                    waveTypeText.text = "Пакет: Прямоугольный";
                    break;
            }
        }
    }
    
    // КНОПКИ - их ты вызываешь из UI
    public void SetGaussian()
    {
        wavePacketType = WavePacketType.Gaussian;
        UpdateWaveTypeText();
        Debug.Log("Выбран Гауссовский пакет");
    }
    
    public void SetExponential()
    {
        wavePacketType = WavePacketType.Exponential;
        UpdateWaveTypeText();
        Debug.Log("Выбран Экспоненциальный пакет");
    }
    
    public void SetRectangular()
    {
        wavePacketType = WavePacketType.Rectangular;
        UpdateWaveTypeText();
        Debug.Log("Выбран Прямоугольный пакет");
    }
    
    void UpdateParticleColor()
    {
        float momentum = Mathf.Abs(velocity) * particleMass;
        float t = Mathf.InverseLerp(0f, momentumForMaxRed, momentum);
        Color targetColor = Color.Lerp(lowMomentumColor, highMomentumColor, t);
        Color currentColor = GetParticleColor();
        Color smoothColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);
        SetParticleColor(smoothColor);
    }
    
    Color GetParticleColor()
    {
        if (particleSpriteRenderer != null)
            return particleSpriteRenderer.color;
        if (particleRenderer != null && particleRenderer.material != null)
            return particleRenderer.material.color;
        return lowMomentumColor;
    }
    
    void SetParticleColor(Color color)
    {
        if (particleSpriteRenderer != null)
            particleSpriteRenderer.color = color;
        if (particleRenderer != null && particleRenderer.material != null)
            particleRenderer.material.color = color;
    }
}