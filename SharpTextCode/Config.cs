using System;

namespace SharpTextCode
{
    public static class Config
    {
        public const string Token = "Group token";
        public const string VerifyCode = "API Verify code";
        public const uint AdministratorID = 0; //Your`e ID

        public static DateTime StartTime;

        public static void Initialize() => StartTime = DateTime.UtcNow;
    }
}
