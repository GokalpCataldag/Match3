using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Match3 match;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            match.StartNewGame();
        }
        else
        {
            if (!match.IsBusy)
            {
                HandleInput();
            }
            match.DoWork();
        }
    }
    private void Awake()
    {
        match.StartNewGame();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Vector2 gridPos = new Vector2(
                    worldPoint.x - match.TileOffset.x + 0.5f,
                    worldPoint.y - match.TileOffset.y + 0.5f
                );

                int x = Mathf.FloorToInt(gridPos.x);
                int y = Mathf.FloorToInt(gridPos.y);
                
                match.TrySelectTile(x, y);
            }
        }
    }
}
