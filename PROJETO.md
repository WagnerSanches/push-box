# Projeto PushBox – especificação para o Claude Code

Você (Claude Code) está na pasta raiz de um projeto Unity 6 (6000.6.x) recém-criado com o template **Universal 3D** (URP) e o **Input System novo**. A tarefa é recriar um jogo que já existia e foi perdido. Tudo o que precisa está neste arquivo.

## O jogo

Sokoban em primeira pessoa. O jogador anda numa grade de quadrados de 1×1 e chuta caixas com Espaço: a caixa desliza exatamente 1 quadrado na direção do chute, se o caminho estiver livre. Quando todos os pontos roxos têm uma caixa em cima, vem a próxima fase. As fases são lidas de um arquivo JSON editável (inclusive no jogo buildado). Em cada fase nascem 2 gnomos que perseguem o jogador pela grade e, ao encostar, empurram o jogador para longe (knockback) e recuam um pouco.

Controles: mouse olha, WASD anda, Espaço chuta, R reinicia a fase (e relê o JSON), Esc fecha o jogo buildado.

## Regras

1. Crie os 4 scripts de jogo **exatamente** como estão abaixo, em `Assets/Scripts/`. Não mude nomes, valores nem lógica: eles já foram testados e ajustados.
2. Crie `Assets/StreamingAssets/fases.json` com o conteúdo exato da seção "fases.json".
3. Escreva o script de editor `Assets/Editor/MontarProjeto.cs` (seção "Montador"). É ele que cria materiais, prefabs e a cena, porque o usuário não quer fazer isso clicando.
4. Não use Rigidbody em nada. Não use tags. Não crie outros scripts de jogo.
5. Comentários e mensagens de log em português.
6. Você não consegue clicar no Unity. Depois de escrever os arquivos, peça para o usuário voltar para a janela do Unity e esperar compilar. Para ver erros de compilação, leia o log do editor em `%LOCALAPPDATA%\Unity\Editor\Editor.log` (procure por `error CS`) e corrija. Repita até compilar sem erros.
7. No final, diga ao usuário: rodar **Ferramentas → Montar Projeto**, abrir `Assets/Scenes/SampleScene`, apertar Play, e fazer **Check in** no Unity Version Control.

## Tamanhos e valores (referência)

A grade tem quadrados de 1. A célula (coluna, linha) do mapa fica na posição de mundo `x = coluna`, `z = -linha`.

| Objeto | Detalhes |
| --- | --- |
| Player | CharacterController: Height 0.5, Radius 0.2, Skin Width 0.02, Step Offset 0.1, Min Move Distance 0, Center (0,0,0). PlayerController: Velocidade 5, Sensibilidade 0.1, Distancia Chute 1.5 |
| Câmera | Filha do Player. Local position (0, 0.1, 0), rotation (0,0,0), Near clip 0.01. Tag MainCamera, com AudioListener |
| Prefab Chao | Cube. Position (0, -0.05, 0), Scale (1, 0.1, 1), material Mat_Chao |
| Prefab Parede | Cube. Position (0, 0.5, 0), Scale (1, 1, 1), material Mat_Parede |
| Prefab Caixa | Cube. Position (0, 0.5, 0), Scale (1, 1, 1), material Mat_Caixa, script Caixa (Velocidade 5). Mantém o BoxCollider. Se existir um modelo em `Assets/Modelos/Caixa`, a raiz vira um objeto vazio com BoxCollider 1×1×1 + Caixa, e o modelo fica no filho "Visual" (ajustado para caber em 1×1×1, com materiais `Mat_Caixa_<material>` usando as texturas `<material>_Base_Color/_Normal_DirectX/_Mixed_AO.png`, máx. 1024) |
| Prefab Ponto | Cube. Position (0, 0.01, 0), Scale (0.8, 0.02, 0.8), material Mat_Ponto. **Sem** collider |
| Prefab Gnomo | Objeto vazio na raiz, Position (0, 0.15, 0). CharacterController: Height 0.3, Radius 0.1, Skin Width 0.01, Step Offset 0.05, Min Move Distance 0, Center (0,0,0). Script Gnomo: Velocidade 3.75 (75% da do player), Forca Empurrao 8, Recuo 4, Distancia Ataque 0.4, Tempo Tonto 0.8. Filho "Visual" (ver Montador) |
| Construtor | Objeto vazio na posição (0,0,0) com ConstrutorFase. Min Gnomos 2, Max Gnomos 2, Distancia Minima 3 |

A Position Y de cada prefab importa: o ConstrutorFase usa a altura do próprio prefab ao instanciar.

