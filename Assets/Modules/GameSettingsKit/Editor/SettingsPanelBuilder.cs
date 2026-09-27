using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettingsKit.Editor
{
    // 설정 창 UI 계층을 자동 생성한다. 메뉴: GameObject > UI > Game Settings Panel
    // 생성 결과는 일반 uGUI 오브젝트라 생성 후 자유롭게 수정할 수 있다.
    public static class SettingsPanelBuilder
    {
        [MenuItem("GameObject/UI/Game Settings Panel", false, 2100)]
        private static void CreateFromMenu(MenuCommand command)
        {
            GameObject context = command.context as GameObject;
            Transform parent = context != null ? context.transform : null;
            Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Game Settings Kit", "Canvas를 선택하거나 씬에 Canvas를 먼저 만들어 주세요.", "OK");
                return;
            }

            SettingsPanelTheme theme = FindFirstTheme();
            SettingsPanel panel = Build(parent != null ? parent : canvas.transform, theme);
            Selection.activeGameObject = panel.gameObject;
        }

        private static SettingsPanelTheme FindFirstTheme()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(SettingsPanelTheme));
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<SettingsPanelTheme>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }

        public static SettingsPanel Build(Transform parent, SettingsPanelTheme theme)
        {
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<SettingsPanelTheme>();
            }

            var res = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
            Sprite panelSprite = theme.panelSprite != null ? theme.panelSprite : res.standard;

            // 루트(항상 활성) / 창(열고 닫는 대상)
            RectTransform root = NewRect("Settings Panel", parent);
            Stretch(root);
            SettingsPanel panel = root.gameObject.AddComponent<SettingsPanel>();

            RectTransform window = NewRect("Window", root);
            Stretch(window);
            panel.window = window.gameObject;

            Image dim = NewRect("Dim", window).gameObject.AddComponent<Image>();
            Stretch(dim.rectTransform);
            dim.color = theme.dimColor; // raycastTarget 유지: 뒤쪽 UI 클릭 차단

            RectTransform frame = NewRect("Frame", window);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = theme.windowSize;
            Image frameImage = frame.gameObject.AddComponent<Image>();
            SetSprite(frameImage, panelSprite, theme.panelColor);
            var frameLayout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            frameLayout.padding = new RectOffset(32, 32, 24, 28);
            frameLayout.spacing = 6;
            frameLayout.childControlWidth = true;
            frameLayout.childControlHeight = true;
            frameLayout.childForceExpandWidth = true;
            frameLayout.childForceExpandHeight = false;
            var fitter = frame.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (theme.accentLineSprite != null)
            {
                RectTransform line = NewRect("Accent Line", frame);
                line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                line.anchorMin = new Vector2(0f, 1f);
                line.anchorMax = new Vector2(1f, 1f);
                line.pivot = new Vector2(0.5f, 1f);
                line.anchoredPosition = new Vector2(0f, -8f);
                line.sizeDelta = new Vector2(-48f, 3f);
                SetSprite(line.gameObject.AddComponent<Image>(), theme.accentLineSprite, theme.accentColor);
            }

            Text title = NewText("Title", frame, theme, theme.title, theme.titleFontSize, theme.accentColor, TextAnchor.MiddleCenter);
            Fixed(title.gameObject, theme.titleFontSize + 28f);

            // 오디오
            RectTransform masterArea = Row(frame, "Master Volume Row", theme.masterVolumeLabel, theme, out _);
            panel.masterVolumeSlider = SliderIn(masterArea, theme, res, out panel.masterVolumeValue);
            RectTransform musicArea = Row(frame, "Music Volume Row", theme.musicVolumeLabel, theme, out panel.musicRow);
            panel.musicVolumeSlider = SliderIn(musicArea, theme, res, out panel.musicVolumeValue);
            RectTransform sfxArea = Row(frame, "Sfx Volume Row", theme.sfxVolumeLabel, theme, out panel.sfxRow);
            panel.sfxVolumeSlider = SliderIn(sfxArea, theme, res, out panel.sfxVolumeValue);

            // 화면
            panel.fullscreenToggle = ToggleIn(Row(frame, "Fullscreen Row", theme.fullscreenLabel, theme, out _), theme, res);
            panel.resolutionDropdown = DropdownIn(Row(frame, "Resolution Row", theme.resolutionLabel, theme, out _), theme, res, panelSprite);
            panel.qualityDropdown = DropdownIn(Row(frame, "Graphics Row", theme.qualityLabel, theme, out _), theme, res, panelSprite);
            panel.vSyncToggle = ToggleIn(Row(frame, "VSync Row", theme.vSyncLabel, theme, out _), theme, res);

            // 하단 버튼
            RectTransform buttons = NewRect("Buttons", frame);
            Fixed(buttons.gameObject, 76f);
            var buttonLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.padding = new RectOffset(0, 0, 20, 0);
            buttonLayout.spacing = 20;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = false;
            panel.resetButton = ButtonIn(buttons, "Reset Button", theme.resetLabel, theme, res, panelSprite, theme.controlColor, theme.textColor);
            panel.closeButton = ButtonIn(buttons, "Close Button", theme.closeLabel, theme, res, panelSprite, theme.accentColor, theme.primaryButtonTextColor);

            window.gameObject.SetActive(false);
            root.SetAsLastSibling(); // 다른 UI 위에 표시
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Game Settings Panel");
            return panel;
        }

        // ---------- 행/컨트롤 ----------

        private static RectTransform Row(Transform frame, string name, string label, SettingsPanelTheme theme, out GameObject rowObject)
        {
            RectTransform row = NewRect(name, frame);
            Fixed(row.gameObject, theme.rowHeight);
            rowObject = row.gameObject;

            Text text = NewText("Label", row, theme, label, theme.labelFontSize, theme.subTextColor, TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = new Vector2(0f, 0f);
            text.rectTransform.anchorMax = new Vector2(0.45f, 1f);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

            RectTransform area = NewRect("Control", row);
            area.anchorMin = new Vector2(0.45f, 0f);
            area.anchorMax = new Vector2(1f, 1f);
            area.offsetMin = area.offsetMax = Vector2.zero;
            return area;
        }

        private static Slider SliderIn(RectTransform area, SettingsPanelTheme theme, DefaultControls.Resources res, out Text valueText)
        {
            GameObject go = DefaultControls.CreateSlider(res);
            go.transform.SetParent(area, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-32f, 0f);
            rt.sizeDelta = new Vector2(-64f, 18f);
            Slider slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            go.transform.Find("Background").GetComponent<Image>().color = theme.controlColor;
            go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = theme.accentColor;
            Image handle = go.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
            handle.color = theme.textColor;
            slider.targetGraphic = handle;

            valueText = NewText("Value", area, theme, "100%", theme.labelFontSize, theme.textColor, TextAnchor.MiddleRight);
            valueText.rectTransform.anchorMin = new Vector2(1f, 0f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
            valueText.rectTransform.pivot = new Vector2(1f, 0.5f);
            valueText.rectTransform.sizeDelta = new Vector2(56f, 0f);
            return slider;
        }

        private static Toggle ToggleIn(RectTransform area, SettingsPanelTheme theme, DefaultControls.Resources res)
        {
            GameObject go = DefaultControls.CreateToggle(res);
            go.transform.SetParent(area, false);
            Object.DestroyImmediate(go.transform.Find("Label").gameObject); // 행 라벨을 사용
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(28f, 28f);
            RectTransform bg = (RectTransform)go.transform.Find("Background");
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.anchoredPosition = Vector2.zero;
            bg.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = theme.controlColor;
            RectTransform check = (RectTransform)bg.Find("Checkmark");
            check.anchorMin = Vector2.zero;
            check.anchorMax = Vector2.one;
            check.sizeDelta = new Vector2(-8f, -8f);
            // 내장 Checkmark 스프라이트는 어두운 회색이라 어두운 테마에서 안 보이므로, 기본은 흰 사각형을 강조색으로 칠함
            SetSprite(check.GetComponent<Image>(), theme.checkmarkSprite != null ? theme.checkmarkSprite : res.standard, theme.accentColor);
            return go.GetComponent<Toggle>();
        }

        private static Dropdown DropdownIn(RectTransform area, SettingsPanelTheme theme, DefaultControls.Resources res, Sprite panelSprite)
        {
            GameObject go = DefaultControls.CreateDropdown(res);
            go.transform.SetParent(area, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 34f);
            SetSprite(go.GetComponent<Image>(), panelSprite, theme.controlColor);

            foreach (Text t in go.GetComponentsInChildren<Text>(true))
            {
                if (theme.font != null) t.font = theme.font;
                t.fontSize = theme.labelFontSize - 2;
                t.color = theme.textColor;
            }
            go.transform.Find("Arrow").GetComponent<Image>().color = theme.textColor;

            Transform template = go.transform.Find("Template");
            template.GetComponent<Image>().color = theme.panelColor;
            Toggle item = template.Find("Viewport/Content/Item").GetComponent<Toggle>();
            item.transform.Find("Item Background").GetComponent<Image>().color = theme.controlColor;
            item.transform.Find("Item Checkmark").GetComponent<Image>().color = theme.accentColor;
            ColorBlock colors = item.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(1f, 1f, 1f, 0.6f);
            colors.pressedColor = Color.white;
            item.colors = colors;
            Transform scrollbar = template.Find("Scrollbar");
            if (scrollbar != null)
            {
                scrollbar.GetComponent<Image>().color = theme.panelColor;
                scrollbar.Find("Sliding Area/Handle").GetComponent<Image>().color = theme.controlColor;
            }
            return go.GetComponent<Dropdown>();
        }

        private static Button ButtonIn(Transform parent, string name, string label, SettingsPanelTheme theme, DefaultControls.Resources res, Sprite sprite, Color bg, Color fg)
        {
            GameObject go = DefaultControls.CreateButton(res);
            go.name = name;
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(180f, 52f);
            SetSprite(go.GetComponent<Image>(), sprite, bg);
            Text text = go.GetComponentInChildren<Text>();
            text.text = label;
            text.fontSize = theme.labelFontSize + 2;
            text.color = fg;
            if (theme.font != null) text.font = theme.font;
            return go.GetComponent<Button>();
        }

        // ---------- 공통 ----------

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Fixed(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static void SetSprite(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
        }

        private static Text NewText(string name, Transform parent, SettingsPanelTheme theme, string value, int size, Color color, TextAnchor align)
        {
            Text text = NewRect(name, parent).gameObject.AddComponent<Text>();
            text.font = theme.font != null ? theme.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
