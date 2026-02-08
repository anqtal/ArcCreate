using UnityEngine;
using UnityEngine.Events;

namespace ArcCreate
{
    [CreateAssetMenu(fileName = "Color", menuName = "ScriptableObject/Color")]
    public class ColorSO : ScriptableObject
    {
        private Color value;

        public Color Value
        {
            get => value;
            set
            {
                this.value = value;
                OnValueChange.Invoke(value);
            }
        }

        public OnChangeEvent OnValueChange { get; set; } = new();

        public class OnChangeEvent : UnityEvent<Color>
        {
        }
    }
}