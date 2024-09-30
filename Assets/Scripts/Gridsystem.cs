using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Gridsystem : MonoBehaviour
{
    public TMP_Text generationText;
    public TMP_Text aliveSquaresText;

    public GameObject gridPrefab;
    public GameObject aliveSquarePrefab;

    GameObject[,] gridArray;

    public int rows;
    public int columns;

    int generation = 0;

    public float distanceBetweenGrid;
    public float gridSize;
    public float updateDelay = 1;

    public float zoomSpeed;
    public float minZoom;
    public float maxZoom;

    bool isPaused = false;

    Vector2 dragStartPos;

    AliveGridManager aliveGridManager;

    public Color color = Color.white;
    void Start()
    {
        BuildGrid();

        aliveGridManager = new AliveGridManager(this);
        aliveGridManager.SpawnAliveGrid();

       InvokeUpdateGrid();
    }

    public void BuildGrid()
    {
        gridArray = new GameObject[rows, columns];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                Vector2 gridPosition = new Vector2(x * (distanceBetweenGrid + gridSize), y * (distanceBetweenGrid + gridSize));

                GameObject newGrid = Instantiate(gridPrefab, gridPosition, Quaternion.identity);

                newGrid.name = $"({x}, {y})";

                gridArray[x, y] = newGrid;
            }
        }

    }

    public GameObject[,] GetGridArray()
    {
        return gridArray;
    }
    public Vector2 GetGridPosition(int x, int y)
    {
        return gridArray[x, y].transform.position;
    }

    void Update()
    {
        Zoom();
        DragCamera();   
        
        if (Input.GetKey(KeyCode.Escape))
        {
            SceneManager.LoadScene("Menu");
            Pause();
        }
    }

    public void InvokeUpdateGrid()
    {
        CancelInvoke(nameof(UpdateGrid));

        InvokeRepeating(nameof(UpdateGrid), 1, updateDelay);
    }

    void UpdateGrid()
    {
        if (!isPaused)
        {
            GridResults results = aliveGridManager.GridUpdate(generation);

            aliveSquaresText.text = $"Alive: {results.aliveCount}";
            generationText.text = $"Generation: {results.generation}";

            generation = results.generation;
        }
    }
    public void Zoom()
    {
        float scrollInp = Input.GetAxis("Mouse ScrollWheel");

        Camera.main.orthographicSize -= scrollInp * zoomSpeed;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
    }

    public void DragCamera()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetMouseButton(0))
        {
            Vector2 currMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 difference = dragStartPos - currMousePos;

            Camera.main.transform.position += new Vector3(difference.x, difference.y, 0);
        }
    }

    public void Pause()
    {
        if (!isPaused)
        {
            CancelInvoke(nameof(UpdateGrid));
            isPaused = true;
        }
        else
        {
            isPaused = false;
            InvokeUpdateGrid();
        }
    }

    public void SlowFast(string pInput)
    {
        if (pInput == "Faster")
        {
            updateDelay = Mathf.Max(0.1f, updateDelay - 0.5f);
        }
        if (pInput == "Slower")
        {
            updateDelay += 0.5f;
        }

        InvokeUpdateGrid();
    }

    public void ColorChange(string colorName)
    {
        color = ConvertStringToColor(colorName);
        aliveGridManager.UpdateAliveColors(color);
    }

    public Color ConvertStringToColor(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "red": return Color.red;
            case "green": return Color.green;
            case "blue": return Color.blue;
            case "yellow": return Color.yellow;
            case "magenta": return Color.magenta;
            case "white": return Color.white;
            default: return Color.white;
        }
    }

}

public class AliveGridManager : MonoBehaviour
{
    Gridsystem gridsystem;

    GameObject[,] aliveGrid;
    GameObject aliveSquare;

    float totalSquares;
    int rows;
    int columns;

    public AliveGridManager(Gridsystem gridSystem)
    {
        this.gridsystem = gridSystem;
    }

    public void SpawnAliveGrid()
    {
        rows = gridsystem.rows;
        columns = gridsystem.columns;

        aliveGrid = new GameObject[rows, columns];  

        totalSquares = (rows * columns);
        float currentAliveSquares = 0;

        float randomSpawn = Random.Range(0.5f, 0.9f);

         aliveSquare = gridsystem.aliveSquarePrefab;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {

                if (Random.value > randomSpawn && currentAliveSquares < totalSquares)
                {
                    Vector2 alivePosition = gridsystem.GetGridPosition(x, y);
                    GameObject newAliveGrid = Object.Instantiate(aliveSquare, alivePosition, Quaternion.identity);
                    currentAliveSquares++;

                    newAliveGrid.name = $"Alive ({x}, {y})";
                    aliveGrid[x, y] = newAliveGrid;
                  
                    
                }
            }
        }

    }

    public int AliveNeighbours(int x, int y)
    {   
        int numbOfNeighbours = 0;

        for (int n = -1; n <= 1; n++)
        {
            for (int m = -1; m <= 1; m++)
            {
                if (n == 0 && m == 0) continue;

                int neighbourX = x + n;
                int neighbourY = y + m;

                if (neighbourX >= 0 && neighbourX < rows && neighbourY >= 0 && neighbourY < columns)
                {
                    if (aliveGrid[neighbourX, neighbourY] != null)
                    {
                        numbOfNeighbours++;
                    }
                }

            }
        }


        return numbOfNeighbours;
    }

    public GridResults GridUpdate (int currGen)
    {
        bool[,] nextUpdate = new bool[rows, columns];
        int aliveCount = 0;
        int generation = currGen + 1;
        Color color = gridsystem.color;

        for (int x = 0; x < rows; x++)
        {
            for(int y = 0; y < columns; y++)
            {
                int numbOfNeighbours = AliveNeighbours(x, y);

                bool isAlive = aliveGrid[x, y] != null;

                if (isAlive)
                {
                  nextUpdate[x, y] = !(numbOfNeighbours < 2 || numbOfNeighbours > 3);
                }
                else
                {               
                  nextUpdate[x, y] = (numbOfNeighbours == 3);                   
                }

            }
        }

        for(int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
               bool isAlive = aliveGrid[x, y] != null;
                
                if (nextUpdate[x,y] && !isAlive)
                {     
                    

                    Vector2 alivePosition = gridsystem.GetGridPosition(x, y);
                    GameObject newAliveGrid = Object.Instantiate(aliveSquare, alivePosition, Quaternion.identity);

                    newAliveGrid.name = $"Alive ({x}, {y})";
                    aliveGrid[x, y] = newAliveGrid;

                    newAliveGrid.GetComponent<SpriteRenderer>().color = color;
                }
                else if (!nextUpdate[x,y] && isAlive)
                {
                    Destroy(aliveGrid[x, y]);
                    aliveGrid[x, y] = null;
                }

                if(aliveGrid[x, y] != null)
                {
                    aliveCount++;
                }
                
            }
        }

       return new GridResults(aliveCount, generation);
    }

    public void UpdateAliveColors(Color color)
    {
        for(int x = 0; x < rows; x++)
        {
            for(int y = 0; y < columns; y++)
            {
                if (aliveGrid[x, y] != null)
                {
                    aliveGrid[x, y].GetComponent<SpriteRenderer>().color = color;
                }
            }
        }
    }
   
}

public class GridResults : MonoBehaviour
{
    public int aliveCount = 0;
    public int generation = 0;

    public GridResults(int aliveCount, int generation)
    {
        this.aliveCount = aliveCount;
        this.generation = generation;
    }
}

