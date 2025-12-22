public interface IHUDHoverStateSink
{
    /// <summary>True when the reticle/raycast is on a HUD element (so HUD should freeze).</summary>
    void SetReticleInsideHud(bool inside);
}
