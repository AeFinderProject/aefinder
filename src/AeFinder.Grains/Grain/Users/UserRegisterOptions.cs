namespace AeFinder.Grains.Grain.Users;

public class UserRegisterOptions
{
    public int CodeExpires { get; set; } = 600; // 5 minutes
    public int EmailSendingInterval { get; set; } = 60; // 1 minutes
    public string SendingCodeEmailTemplate { get; set; }
}