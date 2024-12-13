namespace AeFinder.Grains.State.Users;

public class RegisterVerificationCodeState
{
    public string VerificationCode { get; set; }
    public DateTime SendTime { get; set; }
}