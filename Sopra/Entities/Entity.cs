using Sopra.Helpers;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Sopra.Entities
{
    public class Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public DateTime? DateIn { get; set; }
        [JsonIgnore]
        public DateTime? DateUp { get; set; }
        [JsonIgnore]
        public long UserIn { get; set; }
        [JsonIgnore]
        public long UserUp { get; set; }
        [JsonIgnore]
        public bool? IsDeleted { get; set; }
        public Entity()
        {
            UserIn = 0;
            DateIn = Utility.getCurrentTimestamps();
            IsDeleted = false;
        }
    }
}