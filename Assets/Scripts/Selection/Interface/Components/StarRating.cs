using System;
using Google.MaterialDesign.Icons;
using UnityEngine;

namespace ArcCreate.Selection.Interface
{
    public class StarRating : MonoBehaviour
    {
        [SerializeField] private MaterialIcon[] icons;
        [SerializeField] private string filledIconCode;
        [SerializeField] private string emptyIconCode;

        private int value;

        public int Value
        {
            get => value;
            set
            {
                this.value = value;
                OnValueChanged?.Invoke(value);
                UpdateDisplay();
            }
        }

        public event Action<int> OnValueChanged;

        private void UpdateDisplay()
        {
            for (var i = 0; i < icons.Length; i++) icons[i].iconUnicode = i < value ? filledIconCode : emptyIconCode;
        }
    }
}