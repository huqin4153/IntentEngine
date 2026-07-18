using System.Collections.Generic;
using IntentEngine.Models;

namespace IntentEngine.Contracts
{
    public interface IFlowStepRepository
    {
        List<FlowStep> GetByFunctionId(int functionId);
        FlowStep GetById(int id);
        int Insert(FlowStep step);
        void Update(FlowStep step);
        void Delete(int id);
        void Reorder(int functionId, List<int> stepIds);
    }
}
