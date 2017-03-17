using System.Xml.Serialization;


namespace ContrastDvnrLib.Models
{
    
    public class LibraryIssue
    {

        public string Description { get; set; }

        public LibraryIssue() { }

        public LibraryIssue(string description)
        {
            Description = description;
        }

    }
    

}
