using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.InputSystem.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using XRDevice = UnityEngine.XR.InputDevice;

namespace RenovaLab
{
    public sealed class RenovaLabExperience : MonoBehaviour
    {
        [SerializeField] Camera eyes;
        [SerializeField] Transform rig;
        [SerializeField] Transform rightHand;
        [SerializeField] CharacterController body;
        [SerializeField] RepairStation[] stations;
        [SerializeField] Material pointerMaterial;

        static readonly string[] ToolNames = { "Espátula", "Kit de vedação", "Rolo de pintura" };
        static readonly string[] Instructions = {
            "Preencha a fissura superficial com massa usando a espátula.",
            "Vede a origem simulada da infiltração e deixe a área secar.",
            "Raspe e prepare a base na simulação. Depois, repinte com o rolo."
        };

        TextMeshProUGUI header;
        TextMeshProUGUI guide;
        TextMeshProUGUI footer;
        LineRenderer pointer;
        RepairStation focused;
        Transform[] tools;
        int selectedTool;
        int completed;
        bool triggerWasHeld;
        bool toolButtonWasHeld;
        bool restartWasHeld;
        bool desktop;
        float pitch;

        void Start()
        {
            desktop = Application.isEditor && !XRSettings.isDeviceActive;
            if (desktop)
            {
                var tracking = eyes.GetComponent<TrackedPoseDriver>();
                if (tracking) tracking.enabled = false;
            }

            foreach (var station in stations) station.Prepare();
            CreateDisplay();
            CreateTools();

            pointer = new GameObject("Mira RenovaLab").AddComponent<LineRenderer>();
            pointer.transform.SetParent(transform, false);
            pointer.sharedMaterial = pointerMaterial;
            pointer.positionCount = 2;
            pointer.startWidth = .005f;
            pointer.endWidth = .002f;
            pointer.numCapVertices = 4;
            pointer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pointer.receiveShadows = false;
            RenderStatus();
        }

        void Update()
        {
            XRDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool tracked = device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
            if (desktop && XRSettings.isDeviceActive)
            {
                desktop = false;
                var tracking = eyes.GetComponent<TrackedPoseDriver>();
                if (tracking) tracking.enabled = true;
            }

            if (desktop) MoveOnDesktop();

            bool trigger = tracked && Held(device, CommonUsages.triggerButton);
            bool nextTool = tracked && Held(device, CommonUsages.primaryButton);
            bool restart = tracked && Held(device, CommonUsages.secondaryButton);
            if (desktop)
            {
                trigger = Mouse.current?.leftButton.isPressed == true;
                nextTool = Keyboard.current?.tabKey.isPressed == true;
                restart = Keyboard.current?.rKey.isPressed == true;
            }

            if (nextTool && !toolButtonWasHeld)
                selectedTool = (selectedTool + 1) % ToolNames.Length;
            if (completed == stations.Length && restart && !restartWasHeld)
                Restart();

            Ray ray = desktop
                ? eyes.ScreenPointToRay(Mouse.current?.position.ReadValue() ?? new Vector2(Screen.width / 2f, Screen.height / 2f))
                : new Ray(rightHand.position, rightHand.forward);
            ProcessRay(ray, desktop || tracked, trigger, Time.deltaTime);
            toolButtonWasHeld = nextTool;
            restartWasHeld = restart;
        }

        static bool Held(XRDevice device, InputFeatureUsage<bool> usage) =>
            device.TryGetFeatureValue(usage, out bool pressed) && pressed;

