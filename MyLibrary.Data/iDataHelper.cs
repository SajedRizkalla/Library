using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary.Data
{
    public interface iDataHelper<Table>
    {
        List<Table> GetData();
        List<Table> Search(string SearchItem);
        Table Find(string Id);
        void Add(Table table);
        void Edit(string Id,Table table);
        void Delete(string Id);
        
    }
}
     