using System.Windows.Media;

namespace Task_ManagerCP3.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<Tag> Tags { get; set; } = [];
        public string Notes { get; set; } = string.Empty;
        public bool IsRepeat { get; set; } = false;
        public bool HasNotification { get; set; } = false;
        public bool IsChecked { get; set; } = false;
        public int TaskListID { get; set; }
        public int ProjectID { get; set; }
        public string OriginalTitle { get; set; } = string.Empty;
        public string OriginalNotes { get; set; } = string.Empty;
        public bool OriginalIsChecked { get; set; } = false;
        public DateTime? NotificationTime { get; set; }

        public Brush TagColor { get; set; }

        public string FormattedDate
        {
            get
            {
                return Date.HasValue ? Date.Value.ToString("dd.MM.yyyy HH:mm") : "";
            }
        }

    }
}
