using System.Collections.Generic;
using IntentEngine.Models;

namespace IntentEngine.Contracts
{
    public interface IIntentRepository
    {
        List<Intent> GetAll();
        List<Intent> GetActive();
        Intent GetById(int id);
        int Insert(Intent intent);
        void Update(Intent intent);
        void Delete(int id);
    }
}
