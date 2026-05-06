namespace Events
{
    public class WorldBuildedEvent : AbstractBoostrapDependentEvent
    {
        public WorldBuildedEvent(string bootstrapId) : base(bootstrapId)
        {
        }
    }
}