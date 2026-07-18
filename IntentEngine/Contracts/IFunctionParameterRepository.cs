using System.Collections.Generic;
using IntentEngine.Models;

namespace IntentEngine.Contracts
{
    public interface IFunctionParameterRepository
    {
        List<FunctionParameter> GetByFunctionId(int functionId);
        FunctionParameter GetById(int id);
        int Insert(FunctionParameter param);
        void Update(FunctionParameter param);
        void Delete(int id);
    }
}
