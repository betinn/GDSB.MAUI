using System;

namespace GDSB.Domain.Exceptions
{
    // Senha errada e arquivo adulterado devem ser indistinguíveis para quem usa o app -
    // diferenciá-los na mensagem ajudaria um atacante a confirmar se acertou parte do ataque.
    public class InvalidPasswordOrCorruptFileException : Exception
    {
        private const string DefaultMessage = "Senha incorreta ou arquivo corrompido.";

        public InvalidPasswordOrCorruptFileException()
            : base(DefaultMessage)
        {
        }

        public InvalidPasswordOrCorruptFileException(Exception innerException)
            : base(DefaultMessage, innerException)
        {
        }
    }
}
