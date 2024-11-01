using System;

namespace AeFinder.User;

public class SignatureVerifyException: Exception
{
    public SignatureVerifyException()
    {

    }

    public SignatureVerifyException(string message)
        : base(message)
    {

    }
}