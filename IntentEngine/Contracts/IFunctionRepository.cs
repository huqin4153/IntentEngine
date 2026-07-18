using System.Collections.Generic;
using IntentEngine.Models;

namespace IntentEngine.Contracts
{
    public interface IFunctionRepository
    {
        List<Function> GetByIntentId(int intentId);
        Function GetById(int id);
        int Insert(Function function);
        void Update(Function function);
        void Delete(int id);
        void Reorder(int intentId, List<int> functionIds);
    }
}
