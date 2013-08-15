using System;
using System.Linq;

namespace Stampsy.Social
{
    public class ServiceUser
    {
        private string _firstName;
        private string _lastName;

        public string Name { get; set; }

        string [] AllNames {
            get {
                if (Name == null)
                    return new string [0];

                return Name.Split (new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public string FirstName {
            get { return _firstName ?? AllNames.FirstOrDefault (); }
            set { _firstName = value; }
        }

        public string LastName {
            get { return _lastName ?? AllNames.LastOrDefault (); }
            set { _lastName = value; }
        }

        public string Id { get; set; }
        public string Email { get; set; }
        public string Nickname { get; set; }
        public string Location { get; set; }
        public string ImageUrl { get; set; }
        public Gender? Gender { get; set; }

        public override string ToString ()
        {
            return string.Format ("[ServiceUser: Id={0}, Name={1}, FirstName={2}, LastName={3}, Email={4}, Nickname={5}, Location={6}, ImageUrl={7}]", Id, Name, FirstName, LastName, Email, Nickname, Location, ImageUrl);
        }
    }
}

