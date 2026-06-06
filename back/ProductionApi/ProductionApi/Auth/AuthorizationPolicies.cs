namespace ProductionApi.Auth
{
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string CanWriteDetails = "CanWriteDetails";
        public const string CanWriteEquipment = "CanWriteEquipment";
        public const string CanWriteShifts = "CanWriteShifts";
    }
}