Materiais (shader `Universal Render Pipeline/Lit`, cor em `_BaseColor`):

| Material | Cor (RGB 0–1) |
| --- | --- |
| Mat_Chao | 0.90, 0.90, 0.90 |
| Mat_Parede | 0.35, 0.65, 0.70 |
| Mat_Caixa | 0.80, 0.55, 0.25 |
| Mat_Ponto | 0.60, 0.30, 0.80 |
| Mat_Gnomo | 0.30, 0.55, 0.25 (só para o boneco provisório) |

## Montador (`Assets/Editor/MontarProjeto.cs`)

Script de editor (`using UnityEditor;`) com o item de menu **Ferramentas/Montar Projeto**. Ao clicar, ele deve, nesta ordem:

1. Criar as pastas `Assets/Materiais` e `Assets/Prefabs` se não existirem.
2. Criar (ou atualizar, se já existirem) os 5 materiais da tabela.
3. Criar (ou sobrescrever) os prefabs `Chao`, `Parede`, `Caixa`, `Ponto` e `Gnomo` em `Assets/Prefabs/`, com os valores da tabela, usando `PrefabUtility.SaveAsPrefabAsset` e destruindo os objetos temporários depois.
4. Para o filho "Visual" do Gnomo:
   - Procurar um modelo em `Assets/Modelos/Gnomo` (`AssetDatabase.FindAssets("t:GameObject", ...)`, pega o primeiro). Se achar, instanciar como filho, remover todos os Colliders dele, e **ajustar a escala automaticamente** pelos Renderers para que a altura total fique 0.3 e a base (pés) fique em y = 0 do mundo, centralizado em x/z.
   - Se não achar modelo, usar uma Capsule primitiva como boneco provisório: sem collider, scale (0.2, 0.15, 0.2), material Mat_Gnomo.
   - Se achar o modelo, criar `Assets/Animacoes/Gnomo.controller` com a animação `Walking` do modelo em loop e ligar no Animator do "Visual" (Apply Root Motion desligado).
   - Escrever no Console qual dos dois foi usado.
5. Abrir `Assets/Scenes/SampleScene.unity` (criar se não existir) e, nela:
   - Manter `Directional Light` e `Global Volume` se existirem.
   - Criar ou reaproveitar o objeto `Player` (sem duplicar se o menu for rodado de novo) com os componentes e valores da tabela, na posição (0, 0.25, 0).
   - Reaproveitar a `Main Camera` existente (ou criar uma), colocar como filha do Player com os valores da tabela, e ligar no campo `cameraPlayer`.
   - Criar ou reaproveitar o objeto `Construtor` com ConstrutorFase, ligando os 5 prefabs e o CharacterController do Player nos campos.
   - Marcar a cena como modificada e salvar.
6. Colocar `SampleScene` como a primeira cena da lista de build (`EditorBuildSettings.scenes`).
7. Ajustar o Player Settings para o build: `fullScreenMode = Windowed`, `defaultScreenWidth = 1280`, `defaultScreenHeight = 720`.
8. Mostrar uma janela (`EditorUtility.DisplayDialog`) dizendo que terminou e o que fazer em seguida.

Rodar o menu duas vezes não pode duplicar nada.

## Script: `Assets/Scripts/PlayerController.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float velocidade = 5f;
    public float sensibilidade = 0.1f;
    public Transform cameraPlayer;
    public float distanciaChute = 1.5f;

    CharacterController controller;
    float rotacaoVertical = 0f;
    Vector3 empurrao;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // ---- SAIR (só funciona no jogo buildado) ----
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Application.Quit();

        // ---- OLHAR COM O MOUSE ----
        Vector2 mouse = Mouse.current.delta.ReadValue() * sensibilidade;

        transform.Rotate(0f, mouse.x, 0f);

        rotacaoVertical -= mouse.y;
        rotacaoVertical = Mathf.Clamp(rotacaoVertical, -85f, 85f);
        cameraPlayer.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);

        // ---- ANDAR COM WASD ----
        Vector2 teclas = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) teclas.y += 1;
        if (Keyboard.current.sKey.isPressed) teclas.y -= 1;
        if (Keyboard.current.dKey.isPressed) teclas.x += 1;
        if (Keyboard.current.aKey.isPressed) teclas.x -= 1;

        Vector3 direcao = transform.right * teclas.x + transform.forward * teclas.y;
        controller.SimpleMove(direcao.normalized * velocidade + empurrao);
        empurrao = Vector3.MoveTowards(empurrao, Vector3.zero, 20f * Time.deltaTime);

        // ---- CHUTAR COM ESPAÇO ----
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Vector3 origem = transform.position;

            if (Physics.Raycast(origem, transform.forward, out RaycastHit hit, distanciaChute))
            {
                Caixa caixa = hit.collider.GetComponent<Caixa>();

                if (caixa != null)
                {
                    Vector3 direcaoChute = -hit.normal;
                    direcaoChute = new Vector3(Mathf.Round(direcaoChute.x), 0f, Mathf.Round(direcaoChute.z));
                    caixa.Empurrar(direcaoChute);
                }
            }
        }
    }

    // Chamado pelo gnomo quando ele encosta no player
    public void ReceberEmpurrao(Vector3 forca)
    {
        empurrao = forca;
    }
}
```

## Script: `Assets/Scripts/Caixa.cs`

```csharp
using UnityEngine;

