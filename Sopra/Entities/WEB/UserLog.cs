using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "UserLogs")]
    public class UserLog : Entity
    {
        public long ID { get; set; }
        public long ObjectID { get; set; }
        public long ModuleID { get; set; }
        public long UserID { get; set; }
        public string Description { get; set; }
        public DateTime TransDate { get; set; }
        public DateTime? DateIn { get; set; }
        public DateTime? DateUp { get; set; }
        public long? UserIn { get; set; }
        public long? UserUp { get; set; }
        public bool IsDeleted { get; set; }
    }
}