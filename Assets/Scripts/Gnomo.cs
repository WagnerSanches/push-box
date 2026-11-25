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
