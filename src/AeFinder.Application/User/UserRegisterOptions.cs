namespace AeFinder.User;

public class UserRegisterOptions
{
    public int CodeExpires { get; set; } = 300; // 5 minutes
    public int EmailSendingInterval { get; set; } = 60; // 1 minutes
    public string SendingCodeEmailTemplate { get; set; }
}