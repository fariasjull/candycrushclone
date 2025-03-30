using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilesPecas : MonoBehaviour
{
    private Vector2Int gridPosition; // A posição da peça na grid
    private GridManager gridManager; // Referência ao GridManager
    private SpriteRenderer spriteRenderer;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Se não foi atribuído automaticamente, tente encontrar o GridManager
        if (gridManager == null) {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null) {
            }
        }
    }

    public void Setup(Vector2Int position, GridManager manager) {
        gridPosition = position;
        gridManager = manager;
    }

    void OnMouseDown() {

        if (gridManager == null) {
            return;
        }

        gridManager.TileClicked(this);
    }

    public Vector2Int GetGridPosition() {
        return gridPosition;
    }

    public void SwapPosition(TilesPecas otherTile) {
        Vector2 tempPosition = transform.position;
        transform.position = otherTile.transform.position;
        otherTile.transform.position = tempPosition;
    }

}
