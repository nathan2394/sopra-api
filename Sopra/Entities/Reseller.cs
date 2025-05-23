using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;

namespace Sopra.Entities
{
	public class Reseller
	{
		public long ID { get; set; }
		public long? RefID { get; set; }
		public string? Name { get; set; }
		public string? Address { get; set; }
		public string? Mobile { get; set; }
		public long? ProvinceID { get; set; }
		public string? Province { get; set; }
		public string? Regency { get; set; }
		public string? District { get; set; }
	}
}
