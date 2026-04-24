using UnityEngine;

public sealed partial class SolarSystem
{
    private void OnGUI()
    {
        DrawControlsPanel();
        DrawInteractionPrompt();

        if (selectedBody != null)
        {
            DrawSelectionPanel();
        }
    }

    private void DrawControlsPanel()
    {
        GUILayout.BeginArea(new Rect(UiPadding, UiPadding, ControlPanelWidth, ControlPanelHeight), GUI.skin.box);
        GUILayout.Label("Interactive Solar System");
        GUILayout.Label("Simulated days: " + simulatedDays.ToString("0.0"));
        GUILayout.Label("Speed: " + daysPerSecond.ToString("0.##") + " days/sec");
        GUILayout.Label("Texture: " + planetTextureWidth + " x " + planetTextureHeight);
        GUILayout.Label("Click the Sun, a planet, or the Moon.");
        GUILayout.Label("Space: pause  |  +/-: speed");
        GUILayout.Label("O: orbit lines  |  L: labels  |  R: reset time");
        GUILayout.Label("Esc: return to main view");
        GUILayout.Label("Right mouse drag + wheel: camera");
        GUILayout.EndArea();
    }

    private void DrawInteractionPrompt()
    {
        if (selectedBody != null)
        {
            return;
        }

        Rect promptRect = new Rect(UiPadding, Screen.height - 74f, 420f, 58f);
        GUILayout.BeginArea(promptRect, GUI.skin.box);
        GUILayout.Label("Click a planet or the Moon to zoom in and hear a fun fact.");
        GUILayout.EndArea();
    }

    private void DrawSelectionPanel()
    {
        Rect panelRect = GetInfoPanelRect();
        GUILayout.BeginArea(panelRect, GUI.skin.box);
        GUILayout.Label(selectedBody.Name);
        GUILayout.Space(4f);
        GUILayout.Label(selectedBody.Prompt);
        GUILayout.Space(10f);
        GUILayout.Label(selectedBody.Fact);
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Back to Main View", GUILayout.Height(32f)))
        {
            ReturnToMainView();
        }

        GUILayout.Space(10f);
        GUILayout.Label("Press Esc");
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private Rect GetInfoPanelRect()
    {
        return new Rect(Screen.width - InfoPanelWidth - UiPadding, UiPadding, InfoPanelWidth, InfoPanelHeight);
    }

    private void HandleSelectionInput()
    {
        if (selectedBody != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)))
        {
            ReturnToMainView();
        }

        if (sceneCamera == null || !Input.GetMouseButtonDown(0) || Input.GetMouseButton(1))
        {
            return;
        }

        if (selectedBody != null && IsPointerInsideInfoPanel(Input.mousePosition))
        {
            return;
        }

        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, sceneCamera.farClipPlane))
        {
            if (selectableByCollider.TryGetValue(hit.collider, out SelectableBody body))
            {
                SelectBody(body);
            }
        }
    }

    private bool IsPointerInsideInfoPanel(Vector3 pointerPosition)
    {
        Rect rect = GetInfoPanelRect();
        Vector2 guiPoint = new Vector2(pointerPosition.x, Screen.height - pointerPosition.y);
        return rect.Contains(guiPoint);
    }

    private void SelectBody(SelectableBody body)
    {
        if (body == null)
        {
            return;
        }

        selectedBody = body;
        if (autoPauseOnFocus)
        {
            paused = true;
        }

        if (cameraController != null)
        {
            cameraController.FocusOn(body.FocusTransform, body.FocusDistance);
        }

        PlaySelectionSound(body.TonePitch);
    }

    private void ReturnToMainView()
    {
        selectedBody = null;
        if (cameraController != null)
        {
            cameraController.ReturnHome();
        }
    }

    private void UpdateSelectionEffects()
    {
        float pulse = 0.4f + 0.6f * (Mathf.Sin(Time.time * selectionPulseSpeed) * 0.5f + 0.5f);

        for (int i = 0; i < selectableBodies.Count; i++)
        {
            SelectableBody body = selectableBodies[i];
            if (body == null || body.VisualTransform == null || body.Material == null)
            {
                continue;
            }

            if (body == selectedBody)
            {
                float scale = 1f + selectionPulseScale * pulse;
                body.VisualTransform.localScale = body.BaseScale * scale;
                body.Material.SetColor("_EmissionColor", body.BaseEmission + body.HighlightColor * (0.9f + 1.9f * pulse));
            }
            else
            {
                body.VisualTransform.localScale = Vector3.Lerp(body.VisualTransform.localScale, body.BaseScale, Time.deltaTime * 10f);
                body.Material.SetColor("_EmissionColor", Color.Lerp(body.Material.GetColor("_EmissionColor"), body.BaseEmission, Time.deltaTime * 8f));
            }
        }
    }

    private void SetupAudio()
    {
        interactionAudioSource = GetComponent<AudioSource>();
        if (interactionAudioSource == null)
        {
            interactionAudioSource = gameObject.AddComponent<AudioSource>();
        }

        interactionAudioSource.playOnAwake = false;
        interactionAudioSource.loop = false;
        interactionAudioSource.spatialBlend = 0f;
        interactionAudioSource.volume = interactionVolume;
    }

    private void PlaySelectionSound(float tonePitch)
    {
        if (interactionAudioSource == null)
        {
            return;
        }

        int clipKey = Mathf.RoundToInt(tonePitch * 100f);
        if (!cachedSelectionClips.TryGetValue(clipKey, out AudioClip clip))
        {
            clip = CreateSelectionClip(tonePitch);
            cachedSelectionClips.Add(clipKey, clip);
        }

        interactionAudioSource.PlayOneShot(clip, interactionVolume);
    }

    private AudioClip CreateSelectionClip(float tonePitch)
    {
        const int sampleRate = 44100;
        const float clipLength = 0.28f;
        int sampleCount = Mathf.RoundToInt(sampleRate * clipLength);
        float[] data = new float[sampleCount];

        float frequencyA = 420f * tonePitch;
        float frequencyB = 630f * tonePitch;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float normalized = i / (float)(sampleCount - 1);
            float envelope = Mathf.Sin(normalized * Mathf.PI);
            float wave = Mathf.Sin(t * frequencyA * Mathf.PI * 2f) * 0.75f + Mathf.Sin(t * frequencyB * Mathf.PI * 2f) * 0.25f;
            data[i] = wave * envelope * 0.2f;
        }

        AudioClip clip = AudioClip.Create("SelectionTone_" + Mathf.RoundToInt(tonePitch * 100f), sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void RegisterSelectable(
        string name,
        string prompt,
        string fact,
        Transform focusTransform,
        Transform visualTransform,
        Renderer renderer,
        Material material,
        Color highlightColor,
        float focusDistance,
        float tonePitch)
    {
        Collider collider = visualTransform.GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        material.EnableKeyword("_EMISSION");
        Color baseEmission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;

        SelectableBody body = new SelectableBody
        {
            Name = name,
            Prompt = prompt,
            Fact = fact,
            FocusTransform = focusTransform,
            VisualTransform = visualTransform,
            Renderer = renderer,
            Material = material,
            Collider = collider,
            BaseScale = visualTransform.localScale,
            BaseEmission = baseEmission,
            HighlightColor = highlightColor,
            FocusDistance = focusDistance,
            TonePitch = tonePitch
        };

        selectableBodies.Add(body);
        selectableByCollider[collider] = body;
    }
}
