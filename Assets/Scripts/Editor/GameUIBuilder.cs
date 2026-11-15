using System.Collections.Generic;
using System.IO;
using LunarEyes.Core;
using LunarEyes.Integration;
using LunarEyes.Interaction;
using LunarEyes.Player;
using LunarEyes.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace LunarEyes.EditorTools
{
    /// <summary>
    /// Constrói a UI do jogo a partir do design "Lunar Eyes — Game UI" (Claude Design):
    /// ecrã inicial, HUD de exploração, pausa e painel do terminal (aplicação web).
    /// Referência 1440×810, a mesma das telas do design.
    /// </summary>
    public static class GameUIBuilder
    {
        const string SpriteFolder = "Assets/UI/Sprites";
        const string FontFolder = "Assets/UI/Fonts";

        static TMP_FontAsset fInter, fInterMedium, fInterSemi, fGrotesk, fGroteskSemi, fMono;
        static Sprite sRound, sRoundOutline, sPill, sPillOutline, sGradient, sDiamond;

        static readonly Color TextSoft = UITheme.Hex(0xC9D3DC);

        public static GameObject Build(ThirdPersonCamera cam, GameObject player, StationDoor door, MonitorTerminal terminal)
        {
            LoadFonts();
            LoadSprites();

            var canvasGo = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 810f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(canvasGo.transform.parent);

            var hud = BuildHud(root, out Button pauseButton);
            var title = BuildTitle(root);
            var web = BuildTerminalOverlay(root);
            var pause = BuildPause(root);

            // Ponto de câmara do ecrã inicial: exterior, com a Terra à direita
            var titleCam = new GameObject("Title Camera Point").transform;
            titleCam.position = new Vector3(-9f, 2.0f, -3f);
            titleCam.rotation = Quaternion.LookRotation(new Vector3(-1.1f, 2.9f, 3.2f) - titleCam.position);

            var flowGo = new GameObject("Game Flow");
            var flow = flowGo.AddComponent<GameFlow>();
            Set(flow, "player", player.GetComponent<PlayerController>());
            Set(flow, "playerCamera", cam);
            Set(flow, "titleCameraPoint", titleCam);
            Set(flow, "title", title);
            Set(flow, "pause", pause);
            Set(flow, "hud", hud);
            Set(flow, "door", door);
            Set(flow, "terminal", terminal);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(pauseButton.onClick, flow.TogglePause);

            return canvasGo;
        }

        // ------------------------------------------------------------ HUD (02 · Exploração)

        static HUD BuildHud(RectTransform root, out Button pauseButton)
        {
            var gameplay = Stretch(root, "HUD");
            var group = gameplay.gameObject.AddComponent<CanvasGroup>();

            // Objetivo
            var obj = Box(gameplay, "Objective", sRound, UITheme.DeepSpace.WithAlpha(0.74f));
            Anchor(obj, 0, 1, 0, 1, new Vector2(0, 1), new Vector2(40, -36), new Vector2(340, 118));
            Outline(obj, sRoundOutline, UITheme.BorderSlate);
            var label = Text(obj, "MISSÃO 01 · ANGOLA", fMono, 12, UITheme.MistGrey, TextAlignmentOptions.TopLeft);
            Place(label.rectTransform, 20, -15, 200, 16); label.characterSpacing = 2;
            var statusDot = Box(obj, "Status Dot", sPill, UITheme.AlertAmber);
            Anchor(statusDot, 1, 1, 1, 1, new Vector2(1, 1), new Vector2(-128, -20), new Vector2(7, 7));
            var status = Text(obj, "Alerta pendente", fInterSemi, 12, UITheme.AmberText, TextAlignmentOptions.TopRight);
            Anchor(status.rectTransform, 1, 1, 1, 1, new Vector2(1, 1), new Vector2(-20, -15), new Vector2(104, 16));
            var objective = Text(obj, "Entra na estação e liga o terminal", fGroteskSemi, 20, UITheme.LunarWhite, TextAlignmentOptions.TopLeft);
            Place(objective.rectTransform, 20, -38, 300, 52);
            objective.lineSpacing = -6;
            var segs = new List<Image>();
            for (int i = 0; i < 6; i++)
            {
                var s = Box(obj, "Progress " + i, sPill, UITheme.BorderSlate);
                Place((RectTransform)s.transform, 20 + i * 51, -100, 45, 3);
                segs.Add(s.GetComponent<Image>());
            }

            // Pausa
            var pauseRt = Box(gameplay, "Pause Button", sRound, UITheme.DeepSpace.WithAlpha(0.74f));
            Anchor(pauseRt, 1, 1, 1, 1, new Vector2(1, 1), new Vector2(-40, -36), new Vector2(44, 44));
            Outline(pauseRt, sRoundOutline, UITheme.BorderSlate);
            pauseButton = MakeButton(pauseRt.gameObject);
            for (int i = 0; i < 2; i++)
            {
                var bar = Box(pauseRt, "Bar", sPill, UITheme.MistGrey);
                Anchor(bar, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -5 : 5, 0), new Vector2(3, 14));
            }

            // Marcador no mundo
            var marker = new GameObject("Marker", typeof(RectTransform)).GetComponent<RectTransform>();
            marker.SetParent(gameplay, false);
            Anchor(marker, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(160, 52)); // posição = ponto local relativo ao centro do canvas
            var pill = Box(marker, "Label", sRound, UITheme.DeepSpace.WithAlpha(0.74f));
            Anchor(pill, 0.5f, 1, 0.5f, 1, new Vector2(0.5f, 1), Vector2.zero, new Vector2(150, 26));
            Outline(pill, sRoundOutline, UITheme.SignalCyan.WithAlpha(0.55f));
            var markerText = Text(pill, "Estação · 24 m", fInterSemi, 12, UITheme.CyanText, TextAlignmentOptions.Center);
            Fill(markerText.rectTransform);
            var diamond = Box(marker, "Diamond", sDiamond, UITheme.SignalCyan);
            Anchor(diamond, 0.5f, 0, 0.5f, 0, new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(16, 16));

            // Prompt
            var prompt = Box(gameplay, "Prompt", sRound, UITheme.DeepSpace.WithAlpha(0.84f));
            Anchor(prompt, 0.5f, 0, 0.5f, 0, new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(330, 58));
            Outline(prompt, sRoundOutline, UITheme.BorderSlate);
            var key = Box(prompt, "Key", sRound, UITheme.LunarWhite);
            Anchor(key, 0, 0.5f, 0, 0.5f, new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(38, 38));
            var keyText = Text(key, "E", fMono, 17, UITheme.DeepSpace, TextAlignmentOptions.Center);
            Fill(keyText.rectTransform);
            var pTitle = Text(prompt, "Abrir porta da estação", fInterSemi, 16, UITheme.LunarWhite, TextAlignmentOptions.BottomLeft);
            Anchor(pTitle.rectTransform, 0, 0.5f, 1, 0.5f, new Vector2(0, 0), new Vector2(62, 0), new Vector2(-72, 22));
            var pSub = Text(prompt, "Módulo de observação", fInter, 12, UITheme.MistGrey, TextAlignmentOptions.TopLeft);
            Anchor(pSub.rectTransform, 0, 0.5f, 1, 0.5f, new Vector2(0, 1), new Vector2(62, -1), new Vector2(-72, 18));

            // Controlos
            var controls = new GameObject("Controls", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            controls.SetParent(gameplay, false);
            Anchor(controls, 0, 0, 0, 0, new Vector2(0, 0), new Vector2(40, 32), new Vector2(420, 24));
            float x = 0;
            foreach (var (k, l) in new[] { ("WASD", "mover"), ("Shift", "correr"), ("Rato", "olhar"), ("Esc", "pausa") })
            {
                float kw = 12 + k.Length * 8;
                var kc = Box(controls, "Key " + k, sRound, UITheme.DeepSpace.WithAlpha(0.6f));
                Place((RectTransform)kc.transform, x, 0, kw, 22, bottom: true);
                Outline((RectTransform)kc.transform, sRoundOutline, UITheme.BorderSlate);
                var kt = Text(kc.transform, k, fMono, 12, UITheme.LunarWhite, TextAlignmentOptions.Center);
                Fill(kt.rectTransform);
                var lt = Text(controls, l, fInter, 13, UITheme.MistGrey, TextAlignmentOptions.Left);
                Place(lt.rectTransform, x + kw + 6, 0, 60, 22, bottom: true);
                x += kw + 6 + l.Length * 7.5f + 18;
            }

            var hud = gameplay.gameObject.AddComponent<HUD>();
            Set(hud, "gameplayGroup", group);
            Set(hud, "objectiveText", objective);
            Set(hud, "statusText", status);
            Set(hud, "statusDot", statusDot.GetComponent<Image>());
            SetArray(hud, "progressSegments", segs.ToArray());
            Set(hud, "promptRoot", prompt.gameObject);
            Set(hud, "promptTitle", pTitle);
            Set(hud, "promptSubtitle", pSub);
            Set(hud, "marker", marker);
            Set(hud, "markerLabel", markerText);
            Set(hud, "controlsGroup", controls.GetComponent<CanvasGroup>());
            return hud;
        }

        // ------------------------------------------------------------ Ecrã inicial (01)

        static TitleScreen BuildTitle(RectTransform root)
        {
            var screen = Stretch(root, "Title Screen");
            var group = screen.gameObject.AddComponent<CanvasGroup>();

            var grad = Box(screen, "Gradient", sGradient, UITheme.DeepSpace);
            grad.GetComponent<Image>().type = Image.Type.Simple;
            Anchor(grad, 0, 0, 0.78f, 1, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);

            // Logo
            var ring = Box(screen, "Logo Ring", sPillOutline, UITheme.LunarWhite);
            Anchor(ring, 0, 1, 0, 1, new Vector2(0, 1), new Vector2(80, -56), new Vector2(30, 30));
            var core = Box(ring, "Core", sPill, UITheme.SignalCyan);
            Anchor(core, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            var brand = Text(screen, "LUNAR EYES", fGroteskSemi, 18, UITheme.LunarWhite, TextAlignmentOptions.Left);
            Anchor(brand.rectTransform, 0, 1, 0, 1, new Vector2(0, 1), new Vector2(122, -58), new Vector2(240, 26));
            brand.characterSpacing = 4;

            // Bloco principal
            var main = new GameObject("Main", typeof(RectTransform)).GetComponent<RectTransform>();
            main.SetParent(screen, false);
            Anchor(main, 0, 0.5f, 0, 0.5f, new Vector2(0, 0.5f), new Vector2(80, -24), new Vector2(580, 540));

            var kicker = Text(main, "MISSÃO TERRA · 01", fMono, 13, UITheme.CyanText, TextAlignmentOptions.TopLeft);
            Place(kicker.rectTransform, 0, 0, 400, 18); kicker.characterSpacing = 10;
            var h1 = Text(main, "Observar a Terra\na partir da Lua", fGroteskSemi, 58, UITheme.LunarWhite, TextAlignmentOptions.TopLeft);
            Place(h1.rectTransform, 0, -32, 600, 136); h1.lineSpacing = -18;
            var body = Text(main, "Estás numa estação de investigação lunar. Um alerta chegou sobre o sul de Angola: descobre o que mudou, com dados da NASA, e decide como responder.", fInter, 18, TextSoft, TextAlignmentOptions.TopLeft);
            Place(body.rectTransform, 0, -184, 540, 90); body.fontSize = 17; body.lineSpacing = 6;

            var q = Text(main, "Como queres jogar?", fInter, 13, UITheme.MistGrey, TextAlignmentOptions.TopLeft);
            Place(q.rectTransform, 0, -300, 300, 18);
            var tg = main.gameObject.AddComponent<ToggleGroup>();
            var explorer = ModeCard(main, "Explorador", "Linguagem simples, pistas guiadas", 0, tg, out Image explorerCard);
            var scientist = ModeCard(main, "Cientista", "Séries, tendências e método", 286, tg, out Image scientistCard);

            var start = PrimaryButton(main, "Iniciar missão  →", 0, -424, 220, 56, 17);
            var help = SecondaryButton(main, "Como jogar", 232, -424, 150, 56, 16);

            // Rodapé
            var mock = Box(screen, "Mock Badge", sRoundOutline, UITheme.MistGrey);
            Anchor(mock, 0, 0, 0, 0, new Vector2(0, 0), new Vector2(80, 44), new Vector2(54, 22));
            var mockT = Text(mock, "MOCK", fMono, 11, UITheme.MistGrey, TextAlignmentOptions.Center); Fill(mockT.rectTransform);
            var foot = Text(screen, "Caso em validação · os dados mostrados são ilustrativos até à validação", fInter, 13, UITheme.MistGrey, TextAlignmentOptions.Left);
            Anchor(foot.rectTransform, 0, 0, 0, 0, new Vector2(0, 0), new Vector2(144, 44), new Vector2(600, 22));
            var src = Text(screen, "NASA POWER · GPM IMERG · SMAP · MODIS", fMono, 12, UITheme.MistGrey, TextAlignmentOptions.Right);
            Anchor(src.rectTransform, 1, 0, 1, 0, new Vector2(1, 0), new Vector2(-80, 44), new Vector2(420, 22));

            // Ajuda (Como jogar)
            var helpPanel = Stretch(screen, "Help");
            var dim = helpPanel.gameObject.AddComponent<Image>(); dim.color = UITheme.DeepSpace.WithAlpha(0.6f);
            var card = Box(helpPanel, "Card", sRound, UITheme.SpaceNavy);
            Anchor(card, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 330));
            Outline(card, sRoundOutline, UITheme.BorderSlate);
            var ht = Text(card, "Como jogar", fGroteskSemi, 26, UITheme.LunarWhite, TextAlignmentOptions.TopLeft);
            Place(ht.rectTransform, 32, -28, 400, 34);
            var hb = Text(card,
                "<color=#F4F6F3>WASD</color>  mover o astronauta\n<color=#F4F6F3>Shift</color>  correr\n<color=#F4F6F3>Rato</color>  olhar à volta\n<color=#F4F6F3>E</color>  interagir com a porta e o terminal\n<color=#F4F6F3>Esc</color>  pausa · sair do terminal",
                fInter, 16, UITheme.MistGrey, TextAlignmentOptions.TopLeft);
            Place(hb.rectTransform, 32, -78, 456, 170); hb.lineSpacing = 22;
            var close = PrimaryButton(card, "Percebi", 32, -254, 140, 48, 15);

            var ts = screen.gameObject.AddComponent<TitleScreen>();
            Set(ts, "group", group);
            Set(ts, "startButton", start);
            Set(ts, "helpButton", help);
            Set(ts, "helpPanel", helpPanel.gameObject);
            Set(ts, "closeHelpButton", close);
            Set(ts, "explorerToggle", explorer);
            Set(ts, "scientistToggle", scientist);
            Set(ts, "explorerCard", explorerCard);
            Set(ts, "scientistCard", scientistCard);
            return ts;
        }

        static Toggle ModeCard(RectTransform parent, string title, string sub, float x, ToggleGroup group, out Image card)
        {
            card = Box((Transform)parent, "Mode " + title, sRound, UITheme.DeepSpace.WithAlpha(0.7f));
            Place((RectTransform)card.transform, x, -326, 274, 70);
            var border = Box(card.transform, "Border", sRoundOutline, UITheme.BorderSlate);
            Fill((RectTransform)border.transform);
            border.raycastTarget = false;

            var radio = Box(card.transform, "Radio", sPillOutline, UITheme.MistGrey);
            Place((RectTransform)radio.transform, 16, -18, 18, 18);
            var dot = Box(radio.transform, "Dot", sPill, UITheme.SignalCyan);
            Anchor((RectTransform)dot.transform, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 8));

            var t = Text(card.transform, title, fInterSemi, 15, UITheme.LunarWhite, TextAlignmentOptions.TopLeft);
            Place(t.rectTransform, 46, -15, 210, 20);
            var s = Text(card.transform, sub, fInter, 13, UITheme.MistGrey, TextAlignmentOptions.TopLeft);
            Place(s.rectTransform, 46, -38, 220, 20);
            s.textWrappingMode = TextWrappingModes.NoWrap;

            var toggle = card.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = card;
            toggle.graphic = dot;
            toggle.group = group;
            toggle.transition = Selectable.Transition.None;
            return toggle;
        }

        // ------------------------------------------------------------ Terminal → aplicação web

        static TerminalWebView BuildTerminalOverlay(RectTransform root)
        {
            var panel = Stretch(root, "Terminal Web View");
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            var bg = panel.gameObject.AddComponent<Image>(); bg.color = UITheme.DeepSpace.WithAlpha(0.94f);

            var bar = Box(panel, "Bar", null, UITheme.SpaceNavy);
            Anchor(bar, 0, 1, 1, 1, new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 64));
            var line = Box(bar, "Line", null, UITheme.BorderSlate);
            Anchor(line, 0, 0, 1, 0, new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 1));
            var t = Text(bar, "TERMINAL DE OBSERVAÇÃO · MISSÃO 01", fMono, 12, UITheme.MistGrey, TextAlignmentOptions.Left);
            Anchor(t.rectTransform, 0, 0.5f, 0, 0.5f, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(500, 20));
            t.characterSpacing = 8;
            var exit = SecondaryButton(bar, "Esc   Sair do terminal", 0, 0, 220, 40, 14);
            Anchor((RectTransform)exit.transform, 1, 0.5f, 1, 0.5f, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(220, 40));

            var card = Box(panel, "Card", sRound, UITheme.SpaceNavy);
            Anchor(card, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(620, 240));
            Outline(card, sRoundOutline, UITheme.BorderSlate);
            var k = Text(card, "APLICAÇÃO WEB", fMono, 11, UITheme.CyanText, TextAlignmentOptions.TopLeft);
            Place(k.rectTransform, 36, -32, 300, 16); k.characterSpacing = 10;
            var h = Text(card, "Lunar Eyes · Missão Terra", fGroteskSemi, 30, UITheme.LunarWhite, TextAlignmentOptions.TopLeft);
            Place(h.rectTransform, 36, -56, 540, 40);
            var status = Text(card, "A ligação à aplicação web ainda não está configurada.", fInter, 16, TextSoft, TextAlignmentOptions.TopLeft);
            Place(status.rectTransform, 36, -110, 548, 50);
            var url = Text(card, "URL por configurar", fMono, 12, UITheme.MistGrey, TextAlignmentOptions.TopLeft);
            Place(url.rectTransform, 36, -170, 548, 20);

            var view = panel.gameObject.AddComponent<TerminalWebView>();
            Set(view, "group", group);
            Set(view, "statusText", status);
            Set(view, "urlText", url);
            Set(view, "exitButton", exit);
            return view;
        }

        // ------------------------------------------------------------ Pausa

        static PauseMenu BuildPause(RectTransform root)
        {
            var panel = Stretch(root, "Pause Menu");
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            var bg = panel.gameObject.AddComponent<Image>(); bg.color = UITheme.DeepSpace.WithAlpha(0.72f);

            var card = Box(panel, "Card", sRound, UITheme.SpaceNavy);
            Anchor(card, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 330));
            Outline(card, sRoundOutline, UITheme.BorderSlate);
            var k = Text(card, "MISSÃO 01 · EM PAUSA", fMono, 11, UITheme.MistGrey, TextAlignmentOptions.TopLeft);
            Place(k.rectTransform, 32, -30, 300, 16); k.characterSpacing = 10;
            var h = Text(card, "Pausa", fGroteskSemi, 30, UITheme.LunarWhite, TextAlignmentOptions.TopLeft);
            Place(h.rectTransform, 32, -52, 300, 40);
            var resume = PrimaryButton(card, "Continuar", 32, -112, 336, 52, 16);
            var sound = SecondaryButton(card, "Som: ligado", 32, -176, 336, 48, 15);
            var restart = SecondaryButton(card, "Voltar ao início", 32, -236, 336, 48, 15);
            restart.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var pm = panel.gameObject.AddComponent<PauseMenu>();
            Set(pm, "group", group);
            Set(pm, "resumeButton", resume);
            Set(pm, "soundButton", sound);
            Set(pm, "soundLabel", sound.GetComponentInChildren<TMP_Text>());
            Set(pm, "restartButton", restart);
            return pm;
        }

        // ------------------------------------------------------------ Componentes

        static Button PrimaryButton(Transform parent, string label, float x, float y, float w, float h, int size)
        {
            var img = Box(parent, "Button " + label, sRound, UITheme.SignalCyan);
            Place((RectTransform)img.transform, x, y, w, h);
            var t = Text(img.transform, label, fInterSemi, size, UITheme.OnCyan, TextAlignmentOptions.Center);
            Fill(t.rectTransform);
            return MakeButton(img.gameObject);
        }

        static Button SecondaryButton(Transform parent, string label, float x, float y, float w, float h, int size)
        {
            var img = Box(parent, "Button " + label, sRound, UITheme.DeepSpace.WithAlpha(0.6f));
            Place((RectTransform)img.transform, x, y, w, h);
            Outline((RectTransform)img.transform, sRoundOutline, UITheme.BorderSlate);
            var t = Text(img.transform, label, fInterMedium, size, UITheme.LunarWhite, TextAlignmentOptions.Center);
            Fill(t.rectTransform);
            return MakeButton(img.gameObject);
        }

        static Button MakeButton(GameObject go)
        {
            var b = go.AddComponent<Button>();
            var c = b.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            c.selectedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            c.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            c.colorMultiplier = 1.15f;
            c.fadeDuration = 0.08f;
            b.colors = c;
            return b;
        }

        static RectTransform Stretch(RectTransform parent, string name)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            Fill(rt);
            return rt;
        }

        static Image Box(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        static RectTransform Box(RectTransform parent, string name, Sprite sprite, Color color) =>
            (RectTransform)Box((Transform)parent, name, sprite, color).transform;

        static void Outline(RectTransform target, Sprite sprite, Color color)
        {
            var o = Box(target, "Border", sprite, color);
            Fill(o);
            o.GetComponent<Image>().raycastTarget = false;
        }

        static TextMeshProUGUI Text(Transform parent, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        static void Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Anchor(Image img, float minX, float minY, float maxX, float maxY, Vector2 pivot, Vector2 pos, Vector2 size) =>
            Anchor((RectTransform)img.transform, minX, minY, maxX, maxY, pivot, pos, size);

        /// <summary>Posição a partir do canto superior esquerdo do pai (ou inferior esquerdo com bottom).</summary>
        static void Place(RectTransform rt, float x, float y, float w, float h, bool bottom = false)
        {
            float ay = bottom ? 0f : 1f;
            Anchor(rt, 0, ay, 0, ay, new Vector2(0, ay), new Vector2(x, y), new Vector2(w, h));
        }

        static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"GameUIBuilder: campo em falta {target.GetType().Name}.{field}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------ Fontes e sprites gerados

        static void LoadFonts()
        {
            fInter = FontAsset("Inter-Regular");
            fInterMedium = FontAsset("Inter-Medium");
            fInterSemi = FontAsset("Inter-SemiBold");
            fGrotesk = FontAsset("SpaceGrotesk-Medium");
            fGroteskSemi = FontAsset("SpaceGrotesk-SemiBold");
            fMono = FontAsset("JetBrainsMono-Regular");
        }

        static TMP_FontAsset FontAsset(string name)
        {
            string path = $"{FontFolder}/{name} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{name}.ttf");
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = name + " SDF";
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static void LoadSprites()
        {
            sRound = RoundedSprite("UI_Round8", 64, 10, 0f, 16);
            sRoundOutline = RoundedSprite("UI_RoundOutline8", 64, 10, 1.5f, 16);
            sPill = RoundedSprite("UI_Pill", 64, 32, 0f, 31);
            sPillOutline = RoundedSprite("UI_PillOutline", 64, 32, 2f, 31);
            sDiamond = DiamondSprite("UI_Diamond", 64, 3.5f);
            sGradient = GradientSprite("UI_GradientLeft", 256);
        }

        static Sprite SaveSprite(string name, Texture2D tex, int border)
        {
            string path = $"{SpriteFolder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spriteBorder = new Vector4(border, border, border, border);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Retângulo arredondado branco (preenchido ou só contorno), para 9-slice.</summary>
        static Sprite RoundedSprite(string name, int size, float radius, float stroke, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // distância com sinal a um retângulo arredondado
                float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float outside = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - radius;
                float a = Mathf.Clamp01(0.5f - outside);
                if (stroke > 0f) a *= Mathf.Clamp01(outside + stroke + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return SaveSprite(name, tex, border);
        }

        static Sprite DiamondSprite(string name, int size, float stroke)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Abs(x + 0.5f - half) + Mathf.Abs(y + 0.5f - half); // métrica L1 = losango
                float edge = half - 2f;
                float a = Mathf.Clamp01(edge - d + 0.5f) * Mathf.Clamp01(d - (edge - stroke * 1.4f) + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return SaveSprite(name, tex, 0);
        }

        /// <summary>Gradiente horizontal: opaco à esquerda, transparente à direita (fundo do ecrã inicial).</summary>
        static Sprite GradientSprite(string name, int width)
        {
            var tex = new Texture2D(width, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                float t = x / (width - 1f);
                float a = t < 0.45f ? Mathf.Lerp(0.96f, 0.86f, t / 0.45f) : Mathf.Lerp(0.86f, 0f, Mathf.SmoothStep(0, 1, (t - 0.45f) / 0.55f));
                for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return SaveSprite(name, tex, 0);
        }
    }
}
