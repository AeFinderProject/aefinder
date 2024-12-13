namespace AeFinder.Grains.Grain.Users;

public class UserRegisterOptions
{
    public int VerificationCodePeriod { get; set; } = 300; // 5 minutes
    public int VerificationCodeRegeneratePeriod { get; set; } = 60; // 1 minutes
}