using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperAdmin.Server.Models
{
	class UserModel
	{
		public List<string> Names { get; set; } = new List<string>();
		public List<string> Identifiers { get; set; } = new List<string>();
		public DateTime LastLogin { get; set; } = DateTime.MinValue;
	}
}
