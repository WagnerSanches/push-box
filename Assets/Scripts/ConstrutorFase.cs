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
