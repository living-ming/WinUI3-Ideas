using System.Collections.ObjectModel;

namespace IDEAs.Models
{
    public class Schedule : Item
    {
        public ObservableCollection<Schedule_Event> Schedules { get; set; }
        public ObservableCollection<string> Categories { get; set; }

        public Schedule()
        {
            Categories = new ObservableCollection<string>();
            Schedules = new ObservableCollection<Schedule_Event>();
            FileType = "Schedule";
        }
    }
}
