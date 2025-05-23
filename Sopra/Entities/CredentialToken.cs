using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sopra.Entities
{
    [Table(name: "CredentialTokens")]

    public class CredentialToken : Entity
    {
        //public long Id { get; set; }
        public string? Token { get; set; }   
        public long? CredentialsId { get; set; }

    }
}
