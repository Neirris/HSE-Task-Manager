namespace Task_ManagerCP3.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

}


