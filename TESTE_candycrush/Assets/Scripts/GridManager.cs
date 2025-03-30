using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Adicione essa linha no topo do script


public class GridManager : MonoBehaviour {
    public int width = 8;  // Largura da grade
    public int height = 8; // Altura da grade
    public GameObject[] tilePrefabs; // Array/Lista para armazenar diferentes tipos de pe�as
    private TilesPecas[,] grid; // Armazena as pe�as
    private TilesPecas selectedTile = null;

    public int score = 0; // Vari�vel de pontua��o
    public Text scoreText; // Agora usando o UI > Legacy > Text


    void Start() {
        grid = new TilesPecas[width, height];
        GenerateBoard();
    }

    void GenerateBoard() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector2 position = new Vector2(x, y);
                int randomIndex;
                GameObject tileObj;
                TilesPecas tile;

                do {
                    randomIndex = Random.Range(0, tilePrefabs.Length);
                    tileObj = Instantiate(tilePrefabs[randomIndex], position, Quaternion.identity);
                    tile = tileObj.GetComponent<TilesPecas>();

                    if (tile != null) {
                        tile.Setup(new Vector2Int(x, y), this);
                        grid[x, y] = tile;
                    }
                } while (CheckForMatchAtStart(x, y)); // Repete at� n�o haver match
            }
        }
    }

    bool CheckForMatchAtStart(int x, int y) {
        if (x > 1 && grid[x, y] != null && grid[x - 1, y] != null && grid[x - 2, y] != null) {
            if (grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x - 1, y].GetComponent<SpriteRenderer>().sprite &&
                grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x - 2, y].GetComponent<SpriteRenderer>().sprite)
                return true; // H� um match horizontal
        }

        if (y > 1 && grid[x, y] != null && grid[x, y - 1] != null && grid[x, y - 2] != null) {
            if (grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x, y - 1].GetComponent<SpriteRenderer>().sprite &&
                grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x, y - 2].GetComponent<SpriteRenderer>().sprite)
                return true; // H� um match vertical
        }

        return false; // Sem match, pode usar essa pe�a
    }

    public void TileClicked(TilesPecas clickedTile) {

        if (selectedTile == null) {
            selectedTile = clickedTile;
        }
        else {
            if (AreTilesAdjacent(selectedTile, clickedTile)) {
                SwapTiles(selectedTile, clickedTile);
            }
            selectedTile = null;
        }
    }

    bool AreTilesAdjacent(TilesPecas tile1, TilesPecas tile2) {
        Vector2Int pos1 = tile1.GetGridPosition();
        Vector2Int pos2 = tile2.GetGridPosition();
        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y) == 1;
    }

    void SwapTiles(TilesPecas tile1, TilesPecas tile2) {

        // Pegar as posi��es na grid
        Vector2Int pos1 = tile1.GetGridPosition();
        Vector2Int pos2 = tile2.GetGridPosition();

        // Movimenta��o animada das pe�as
        StartCoroutine(MoveTilesWithAnimation(tile1, tile2, pos1, pos2));

        // Atualizar a matriz l�gica da grid
        grid[pos1.x, pos1.y] = tile2;
        grid[pos2.x, pos2.y] = tile1;

        // Atualizar as posi��es dentro dos scripts das pe�as
        tile1.Setup(pos2, this);
        tile2.Setup(pos1, this);
    }

    IEnumerator MoveTilesWithAnimation(TilesPecas tile1, TilesPecas tile2, Vector2Int pos1, Vector2Int pos2) {
        float moveSpeed = 0.2f; // Velocidade de movimento
        Vector2 targetPositionTile1 = new Vector2(pos2.x, pos2.y);
        Vector2 targetPositionTile2 = new Vector2(pos1.x, pos1.y);

        float timeElapsed = 0f;
        Vector2 startPosTile1 = tile1.transform.position;
        Vector2 startPosTile2 = tile2.transform.position;

        while (timeElapsed < moveSpeed) {
            tile1.transform.position = Vector2.Lerp(startPosTile1, targetPositionTile1, timeElapsed / moveSpeed);
            tile2.transform.position = Vector2.Lerp(startPosTile2, targetPositionTile2, timeElapsed / moveSpeed);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        tile1.transform.position = targetPositionTile1;
        tile2.transform.position = targetPositionTile2;

        // Depois que a anima��o terminar, verificamos as combina��es.
        StartCoroutine(DelayedFindMatches());
    }


    // Pequeno atraso para esperar a anima��o da troca antes de verificar combina��es
    IEnumerator DelayedFindMatches() {
        bool matchesFound = false;

        // Aguarda um pouco para a anima��o de movimento ser conclu�da
        yield return new WaitForSeconds(0.3f);

        // Enquanto houver pe�as a remover, vamos continuar verificando
        do {
            List<TilesPecas> piecesToRemove = new List<TilesPecas>();
            matchesFound = false;  // Inicializa como falso

            // Verifica todas as pe�as da grid em busca de combina��es
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    TilesPecas tile = grid[x, y];
                    if (tile != null) {
                        // Verifica combina��es horizontais
                        List<TilesPecas> horizontalMatch = GetHorizontalMatch(tile);
                        if (horizontalMatch.Count >= 3) {
                            piecesToRemove.AddRange(horizontalMatch);
                            matchesFound = true;
                        }

                        // Verifica combina��es verticais
                        List<TilesPecas> verticalMatch = GetVerticalMatch(tile);
                        if (verticalMatch.Count >= 3) {
                            piecesToRemove.AddRange(verticalMatch);
                            matchesFound = true;
                        }
                    }
                }
            }

            // Se houver pe�as a remover, processa a remo��o e repete a verifica��o
            if (matchesFound) {
                RemoveMatches(piecesToRemove);
                yield return new WaitForSeconds(0.3f);  // Aguarda para as anima��es de remo��o
                FillEmptySpaces();  // Preenche os espa�os vazios com novas pe�as
                Debug.Log("Pe�as a remover: " + piecesToRemove.Count);

            }

        } while (matchesFound);  // Continua verificando enquanto houver combina��es a serem removidas
    }




    void FindMatches() {
        // Crie uma lista para armazenar as pe�as a serem removidas
        List<TilesPecas> piecesToRemove = new List<TilesPecas>();

        // Verificar na horizontal
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                TilesPecas tile = grid[x, y];
                if (tile != null) {
                    List<TilesPecas> horizontalMatch = GetHorizontalMatch(tile);
                    if (horizontalMatch.Count >= 3) {
                        piecesToRemove.AddRange(horizontalMatch);  // Adiciona todas as pe�as da combina��o
                    }

                    List<TilesPecas> verticalMatch = GetVerticalMatch(tile);
                    if (verticalMatch.Count >= 3) {
                        piecesToRemove.AddRange(verticalMatch);
                    }
                }
            }
        }

        // Remover pe�as
        RemoveMatches(piecesToRemove);
    }

    List<TilesPecas> GetHorizontalMatch(TilesPecas startTile) {
        List<TilesPecas> match = new List<TilesPecas>();
        int x = startTile.GetGridPosition().x;
        int y = startTile.GetGridPosition().y;
        TilesPecas currentTile = grid[x, y];

        // Procurar na dire��o horizontal
        match.Add(currentTile);

        // Para esquerda
        for (int i = x - 1; i >= 0; i--) {
            if (grid[i, y] != null && grid[i, y].GetComponent<SpriteRenderer>().sprite == currentTile.GetComponent<SpriteRenderer>().sprite) {
                match.Insert(0, grid[i, y]);
            }
            else {
                break;
            }
        }

        // Para direita
        for (int i = x + 1; i < width; i++) {
            if (grid[i, y] != null && grid[i, y].GetComponent<SpriteRenderer>().sprite == currentTile.GetComponent<SpriteRenderer>().sprite) {
                match.Add(grid[i, y]);
            }
            else {
                break;
            }
        }

        return match;
    }

    List<TilesPecas> GetVerticalMatch(TilesPecas startTile) {
        List<TilesPecas> match = new List<TilesPecas>();
        int x = startTile.GetGridPosition().x;
        int y = startTile.GetGridPosition().y;
        TilesPecas currentTile = grid[x, y];

        // Procurar na dire��o vertical
        match.Add(currentTile);

        // Para cima
        for (int i = y + 1; i < height; i++) {
            if (grid[x, i] != null && grid[x, i].GetComponent<SpriteRenderer>().sprite == currentTile.GetComponent<SpriteRenderer>().sprite) {
                match.Add(grid[x, i]);
            }
            else {
                break;
            }
        }

        // Para baixo
        for (int i = y - 1; i >= 0; i--) {
            if (grid[x, i] != null && grid[x, i].GetComponent<SpriteRenderer>().sprite == currentTile.GetComponent<SpriteRenderer>().sprite) {
                match.Insert(0, grid[x, i]);
            }
            else {
                break;
            }
        }

        return match;
    }

    void RemoveMatches(List<TilesPecas> piecesToRemove) {
        // Remover pe�as duplicadas (evita contagem incorreta)
        List<TilesPecas> uniquePiecesToRemove = new List<TilesPecas>();

        foreach (TilesPecas tile in piecesToRemove) {
            if (!uniquePiecesToRemove.Contains(tile)) {
                uniquePiecesToRemove.Add(tile);
            }
        }

        // Agora, use uniquePiecesToRemove em vez de piecesToRemove
        int points = 0;

        // Verifica quantas pe�as est�o sendo removidas
        Debug.Log("N�mero de pe�as a serem removidas (�nicas): " + uniquePiecesToRemove.Count);

        // Remover as pe�as
        foreach (TilesPecas tile in uniquePiecesToRemove) {
            Vector2Int pos = tile.GetGridPosition();
            grid[pos.x, pos.y] = null;  // Remove da grid
            Destroy(tile.gameObject);  // Remove a pe�a da cena
            points += 10;  // Adiciona 10 pontos para cada pe�a removida
            Debug.Log("Pe�a removida: " + tile.name);  // Verifica qual pe�a est� sendo removida
        }

        // Atualiza a pontua��o
        AddScore(points);

        // Preenche os espa�os vazios com novas pe�as
        FillEmptySpaces();
    }

    void AddScore(int points) {
            score += points;  // Adiciona a pontua��o ao total
            scoreText.text = "Score: " + score.ToString();  // Atualiza a UI com a pontua��o
        }


    void FillEmptySpaces() {
        // Preenche os espa�os vazios na grid movendo pe�as para baixo
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (grid[x, y] == null) {
                    // Move pe�as para baixo, come�ando de baixo para cima
                    for (int i = y + 1; i < height; i++) {
                        if (grid[x, i] != null) {
                            grid[x, i].transform.position = new Vector2(x, i - 1);  // Move a pe�a para baixo
                            grid[x, i].Setup(new Vector2Int(x, i - 1), this);  // Atualiza a posi��o da pe�a
                            grid[x, i - 1] = grid[x, i];  // Atualiza a grid
                            grid[x, i] = null;  // Limpa a posi��o original
                            break;
                        }
                    }
                }
            }
        }

        // Preenche os espa�os vazios com novas pe�as
        SpawnNewTiles();
    }



    void SpawnNewTiles() {
        // Preenche a grid com novas pe�as nas posi��es vazias
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (grid[x, y] == null) {
                    Vector2 position = new Vector2(x, y);  // Posi��o da nova pe�a
                    int randomIndex = Random.Range(0, tilePrefabs.Length);  // Escolhe aleatoriamente um tipo de pe�a
                    GameObject newTileObj = Instantiate(tilePrefabs[randomIndex], position, Quaternion.identity);  // Cria a nova pe�a
                    TilesPecas newTile = newTileObj.GetComponent<TilesPecas>();

                    if (newTile != null) {
                        newTile.Setup(new Vector2Int(x, y), this);  // Configura a nova pe�a
                        grid[x, y] = newTile;  // Atualiza a grid com a nova pe�a
                    }
                }
            }
        }
    }


}