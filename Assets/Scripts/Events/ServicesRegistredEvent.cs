namespace Events
{
    public class ServicesRegistredEvent : AbstractBoostrapDependentEvent
    {
        public ServicesRegistredEvent(string bootstrapId) : base(bootstrapId)
        {
        }
    }
}