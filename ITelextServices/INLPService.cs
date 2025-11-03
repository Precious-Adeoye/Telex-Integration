using Telex_Integration.Models;

namespace Telex_Integration.ITelextServices
{
    public interface INLPService
    {
        TaskCommand ParseCommand(string message);
    }
}
