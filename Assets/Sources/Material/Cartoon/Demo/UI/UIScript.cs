using UnityEngine;
using UnityEngine.UIElements;

namespace CartoonDemo
{
    [RequireComponent(typeof(UIDocument))]
    public class UIScript : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] LightAnchor lightAnchor;

        static readonly int State = Animator.StringToHash("State");

        void Awake()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document.rootVisualElement;
            VisualElement animationStateContainer = root.Q("AnimationState").Q("unity-content");
            var animationStateButtons = animationStateContainer.Query<Button>().ToList();
            for (int i = 0; i < animationStateButtons.Count; i++)
            {
                int animationState = i;
                Button button = animationStateButtons[i];
                button.clicked += () => {
                    animator.SetInteger(State, animationState);
                };
            }
            VisualElement lightDirectionContainer = root.Q("LightDirection").Q("unity-content");
            var lightDirectionSliders = lightDirectionContainer.Query<Slider>();
            lightDirectionSliders.AtIndex(0).SetValueWithoutNotify(lightAnchor.yaw);
            lightDirectionSliders.AtIndex(0).RegisterValueChangedCallback(evt => {
                lightAnchor.yaw = evt.newValue;
                lightAnchor.UpdateTransform(Camera.main, Vector3.zero);
            });
            lightDirectionSliders.AtIndex(1).SetValueWithoutNotify(lightAnchor.pitch);
            lightDirectionSliders.AtIndex(1).RegisterValueChangedCallback(evt => {
                lightAnchor.pitch = evt.newValue;
                lightAnchor.UpdateTransform(Camera.main, Vector3.zero);
            });
        }
    }
}