public class Caixa : MonoBehaviour
{
    public float velocidade = 5f;

    Vector3 destino;
    bool movendo = false;

    void Start()
    {
        destino = transform.position;
    }

    void Update()
    {
        if (movendo)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidade * Time.deltaTime);

            if (transform.position == destino)
            {
                movendo = false;
                ConstrutorFase.instancia.VerificarVitoria();
            }
        }
    }

    public void Empurrar(Vector3 direcao)
    {
        if (movendo) return;

        float tamanho = transform.localScale.x;

        // Tem parede, outra caixa ou gnomo no caminho? Então não move.
        // O raio sai mais baixo para enxergar os gnomos, que são baixinhos.
        Vector3 origem = transform.position + Vector3.down * 0.3f;
        if (Physics.Raycast(origem, direcao, tamanho))
            return;

        destino = transform.position + direcao * tamanho;
        movendo = true;
    }
}
```

## Script: `Assets/Scripts/Gnomo.cs`

```csharp
using UnityEngine;

public class Gnomo : MonoBehaviour
{
    public float velocidade = 4f;
    public float forcaEmpurrao = 8f;   // quanto o player é jogado longe
    public float recuo = 4f;           // quanto o gnomo volta depois de bater
    public float distanciaAtaque = 0.4f;
    public float tempoTonto = 0.8f;    // tempo parado depois de bater

