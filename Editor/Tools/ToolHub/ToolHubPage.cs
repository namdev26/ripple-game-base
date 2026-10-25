using System;

[Serializable]
internal abstract class ToolHubPage : IDisposable
{
    internal virtual void Initialize(Action repaint) { }
    internal abstract void Draw();
    public virtual void Dispose() { }
}
