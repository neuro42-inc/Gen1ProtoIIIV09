﻿using System;

namespace n42_Robot_PROTO_III
{
    public abstract class XBoxComponent<T>
    {
        public event EventHandler<ValueChangeArgs<T>> ValueChanged;

        public T PreviousValue
        {
            get;
            private set;
        }

        private T value;
        public virtual T Value
        {
            get => value;
            internal set
            {
                PreviousValue = Value;
                if (!Value.Equals(value))
                {
                    this.value = value;
                    OnComponentChanged(new ValueChangeArgs<T>(Value));
                }
            }
        }

        public bool IsValueChanged

        {
            get => !Value.Equals(PreviousValue);
        }

        public XBoxComponent(T initialValue)
        {
            value = initialValue;
        }

        protected virtual void OnComponentChanged(ValueChangeArgs<T> e)
        {
            ValueChanged?.Invoke(this, e);
        }
    }
}
