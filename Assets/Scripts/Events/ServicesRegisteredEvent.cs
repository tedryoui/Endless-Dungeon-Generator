namespace Events
{
    public class ServicesRegisteredEvent : AbstractBoostrapDependentEvent
    {
        public ServicesRegisteredEvent(string bootstrapId) : base(bootstrapId)
        {
        }
    }
}