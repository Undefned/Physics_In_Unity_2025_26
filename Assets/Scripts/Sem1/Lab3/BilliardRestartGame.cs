using UnityEngine;
using System.Collections.Generic;

public class BilliardRestartGame : MonoBehaviour
{
    public string cueBallTag = "CueBall";
    public string ballTag = "BilliardBall";
    public const int finalScore = 15;
    
    private List<BallData> allBalls = new List<BallData>();

    [System.Serializable]
    public class BallData
    {
        public GameObject ballObject;
        public Vector3 startPosition;
        public Quaternion startRotation;
    }

    void Start()
    {
        // Сохраняем все шары при старте игры
        SaveAllBalls();
    }

    void Update()
    {
        if (BilliardAddScore.count >= finalScore)
        {
            RestartGame();
        }
    }

    void SaveAllBalls()
    {
        allBalls.Clear();
        
        // Сохраняем все шары с тегом BilliardBall 
        GameObject[] balls = GameObject.FindGameObjectsWithTag(ballTag);
        foreach (GameObject ball in balls)
        {
            BallData ballData = new BallData
            {
                ballObject = ball,
                startPosition = ball.transform.position,
                startRotation = ball.transform.rotation
            };
            allBalls.Add(ballData);
            Debug.Log($"Saved: {ball.name} at {ball.transform.position}");
        }
        
        // Сохраняем биток
        GameObject cueBall = GameObject.FindGameObjectWithTag(cueBallTag);
        if (cueBall != null)
        {
            BallData cueBallData = new BallData
            {
                ballObject = cueBall,
                startPosition = cueBall.transform.position,
                startRotation = cueBall.transform.rotation
            };
            allBalls.Add(cueBallData);
            Debug.Log($"Saved: {cueBall.name} at {cueBall.transform.position}");
        }
        
        Debug.Log($"Total balls saved: {allBalls.Count}");
    }

    public void RestartGame()
    {
        // Сбрасываем счет
        BilliardAddScore.count = 0;
        
        // Восстанавливаем все шары
        ResetAllBalls();
        
        Debug.Log("Game restarted! All balls reset to start positions.");
    }

    void ResetAllBalls()
    {
        foreach (BallData ballData in allBalls)
        {
            if (ballData.ballObject != null)
            {
                // Восстанавливаем позицию и поворот
                ballData.ballObject.transform.position = ballData.startPosition;
                ballData.ballObject.transform.rotation = ballData.startRotation;
                
                // Сбрасываем физику
                Rigidbody rb = ballData.ballObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                // Активируем шар
                ballData.ballObject.SetActive(true);
                
                Debug.Log($"Reset: {ballData.ballObject.name}");
            }
        }
    }

    // Метод для поиска шаров по имени (если нужно)
    public void FindBallsByName()
    {
        for (int i = 1; i <= 15; i++)
        {
            string ballName = $"PoolBall_{i}";
            GameObject ball = GameObject.Find(ballName);
            if (ball != null)
            {
                Debug.Log($"Found: {ballName}");
            }
        }
    }

    [ContextMenu("Manual Restart")]
    public void ManualRestart()
    {
        RestartGame();
    }
}