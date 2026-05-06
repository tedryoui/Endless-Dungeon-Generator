namespace Events
{
    public abstract class AbstractBoostrapDependentEvent
    {
        private string _bootstrapId;

        public string BootstrapId => _bootstrapId;

        public AbstractBoostrapDependentEvent(string bootstrapId)
        {
            _bootstrapId = bootstrapId;
            
        }
    }
}