using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IVCE.DAI.Adapters
{
    // test cicd
    public interface IAADHttpAdapter<TResponse, TRequest>
    {
        Task<IEnumerable<TResponse>> ReceiveAsync(TRequest request);

        TResponse Receive(TRequest request);
    }
    public class AADHttpAdapter
    {
    }
}
