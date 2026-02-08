using System;

namespace ArcCreate
{
    public class State<T>
    {
        private T value;

        public State()
        {
            value = default;
        }

        public State(T defaultValue)
        {
            value = defaultValue;
        }

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                OnValueChange?.Invoke(value);
            }
        }

        public event Action<T> OnValueChange;

        public void SetValueWithoutNotify(T value)
        {
            this.value = value;
        }
    }
}