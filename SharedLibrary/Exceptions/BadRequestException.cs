using System;

namespace SharedLibrary.Exceptions
{
    public class BadRequestException : CustomException
    {
        public BadRequestException(string message) : base(message, 400)
        {
        }
    }
}