    CharacterController controller;
    PlayerController player;
    Vector3 empurrao;
    float tonto = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        player = FindAnyObjectByType<PlayerController>();
    }

    void Update()
    {
        Vector3 movimento = Vector3.zero;

        if (tonto > 0f)
        {
            tonto -= Time.deltaTime;
        }
        else
        {
            Vector3 alvo = ConstrutorFase.instancia.ProximoPasso(transform.position, player.transform.position);
            Vector3 direcao = alvo - transform.position;
            direcao.y = 0f;

            if (direcao.magnitude > 0.05f)
            {
                movimento = direcao.normalized * velocidade;
                transform.rotation = Quaternion.LookRotation(direcao);
            }
        }

        controller.SimpleMove(movimento + empurrao);
        empurrao = Vector3.MoveTowards(empurrao, Vector3.zero, 15f * Time.deltaTime);

        // ---- ENCOSTOU NO PLAYER? ----
        Vector3 diferenca = player.transform.position - transform.position;
        diferenca.y = 0f;

        if (tonto <= 0f && diferenca.magnitude < distanciaAtaque)
        {
            Vector3 direcaoBatida = diferenca.normalized;
            player.ReceberEmpurrao(direcaoBatida * forcaEmpurrao);
            empurrao = -direcaoBatida * recuo;
            tonto = tempoTonto;
        }
    }
}
```

## Script: `Assets/Scripts/ConstrutorFase.cs`

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class Fase
{
    public string nome;
    public string[] mapa;
}

[System.Serializable]
public class ListaDeFases
{
    public Fase[] fases;
}

public class ConstrutorFase : MonoBehaviour
{
    public static ConstrutorFase instancia;

    [Header("Prefabs")]
    public GameObject prefabChao;
    public GameObject prefabParede;
    public GameObject prefabCaixa;
    public GameObject prefabPonto;

    [Header("Player")]
    public CharacterController player;

    [Header("Gnomos")]
    public GameObject prefabGnomo;
    public int minGnomos = 2;
    public int maxGnomos = 2;
    public int distanciaMinima = 3; // em quadrados, longe do player

    ListaDeFases lista;
    int faseAtual = 0;
    List<Transform> caixas = new List<Transform>();
    List<Transform> pontos = new List<Transform>();
    bool trocandoFase = false;

    HashSet<Vector2Int> chao = new HashSet<Vector2Int>();
    Vector2Int celulaPlayer;
    static readonly Vector2Int[] direcoes = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        CarregarFases();
        ConstruirFase(faseAtual);
    }

    // Lê o fases.json da pasta StreamingAssets (editável depois do build)
    void CarregarFases()
    {
        string caminho = Path.Combine(Application.streamingAssetsPath, "fases.json");
        lista = JsonUtility.FromJson<ListaDeFases>(File.ReadAllText(caminho));

        if (faseAtual >= lista.fases.Length)
            faseAtual = 0;
    }

    void Update()
    {
        // R relê o fases.json e reinicia a fase atual
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            CarregarFases();
            ConstruirFase(faseAtual);
        }
    }

    void ConstruirFase(int indice)
    {
        // 1. Apaga tudo da fase anterior
        CancelInvoke();
        foreach (Transform filho in transform)
            Destroy(filho.gameObject);
        caixas.Clear();
        pontos.Clear();
        chao.Clear();
        trocandoFase = false;

        Fase fase = lista.fases[indice];

        // 2. Lê o mapa caractere por caractere
        for (int linha = 0; linha < fase.mapa.Length; linha++)
        {
            string textoLinha = fase.mapa[linha];

            for (int coluna = 0; coluna < textoLinha.Length; coluna++)
            {
                char c = textoLinha[coluna];

                Criar(prefabChao, coluna, linha);

                if (c != '#')
                    chao.Add(new Vector2Int(coluna, linha));

                if (c == '#')
                    Criar(prefabParede, coluna, linha);

                if (c == '$' || c == '*')
                    caixas.Add(Criar(prefabCaixa, coluna, linha));

                if (c == '.' || c == '*' || c == '+')
                    pontos.Add(Criar(prefabPonto, coluna, linha));

                if (c == '@' || c == '+')
                    PosicionarPlayer(coluna, linha);
            }
        }

        // 3. Gnomos
        CriarGnomos();
    }

    Transform Criar(GameObject prefab, int coluna, int linha)
    {
        Vector3 posicao = new Vector3(coluna, prefab.transform.position.y, -linha);
        GameObject obj = Instantiate(prefab, posicao, prefab.transform.rotation, transform);
        return obj.transform;
    }

    void PosicionarPlayer(int coluna, int linha)
    {
        player.enabled = false;
        player.transform.position = new Vector3(coluna, player.height / 2f, -linha);
        player.transform.rotation = Quaternion.identity;
        player.enabled = true;
        celulaPlayer = new Vector2Int(coluna, linha);
    }

    Vector2Int Celula(Vector3 posicao)
    {
        return new Vector2Int(Mathf.RoundToInt(posicao.x), Mathf.RoundToInt(-posicao.z));
    }

    // Distância (em quadrados) de cada casa até "inicio", contornando paredes e caixas
    Dictionary<Vector2Int, int> Distancias(Vector2Int inicio)
    {
        HashSet<Vector2Int> bloqueadas = new HashSet<Vector2Int>();
        foreach (Transform caixa in caixas)
            bloqueadas.Add(Celula(caixa.position));

        Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();
        Queue<Vector2Int> fila = new Queue<Vector2Int>();
        dist[inicio] = 0;
        fila.Enqueue(inicio);

        while (fila.Count > 0)
        {
            Vector2Int atual = fila.Dequeue();
            foreach (Vector2Int d in direcoes)
            {
                Vector2Int vizinha = atual + d;
                if (!chao.Contains(vizinha) || bloqueadas.Contains(vizinha) || dist.ContainsKey(vizinha))
                    continue;
                dist[vizinha] = dist[atual] + 1;
                fila.Enqueue(vizinha);
            }
        }
        return dist;
    }

    // Para onde o gnomo deve andar agora para chegar no alvo
    public Vector3 ProximoPasso(Vector3 gnomo, Vector3 alvo)
    {
        Vector2Int origem = Celula(gnomo);
        Vector2Int destino = Celula(alvo);
        if (origem == destino)
            return alvo;

        Dictionary<Vector2Int, int> dist = Distancias(destino);

        Vector2Int melhor = origem;
        int menor = int.MaxValue;
        foreach (Vector2Int d in direcoes)
        {
            Vector2Int vizinha = origem + d;
            if (dist.TryGetValue(vizinha, out int n) && n < menor)
            {
                menor = n;
                melhor = vizinha;
            }
        }
        return new Vector3(melhor.x, gnomo.y, -melhor.y);
    }

    void CriarGnomos()
    {
        if (prefabGnomo == null) return;

        Dictionary<Vector2Int, int> dist = Distancias(celulaPlayer);
        List<Vector2Int> opcoes = new List<Vector2Int>();
        foreach (var par in dist)
            if (par.Value >= distanciaMinima)
                opcoes.Add(par.Key);

        int quantidade = Random.Range(minGnomos, maxGnomos + 1);
        for (int i = 0; i < quantidade && opcoes.Count > 0; i++)
        {
            int sorteio = Random.Range(0, opcoes.Count);
            Vector2Int casa = opcoes[sorteio];
            opcoes.RemoveAt(sorteio);
            Criar(prefabGnomo, casa.x, casa.y);
        }
    }

    public void VerificarVitoria()
    {
        if (trocandoFase) return;

        foreach (Transform ponto in pontos)
        {
            bool temCaixa = false;

            foreach (Transform caixa in caixas)
            {
                Vector3 diferenca = caixa.position - ponto.position;
                diferenca.y = 0f;

                if (diferenca.magnitude < 0.1f)
                {
                    temCaixa = true;
                    break;
                }
            }

            if (!temCaixa)
                return;
        }

        trocandoFase = true;
        Invoke(nameof(ProximaFase), 1f);
    }

    void ProximaFase()
    {
        faseAtual++;

        if (faseAtual >= lista.fases.Length)
            faseAtual = 0; // terminou todas: volta para a primeira

        ConstruirFase(faseAtual);
    }
}
```

