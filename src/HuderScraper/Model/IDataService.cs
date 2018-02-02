using System.Threading.Tasks;

namespace HuderScraper.Models
{
    public interface IDataService
    {
        Task<DataItem> GetData();
    }
}