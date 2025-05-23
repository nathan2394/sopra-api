using Microsoft.EntityFrameworkCore;

namespace Sopra.Entities
{
    [Keyless]
    public class spAfterSave
    {
        //[Key]
        public int Err { get; set; }
        public string ErrMessage { get; set; }
    }
}