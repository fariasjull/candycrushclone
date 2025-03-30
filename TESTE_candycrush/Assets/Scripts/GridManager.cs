using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Adicione essa linha no topo do script


public class GridManager : MonoBehaviour {
    public int width = 8;  // Largura da grade
    public int height = 8; // Altura da grade
    public GameObject[] tilePrefabs; // Array/Lista para armazenar diferentes tipos de peças
    private TilesPecas[,] grid; // Armazena as peças
    private TilesPecas selectedTile = null;

    public int score = 0; // Variável de pontuação
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
                } while (CheckForMatchAtStart(x, y)); // Repete até não haver match
            }
        }
    }

    bool CheckForMatchAtStart(int x, int y) {
        if (x > 1 && grid[x, y] != null && grid[x - 1, y] != null && grid[x - 2, y] != null) {
            if (grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x - 1, y].GetComponent<SpriteRenderer>().sprite &&
                grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x - 2, y].GetComponent<SpriteRenderer>().sprite)
                return true; // Há um match horizontal
        }

        if (y > 1 && grid[x, y] != null && grid[x, y - 1] != null && grid[x, y - 2] != null) {
            if (grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x, y - 1].GetComponent<SpriteRenderer>().sprite &&
                grid[x, y].GetComponent<SpriteRenderer>().sprite == grid[x, y - 2].GetComponent<SpriteRenderer>().sprite)
                return true; // Há um match vertical
        }

        return false; // Sem match, pode usar essa peça
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

        // Pegar as posições na grid
        Vector2Int pos1 = tile1.GetGridPosition();
        Vector2Int pos2 = tile2.GetGridPosition();

        // Movimentação animada das peças
        StartCoroutine(MoveTilesWithAnimation(tile1, tile2, pos1, pos2));

        // Atualizar a matriz lógica da grid
        grid[pos1.x, pos1.y] = tile2;
        grid[pos2.x, pos2.y] = tile1;

        // Atualizar as posições dentro dos scripts das peças
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

        // Depois que a animação terminar, verificamos as combinações.
        StartCoroutine(DelayedFindMatches());
    }


    // Pequeno atraso para esperar a animação da troca antes de verificar combinações
    IEnumerator DelayedFindMatches() {
        bool matchesFound = false;

        // Aguarda um pouco para a animação de movimento ser concluída
        yield return new WaitForSeconds(0.3f);

        // Enquanto houver peças a remover, vamos continuar verificando
        do {
            List<TilesPecas> piecesToRemove = new List<TilesPecas>();
            matchesFound = false;  // Inicializa como falso

            // Verifica todas as peças da grid em busca de combinações
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    TilesPecas tile = grid[x, y];
                    if (tile != null) {
                        // Verifica combinações horizontais
                        List<TilesPecas> horizontalMatch = GetHorizontalMatch(tile);
                        if (horizontalMatch.Count >= 3) {
                            piecesToRemove.AddRange(horizontalMatch);
                            matchesFound = true;
                        }

                        // Verifica combinações verticais
                        List<TilesPecas> verticalMatch = GetVerticalMatch(tile);
                        if (verticalMatch.Count >= 3) {
                            piecesToRemove.AddRange(verticalMatch);
                            matchesFound = true;
                        }
                    }
                }
            }

            // Se houver peças a remover, processa a remoção e repete a verificação
            if (matchesFound) {
                RemoveMatches(piecesToRemove);
                yield return new WaitForSeconds(0.3f);  // Aguarda para as animações de remoção
                FillEmptySpaces();  // Preenche os espaços vazios com novas peças
                Debug.Log("Peças a remover: " + piecesToRemove.Count);

            }

        } while (matchesFound);  // Continua verificando enquanto houver combinações a serem removidas
    }




    void FindMatches() {
        // Crie uma lista para armazenar as peças a serem removidas
        List<TilesPecas> piecesToRemove = new List<TilesPecas>();

        // Verificar na horizontal
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                TilesPecas tile = grid[x, y];
                if (tile != null) {
                    List<TilesPecas> horizontalMatch = GetHorizontalMatch(tile);
                    if (horizontalMatch.Count >= 3) {
                        piecesToRemove.AddRange(horizontalMatch);  // Adiciona todas as peças da combinação
                    }

                    List<TilesPecas> verticalMatch = GetVerticalMatch(tile);
                    if (verticalMatch.Count >= 3) {
                        piecesToRemove.AddRange(verticalMatch);
                    }
                }
            }
        }

        // Remover peças
        RemoveMatches(piecesToRemove);
    }

    List<TilesPecas> GetHorizontalMatch(TilesPecas startTile) {
        List<TilesPecas> match = new List<TilesPecas>();
        int x = startTile.GetGridPosition().x;
        int y = startTile.GetGridPosition().y;
        TilesPecas currentTile = grid[x, y];

        // Procurar na direção horizontal
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

        // Procurar na direção vertical
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
        // Remover peças duplicadas (evita contagem incorreta)
        List<TilesPecas> uniquePiecesToRemove = new List<TilesPecas>();

        foreach (TilesPecas tile in piecesToRemove) {
            if (!uniquePiecesToRemove.Contains(tile)) {
                uniquePiecesToRemove.Add(tile);
            }
        }

        // Agora, use uniquePiecesToRemove em vez de piecesToRemove
        int points = 0;

        // Verifica quantas peças estão sendo removidas
        Debug.Log("Número de peças a serem removidas (únicas): " + uniquePiecesToRemove.Count);

        // Remover as peças
        foreach (TilesPecas tile in uniquePiecesToRemove) {
            Vector2Int pos = tile.GetGridPosition();
            grid[pos.x, pos.y] = null;  // Remove da grid
            Destroy(tile.gameObject);  // Remove a peça da cena
            points += 10;  // Adiciona 10 pontos para cada peça removida
            Debug.Log("Peça removida: " + tile.name);  // Verifica qual peça está sendo removida
        }

        // Atualiza a pontuação
        AddScore(points);

        // Preenche os espaços vazios com novas peças
        FillEmptySpaces();
    }

    void AddScore(int points) {
            score += points;  // Adiciona a pontuação ao total
            scoreText.text = "Score: " + score.ToString();  // Atualiza a UI com a pontuação
        }


    void FillEmptySpaces() {
        // Preenche os espaços vazios na grid movendo peças para baixo
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (grid[x, y] == null) {
                    // Move peças para baixo, começando de baixo para cima
                    for (int i = y + 1; i < height; i++) {
                        if (grid[x, i] != null) {
                            grid[x, i].transform.position = new Vector2(x, i - 1);  // Move a peça para baixo
                            grid[x, i].Setup(new Vector2Int(x, i - 1), this);  // Atualiza a posição da peça
                            grid[x, i - 1] = grid[x, i];  // Atualiza a grid
                            grid[x, i] = null;  // Limpa a posição original
                            break;
                        }
                    }
                }
            }
        }

        // Preenche os espaços vazios com novas peças
        SpawnNewTiles();
    }



    void SpawnNewTiles() {
        // Preenche a grid com novas peças nas posições vazias
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (grid[x, y] == null) {
                    Vector2 position = new Vector2(x, y);  // Posição da nova peça
                    int randomIndex = Random.Range(0, tilePrefabs.Length);  // Escolhe aleatoriamente um tipo de peça
                    GameObject newTileObj = Instantiate(tilePrefabs[randomIndex], position, Quaternion.identity);  // Cria a nova peça
                    TilesPecas newTile = newTileObj.GetComponent<TilesPecas>();

                    if (newTile != null) {
                        newTile.Setup(new Vector2Int(x, y), this);  // Configura a nova peça
                        grid[x, y] = newTile;  // Atualiza a grid com a nova peça
                    }
                }
            }
        }
    }


}