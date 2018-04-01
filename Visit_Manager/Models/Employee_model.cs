using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Visit_Manager.Models
{
    public class Employee_model:IValidatableObject
    {
        public string Name_valid_message { get; set; }
        public string Password_valid_message { get; set; }
        //public string Surname { get; set; }


        //[Required(ErrorMessageResourceType = typeof(Resources.Global),
        //          ErrorMessageResourceName = "Login_required")]
        public string Login { get; set; }
        public string Password { get; set; }

        [Display(Name = "Remember_me", ResourceType = typeof(Resources.Global))]
        public bool Remember_me { get; set; }

        //public string Guid { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> list_of_validation_result = new List<ValidationResult>();
            //if (Name == "Jan")
            //{
            //    listarezultatowWalidacji.Add(new ValidationResult("Imie nie może być Jan", new[] { "Name" }));
            //}
            //if (Password.Length < 2)
            //{
            //    list_of_validation_result.Add(new ValidationResult("Hasło jest za krótkie. Minimum 2 znaki", new[] { "Password" }));
            //}
            //if (Login == null)
            //{
            //    list_of_validation_result.Add(new ValidationResult(Resources.Global.Login_required, new[] { "Login" }));
            //}
            //if (Password == null)
            //{
            //    list_of_validation_result.Add(new ValidationResult(Resources.Global.Password_required, new[] { "Password" }));
            //}

            return list_of_validation_result;
        }





    }
}