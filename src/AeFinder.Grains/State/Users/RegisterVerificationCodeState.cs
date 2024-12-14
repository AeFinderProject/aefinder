namespace AeFinder.Grains.State.Users;

public class RegisterVerificationCodeState
{
    public string Code { get; set; }
    public DateTime SendingTime { get; set; }
}