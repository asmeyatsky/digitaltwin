using System.Threading.Tasks;

namespace DigitalTwin.Core.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
    }
}
