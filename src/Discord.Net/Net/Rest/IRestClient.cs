using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Discord.Net.Rest
{
    public interface IRestClient
    {
        void SetHeader(string key, string value);

        Task<Stream> Send(string method, string endpoint, string json = null);
        Task<Stream> Send(string method, string endpoint, IReadOnlyDictionary<string, object> multipartParams);
    }
}
