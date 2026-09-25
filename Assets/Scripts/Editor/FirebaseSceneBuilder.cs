using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Menú "SID2 > Construir escena Firebase": crea toda la interfaz (acceso, menú,
// juego y game over) con estética de terminal verde en la escena abierta
// y conecta las referencias de los scripts.
public static class FirebaseSceneBuilder
{
    private const string CanvasName = "FirebaseUI";
    private const string ServicesName = "FirebaseServices";
    private const string GameName = "Game";

    private const string FontPath = "Assets/Fonts/VT323-Regular.ttf";
    private const string FontAssetPath = "Assets/Fonts/VT323-Regular SDF.asset";

    private static readonly Color Background = TerminalTheme.Background;
    private static readonly Color Green = TerminalTheme.Green;
    private static readonly Color DimGreen = TerminalTheme.DimGreen;

    private static TMP_FontAsset _font;

    [MenuItem("SID2/Construir escena Firebase")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();

        GameObject existing = FindRoot(scene, CanvasName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Construir escena Firebase",
                "Ya existe una interfaz de Firebase en la escena. ¿Quieres reemplazarla?", "Reemplazar", "Cancelar"))
            {
                return;
            }
            Object.DestroyImmediate(existing);
            DestroyRoot(scene, ServicesName);
            DestroyRoot(scene, GameName);
        }

        _font = GetOrCreateFont();

        DisableOldUI(scene);
        EnsureEventSystem(scene);
        SetupCamera();

        // ---------- Servicios y juego ----------
        var services = new GameObject(ServicesName);
        services.AddComponent<FirebaseService>();
        services.AddComponent<AuthStateHandler>();
        Undo.RegisterCreatedObjectUndo(services, "Construir escena Firebase");

        var game = new GameObject(GameName);
        CatchGameManager gameManager = game.AddComponent<CatchGameManager>();
        Undo.RegisterCreatedObjectUndo(game, "Construir escena Firebase");

        // ---------- Canvas ----------
        var canvasObject = new GameObject(CanvasName, typeof(RectTransform));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasObject, "Construir escena Firebase");
        Transform root = canvasObject.transform;

        UIManager uiManager = canvasObject.AddComponent<UIManager>();

        GameObject loginPanel = BuildLoginPanel(root);
        GameObject homePanel = BuildHomePanel(root);
        GameObject gamePanel = BuildGamePanel(root, gameManager);
        GameObject gameOverPanel = BuildGameOverPanel(root, gameManager);

        Set(uiManager, "_loginPanel", loginPanel);
        Set(uiManager, "_homePanel", homePanel);
        Set(uiManager, "_gamePanel", gamePanel);
        Set(uiManager, "_gameOverPanel", gameOverPanel);

        BuildChrome(root);

        // Mensajes de estado
        TMP_Text status = CreateText(root, "StatusMessage", "", 40, TextAlignmentOptions.Center, Green);
        Anchor(status.rectTransform, new Vector2(0.15f, 0f), new Vector2(0.85f, 0f), new Vector2(0, 230), new Vector2(0, 280));
        var statusMessage = status.gameObject.AddComponent<StatusMessage>();
        Set(statusMessage, "_label", status);

        // Líneas de barrido CRT por encima de todo
        var scanlines = CreateUIObject(root, "Scanlines");
        var scanlineImage = scanlines.AddComponent<RawImage>();
        scanlineImage.raycastTarget = false;
        scanlineImage.color = Color.clear; // CrtOverlay genera la textura al dar Play
        scanlines.AddComponent<CrtOverlay>();
        Stretch(scanlines.GetComponent<RectTransform>());

        // Deja solo el acceso visible en el editor
        homePanel.SetActive(false);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasObject;
        Debug.Log("Escena Firebase construida. Guarda la escena (Ctrl+S) y dale Play.");
    }

    // ================= Paneles =================

    private static GameObject BuildLoginPanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "LoginPanel");
        CreateHeader(panel.transform, "ACCESO");
        Transform content = CreateContent(panel.transform, 820);

        TMP_InputField email = CreateInputRow(content, "Email", "CORREO", "tu@correo.com", TMP_InputField.ContentType.EmailAddress);
        TMP_InputField password = CreateInputRow(content, "Password", "CLAVE", "******", TMP_InputField.ContentType.Password);
        CreateSpacer(content, 12);
        CreateDivider(content);
        CreateSpacer(content, 12);

        Button login = CreateMenuItem(content, "LoginButton", "ENTRAR");
        var buttonLogin = login.gameObject.AddComponent<ButtonLogin>();
        Set(buttonLogin, "_loginButton", login);
        Set(buttonLogin, "_emailInputField", email);
        Set(buttonLogin, "_passwordInputField", password);

        Button register = CreateMenuItem(content, "RegisterButton", "CREAR CUENTA");
        var buttonRegister = register.gameObject.AddComponent<ButtonRegister>();
        Set(buttonRegister, "_registerButton", register);
        Set(buttonRegister, "_emailInputField", email);
        Set(buttonRegister, "_passwordInputField", password);

        Button reset = CreateMenuItem(content, "ResetPasswordButton", "OLVIDÉ MI CLAVE");
        var buttonReset = reset.gameObject.AddComponent<ButtonResetPassword>();
        Set(buttonReset, "_resetButton", reset);
        Set(buttonReset, "_emailInputField", email);

        return panel;
    }

    private static GameObject BuildHomePanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "HomePanel");
        CreateHeader(panel.transform, "MENÚ");

        var columns = CreateUIObject(panel.transform, "Columns");
        var columnsRect = columns.GetComponent<RectTransform>();
        columnsRect.anchorMin = columnsRect.anchorMax = new Vector2(0.5f, 1f);
        columnsRect.pivot = new Vector2(0.5f, 1f);
        columnsRect.sizeDelta = new Vector2(1440, 520);
        columnsRect.anchoredPosition = new Vector2(0, -250);
        var columnsLayout = columns.AddComponent<HorizontalLayoutGroup>();
        columnsLayout.spacing = 120;
        columnsLayout.childControlWidth = true;
        columnsLayout.childControlHeight = true;
        columnsLayout.childForceExpandWidth = false;
        columnsLayout.childForceExpandHeight = true;

        // Columna izquierda: jugador y opciones
        Transform left = CreateColumn(columns.transform, "Player", 620);
        TMP_Text greeting = CreateText(left, "Greeting", "> JUGADOR", 60, TextAlignmentOptions.Left, Green, 64);
        TMP_Text best = CreateText(left, "BestScore", "RÉCORD : ...", 44, TextAlignmentOptions.Left, Green, 52);
        var profileLabels = left.gameObject.AddComponent<ProfileLabels>();
        Set(profileLabels, "_greetingLabel", greeting);
        Set(profileLabels, "_bestScoreLabel", best);

        CreateSpacer(left, 8);
        CreateDivider(left);
        CreateSpacer(left, 8);

        Button play = CreateMenuItem(left, "PlayButton", "JUGAR");
        AddGameButton(play, GameButtonAction.StartGame);

        Button logout = CreateMenuItem(left, "LogoutButton", "SALIR");
        var buttonLogout = logout.gameObject.AddComponent<ButtonLogout>();
        Set(buttonLogout, "_logoutButton", logout);

        CreateSpacer(left, 8);
        CreateDivider(left);
        CreateSpacer(left, 8);
        CreateText(left, "Legend", "ATRAPA  : CUADROS\nESQUIVA : CÍRCULOS", 38, TextAlignmentOptions.Left, Green, 90);

        // Columna derecha: tabla de puntajes
        Transform board = CreateColumn(columns.transform, "Leaderboard", 700);
        CreateText(board, "Title", "---- TOP 10 ----", 48, TextAlignmentOptions.Center, Green, 60);

        GameObject header = CreateLeaderboardRow(board, "Header", "#", "JUGADOR", "PUNTOS", DimGreen);
        header.GetComponent<Image>().color = Color.clear;

        var rows = CreateUIObject(board, "Rows");
        var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 0;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        rows.AddComponent<LayoutElement>().flexibleHeight = 1;

        GameObject template = CreateLeaderboardRow(rows.transform, "RowTemplate", "01", "JUGADOR", "0", Green);
        template.SetActive(false);

        TMP_Text empty = CreateText(rows.transform, "EmptyLabel", "CARGANDO...", 38, TextAlignmentOptions.Center, DimGreen, 60);

        var leaderboard = board.gameObject.AddComponent<Leaderboard>();
        Set(leaderboard, "_rowsContainer", rows.transform);
        Set(leaderboard, "_rowTemplate", template);
        Set(leaderboard, "_emptyLabel", empty);

        return panel;
    }

    private static GameObject BuildGamePanel(Transform root, CatchGameManager gameManager)
    {
        // Panel transparente: el juego se ve detrás (mundo 2D).
        GameObject panel = CreatePanel(root, "GamePanel");

        TMP_Text score = CreateText(panel.transform, "ScoreLabel", "PUNTOS : 0000", 52, TextAlignmentOptions.Left, Green);
        Anchor(score.rectTransform, new Vector2(0f, 1f), new Vector2(0.4f, 1f), new Vector2(80, -170), new Vector2(0, -100));

        TMP_Text lives = CreateText(panel.transform, "LivesLabel", "VIDAS : 3", 52, TextAlignmentOptions.Right, Green);
        Anchor(lives.rectTransform, new Vector2(0.6f, 1f), new Vector2(1f, 1f), new Vector2(0, -170), new Vector2(-80, -100));

        var endHolder = CreateUIObject(panel.transform, "EndGame");
        var endRect = endHolder.GetComponent<RectTransform>();
        endRect.anchorMin = endRect.anchorMax = new Vector2(0.5f, 1f);
        endRect.pivot = new Vector2(0.5f, 1f);
        endRect.anchoredPosition = new Vector2(0, -104);
        endRect.sizeDelta = new Vector2(400, 60);
        var endLayout = endHolder.AddComponent<HorizontalLayoutGroup>();
        endLayout.childAlignment = TextAnchor.MiddleCenter;
        endLayout.childControlWidth = true;
        endLayout.childControlHeight = true;
        endLayout.childForceExpandWidth = false;
        Button end = CreateMenuItem(endHolder.transform, "EndGameButton", "[ TERMINAR ]");
        AddGameButton(end, GameButtonAction.EndGame);

        Set(gameManager, "_scoreLabel", score);
        Set(gameManager, "_livesLabel", lives);
        return panel;
    }

    private static GameObject BuildGameOverPanel(Transform root, CatchGameManager gameManager)
    {
        GameObject panel = CreatePanel(root, "GameOverPanel");
        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(Background.r, Background.g, Background.b, 0.9f);

        CreateHeader(panel.transform, "FIN DEL JUEGO");
        Transform content = CreateContent(panel.transform, 700);

        TMP_Text finalScore = CreateText(content, "FinalScore", "0", 180, TextAlignmentOptions.Center, Green, 170);
        finalScore.fontStyle = FontStyles.Bold;
        finalScore.GetComponent<LayoutElement>().flexibleWidth = 1;
        TMP_Text record = CreateText(content, "RecordLabel", "", 44, TextAlignmentOptions.Center, Green, 56);
        record.GetComponent<LayoutElement>().flexibleWidth = 1;

        CreateSpacer(content, 12);
        CreateDivider(content);
        CreateSpacer(content, 12);

        Button again = CreateMenuItem(content, "PlayAgainButton", "JUGAR DE NUEVO");
        AddGameButton(again, GameButtonAction.StartGame);
        CreateNavigationButton(content, "MenuButton", "MENÚ", AppScreen.Home);

        Set(gameManager, "_finalScoreLabel", finalScore);
        Set(gameManager, "_recordLabel", record);

        return panel;
    }

    // Marco, esquinas y pie de página visibles en todas las pantallas.
    private static void BuildChrome(Transform root)
    {
        GameObject chrome = CreateUIObject(root, "Chrome");
        Stretch(chrome.GetComponent<RectTransform>());

        // Marco fino
        const float inset = 28f;
        const float thickness = 2f;
        Color frameColor = new Color(DimGreen.r, DimGreen.g, DimGreen.b, 0.6f);
        CreateLine(chrome.transform, "FrameTop", frameColor, new Vector2(0, 1), new Vector2(1, 1), new Vector2(inset, -inset - thickness), new Vector2(-inset, -inset));
        CreateLine(chrome.transform, "FrameBottom", frameColor, new Vector2(0, 0), new Vector2(1, 0), new Vector2(inset, inset), new Vector2(-inset, inset + thickness));
        CreateLine(chrome.transform, "FrameLeft", frameColor, new Vector2(0, 0), new Vector2(0, 1), new Vector2(inset, inset), new Vector2(inset + thickness, -inset));
        CreateLine(chrome.transform, "FrameRight", frameColor, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-inset - thickness, inset), new Vector2(-inset, -inset));

        TMP_Text topLeft = CreateText(chrome.transform, "TopLeft", "SID2 2026", 40, TextAlignmentOptions.Left, Green);
        Anchor(topLeft.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(80, -100), new Vector2(0, -50));

        TMP_Text topRight = CreateText(chrome.transform, "TopRight", ProjectInfo.GameName.ToUpperInvariant() + " >", 40, TextAlignmentOptions.Right, Green);
        Anchor(topRight.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(-80, -50));

        // Pie de página con el nombre completo
        TMP_Text footer = CreateText(chrome.transform, "AuthorFooter", ProjectInfo.AuthorFullName, 44, TextAlignmentOptions.Center, Green);
        Anchor(footer.rectTransform, new Vector2(0.25f, 0), new Vector2(0.75f, 0), new Vector2(0, 60), new Vector2(0, 110));
        var authorFooter = footer.gameObject.AddComponent<AuthorFooter>();
        Set(authorFooter, "_label", footer);

        TMP_Text stack = CreateText(chrome.transform, "Stack", "UNITY + FIREBASE", 30, TextAlignmentOptions.Right, Green);
        Anchor(stack.rectTransform, new Vector2(0.6f, 0), new Vector2(1, 0), new Vector2(0, 44), new Vector2(-80, 84));
    }

    // ================= Helpers de UI =================

    private static GameObject CreateUIObject(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = CreateUIObject(parent, name);
        Stretch(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = CreateUIObject(parent, name);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateLine(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image line = CreateImage(parent, name, color);
        Anchor(line.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
    }

    // "-------- TITULO --------" arriba y al centro.
    private static void CreateHeader(Transform parent, string title)
    {
        TMP_Text header = CreateText(parent, "Header", "-------- " + title + " --------", 72, TextAlignmentOptions.Center, Green);
        Anchor(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -220), new Vector2(0, -130));
    }

    // Columna de contenido alineada a la izquierda, bajo el encabezado.
    private static Transform CreateContent(Transform parent, float width)
    {
        GameObject content = CreateUIObject(parent, "Content");
        var rect = content.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(width, 0);
        rect.anchoredPosition = new Vector2(0, -260);
        AddVerticalLayout(content);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return content.transform;
    }

    private static Transform CreateColumn(Transform parent, string name, float width)
    {
        GameObject column = CreateUIObject(parent, name);
        AddVerticalLayout(column);
        column.AddComponent<LayoutElement>().preferredWidth = width;
        return column.transform;
    }

    private static void AddVerticalLayout(GameObject go)
    {
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        CreateUIObject(parent, "Spacer").AddComponent<LayoutElement>().preferredHeight = height;
    }

    private static void CreateDivider(Transform parent)
    {
        Image divider = CreateImage(parent, "Divider", Green);
        var layout = divider.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 3;
        layout.flexibleWidth = 1;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size,
        TextAlignmentOptions alignment, Color color, float preferredHeight = -1)
    {
        GameObject go = CreateUIObject(parent, name);
        var label = go.AddComponent<TextMeshProUGUI>();
        ApplyFont(label);
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        if (preferredHeight > 0)
        {
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1;
        }
        return label;
    }

    private static void ApplyFont(TMP_Text text)
    {
        if (_font != null) text.font = _font;
    }

    // Fila "ETIQUETA : ______" con un campo de texto subrayado.
    private static TMP_InputField CreateInputRow(Transform parent, string name, string label, string placeholder, TMP_InputField.ContentType contentType)
    {
        GameObject row = CreateUIObject(parent, name + "Row");
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;
        var rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 72;
        rowElement.flexibleWidth = 1;

        TMP_Text labelText = CreateText(row.transform, "Label", label + " :", 52, TextAlignmentOptions.Left, Green);
        labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;

        GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = name + "Field";
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(row.transform, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1;

        go.GetComponent<Image>().color = Color.clear;
        var input = go.GetComponent<TMP_InputField>();
        input.contentType = contentType;
        if (_font != null) input.fontAsset = _font;
        input.pointSize = 52;
        input.textComponent.color = Green;
        input.caretColor = Green;
        input.customCaretColor = true;
        input.caretWidth = 3;
        input.selectionColor = new Color(Green.r, Green.g, Green.b, 0.35f);

        if (input.placeholder is TMP_Text placeholderText)
        {
            placeholderText.text = placeholder;
            placeholderText.color = DimGreen;
            placeholderText.fontStyle = FontStyles.Normal;
        }

        Image underline = CreateImage(go.transform, "Underline", DimGreen);
        Anchor(underline.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 7));

        return input;
    }

    // Opción de menú: texto verde que se invierte al pasar el mouse.
    private static Button CreateMenuItem(Transform parent, string name, string text)
    {
        GameObject go = CreateUIObject(parent, name);
        var background = go.AddComponent<Image>();
        background.color = Color.clear;

        var button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = background;

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 22, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        go.AddComponent<LayoutElement>().preferredHeight = 64;

        TMP_Text label = CreateText(go.transform, "Label", text, 58, TextAlignmentOptions.Left, Green);

        var terminalButton = go.AddComponent<TerminalButton>();
        Set(terminalButton, "_background", background);
        Set(terminalButton, "_label", label);
        return button;
    }

    private static void CreateNavigationButton(Transform parent, string name, string text, AppScreen target)
    {
        Button button = CreateMenuItem(parent, name, text);
        var navigation = button.gameObject.AddComponent<NavigationButton>();
        Set(navigation, "_button", button);
        SetEnum(navigation, "_target", (int)target);
    }

    private static void AddGameButton(Button button, GameButtonAction action)
    {
        var gameButton = button.gameObject.AddComponent<GameButton>();
        Set(gameButton, "_button", button);
        SetEnum(gameButton, "_action", (int)action);
    }

    private static GameObject CreateLeaderboardRow(Transform parent, string name, string rank, string player, string score, Color color)
    {
        Image background = CreateImage(parent, name, Color.clear);
        var layout = background.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 0, 0);
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        var element = background.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 42;
        element.flexibleWidth = 1;

        TMP_Text rankText = CreateText(background.transform, "Rank", rank, 40, TextAlignmentOptions.Left, color);
        rankText.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;

        TMP_Text nameText = CreateText(background.transform, "Name", player, 40, TextAlignmentOptions.Left, color);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text scoreText = CreateText(background.transform, "Score", score, 40, TextAlignmentOptions.Right, color);
        scoreText.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

        return background.gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // ================= Fuente =================

    // Crea (una sola vez) el Font Asset de TextMesh Pro para la fuente VT323.
    private static TMP_FontAsset GetOrCreateFont()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset != null) return fontAsset;

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            Debug.LogWarning("No se encontró " + FontPath + "; se usará la fuente por defecto de TextMesh Pro.");
            return null;
        }

        fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        if (fontAsset == null)
        {
            Debug.LogWarning("No se pudo crear el Font Asset de VT323; se usará la fuente por defecto.");
            return null;
        }

        fontAsset.name = "VT323-Regular SDF";
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        fontAsset.atlasTexture.name = "VT323-Regular Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        fontAsset.material.name = "VT323-Regular Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    // ================= Helpers de escena =================

    private static void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        Undo.RecordObject(camera, "Fondo de cámara");
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
    }

    private static void Set(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No se encontró el campo {propertyName} en {target.GetType().Name}");
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No se encontró el campo {propertyName} en {target.GetType().Name}");
            return;
        }
        property.enumValueIndex = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == name) return go;
        }
        return null;
    }

    private static void DestroyRoot(Scene scene, string name)
    {
        GameObject go = FindRoot(scene, name);
        if (go != null) Object.DestroyImmediate(go);
    }

    // Apaga (sin borrar) los Canvas anteriores y el script de la actividad REST anterior.
    private static void DisableOldUI(Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == CanvasName) continue;

            if (go.GetComponent<Canvas>() != null && go.activeSelf)
            {
                Undo.RecordObject(go, "Apagar UI anterior");
                go.SetActive(false);
                Debug.Log("Se apagó el Canvas anterior: " + go.name + " (puedes volver a activarlo si lo necesitas).");
            }

            foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "Script" && behaviour.enabled)
                {
                    Undo.RecordObject(behaviour, "Apagar script REST");
                    behaviour.enabled = false;
                }
            }
        }
    }

    private static void EnsureEventSystem(Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            EventSystem eventSystem = go.GetComponentInChildren<EventSystem>(true);
            if (eventSystem != null)
            {
                eventSystem.gameObject.SetActive(true);
                return;
            }
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Crear EventSystem");
    }
}
