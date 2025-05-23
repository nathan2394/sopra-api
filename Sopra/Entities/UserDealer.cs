using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Entities
{
    [Table(name: "UserDealers")]
    public class UserDealer : Entity
    {
        public long RefID { get; set; }
        public long? UserId { get; set; }
        public long? DealerId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Dealer? Dealer { get; set; }
    }
}
