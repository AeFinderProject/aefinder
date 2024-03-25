using System;

namespace AeFinder.Login.Dto;

public enum RegisterResult : Byte
{
    UserAlreadyExists = 1,

    RegistrationSuccess = 2
}