        void MoveOnDesktop()
        {
            if (Mouse.current?.rightButton.isPressed == true)
            {
                Vector2 delta = Mouse.current.delta.ReadValue() * .15f;
                rig.Rotate(0, delta.x, 0);
                pitch = Mathf.Clamp(pitch - delta.y, -70, 70);
                eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            var keys = Keyboard.current;
            if (keys == null) return;
            float x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
            float z = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
            Vector3 movement = Vector3.ClampMagnitude(rig.right * x + rig.forward * z, 1);
            body.Move((movement * 2f + Vector3.down * 2f) * Time.deltaTime);
        }

        void ProcessRay(Ray ray, bool hasAim, bool trigger, float elapsed)
        {
            focused = null;
            Vector3 end = ray.GetPoint(5);
            bool applying = false;
            if (hasAim && Physics.Raycast(ray, out var hit, 5, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                focused = hit.collider.GetComponent<RepairStation>();
                if (focused && trigger)
                {
                    if (!focused.Identified && !triggerWasHeld)
                        focused.Identify();
                    else if (focused.Identified && focused.AcceptsApplication && focused.Tool == selectedTool)
                    {
                        applying = true;
                        if (focused.Apply(elapsed))
                        {
                            completed++;
                            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                            if (device.isValid) device.SendHapticImpulse(0, .45f, .12f);
                        }
                    }
                }
            }

            triggerWasHeld = trigger;
            pointer.enabled = hasAim && !desktop;
            pointer.SetPosition(0, ray.origin + ray.direction * .045f);
            pointer.SetPosition(1, end);
            pointer.startColor = pointer.endColor = focused ? new Color(.28f, 1, .82f) : new Color(.45f, .72f, 1, .65f);
            for (int i = 0; i < tools.Length; i++) tools[i].gameObject.SetActive(applying && i == selectedTool);
            if (applying)
            {
                Transform tool = tools[selectedTool];
                tool.SetPositionAndRotation(end + focused.transform.forward * .085f +
                    focused.transform.up * (.055f * Mathf.Sin(Time.time * 13f)), focused.transform.rotation);
            }
            RenderStatus();
        }

        void Restart()
        {
            completed = 0;
            selectedTool = 0;
            foreach (var station in stations) station.ResetRepair();
        }

        void RenderStatus()
        {
            header.SetText("RENOVALAB VR   •   {0}/3 reparos", completed);
            string message;
            if (completed == stations.Length)
                message = "Vistoria concluída! Inspeções e manutenção preventiva ajudam a preservar o Sebrae Lab.";
            else if (!focused)
                message = "Encontre três marcas na parede e aponte para cada uma para fazer a vistoria.";
            else if (focused.Repaired)
                message = focused.Diagnosis + " • Reparado. " + focused.Lesson;
            else if (!focused.Identified)
                message = "Área danificada. Aperte " + (desktop ? "o botão esquerdo" : "o gatilho") + " para identificar.";
            else if (focused.Tool != selectedTool)
                message = focused.Diagnosis + ". Selecione: " + ToolNames[focused.Tool] + ".";
            else
                message = focused.Diagnosis + ". " + Instructions[focused.Tool] +
                    (focused.Progress > 0 ? $"  {Mathf.RoundToInt(focused.Progress * 100)}%" : " Segure para aplicar.");

            guide.SetText(message);
            string controls = desktop ? "Tab troca • Clique aplica • WASD anda • Botão direito olha"
                : "A troca • Gatilho aplica • Analógicos movem e giram";
            if (completed == stations.Length) controls = desktop ? "R reinicia a visita" : "B reinicia a visita";
            footer.SetText(ToolNames[selectedTool] + "   |   " + controls);
        }

        void CreateDisplay()
        {
            var panel = new GameObject("Guia de conservação", typeof(RectTransform), typeof(Canvas), typeof(Image));
            panel.transform.SetParent(eyes.transform, false);
            panel.transform.localPosition = new Vector3(0, -.33f, 1.38f);
            panel.transform.localScale = Vector3.one * .0011f;
            panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(780, 184);
            panel.GetComponent<Image>().color = new Color(.025f, .085f, .13f, .96f);
            header = AddLabel(panel.transform, "Título", 54, 34, 28, new Color(.43f, 1, .83f));
            guide = AddLabel(panel.transform, "Instruções", 0, 76, 26, Color.white);
            footer = AddLabel(panel.transform, "Controles", -61, 33, 21, new Color(.71f, .83f, .92f));
        }

        static TextMeshProUGUI AddLabel(Transform parent, string name, float y, float height, float fontSize, Color color)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchoredPosition = new Vector2(0, y);
            label.rectTransform.sizeDelta = new Vector2(740, height);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        void CreateTools()
        {
            tools = new Transform[3];
            for (int i = 0; i < tools.Length; i++)
            {
                tools[i] = new GameObject(ToolNames[i]).transform;
                tools[i].SetParent(transform, false);
                ToolPart(tools[i], PrimitiveType.Cube, new Vector3(0, -.095f, .035f),
                    new Vector3(.032f, .13f, .032f), Quaternion.identity);
            }

            ToolPart(tools[0], PrimitiveType.Cube, Vector3.zero, new Vector3(.14f, .1f, .012f), Quaternion.identity);
            ToolPart(tools[1], PrimitiveType.Cylinder, new Vector3(0, .008f, .038f),
                new Vector3(.051f, .085f, .051f), Quaternion.Euler(90, 0, 0));
            ToolPart(tools[2], PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(.062f, .086f, .062f), Quaternion.Euler(0, 0, 90));
            foreach (Transform tool in tools) tool.gameObject.SetActive(false);
        }

        void ToolPart(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            var part = GameObject.CreatePrimitive(type);
            part.GetComponent<Collider>().enabled = false;
            part.transform.SetParent(parent, false);
            part.transform.SetLocalPositionAndRotation(position, rotation);
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = pointerMaterial;
        }
    }
}
