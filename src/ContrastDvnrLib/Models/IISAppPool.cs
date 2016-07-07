namespace ContrastDvnrLib.Models
{
    public class IISAppPool
    {

        public string Name { get; set; }

        public bool X64 { get; set; }

        public string CLRVersion { get; set; }

        public string PipelineMode { get; set; }

        public string UserName { get; set; }

        public string Identity { get; set; }

        public int NumApplications { get; set; }

    }
}
