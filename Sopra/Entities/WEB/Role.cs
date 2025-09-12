using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "Roles")]
    public class Role : Entity
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public DateTime? DateIn { get; set; }
        public DateTime? DateUp { get; set; }
        public long? UserIn { get; set; }
        public long? UserUp { get; set; }
        public bool IsDeleted { get; set; }
    }
}