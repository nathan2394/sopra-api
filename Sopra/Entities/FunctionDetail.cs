using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "FunctionDetails")]
    public class FunctionDetail : Entity
    {
        public long RefID { get; set; }
        public long FunctionsID { get; set; }
        public long ObjectID { get; set; }
        public string Type { get; set; }
    }
}
