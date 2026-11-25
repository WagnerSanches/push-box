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
