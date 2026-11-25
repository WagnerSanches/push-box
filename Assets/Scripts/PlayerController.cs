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
