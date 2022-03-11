using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float speed = 6.0f;
    public Text scoreText;
    public GameObject restart, exitButton, EndTextObject, SubTextObject, SecretPassage;
    public GameObject[] levels;

    public bool gravitationOn;

    private Rigidbody rb;
    private Vector3 moveDirection, tempPosition = Vector3.zero;
    private float gravitationalForce = -1.5f;
    private int bonusScore, score = 0;
    private bool hasWaited, waiting, gameOver, pause = false;
    private float tempScore;
    private int currentLevel;
    private float windForce;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        LevelChange(0);
    }


    void Update()
    {
        moveDirection = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);
        moveDirection *= speed;
        Cursor.visible = waiting || pause || gameOver;

        if (Mathf.Abs((transform.position - tempPosition).magnitude) > 3f)
        {
            tempPosition = transform.position;
            score += 1;
        }

        scoreText.text = "Score: " + (score + bonusScore);

        if (Input.GetKeyDown(KeyCode.G))
        {
            gravitationOn = !gravitationOn;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("screenshot.png");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pause)
            {
                Time.timeScale = 1;
                EndTextObject.SetActive(false);
                exitButton.SetActive(false);
                pause = false;
            }
            else
            {
                Time.timeScale = 0;
                EndTextObject.SetActive(true);
                EndTextObject.GetComponent<UnityEngine.UI.Text>().text = "Pause";
                exitButton.SetActive(true);
                pause = true;
            }
        }

        if (waiting)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                LevelChange(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                LevelChange(1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                LevelChange(2);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                waiting = false;
                EndTextObject.SetActive(false);
                SubTextObject.SetActive(false);
                Time.timeScale = 1;
            }
        }

        if (gameOver && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            Restart();
        }
    }

    void FixedUpdate()
    {
        if (Mathf.Abs(rb.velocity.y) > 2)
        {
            moveDirection.y = 0;
        }

        if (Mathf.Abs(rb.velocity.x) > 2)
        {
            moveDirection.x = 0;
        }

        rb.AddForce(moveDirection, ForceMode.Acceleration);
        rb.AddForce(-rb.velocity * 0.8f, ForceMode.Acceleration); //brake after release of key
        rb.AddForce(new Vector3(windForce, 0f, 0f), ForceMode.Acceleration);

        if (gravitationOn)
        {
            rb.AddForce(new Vector3(0f, gravitationalForce, 0f) * rb.mass);
        }
    }

    public void exitGame()
    {
        Application.Quit();
    }

    public void Restart()
    {
        gameOver = false;
        LevelChange(currentLevel);
    }

    void OnCollisionEnter(Collision collision)
    {
        EndGame("Game Over!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finish"))
        {
            EndGame("You have Won!");
        }

        if (other.CompareTag("Target"))
        {
            SecretPassage.SetActive(false);
            LevelChange(currentLevel + 1);
        }

        if (other.CompareTag("SecretPassage"))
        {
            SecretPassage.SetActive(true);
            Destroy(other.gameObject);
        }

        if (other.CompareTag("BonusPoints"))
        {
            bonusScore = 100;
            Destroy(other.gameObject);
        }

        if (other.CompareTag("WindRight"))
        {
            windForce = 1;
        }

        if (other.CompareTag("WindLeft"))
        {
            windForce = -1;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WindRight") || other.CompareTag("WindRight"))
        {
            windForce = 0;
        }
    }

    private void EndGame(string text)
    {
        Time.timeScale = 0;
        EndTextObject.SetActive(true);
        restart.SetActive(true);
        EndTextObject.GetComponent<UnityEngine.UI.Text>().text = text;
        gameOver = true;
    }

    private void LevelChange(int level)
    {
        foreach (var levelObject in levels)
        {
            levelObject.SetActive(false);
        }

        levels[level].SetActive(true);
        restart.SetActive(false);
        exitButton.SetActive(false);

        gravitationOn = level > 0;

        Time.timeScale = 0;
        currentLevel = level;
        score = 0;
        windForce = 0;
        transform.position = Vector3.zero;
        tempPosition = transform.position;
        rb.velocity = Vector3.zero;

        EndTextObject.SetActive(true);
        SubTextObject.SetActive(true);
        EndTextObject.GetComponent<UnityEngine.UI.Text>().text = "Level " + (level + 1);
        SubTextObject.GetComponent<UnityEngine.UI.Text>().text = "Press Space to continue";
        waiting = true;
    }
}