namespace HospitalManagementAPI
{
    public class GlobalLoginState
    {
        public int ID { get; set; }
        public int FailedAttempts { get; set; }
        public bool IsLocked { get; set; }
    }
}
