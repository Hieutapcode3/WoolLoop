using System;
using UnityEngine;

namespace WoolGame
{
    public readonly struct ProgressChangedEvent
    {
        public ProgressChangedEvent(float progress, Vector3 worldPosition)
        {
            Progress = progress;
            WorldPosition = worldPosition;
        }

        public float Progress { get; }
        public Vector3 WorldPosition { get; }
    }

    public readonly struct ColorChangedEvent
    {
        public ColorChangedEvent(Color color)
        {
            Color = color;
        }

        public Color Color { get; }
    }

    public interface IEntityDomainEvents
    {
        event Action<IEntityDomain> Created;
        event Action<EntityState> StateChanged;
        event Action<ProgressChangedEvent> ProgressChanged;
        event Action<ColorChangedEvent> ColorChanged;
        event Action ClearStarted;
        event Action ClearCompleted;
        event Action ReturnedToSlot;
        event Action Destroyed;
    }
}
