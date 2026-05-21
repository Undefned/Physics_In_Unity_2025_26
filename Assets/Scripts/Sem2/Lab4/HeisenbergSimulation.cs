using UnityEngine;
using TMPro;

public class HeisenbergSimulation : MonoBehaviour
{
    // UI-поля для отображения текущих значений неопределенностей и типа пакета.
    public TMP_Text deltaXText;
    public TMP_Text deltaPText;
    public TMP_Text waveTypeText;

    // Объекты сцены: частица и две границы, задающие область локализации.
    public Transform particle;
    public Transform leftClamp;
    public Transform rightClamp;

    // Линии графиков вероятностных распределений в координатном и импульсном представлениях.
    public LineRenderer positionGraph;
    public LineRenderer momentumGraph;

    public int graphPoints = 200;

    public float graphWidth = 6f;
    public float positionGraphY = -2.5f;
    public float momentumGraphY = 2.5f;

    public float minClampDistance = 0.4f;
    public float hbar = 1f;
    public Color positionGraphColor = Color.cyan;
    public Color momentumGraphColor = Color.green;
    public float jitterStrength = 14f;
    public float velocityDamping = 0.995f;

    [Header("Particle Color By Momentum")]
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

    public WavePacketType wavePacketType = WavePacketType.Gaussian;

    private float velocity;
    private float particleX;
    private SpriteRenderer particleSpriteRenderer;
    private Renderer particleRenderer;

    void Start()
    {
        // Настраиваем цвета и материалы графиков, чтобы линии корректно отображались.
        positionGraph.startColor = positionGraphColor;
        positionGraph.endColor = positionGraphColor;

        momentumGraph.startColor = momentumGraphColor;
        momentumGraph.endColor = momentumGraphColor;

        positionGraph.material = new Material(Shader.Find("Sprites/Default"));
        momentumGraph.material = new Material(Shader.Find("Sprites/Default"));
        positionGraph.positionCount = graphPoints;
        momentumGraph.positionCount = graphPoints;

        particleX = particle.position.x;
        particleSpriteRenderer = particle.GetComponent<SpriteRenderer>();
        particleRenderer = particle.GetComponent<Renderer>();

        // Начальный цвет частицы (низкий импульс).
        SetParticleColor(lowMomentumColor);
    }

    void Update()
    {
        // Основной цикл симуляции: ограничение границ, движение частицы,
        // перерисовка графиков и обновление текстовой информации.
        LimitClamps();
        UpdateParticle();
        DrawGraphs();
        UpdateUIText();
    }

    void LimitClamps()
    {
        // Не даем границам сблизиться меньше минимальной дистанции.
        if (rightClamp.position.x - leftClamp.position.x < minClampDistance)
        {
            float center = (leftClamp.position.x + rightClamp.position.x) / 2f;
            leftClamp.position = new Vector3(center - minClampDistance / 2f, leftClamp.position.y, 0);
            rightClamp.position = new Vector3(center + minClampDistance / 2f, rightClamp.position.y, 0);
        }
    }

    void UpdateParticle()
    {
        float deltaX = Mathf.Abs(rightClamp.position.x - leftClamp.position.x);

        // Принцип неопределенности: чем меньше deltaX, тем больше deltaP.
        float deltaP = hbar / (2f * deltaX);

        // Добавляем случайный "толчок" по скорости в пределах неопределенности импульса.
        velocity += Random.Range(-deltaP, deltaP) * Time.deltaTime * jitterStrength;
        velocity *= velocityDamping;

        particleX += velocity * Time.deltaTime;

        if (particleX < leftClamp.position.x)
        {
            particleX = leftClamp.position.x;
            velocity *= -0.7f;
        }

        if (particleX > rightClamp.position.x)
        {
            particleX = rightClamp.position.x;
            velocity *= -0.7f;
        }

        particle.position = new Vector3(particleX, particle.position.y, 0);
        UpdateParticleColor();
    }

    void DrawGraphs()
    {
        // Пересчитываем неопределенности для визуализации обоих распределений.
        float deltaX = Mathf.Abs(rightClamp.position.x - leftClamp.position.x);
        float deltaP = hbar / (2f * deltaX);

        DrawPositionGraph(deltaX);
        DrawMomentumGraph(deltaP);
    }

    void DrawPositionGraph(float deltaX)
    {
        for (int i = 0; i < graphPoints; i++)
        {
            float t = i / (float)(graphPoints - 1);
            float x = -graphWidth / 2f + t * graphWidth;

            float center = (leftClamp.position.x + rightClamp.position.x) / 2f;
            float value = GetPositionProbability(x, center, deltaX);

            positionGraph.SetPosition(i, new Vector3(x, positionGraphY + value, 0));
        }
    }

    void DrawMomentumGraph(float deltaP)
    {
        for (int i = 0; i < graphPoints; i++)
        {
            float t = i / (float)(graphPoints - 1);
            float p = -graphWidth / 2f + t * graphWidth;

            float value = GetMomentumProbability(p, deltaP);

            momentumGraph.SetPosition(i, new Vector3(p, momentumGraphY + value, 0));
        }
    }

    float GetPositionProbability(float x, float center, float deltaX)
    {
        float dx = x - center;

        // Выбираем форму волнового пакета в координатном представлении.
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

    void UpdateUIText()
    {
        // Показываем текущие значения на UI.
        float deltaX = Mathf.Abs(rightClamp.position.x - leftClamp.position.x);
        float deltaP = hbar / (2f * deltaX);

        deltaXText.text = "Δx = " + deltaX.ToString("F2");
        deltaPText.text = "Δp = " + deltaP.ToString("F2");
        waveTypeText.text = "Пакет: " + wavePacketType.ToString();
    }

    public void SetGaussian()
    {
        wavePacketType = WavePacketType.Gaussian;
    }

    public void SetExponential()
    {
        wavePacketType = WavePacketType.Exponential;
    }

    public void SetRectangular()
    {
        wavePacketType = WavePacketType.Rectangular;
    }

    float GetMomentumProbability(float p, float deltaP)
    {
        // Ширина распределения по импульсу растет с увеличением deltaP.
        float width = deltaP * 8f + 0.2f;

        // Формы в импульсном пространстве, согласованные с выбранным типом пакета.
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

    void UpdateParticleColor()
    {
        // Визуальная подсказка: чем больше импульс, тем "краснее" частица.
        float momentum = Mathf.Abs(velocity) * particleMass;
        float t = Mathf.InverseLerp(0f, momentumForMaxRed, momentum);
        Color targetColor = Color.Lerp(lowMomentumColor, highMomentumColor, t);
        Color currentColor = GetParticleColor();
        Color smoothColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);

        SetParticleColor(smoothColor);
    }

    Color GetParticleColor()
    {
        // Поддержка и SpriteRenderer, и стандартного Renderer.
        if (particleSpriteRenderer != null)
        {
            return particleSpriteRenderer.color;
        }

        if (particleRenderer != null && particleRenderer.material != null)
        {
            return particleRenderer.material.color;
        }

        return lowMomentumColor;
    }

    void SetParticleColor(Color color)
    {
        // Применяем цвет к доступным компонентам рендера.
        if (particleSpriteRenderer != null)
        {
            particleSpriteRenderer.color = color;
        }

        if (particleRenderer != null && particleRenderer.material != null)
        {
            particleRenderer.material.color = color;
        }
    }
}
