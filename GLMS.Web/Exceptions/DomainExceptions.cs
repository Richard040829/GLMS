namespace GLMS.Web.Exceptions;

public abstract class DomainValidationException : Exception
{
    protected DomainValidationException(string message) : base(message) { }
}

public class FileValidationException : DomainValidationException
{
    public FileValidationException(string message) : base(message) { }
}

public class ContractWorkflowException : DomainValidationException
{
    public ContractWorkflowException(string message) : base(message) { }
}

public class EntityNotFoundException : DomainValidationException
{
    public EntityNotFoundException(string message) : base(message) { }
}
