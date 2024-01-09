using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThreeAmigos.Areas.Identity.Data;

namespace ThreeAmigos.Models.AccountBalances
{
    public class AccountBalance
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountBalanceId { get; set; }
        public string CustomerID { get; set; }
        public ThreeAmigosUser ThreeAmigosUser { get; set; }
        public int Balance { get; set; }

    }
}
