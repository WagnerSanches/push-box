using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Monta materiais, prefabs e a cena do PushBox automaticamente.
// Menu: Ferramentas → Montar Projeto. Pode ser rodado várias vezes sem duplicar nada.
public static class MontarProjeto
{
    const string pastaMateriais = "Assets/Materiais";
    const string pastaPrefabs = "Assets/Prefabs";
    const string pastaModeloGnomo = "Assets/Modelos/Gnomo";
    const string pastaModeloCaixa = "Assets/Modelos/Caixa";
    const string pastaTexturaGrama = "Assets/Texturas/Grama";
    const string pastaTexturaMuro = "Assets/Texturas/Muro";
    const int tamanhoMaximoTextura = 1024; // texturas menores = mais leve na placa de vídeo integrada
    const string caminhoCena = "Assets/Scenes/SampleScene.unity";
    const float velocidadePlayer = 5f;
    const float alturaGnomo = 0.3f;          // altura total do gnomo (pés em y = 0)
    const string animacaoGnomo = "Walking";  // animação do modelo que fica tocando em loop

    [MenuItem("Ferramentas/Montar Projeto")]
    public static void Montar()
    {
        // Pergunta se quer salvar a cena aberta antes de trocar
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // 1. Pastas
        CriarPasta("Assets", "Materiais");
        CriarPasta("Assets", "Prefabs");
        CriarPasta("Assets", "Scenes");

        // 2. Materiais
        Material matChao = CriarMaterial("Mat_Chao", new Color(0.90f, 0.90f, 0.90f));
        Material matParede = CriarMaterial("Mat_Parede", new Color(0.35f, 0.65f, 0.70f));

        // Texturas (se as pastas existirem): grama inteira em cada quadrado do chão;
        // no muro, a textura é um atlas, então usa só o recorte do painel do meio
        AplicarTexturas(matChao, pastaTexturaGrama, "Grama", Vector2.one, Vector2.zero);
        AplicarTexturas(matParede, pastaTexturaMuro, "Muro", new Vector2(0.26f, 0.26f), new Vector2(0.10f, 0.452f));
        Material matCaixa = CriarMaterial("Mat_Caixa", new Color(0.80f, 0.55f, 0.25f));
        Material matPonto = CriarMaterial("Mat_Ponto", new Color(0.60f, 0.30f, 0.80f));
        Material matGnomo = CriarMaterial("Mat_Gnomo", new Color(0.30f, 0.55f, 0.25f));
        AssetDatabase.SaveAssets();

        // 3. Prefabs
        GameObject prefabChao = CriarPrefabCubo("Chao", new Vector3(0f, -0.05f, 0f), new Vector3(1f, 0.1f, 1f), matChao, true, null);
        GameObject prefabParede = CriarPrefabCubo("Parede", new Vector3(0f, 0.5f, 0f), Vector3.one, matParede, true, null);
        GameObject prefabCaixa = CriarPrefabCaixa(matCaixa);
        GameObject prefabPonto = CriarPrefabCubo("Ponto", new Vector3(0f, 0.01f, 0f), new Vector3(0.8f, 0.02f, 0.8f), matPonto, false, null);

        // 4. Gnomo (com o filho "Visual")
        GameObject prefabGnomo = CriarPrefabGnomo(matGnomo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 5. Cena
        MontarCena(prefabChao, prefabParede, prefabCaixa, prefabPonto, prefabGnomo);

        // 6. Lista de build: SampleScene em primeiro
        AjustarCenasDoBuild();

        // 7. Player Settings: janela 1280x720
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;

        // Qualidade mais leve (sem SSAO, resolução 80%): roda bem em placa de vídeo integrada
        UsarQualidadeLeve();
        AssetDatabase.SaveAssets();

        Debug.Log("[Montador] Projeto montado com sucesso.");

        // 8. Aviso final
        EditorUtility.DisplayDialog("Montar Projeto",
            "Pronto! Materiais, prefabs e a cena SampleScene foram criados.\n\n" +
            "Próximos passos:\n" +
            "1. Abra Assets/Scenes/SampleScene (já deve estar aberta).\n" +
            "2. Aperte Play para testar.\n" +
            "3. Faça Check in no Unity Version Control.",
            "OK");
    }

    // ------------------------------------------------------------------
    // Pastas e materiais
    // ------------------------------------------------------------------

    static void CriarPasta(string pai, string nome)
    {
        if (!AssetDatabase.IsValidFolder(pai + "/" + nome))
            AssetDatabase.CreateFolder(pai, nome);
    }

    static Material CriarMaterial(string nome, Color cor)
    {
        string caminho = pastaMateriais + "/" + nome + ".mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            Debug.LogError("[Montador] Shader 'Universal Render Pipeline/Lit' não encontrado. O projeto usa URP?");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, caminho);
        }
        else if (shader != null && mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", cor);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ------------------------------------------------------------------
    // Prefabs
    // ------------------------------------------------------------------

    static GameObject CriarPrefabCubo(string nome, Vector3 posicao, Vector3 escala, Material mat, bool manterCollider, System.Action<GameObject> extra)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = nome;
        obj.transform.position = posicao;
        obj.transform.localScale = escala;
        obj.GetComponent<MeshRenderer>().sharedMaterial = mat;

        if (!manterCollider)
            Object.DestroyImmediate(obj.GetComponent<Collider>());

        extra?.Invoke(obj);

        return SalvarPrefab(obj, nome);
    }

    static GameObject SalvarPrefab(GameObject obj, string nome)
    {
        string caminho = pastaPrefabs + "/" + nome + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, caminho);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    // Caixa: usa o modelo de Assets/Modelos/Caixa se existir; senão, um cubo com Mat_Caixa.
    // A raiz sempre tem escala 1 e BoxCollider de 1x1x1 (o script Caixa depende disso).
    static GameObject CriarPrefabCaixa(Material matCaixa)
    {
        GameObject modelo = ProcurarModelo(pastaModeloCaixa);
        if (modelo == null)
        {
            Debug.Log("[Montador] Caixa: nenhum modelo em " + pastaModeloCaixa + ", usando cubo com Mat_Caixa.");
            return CriarPrefabCubo("Caixa", new Vector3(0f, 0.5f, 0f), Vector3.one, matCaixa, true,
                obj => obj.AddComponent<Caixa>().velocidade = 5f);
        }

        // Configura as texturas antes de usar (pode reimportar)
        string caminhoModelo = AssetDatabase.GetAssetPath(modelo);
        ConfigurarTexturas(pastaModeloCaixa);

        GameObject raiz = new GameObject("Caixa");
        raiz.transform.position = new Vector3(0f, 0.5f, 0f);
        raiz.AddComponent<BoxCollider>(); // tamanho 1x1x1, centro (0,0,0)
        raiz.AddComponent<Caixa>().velocidade = 5f;

        GameObject visual = (GameObject)Object.Instantiate(modelo);
        visual.name = "Visual";
        visual.transform.SetParent(raiz.transform, false);
        visual.transform.localPosition = Vector3.zero;

        foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);

        AjustarEscalaModelo(raiz.transform, visual.transform, 1f, false, "Caixa");

        // Troca cada material do modelo (b_box, fb_box, ...) por um URP com as texturas de mesmo nome
        Dictionary<string, Material> criados = new Dictionary<string, Material>();
        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string nomeOriginal = mats[i].name;
                if (!criados.TryGetValue(nomeOriginal, out Material novo))
                {
                    novo = CriarMaterialTexturizado("Mat_Caixa_" + nomeOriginal, pastaModeloCaixa, nomeOriginal);
                    criados[nomeOriginal] = novo;
                }
                mats[i] = novo;
            }
            r.sharedMaterials = mats;
        }

        Debug.Log("[Montador] Caixa: usando o modelo '" + caminhoModelo + "' com " + criados.Count + " materiais.");
        return SalvarPrefab(raiz, "Caixa");
    }

    // Material URP Lit com as texturas <prefixo>_Base_Color, _Normal_DirectX e _Mixed_AO (as que existirem)
    static Material CriarMaterialTexturizado(string nome, string pasta, string prefixo)
    {
        Material mat = CriarMaterial(nome, Color.white);

        Texture2D cor = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/" + prefixo + "_Base_Color.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/" + prefixo + "_Normal_DirectX.png");
        Texture2D ao = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/" + prefixo + "_Mixed_AO.png");

        mat.SetTexture("_BaseMap", cor);

        mat.SetTexture("_BumpMap", normal);
        if (normal != null) mat.EnableKeyword("_NORMALMAP");
        else mat.DisableKeyword("_NORMALMAP");

        mat.SetTexture("_OcclusionMap", ao);
        if (ao != null) mat.EnableKeyword("_OCCLUSIONMAP");
        else mat.DisableKeyword("_OCCLUSIONMAP");

        mat.SetFloat("_Smoothness", 0.2f); // madeira fosca
        EditorUtility.SetDirty(mat);

        if (cor == null)
            Debug.LogWarning("[Montador] Textura " + prefixo + "_Base_Color.png não encontrada em " + pasta + ".");
        return mat;
    }

    // Coloca <prefixo>_BaseColor.png e <prefixo>_Normal.png da pasta no material.
    // Sem a textura de cor, o material fica só com a cor lisa da tabela.
    static void AplicarTexturas(Material mat, string pasta, string prefixo, Vector2 repeticao, Vector2 deslocamento)
    {
        Texture2D cor = null;
        Texture2D normal = null;

        if (AssetDatabase.IsValidFolder(pasta))
        {
            ConfigurarTexturas(pasta);
            cor = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/" + prefixo + "_BaseColor.png");
            normal = AssetDatabase.LoadAssetAtPath<Texture2D>(pasta + "/" + prefixo + "_Normal.png");
        }

        if (cor == null)
        {
            // Sem textura: limpa o que tinha de uma montagem anterior
            mat.SetTexture("_BaseMap", null);
            mat.SetTexture("_BumpMap", null);
            mat.DisableKeyword("_NORMALMAP");
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetTextureOffset("_BaseMap", Vector2.zero);
            EditorUtility.SetDirty(mat);
            return;
        }

        mat.SetColor("_BaseColor", Color.white); // a cor vem da textura
        mat.SetTexture("_BaseMap", cor);
        mat.SetTextureScale("_BaseMap", repeticao);
        mat.SetTextureOffset("_BaseMap", deslocamento);

        mat.SetTexture("_BumpMap", normal);
        if (normal != null) mat.EnableKeyword("_NORMALMAP");
        else mat.DisableKeyword("_NORMALMAP");

        mat.SetFloat("_Smoothness", 0.1f); // grama e cimento são foscos
        EditorUtility.SetDirty(mat);

        Debug.Log("[Montador] " + mat.name + ": textura '" + prefixo + "' aplicada" + (normal != null ? " (com normal map)." : "."));
    }

    // Ajusta a importação das texturas da pasta: tamanho máximo, normal map (DirectX → inverte o verde) e AO linear
    static void ConfigurarTexturas(string pasta)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { pasta }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (imp == null) continue;

            bool mudou = false;
            if (imp.maxTextureSize != tamanhoMaximoTextura)
            {
                imp.maxTextureSize = tamanhoMaximoTextura;
                mudou = true;
            }

            if (caminho.Contains("_Normal"))
            {
                if (imp.textureType != TextureImporterType.NormalMap)
                {
                    imp.textureType = TextureImporterType.NormalMap;
                    mudou = true;
                }
                bool directX = caminho.Contains("DirectX");
                if (imp.flipGreenChannel != directX)
                {
                    imp.flipGreenChannel = directX;
                    mudou = true;
                }
            }
            else if (caminho.Contains("_AO") && imp.sRGBTexture)
            {
                imp.sRGBTexture = false;
                mudou = true;
            }

            if (mudou)
                imp.SaveAndReimport();
        }
    }

    static GameObject CriarPrefabGnomo(Material matGnomo)
    {
        GameObject raiz = new GameObject("Gnomo");
        raiz.transform.position = new Vector3(0f, alturaGnomo / 2f, 0f);

        CharacterController cc = raiz.AddComponent<CharacterController>();
        cc.height = alturaGnomo;
        cc.radius = 0.1f;
        cc.skinWidth = 0.01f;
        cc.stepOffset = 0.05f;
        cc.minMoveDistance = 0f;
        cc.center = Vector3.zero;

        Gnomo gnomo = raiz.AddComponent<Gnomo>();
        gnomo.velocidade = velocidadePlayer * 0.75f; // 75% da velocidade do player (3.75)
        gnomo.forcaEmpurrao = 8f;
        gnomo.recuo = 4f;
        gnomo.distanciaAtaque = 0.4f;
        gnomo.tempoTonto = 0.8f;

        GameObject modelo = ProcurarModeloGnomo();
        if (modelo != null)
        {
            // Prepara a animação antes de instanciar (pode reimportar o modelo)
            string caminhoModelo = AssetDatabase.GetAssetPath(modelo);
            RuntimeAnimatorController controlador = CriarAnimacaoGnomo(caminhoModelo);
            modelo = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoModelo);

            GameObject visual = (GameObject)Object.Instantiate(modelo);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = Vector3.zero;

            // O visual não pode atrapalhar a física do gnomo
            foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            AjustarEscalaModelo(raiz.transform, visual.transform, alturaGnomo, true, "Gnomo");

            if (controlador != null)
            {
                Animator animator = visual.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = visual.AddComponent<Animator>();
                    animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(caminhoModelo);
                }
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false; // quem move o gnomo é o script, não a animação
            }

            Debug.Log("[Montador] Gnomo: usando o modelo '" + caminhoModelo + "'.");
        }
        else
        {
            GameObject capsula = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsula.name = "Visual";
            Object.DestroyImmediate(capsula.GetComponent<Collider>());
            capsula.transform.SetParent(raiz.transform, false);
            capsula.transform.localPosition = Vector3.zero;
            capsula.transform.localScale = new Vector3(0.2f, alturaGnomo / 2f, 0.2f);
            capsula.GetComponent<MeshRenderer>().sharedMaterial = matGnomo;
            Debug.Log("[Montador] Gnomo: nenhum modelo em " + pastaModeloGnomo + ", usando cápsula provisória.");
        }

        return SalvarPrefab(raiz, "Gnomo");
    }

    static GameObject ProcurarModeloGnomo()
    {
        return ProcurarModelo(pastaModeloGnomo);
    }

    // Primeiro modelo (FBX etc.) encontrado na pasta
    static GameObject ProcurarModelo(string pasta)
    {
        if (!AssetDatabase.IsValidFolder(pasta))
            return null;

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { pasta });
        if (guids.Length == 0)
            return null;

        return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    // Cria um Animator Controller que toca a animação "Walking" do modelo em loop
    static RuntimeAnimatorController CriarAnimacaoGnomo(string caminhoModelo)
    {
        ModelImporter importador = AssetImporter.GetAtPath(caminhoModelo) as ModelImporter;
        if (importador == null)
            return null;

        // Liga o loop da animação de andar nas configurações de importação
        ModelImporterClipAnimation[] clipes = importador.clipAnimations;
        if (clipes == null || clipes.Length == 0)
            clipes = importador.defaultClipAnimations;

        bool mudou = false;
        foreach (ModelImporterClipAnimation c in clipes)
        {
            if (c.name.Contains(animacaoGnomo) && !c.loopTime)
            {
                c.loopTime = true;
                mudou = true;
            }
        }
        if (mudou)
        {
            importador.clipAnimations = clipes;
            importador.SaveAndReimport();
        }

        AnimationClip clipe = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminhoModelo))
        {
            if (o is AnimationClip a && a.name.Contains(animacaoGnomo) && !a.name.StartsWith("__preview__"))
            {
                clipe = a;
                break;
            }
        }

        if (clipe == null)
        {
            Debug.LogWarning("[Montador] Animação '" + animacaoGnomo + "' não encontrada no modelo; o gnomo vai ficar parado.");
            return null;
        }

        // Recria o controller do zero para não acumular estados repetidos
        CriarPasta("Assets", "Animacoes");
        string caminhoControlador = "Assets/Animacoes/Gnomo.controller";
        AssetDatabase.DeleteAsset(caminhoControlador);
        AnimatorController controlador = AnimatorController.CreateAnimatorControllerAtPathWithClip(caminhoControlador, clipe);

        Debug.Log("[Montador] Gnomo: animação '" + clipe.name + "' em loop.");
        return controlador;
    }

    // Ajusta a escala do modelo, deixa a base em y = 0 do mundo e centraliza em x/z.
    // pelaAltura = true: a altura vira "tamanho". false: o maior lado vira "tamanho" (cabe no quadrado).
    static void AjustarEscalaModelo(Transform raiz, Transform visual, float tamanho, bool pelaAltura, string nome)
    {
        Bounds limites;
        float medida = 0f;
        if (CalcularLimites(visual, out limites))
            medida = pelaAltura ? limites.size.y : Mathf.Max(limites.size.x, limites.size.y, limites.size.z);

        if (medida <= 0.0001f)
        {
            Debug.LogWarning("[Montador] O modelo " + nome + " não tem Renderers com tamanho; a escala não foi ajustada.");
            return;
        }

        // Escala em torno do pivô do visual e calcula onde os limites vão parar,
        // sem medir de novo (assim a medição é feita uma vez só, com escala 1)
        float fator = tamanho / medida;
        Vector3 pivo = visual.position;
        visual.localScale = visual.localScale * fator;

        Vector3 minimo = pivo + (limites.min - pivo) * fator;
        Vector3 centro = pivo + (limites.center - pivo) * fator;
        Vector3 ajuste = new Vector3(
            raiz.position.x - centro.x,
            0f - minimo.y,
            raiz.position.z - centro.z);
        visual.position += ajuste;

        Debug.Log("[Montador] " + nome + ": tamanho original do modelo " + limites.size.ToString("0.###") +
                  ", escala aplicada " + fator.ToString("0.####") + ".");
    }

    // Mede os limites pelos vértices de verdade (os bounds do SkinnedMeshRenderer
    // no editor são aproximados e deixavam o gnomo pequeno e flutuando)
    static bool CalcularLimites(Transform alvo, out Bounds limites)
    {
        limites = new Bounds();
        bool achou = false;

        foreach (Renderer r in alvo.GetComponentsInChildren<Renderer>(true))
        {
            Vector3[] vertices = null;
            Matrix4x4 matriz = r.transform.localToWorldMatrix;
            Mesh temporaria = null;

            if (r is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                // Malha na pose atual; BakeMesh sem escala devolve vértices já no tamanho do mundo
                temporaria = new Mesh();
                skinned.BakeMesh(temporaria, false);
                vertices = temporaria.vertices;
                matriz = Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
            }
            else
            {
                MeshFilter filtro = r.GetComponent<MeshFilter>();
                if (filtro != null && filtro.sharedMesh != null)
                    vertices = filtro.sharedMesh.vertices;
            }

            if (vertices == null || vertices.Length == 0)
            {
                // Sem malha legível: usa os bounds do próprio Renderer
                Encapsular(ref limites, ref achou, r.bounds.min);
                Encapsular(ref limites, ref achou, r.bounds.max);
            }
            else
            {
                foreach (Vector3 v in vertices)
                    Encapsular(ref limites, ref achou, matriz.MultiplyPoint3x4(v));
            }

            if (temporaria != null)
                Object.DestroyImmediate(temporaria);
        }
        return achou;
    }

    static void Encapsular(ref Bounds limites, ref bool achou, Vector3 ponto)
    {
        if (!achou)
        {
            limites = new Bounds(ponto, Vector3.zero);
            achou = true;
        }
        else
        {
            limites.Encapsulate(ponto);
        }
    }

    // ------------------------------------------------------------------
    // Cena
    // ------------------------------------------------------------------

    static void MontarCena(GameObject prefabChao, GameObject prefabParede, GameObject prefabCaixa, GameObject prefabPonto, GameObject prefabGnomo)
    {
        Scene cena;
        if (File.Exists(caminhoCena))
        {
            cena = EditorSceneManager.OpenScene(caminhoCena, OpenSceneMode.Single);
        }
        else
        {
            cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(cena, caminhoCena);
        }

        // ---- Player ----
        GameObject player = ProcurarNaCena(cena, "Player");
        if (player == null)
            player = new GameObject("Player");

        player.transform.SetParent(null);
        player.transform.position = new Vector3(0f, 0.25f, 0f);
        player.transform.rotation = Quaternion.identity;
        player.transform.localScale = Vector3.one;

        CharacterController cc = PegarOuAdicionar<CharacterController>(player);
        cc.height = 0.5f;
        cc.radius = 0.2f;
        cc.skinWidth = 0.02f;
        cc.stepOffset = 0.1f;
        cc.minMoveDistance = 0f;
        cc.center = Vector3.zero;

        PlayerController pc = PegarOuAdicionar<PlayerController>(player);
        pc.velocidade = velocidadePlayer;
        pc.sensibilidade = 0.1f;
        pc.distanciaChute = 1.5f;

        // ---- Câmera ----
        GameObject cameraObj = ProcurarNaCena(cena, "Main Camera");
        if (cameraObj == null || cameraObj.GetComponent<Camera>() == null)
        {
            Camera qualquer = ProcurarComponenteNaCena<Camera>(cena);
            cameraObj = qualquer != null ? qualquer.gameObject : new GameObject("Main Camera");
        }
        cameraObj.name = "Main Camera";
        cameraObj.tag = "MainCamera";

        Camera cam = PegarOuAdicionar<Camera>(cameraObj);
        cam.nearClipPlane = 0.01f;
        PegarOuAdicionar<AudioListener>(cameraObj);

        cameraObj.transform.SetParent(player.transform, false);
        cameraObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        cameraObj.transform.localRotation = Quaternion.identity;
        cameraObj.transform.localScale = Vector3.one;

        pc.cameraPlayer = cameraObj.transform;
        EditorUtility.SetDirty(pc);

        // ---- Construtor ----
        GameObject construtorObj = ProcurarNaCena(cena, "Construtor");
        if (construtorObj == null)
            construtorObj = new GameObject("Construtor");

        construtorObj.transform.SetParent(null);
        construtorObj.transform.position = Vector3.zero;
        construtorObj.transform.rotation = Quaternion.identity;
        construtorObj.transform.localScale = Vector3.one;

        ConstrutorFase construtor = PegarOuAdicionar<ConstrutorFase>(construtorObj);
        construtor.prefabChao = prefabChao;
        construtor.prefabParede = prefabParede;
        construtor.prefabCaixa = prefabCaixa;
        construtor.prefabPonto = prefabPonto;
        construtor.prefabGnomo = prefabGnomo;
        construtor.player = cc;
        construtor.minGnomos = 2;
        construtor.maxGnomos = 2;
        construtor.distanciaMinima = 3;
        EditorUtility.SetDirty(construtor);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
    }

    // Procura um objeto pelo nome em toda a cena (inclusive inativos e filhos)
    static GameObject ProcurarNaCena(Scene cena, string nome)
    {
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nome)
                    return t.gameObject;
            }
        }
        return null;
    }

    static T ProcurarComponenteNaCena<T>(Scene cena) where T : Component
    {
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            T comp = raiz.GetComponentInChildren<T>(true);
            if (comp != null)
                return comp;
        }
        return null;
    }

    static T PegarOuAdicionar<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null)
            comp = obj.AddComponent<T>();
        return comp;
    }

    // ------------------------------------------------------------------
    // Qualidade
    // ------------------------------------------------------------------

    static void UsarQualidadeLeve()
    {
        int indice = System.Array.IndexOf(QualitySettings.names, "Mobile");
        if (indice < 0)
        {
            Debug.LogWarning("[Montador] Nível de qualidade 'Mobile' não encontrado; a qualidade não foi alterada.");
            return;
        }

        // Nível usado no editor agora
        QualitySettings.SetQualityLevel(indice, true);

        // Nível padrão do build para PC (Standalone)
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (assets.Length == 0) return;

        SerializedObject so = new SerializedObject(assets[0]);
        so.FindProperty("m_CurrentQuality").intValue = indice;

        SerializedProperty porPlataforma = so.FindProperty("m_PerPlatformDefaultQuality");
        if (porPlataforma != null && porPlataforma.isArray)
        {
            for (int i = 0; i < porPlataforma.arraySize; i++)
            {
                SerializedProperty par = porPlataforma.GetArrayElementAtIndex(i);
                if (par.FindPropertyRelative("first").stringValue == "Standalone")
                    par.FindPropertyRelative("second").intValue = indice;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[Montador] Qualidade ajustada para 'Mobile' (mais leve).");
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    static void AjustarCenasDoBuild()
    {
        List<EditorBuildSettingsScene> cenas = new List<EditorBuildSettingsScene>();
        cenas.Add(new EditorBuildSettingsScene(caminhoCena, true));

        foreach (EditorBuildSettingsScene c in EditorBuildSettings.scenes)
        {
            if (c.path != caminhoCena)
                cenas.Add(c);
        }

        EditorBuildSettings.scenes = cenas.ToArray();
    }
}