## fases.json (`Assets/StreamingAssets/fases.json`)

Símbolos: `#` parede, `$` caixa, `.` ponto, `@` player, `*` caixa em cima de ponto, `+` player em cima de ponto, espaço = chão. Todas as fases abaixo já foram verificadas por um solver e têm solução.

```json
{
  "fases": [
    {
      "nome": "Fase 1",
      "mapa": [
        "#######",
        "#     #",
        "# @$ .#",
        "#     #",
        "#######"
      ]
    },
    {
      "nome": "Fase 2",
      "mapa": [
        "########",
        "#      #",
        "#  $ . #",
        "# @    #",
        "#  $ . #",
        "#      #",
        "########"
      ]
    },
    {
      "nome": "Fase 3",
      "mapa": [
        "######",
        "# . .##",
        "#     ###",
        "#.   @  #",
        "##  $$$ #",
        " ###  ###",
        "   #  #",
        "   ####"
      ]
    },
    {
      "nome": "Fase 4",
      "mapa": [
        "#########",
        "#       #",
        "# $$  #.#",
        "###@  . #",
        "  #$ # .#",
        "  #  ## #",
        "  #     #",
        "  #######"
      ]
    },
    {
      "nome": "Fase 5",
      "mapa": [
        " #####",
        " #  .###",
        "## #$$ #",
        "# .    #",
        "#.  #  #",
        "###$  ##",
        "  #@  #",
        "  #####"
      ]
    },
    {
      "nome": "Fase 6",
      "mapa": [
        "   #######",
        "####  #  #",
        "#     $  #",
        "#.  ###  #",
        "# .#### ##",
        "# +$   $ #",
        "#   #    #",
        "##########"
      ]
    },
    {
      "nome": "Fase 7",
      "mapa": [
        "    #####",
        "#####@  #",
        "# $ $$$ #",
        "#     ###",
        "#.     ##",
        "###.    #",
        "#  ##  .#",
        "# .     #",
        "#########"
      ]
    },
    {
      "nome": "Fase 8",
      "mapa": [
        "#########",
        "#       #",
        "#    $# #",
        "#  #$ $@#",
        "#    ## #",
        "#   #.  #",
        "#    .  #",
        "#    .  #",
        "#########"
      ]
    },
    {
      "nome": "Fase 9",
      "mapa": [
        "##########",
        "#@ # .   #",
        "#$$   #  #",
        "# $ ##. ##",
        "#.$ #    #",
        "# #      #",
        "#     .  #",
        "###    ###",
        "  ######"
      ]
    }
  ]
}
```

## Como conferir que deu certo

- Antes do Play, a cena tem só Directional Light, Global Volume, Player (com Main Camera dentro) e Construtor.
- No Play, aparece a Fase 1 com chão fino, paredes, uma caixa, um ponto roxo e 2 gnomos (ou menos, se a fase for pequena).
- Espaço empurra a caixa exatamente 1 quadrado; parede, outra caixa ou gnomo no caminho bloqueiam.
- O player passa em corredores de 1 quadrado entre caixa e parede.
- Os gnomos contornam paredes e caixas até o player e, ao encostar, jogam o player longe.
- Com todas as caixas nos pontos, a próxima fase aparece depois de 1 segundo. R reinicia a fase.